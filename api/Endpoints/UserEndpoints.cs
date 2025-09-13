using Api.Contracts;
using Api.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/users", async () =>
        {
            var cs = Database.BuildConnectionString();
            await using var conn = new MySqlConnection(cs);
            await conn.OpenAsync();
            var cmd = new MySqlCommand("SELECT Id, Username, IsAdmin, CreatedAt FROM Users ORDER BY Id", conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<object>();
            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    id = reader.GetInt32(0),
                    username = reader.GetString(1),
                    isAdmin = reader.GetBoolean(2),
                    createdAt = reader.GetDateTime(3).ToUniversalTime()
                });
            }
            return Results.Ok(list);
        });

        app.MapGet("/users/{id:int}", async (int id) =>
        {
            var cs = Database.BuildConnectionString();
            await using var conn = new MySqlConnection(cs);
            await conn.OpenAsync();
            var cmd = new MySqlCommand("SELECT Id, Username, IsAdmin, CreatedAt FROM Users WHERE Id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return Results.NotFound();
            var obj = new
            {
                id = reader.GetInt32(0),
                username = reader.GetString(1),
                isAdmin = reader.GetBoolean(2),
                createdAt = reader.GetDateTime(3).ToUniversalTime()
            };
            return Results.Ok(obj);
        });

        app.MapPost("/users", async ([FromBody] CreateUserRequest req) =>
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest(new { error = "username and password are required" });
            var cs = Database.BuildConnectionString();
            await using var conn = new MySqlConnection(cs);
            await conn.OpenAsync();
            var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);
            var cmd = new MySqlCommand("INSERT INTO Users(Username, PasswordHash, IsAdmin) VALUES(@u, @p, @a); SELECT LAST_INSERT_ID();", conn);
            cmd.Parameters.AddWithValue("@u", req.Username);
            cmd.Parameters.AddWithValue("@p", hash);
            cmd.Parameters.AddWithValue("@a", req.IsAdmin ?? false);
            var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return Results.Created($"/users/{id}", new { id, username = req.Username, isAdmin = req.IsAdmin ?? false });
        }).RequireAuthorization();

        app.MapPut("/users/{id:int}", async (int id, [FromBody] UpdateUserRequest req) =>
        {
            var cs = Database.BuildConnectionString();
            await using var conn = new MySqlConnection(cs);
            await conn.OpenAsync();
            var setParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(req?.Username)) setParts.Add("Username=@u");
            if (!string.IsNullOrWhiteSpace(req?.Password)) setParts.Add("PasswordHash=@p");
            if (req?.IsAdmin is not null) setParts.Add("IsAdmin=@a");
            if (setParts.Count == 0) return Results.BadRequest(new { error = "no fields" });
            var sql = $"UPDATE Users SET {string.Join(",", setParts)} WHERE Id=@id";
            var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            if (!string.IsNullOrWhiteSpace(req?.Username)) cmd.Parameters.AddWithValue("@u", req.Username);
            if (!string.IsNullOrWhiteSpace(req?.Password)) cmd.Parameters.AddWithValue("@p", BCrypt.Net.BCrypt.HashPassword(req.Password));
            if (req?.IsAdmin is not null) cmd.Parameters.AddWithValue("@a", (bool)req.IsAdmin);
            var rows = await cmd.ExecuteNonQueryAsync();
            if (rows == 0) return Results.NotFound();
            return Results.NoContent();
        }).RequireAuthorization();

        app.MapDelete("/users/{id:int}", async (int id) =>
        {
            var cs = Database.BuildConnectionString();
            await using var conn = new MySqlConnection(cs);
            await conn.OpenAsync();
            var cmd = new MySqlCommand("DELETE FROM Users WHERE Id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            var rows = await cmd.ExecuteNonQueryAsync();
            if (rows == 0) return Results.NotFound();
            return Results.NoContent();
        }).RequireAuthorization();

        return app;
    }
}

