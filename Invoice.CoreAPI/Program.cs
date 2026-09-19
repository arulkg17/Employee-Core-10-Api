using Invoice.BAL.Contracts;
using Invoice.BAL.Mapper;
using Invoice.BAL.Services;
using Invoice.DAL.Contracts;
using Invoice.DAL.Repositories;
using Invoice.Data.Db;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using System.Data;
using System.Text;
using Invoice.CoreAPI.Middleware;
using Serilog;
using Asp.Versioning;

Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

// ============================================================
// Controllers
// ============================================================

builder.Services.AddControllers();

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

// ============================================================
// Database Connection
// ============================================================

builder.Services.AddScoped<IDbConnection>(sp =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "DefaultConnection is not configured.");
    }

    return new SqlConnection(connectionString);
});

// ============================================================ 
// Entity Framework Core - AppDbContext 
// ============================================================
 
builder.Services.AddDbContext<AppDbContext>(options =>
 { 
     var connectionString = builder.Configuration.GetConnectionString("DefaultConnection"); 
     if (string.IsNullOrWhiteSpace(connectionString)) 
     { 
         throw new InvalidOperationException( "DefaultConnection is not configured."); 
     } 
     options.UseSqlServer(connectionString); 
 });

// ============================================================
// AutoMapper
// ============================================================

builder.Services.AddAutoMapper(cfg => 
{ 
    cfg.AddProfile<CategoryProfile>();
    cfg.AddProfile<ItemmasterProfile>();
    cfg.AddProfile<CustomerProfile>();
    cfg.AddProfile<VendorProfile>();
    cfg.AddProfile<UserProfile>();
});

// ============================================================
// Category - Repository / Service
// ============================================================
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
// ============================================================
// Itemmaster - Repository / Service
// ============================================================
builder.Services.AddScoped<IItemmasterRepository, ItemmasterRepositoryEFSp>();
builder.Services.AddScoped<IItemmasterService, ItemmasterServiceEFSp>();
// ============================================================
// Customer - Repository / Service
// ============================================================
builder.Services.AddScoped<ICustomerRepository, CustomerRepositoryEFSp>();
builder.Services.AddScoped<ICustomerService, CustomerServiceEFSp>();
// ============================================================
// Vendor - Repository / Service
// ============================================================
builder.Services.AddScoped<IVendorRepository, VendorRepositoryEFSp>();
builder.Services.AddScoped<IVendorService, VendorServiceEFSp>();
// ============================================================
// User - Repository / Service
// ============================================================
builder.Services.AddScoped<IUserRepository, UserRepositorySpDap>();
builder.Services.AddScoped<IUserService, UserServiceSpDap>();


// ============================================================
// Swagger
// ============================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Invoice Core API",
        Version = "v1",
        Description = "Invoice Management Core API"
    });

    // JWT Bearer Authentication
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
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

    // Tell Swagger to send the Bearer token
    // with secured API operations.
    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(
                "Bearer",
                document)]
                = []
        });
});




// ============================================================
// JWT Authentication
// ============================================================

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Jwt:Key is not configured.");
}

if (string.IsNullOrWhiteSpace(jwtIssuer))
{
    throw new InvalidOperationException(
        "Jwt:Issuer is not configured.");
}

if (string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "Jwt:Audience is not configured.");
}

var signingKey = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,

                ClockSkew = TimeSpan.Zero
            };
    });


// ============================================================
// Authorization
// ============================================================

builder.Services.AddAuthorization();


// ============================================================
// Build Application
// ============================================================

var app = builder.Build();


app.UseExceptionLogging();

// ============================================================
// Swagger
// ============================================================

app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "Invoice Core API V1");

    options.RoutePrefix = "swagger";
});


// ============================================================
// Authentication / Authorization
// ============================================================

app.UseAuthentication();
app.UseAuthorization();


// ============================================================
// Controllers
// ============================================================

app.MapControllers();


// ============================================================
// Run
// ============================================================

app.Run();

