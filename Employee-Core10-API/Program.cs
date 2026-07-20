using Employee_Core10_API.Contracts;
using Employee_Core10_API.Data;
using Employee_Core10_API.Repositories;
using Employee_Core10_API.Services;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

// Swagger (IMPORTANT)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? "Server=LAPTOP-BIG8QIRC;Database=Accounts;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;";
builder.Services.AddDbContext<AppDBContext>(options =>
    options.UseSqlServer(conn));

builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

var app = builder.Build();

// Enable Swagger JSON
app.UseSwagger();

// Enable Swagger UI
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee API V1");
    c.RoutePrefix = "swagger"; // opens at /swagger
});

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.MapOpenApi();
//}

app.UseAuthorization();

app.MapControllers();

app.Run();
