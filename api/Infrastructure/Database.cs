using MySql.Data.MySqlClient;

namespace Api.Infrastructure;

public static class Database
{
    public static string BuildConnectionString()
    {
        var host = Environment.GetEnvironmentVariable("MYSQL_HOST") ?? "mysql";
        var port = Environment.GetEnvironmentVariable("MYSQL_PORT") ?? "3306";
        var database = Environment.GetEnvironmentVariable("MYSQL_DATABASE") ?? "appdb";
        var user = Environment.GetEnvironmentVariable("MYSQL_USER") ?? "appuser";
        var password = Environment.GetEnvironmentVariable("MYSQL_PASSWORD") ?? "apppass";
        return $"Server={host};Port={port};Database={database};User ID={user};Password={password};SslMode=None;AllowPublicKeyRetrieval=True;";
    }

    public static async Task EnsureUsersTableAsync()
    {
        var cs = BuildConnectionString();
        await using var conn = new MySqlConnection(cs);
        await conn.OpenAsync();
        var sql = @"CREATE TABLE IF NOT EXISTS Users (
            Id INT AUTO_INCREMENT PRIMARY KEY,
            Username VARCHAR(100) NOT NULL UNIQUE,
            PasswordHash VARCHAR(200) NOT NULL,
            IsAdmin TINYINT(1) NOT NULL DEFAULT 0,
            CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
        await using var cmd = new MySqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
        try
        {
            var alter = @"ALTER TABLE Users ADD COLUMN IF NOT EXISTS IsAdmin TINYINT(1) NOT NULL DEFAULT 0 AFTER PasswordHash";
            await using var cmdAlter = new MySqlCommand(alter, conn);
            await cmdAlter.ExecuteNonQueryAsync();
        }
        catch { }
    }
}

