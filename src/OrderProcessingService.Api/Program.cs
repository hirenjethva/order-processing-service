using OrderProcessingService.Api.Middleware;
using OrderProcessingService.Application.Repositories;
using OrderProcessingService.Application.Services;
using OrderProcessingService.Infrastructure.Configuration;
using OrderProcessingService.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDB"));
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQ"));

builder.Services.AddInfrastructure();

builder.Services.AddSingleton<IOrderStatusTransitionService, OrderStatusTransitionService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductCatalogService, ProductCatalogService>();

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

await app.Services.GetRequiredService<IProductRepository>().SeedProductsAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
