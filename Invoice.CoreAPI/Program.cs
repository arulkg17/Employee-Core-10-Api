using Invoice.BAL.Contracts;
using Invoice.BAL.Services;
using Invoice.DAL.Contracts;
using Invoice.DAL.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

// ====================================================
// Add services to the container
// ====================================================

builder.Services.AddControllers();

// ====================================================
// Database
// ====================================================

builder.Services.AddScoped<IDbConnection>(sp =>
{
    var connectionString =
        builder.Configuration.GetConnectionString(
            "DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "DefaultConnection is not configured.");
    }

    return new SqlConnection(connectionString);
});

// ====================================================
// User / BAL / DAL Services
// ====================================================

builder.Services.AddScoped<IUserService, UserServiceSpDap>();

builder.Services.AddScoped<IUserRepository, UserRepositorySpDap>();

// ====================================================
// Swagger
// ====================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Invoice Core API",
        Version = "v1",
        Description = "Invoice Management Core API"
    });

    // ------------------------------------------------
    // JWT Bearer Authentication
    // ------------------------------------------------

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",

            Type = SecuritySchemeType.Http,

            Scheme = "bearer",

            BearerFormat = "JWT",

            In = ParameterLocation.Header,

            Description =
                "Enter your JWT token.\r\n\r\n" +
                "Example: Bearer eyJhbGciOiJIUzI1NiIs..."
        });

   
   
});

// ====================================================
// JWT Authentication
// ====================================================

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = false,

                ValidateAudience = false,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = false
            };
    });

// ====================================================
// Authorization
// ====================================================

builder.Services.AddAuthorization();

var app = builder.Build();

// ====================================================
// Swagger
// ====================================================

app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "Invoice Core API V1");

    options.RoutePrefix = "swagger";
});

// ====================================================
// Authentication / Authorization
// ====================================================

app.UseAuthentication();

app.UseAuthorization();

// ====================================================
// Controllers
// ====================================================

app.MapControllers();

app.Run();
