using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;

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

// JWT Auth configuration
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? "dev_secret_key_change_me";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors("AppCors");
app.UseAuthentication();
app.UseAuthorization();

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

// remove demo login (replaced below)

app.MapGet("/auth/me", (ClaimsPrincipal user) =>
{
    if (user?.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }
    var name = user.FindFirstValue(ClaimTypes.Name) ?? user.Identity?.Name ?? "unknown";
    var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? name;
    return Results.Ok(new { sub, name });
}).RequireAuthorization();

app.MapGet("/secure", () => Results.Ok(new { secret = "42" }))
   .RequireAuthorization();

// DB ping endpoint (MySQL)
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

// Ensure Users table exists on startup
async Task EnsureUsersTableAsync()
{
    var host = Environment.GetEnvironmentVariable("MYSQL_HOST") ?? "mysql";
    var port = Environment.GetEnvironmentVariable("MYSQL_PORT") ?? "3306";
    var database = Environment.GetEnvironmentVariable("MYSQL_DATABASE") ?? "appdb";
    var user = Environment.GetEnvironmentVariable("MYSQL_USER") ?? "appuser";
    var password = Environment.GetEnvironmentVariable("MYSQL_PASSWORD") ?? "apppass";
    var cs = $"Server={host};Port={port};Database={database};User ID={user};Password={password};SslMode=None;AllowPublicKeyRetrieval=True;";

    await using var conn = new MySqlConnection(cs);
    await conn.OpenAsync();
    var sql = @"CREATE TABLE IF NOT EXISTS Users (
        Id INT AUTO_INCREMENT PRIMARY KEY,
        Username VARCHAR(100) NOT NULL UNIQUE,
        PasswordHash VARCHAR(200) NOT NULL,
        CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
    await using var cmd = new MySqlCommand(sql, conn);
    await cmd.ExecuteNonQueryAsync();
}

await EnsureUsersTableAsync();

// Users CRUD
app.MapGet("/users", async () =>
{
    var cs = BuildConnectionString();
    await using var conn = new MySqlConnection(cs);
    await conn.OpenAsync();
    var cmd = new MySqlCommand("SELECT Id, Username, CreatedAt FROM Users ORDER BY Id", conn);
    await using var reader = await cmd.ExecuteReaderAsync();
    var list = new List<object>();
    while (await reader.ReadAsync())
    {
        list.Add(new
        {
            id = reader.GetInt32(0),
            username = reader.GetString(1),
            createdAt = reader.GetDateTime(2).ToUniversalTime()
        });
    }
    return Results.Ok(list);
});

app.MapGet("/users/{id:int}", async (int id) =>
{
    var cs = BuildConnectionString();
    await using var conn = new MySqlConnection(cs);
    await conn.OpenAsync();
    var cmd = new MySqlCommand("SELECT Id, Username, CreatedAt FROM Users WHERE Id=@id", conn);
    cmd.Parameters.AddWithValue("@id", id);
    await using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync()) return Results.NotFound();
    var obj = new
    {
        id = reader.GetInt32(0),
        username = reader.GetString(1),
        createdAt = reader.GetDateTime(2).ToUniversalTime()
    };
    return Results.Ok(obj);
});

app.MapPost("/users", async ([FromBody] Credentials req) =>
{
    if (req is null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest(new { error = "username and password are required" });
    var cs = BuildConnectionString();
    await using var conn = new MySqlConnection(cs);
    await conn.OpenAsync();
    var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);
    var cmd = new MySqlCommand("INSERT INTO Users(Username, PasswordHash) VALUES(@u, @p); SELECT LAST_INSERT_ID();", conn);
    cmd.Parameters.AddWithValue("@u", req.Username);
    cmd.Parameters.AddWithValue("@p", hash);
    var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
    return Results.Created($"/users/{id}", new { id, username = req.Username });
}).RequireAuthorization();

