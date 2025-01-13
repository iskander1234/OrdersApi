using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrdersApi.Domain.Entities;
using OrdersApi.Infrastructure.Data;
using OrdersApi.Services;
using OrdersApi.Services.InterfaceService;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Настройка Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console() // Логирование в консоль
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day) // Логирование в файл
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddMediatR(typeof(Program).Assembly);
// Add services to the container.
builder.Services.AddControllers();

//Используем IMemoryCache для кэширования данных о заказах
builder.Services.AddMemoryCache();
//Настройка DI для DbContext 
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseInMemoryDatabase("OrdersDb"));

// Добавляем аутентификацию JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("Token validated successfully.");
                return Task.CompletedTask;
            }
        };
    });


// Swagger Auth  
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Orders API",
        Version = "v1"
    });

    // Добавляем схему авторизации
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Введите токен JWT в формате: Bearer <токен>"
    });

    // Указываем, что для всех операций требуется авторизация
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UserPolicy", policy => policy.RequireRole("User"));
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
});

builder.Services.AddScoped<IOrderService, OrderService>();


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();


var app = builder.Build();

// Добавляем тестовые данные
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    context.Orders.Add(new Order
    {
        OrderId = Guid.NewGuid(),
        CustomerName = "Test User",
        Status = "pending",
        TotalPrice = 100.50m,
        Products = new List<Product>
        {
            new Product { ProductId = Guid.NewGuid(), Name = "Ski", Price = 50, Quantity = 1 },
            new Product { ProductId = Guid.NewGuid(), Name = "Boots", Price = 55, Quantity = 1 }
        }
    });
    context.SaveChanges();
}
// глобальная обработка ошибок
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.ContentType = "application/json";
        var error = context.Features.Get<IExceptionHandlerFeature>();
        if (error != null)
        {
            var result = JsonSerializer.Serialize(new { error = error.Error.Message });
            await context.Response.WriteAsync(result);
        }
    });
});

// метрика
app.Use(async (context, next) =>
{
    var endpoint = context.GetEndpoint()?.DisplayName ?? "Unknown";
    Console.WriteLine($"Endpoint {endpoint} was called");
    await next();
});

app.Use(async (context, next) =>
{
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        Console.WriteLine($"Authorization Header: {context.Request.Headers["Authorization"]}");
    }
    else
    {
        Console.WriteLine("Authorization Header is missing");
    }
    await next();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();