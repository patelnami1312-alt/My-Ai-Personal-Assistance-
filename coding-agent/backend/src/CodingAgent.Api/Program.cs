using System.Text.Json.Serialization;
using CodingAgent.Infrastructure;
using CodingAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

const string CorsPolicy = "spa";
builder.Services.AddCors(o => o.AddPolicy(CorsPolicy, p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5174"])
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddCodingAgentInfrastructure(builder.Configuration);

var app = builder.Build();

// Apply migrations on startup so first run just works.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CodingAgentDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors(CorsPolicy);
app.MapControllers();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", app = "coding-agent", time = DateTime.UtcNow }));

app.Run();
