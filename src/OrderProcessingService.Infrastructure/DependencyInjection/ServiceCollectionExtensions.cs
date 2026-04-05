using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OrderProcessingService.Application.Caching;
using OrderProcessingService.Application.Messaging;
using OrderProcessingService.Application.Repositories;
using OrderProcessingService.Infrastructure.Caching;
using OrderProcessingService.Infrastructure.Configuration;
using OrderProcessingService.Infrastructure.Messaging;
using OrderProcessingService.Infrastructure.Repositories;
using StackExchange.Redis;

namespace OrderProcessingService.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers infrastructure: Mongo (<see cref="IOrderRepository"/>, <see cref="IProductRepository"/>, MongoDB client/database),
    /// Redis (<see cref="ICacheService"/>), RabbitMQ (<see cref="IMessagePublisher"/> as hosted service).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddMongoRepositories();
        services.AddRedisCaching();
        services.AddRabbitMqMessaging();

        return services;
    }

    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services)
    {
        services.AddSingleton<RabbitMqMessagePublisher>();
        services.AddSingleton<IMessagePublisher>(sp => sp.GetRequiredService<RabbitMqMessagePublisher>());
        services.AddHostedService(sp => sp.GetRequiredService<RabbitMqMessagePublisher>());

        return services;
    }

    public static IServiceCollection AddRedisCaching(this IServiceCollection services)
    {
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var redis = sp.GetRequiredService<IOptions<RedisSettings>>().Value;
            var options = ConfigurationOptions.Parse(redis.ConnectionString);
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);
        });

        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }

    public static IServiceCollection AddMongoRepositories(this IServiceCollection services)
    {
        services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            return new MongoClient(settings.ConnectionString);
        });

        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(settings.DatabaseName);
        });

        services.AddSingleton<IOrderRepository, MongoOrderRepository>();
        services.AddSingleton<IProductRepository, MongoProductRepository>();

        return services;
    }
}
