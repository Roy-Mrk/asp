using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Api.Infrastructure;
using Api.Endpoints;

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

// Services
builder.Services.AddSingleton<JwtTokenService>();

var app = builder.Build();

app.UseCors("AppCors");
app.UseAuthentication();
app.UseAuthorization();

// Ensure DB schema
await Database.EnsureUsersTableAsync();

// Map endpoints
app.MapGeneralEndpoints();
app.MapAuthEndpoints();
app.MapUserEndpoints();

app.Run();
