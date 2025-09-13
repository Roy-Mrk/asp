using Api.Contracts;
using Api.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async ([FromBody] Credentials req, JwtTokenService jwt) =>
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            {
                return Results.BadRequest(new { error = "username and password are required" });
            }

            var cs = Database.BuildConnectionString();
            await using var conn = new MySqlConnection(cs);
            await conn.OpenAsync();
            var cmd = new MySqlCommand("SELECT PasswordHash, IsAdmin FROM Users WHERE Username=@u", conn);
            cmd.Parameters.AddWithValue("@u", req.Username);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return Results.Unauthorized();
            var dbHash = reader.GetString(0);
            var isAdmin = reader.GetBoolean(1);
            if (!BCrypt.Net.BCrypt.Verify(req.Password, dbHash)) return Results.Unauthorized();

            var token = jwt.IssueToken(req.Username, isAdmin);
            return Results.Ok(new { token });
        });

        return app;
    }
}

