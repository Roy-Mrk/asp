using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;

namespace Api.Endpoints;

public static class GeneralEndpoints
{
    public static IEndpointRouteBuilder MapGeneralEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/ping", () => Results.Ok(new { message = "pong" }));
        app.MapGet("/time", () => Results.Ok(new { utc = DateTime.UtcNow }));

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

        app.MapGet("/auth/me", (ClaimsPrincipal user) =>
        {
            if (user?.Identity?.IsAuthenticated != true)
            {
                return Results.Unauthorized();
            }
            var name = user.FindFirstValue(ClaimTypes.Name) ?? user.Identity?.Name ?? "unknown";
            var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? name;
            var isAdminClaim = user.FindFirstValue(ClaimTypes.Role);
            var isAdmin = string.Equals(isAdminClaim, "admin", StringComparison.OrdinalIgnoreCase);
            return Results.Ok(new { sub, name, isAdmin });
        }).RequireAuthorization();

        app.MapGet("/secure", () => Results.Ok(new { secret = "42" }))
           .RequireAuthorization();

        app.MapGet("/db/ping", async () =>
        {
            var host = Environment.GetEnvironmentVariable("MYSQL_HOST") ?? "mysql";
            var port = Environment.GetEnvironmentVariable("MYSQL_PORT") ?? "3306";
            var database = Environment.GetEnvironmentVariable("MYSQL_DATABASE") ?? "appdb";
            var user = Environment.GetEnvironmentVariable("MYSQL_USER") ?? "appuser";
            var password = Environment.GetEnvironmentVariable("MYSQL_PASSWORD") ?? "apppass";

            var cs = $"Server={host};Port={port};Database={database};User ID={user};Password={password};SslMode=None;AllowPublicKeyRetrieval=True;";
            try
            {
                await using var conn = new MySqlConnection(cs);
                await conn.OpenAsync();
                await using var cmd = new MySqlCommand("SELECT 1", conn);
                var result = await cmd.ExecuteScalarAsync();
                return Results.Ok(new { ok = true, result });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        return app;
    }
}

