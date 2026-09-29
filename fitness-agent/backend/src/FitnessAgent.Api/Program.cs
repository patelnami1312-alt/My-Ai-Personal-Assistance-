using System.Text.Json.Serialization;
using FitnessAgent.Infrastructure;
using FitnessAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

const string CorsPolicy = "spa";
builder.Services.AddCors(o => o.AddPolicy(CorsPolicy, p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5175"])
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddFitnessAgentInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FitnessAgentDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors(CorsPolicy);
app.MapControllers();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", app = "fitness-agent", time = DateTime.UtcNow }));

app.Run();