app.MapPut("/users/{id:int}", async (int id, [FromBody] UpdateUserRequest req) =>
{
    var cs = BuildConnectionString();
    await using var conn = new MySqlConnection(cs);
    await conn.OpenAsync();
    var setParts = new List<string>();
    if (!string.IsNullOrWhiteSpace(req?.Username)) setParts.Add("Username=@u");
    if (!string.IsNullOrWhiteSpace(req?.Password)) setParts.Add("PasswordHash=@p");
    if (setParts.Count == 0) return Results.BadRequest(new { error = "no fields" });
    var sql = $"UPDATE Users SET {string.Join(",", setParts)} WHERE Id=@id";
    var cmd = new MySqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@id", id);
    if (!string.IsNullOrWhiteSpace(req?.Username)) cmd.Parameters.AddWithValue("@u", req.Username);
    if (!string.IsNullOrWhiteSpace(req?.Password)) cmd.Parameters.AddWithValue("@p", BCrypt.Net.BCrypt.HashPassword(req.Password));
    var rows = await cmd.ExecuteNonQueryAsync();
    if (rows == 0) return Results.NotFound();
    return Results.NoContent();
}).RequireAuthorization();

app.MapDelete("/users/{id:int}", async (int id) =>
{
    var cs = BuildConnectionString();
    await using var conn = new MySqlConnection(cs);
    await conn.OpenAsync();
    var cmd = new MySqlCommand("DELETE FROM Users WHERE Id=@id", conn);
    cmd.Parameters.AddWithValue("@id", id);
    var rows = await cmd.ExecuteNonQueryAsync();
    if (rows == 0) return Results.NotFound();
    return Results.NoContent();
}).RequireAuthorization();

// Replace login to validate against DB
app.MapPost("/auth/login", async ([FromBody] Credentials req) =>
{
    if (req is null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
    {
        return Results.BadRequest(new { error = "username and password are required" });
    }
    var cs = BuildConnectionString();
    await using var conn = new MySqlConnection(cs);
    await conn.OpenAsync();
    var cmd = new MySqlCommand("SELECT PasswordHash FROM Users WHERE Username=@u", conn);
    cmd.Parameters.AddWithValue("@u", req.Username);
    var dbHashObj = await cmd.ExecuteScalarAsync();
    if (dbHashObj is null) return Results.Unauthorized();
    var dbHash = Convert.ToString(dbHashObj);
    if (!BCrypt.Net.BCrypt.Verify(req.Password, dbHash)) return Results.Unauthorized();

    var token = IssueJwt(req.Username);
    return Results.Ok(new { token });
});

// helpers
static string BuildConnectionString()
{
    var host = Environment.GetEnvironmentVariable("MYSQL_HOST") ?? "mysql";
    var port = Environment.GetEnvironmentVariable("MYSQL_PORT") ?? "3306";
    var database = Environment.GetEnvironmentVariable("MYSQL_DATABASE") ?? "appdb";
    var user = Environment.GetEnvironmentVariable("MYSQL_USER") ?? "appuser";
    var password = Environment.GetEnvironmentVariable("MYSQL_PASSWORD") ?? "apppass";
    return $"Server={host};Port={port};Database={database};User ID={user};Password={password};SslMode=None;AllowPublicKeyRetrieval=True;";
}

string IssueJwt(string username)
{
    var jwtKeyLocal = Environment.GetEnvironmentVariable("JWT_KEY") ?? "dev_secret_key_change_me";
    var signingKeyLocal = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKeyLocal));
    var claims = new[]
    {
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, username),
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, username)
    };
    var creds = new SigningCredentials(signingKeyLocal, SecurityAlgorithms.HmacSha256);
    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        claims: claims,
        notBefore: DateTime.UtcNow,
        expires: DateTime.UtcNow.AddHours(8),
        signingCredentials: creds
    );
    return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
}

app.Run();

// DTOs (must appear after top-level statements)
public record Credentials(string Username, string Password);
public record UpdateUserRequest(string? Username, string? Password);
