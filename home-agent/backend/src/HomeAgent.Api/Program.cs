using System.Text.Json.Serialization;
using HomeAgent.Infrastructure;
using HomeAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

const string CorsPolicy = "spa";
builder.Services.AddCors(o => o.AddPolicy(CorsPolicy, p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5177"])
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddHomeAgentInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HomeAgentDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors(CorsPolicy);
app.MapControllers();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", app = "home-agent", time = DateTime.UtcNow }));

app.Run();
