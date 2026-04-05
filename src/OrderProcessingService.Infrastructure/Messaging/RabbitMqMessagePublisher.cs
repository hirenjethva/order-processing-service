using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderProcessingService.Application.Messaging;
using OrderProcessingService.Infrastructure.Configuration;
using RabbitMQ.Client;

namespace OrderProcessingService.Infrastructure.Messaging;

public sealed class RabbitMqMessagePublisher : IMessagePublisher, IHostedService
{
    public const string OrdersExchangeName = "orders";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IOptions<RabbitMqSettings> _settings;
    private readonly ILogger<RabbitMqMessagePublisher> _logger;
    private readonly object _sync = new();

    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqMessagePublisher(
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqMessagePublisher> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            TryConnectInternal();
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            DisposeConnection();
        }

        return Task.CompletedTask;
    }

    public Task PublishAsync<T>(string routingKey, T message)
    {
        byte[] bodyBytes;
        try
        {
            bodyBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, JsonOptions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize message for routing key {RoutingKey}", routingKey);
            return Task.CompletedTask;
        }

        lock (_sync)
        {
            try
            {
                if (_channel is not { IsOpen: true })
                {
                    if (!TryConnectInternal())
                    {
                        _logger.LogWarning(
                            "RabbitMQ is unavailable; message was not published. RoutingKey={RoutingKey}",
                            routingKey);
                        return Task.CompletedTask;
                    }
                }

                var props = _channel!.CreateBasicProperties();
                props.ContentType = "application/json";
                props.DeliveryMode = 2;

                _channel.BasicPublish(
                    exchange: OrdersExchangeName,
                    routingKey: routingKey,
                    mandatory: false,
                    basicProperties: props,
                    body: bodyBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message to exchange {Exchange}, routing key {RoutingKey}",
                    OrdersExchangeName, routingKey);
            }
        }

        return Task.CompletedTask;
    }

    private bool TryConnectInternal()
    {
        try
        {
            DisposeConnection();

            var s = _settings.Value;
            var port = s.Port > 0 ? s.Port : 5672;

            var factory = new ConnectionFactory
            {
                HostName = s.Host,
                Port = port,
                UserName = s.Username,
                Password = s.Password,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(OrdersExchangeName, ExchangeType.Topic, durable: true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not connect to RabbitMQ at {Host}:{Port}", _settings.Value.Host,
                _settings.Value.Port > 0 ? _settings.Value.Port : 5672);
            DisposeConnection();
            return false;
        }
    }

    private void DisposeConnection()
    {
        try
        {
            if (_channel is { IsOpen: true })
                _channel.Close();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error closing RabbitMQ channel");
        }

        try
        {
            _channel?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error disposing RabbitMQ channel");
        }

        _channel = null;

        try
        {
            if (_connection is { IsOpen: true })
                _connection.Close();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error closing RabbitMQ connection");
        }

        try
        {
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error disposing RabbitMQ connection");
        }

        _connection = null;
    }
}
