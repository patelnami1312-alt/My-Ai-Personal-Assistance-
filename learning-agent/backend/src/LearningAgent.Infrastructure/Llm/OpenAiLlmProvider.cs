using LearningAgent.Core.Abstractions;
using LearningAgent.Core.Llm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace LearningAgent.Infrastructure.Llm;

public class LlmOptions
{
    public const string SectionName = "Llm";
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gpt-4o";
    public int MaxTokens { get; set; } = 1500;
}

/// <summary>OpenAI implementation of <see cref="ILlmProvider"/> using the official OpenAI SDK (Chat Completions).</summary>
public class OpenAiLlmProvider : ILlmProvider
{
    private readonly ChatClient _client;
    private readonly LlmOptions _opts;
    private readonly ILogger<OpenAiLlmProvider> _log;

    public OpenAiLlmProvider(IOptions<LlmOptions> opts, ILogger<OpenAiLlmProvider> log)
    {
        _opts = opts.Value;
        _log = log;
        var apiKey = _opts.ApiKey
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException(
                "No OpenAI API key. Set Llm:ApiKey in configuration/user-secrets or the OPENAI_API_KEY environment variable.");
        var model = string.IsNullOrWhiteSpace(_opts.Model) ? "gpt-4o" : _opts.Model;
        _client = new ChatClient(model, apiKey);
    }

    public string Name => $"openai:{_opts.Model}";

    public async Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            messages.Add(new SystemChatMessage(request.SystemPrompt));
        messages.AddRange(request.Messages.SelectMany(ToChatMessages));

        var options = new ChatCompletionOptions
        {
            Temperature = (float)request.Temperature,
            MaxOutputTokenCount = request.MaxTokens > 0 ? request.MaxTokens : _opts.MaxTokens,
        };
        foreach (var tool in request.Tools)
        {
            options.Tools.Add(ChatTool.CreateFunctionTool(
                tool.Name,
                tool.Description,
                BinaryData.FromString(tool.ParametersSchema.ToJsonString())));
        }

        OpenAI.Chat.ChatCompletion completion;
        try
        {
            completion = await _client.CompleteChatAsync(messages, options, ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "OpenAI CompleteChat failed");
            throw;
        }

        var text = string.Concat(completion.Content
            .Where(p => p.Kind == ChatMessageContentPartKind.Text)
            .Select(p => p.Text));

        var toolCalls = completion.ToolCalls
            .Where(tc => tc.Kind == ChatToolCallKind.Function)
            .Select(tc => new LlmToolCall(tc.Id, tc.FunctionName, tc.FunctionArguments.ToString()))
            .ToList();

        return new LlmResponse
        {
            Text = string.IsNullOrWhiteSpace(text) ? null : text,
            ToolCalls = toolCalls,
            StopReason = completion.FinishReason.ToString(),
            InputTokens = completion.Usage?.InputTokenCount ?? 0,
            OutputTokens = completion.Usage?.OutputTokenCount ?? 0,
        };
    }

    private static IEnumerable<ChatMessage> ToChatMessages(LlmMessage m)
    {
        switch (m.Role)
        {
            case LlmRole.User:
                yield return new UserChatMessage(m.Text ?? "");
                break;

            case LlmRole.Assistant:
            {
                var calls = m.ToolCalls
                    .Select(c => ChatToolCall.CreateFunctionToolCall(
                        c.Id, c.Name, BinaryData.FromString(string.IsNullOrWhiteSpace(c.ArgumentsJson) ? "{}" : c.ArgumentsJson)))
                    .ToList();

                AssistantChatMessage msg = calls.Count > 0
                    ? new AssistantChatMessage(calls)
                    : new AssistantChatMessage(m.Text ?? "");
                if (calls.Count > 0 && !string.IsNullOrWhiteSpace(m.Text))
                    msg.Content.Add(ChatMessageContentPart.CreateTextPart(m.Text));
                yield return msg;
                break;
            }

            case LlmRole.Tool:
                foreach (var r in m.ToolResults)
                    yield return new ToolChatMessage(r.ToolCallId, r.Content);
                break;

            default:
                yield return new UserChatMessage(m.Text ?? "");
                break;
        }
    }
}
