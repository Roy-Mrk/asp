using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// CORS: allow origins from env CORS_ORIGINS (comma-separated), default localhost:3000
var originsEnv = Environment.GetEnvironmentVariable("CORS_ORIGINS");
var allowedOrigins = (originsEnv ?? "http://localhost:3000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AppCors", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

app.UseCors("AppCors");

// Basic endpoints for sanity check
app.MapGet("/ping", () => Results.Ok(new { message = "pong" }));
app.MapGet("/time", () => Results.Ok(new { utc = DateTime.UtcNow }));

// Additional endpoints
app.MapGet("/greet", (string? name) =>
{
    var target = string.IsNullOrWhiteSpace(name) ? "world" : name.Trim();
    return Results.Ok(new { message = $"Hello, {target}!" });
});

app.MapPost("/echo", async (HttpContext httpContext) =>
{
    using var reader = new StreamReader(httpContext.Request.Body);
    var body = await reader.ReadToEndAsync();
    return Results.Ok(new { echo = body });
});

app.Run();

