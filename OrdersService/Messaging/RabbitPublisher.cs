using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace OrdersService.Messaging
{
    public class RabbitPublisher : IMessagePublisher, IDisposable
    {
        private readonly ILogger<RabbitPublisher> _logger;
        private IConnection? _connection;
        private IModel? _channel;
        private readonly object _lock = new();

        private const string ExchangeName = "order-payments";

        public RabbitPublisher(ILogger<RabbitPublisher> logger)
        {
            _logger = logger;
        }

        public async Task PublishAsync(string messageType, string payload)
        {
            EnsureConnected();

            var body = Encoding.UTF8.GetBytes(payload);

            var properties = _channel!.CreateBasicProperties();
            properties.ContentType = "application/json";
            properties.Type = messageType;
            properties.DeliveryMode = 2;

            _channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: "",
                basicProperties: properties,
                body: body
            );

            _logger.LogInformation("Published RabbitMQ message: {Type}, Payload: {Payload}", messageType, payload);

            await Task.CompletedTask;
        }

        private void EnsureConnected()
        {
            if (_connection != null && _connection.IsOpen && _channel != null && _channel.IsOpen)
                return;

            lock (_lock)
            {
                if (_connection != null && _connection.IsOpen && _channel != null && _channel.IsOpen)
                    return;

                var factory = new ConnectionFactory
                {
                    HostName = "rabbitmq",
                    Port = 5672,
                    UserName = "guest",
                    Password = "guest"
                };

                int retries = 5;
                while (retries > 0)
                {
                    try
                    {
                        _connection = factory.CreateConnection();
                        _channel = _connection.CreateModel();

                        _channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Fanout, durable: true);

                        _logger.LogInformation("RabbitPublisher connected to RabbitMQ.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        retries--;
                        _logger.LogWarning(ex, "Failed to connect to RabbitMQ. Retries left: {Retries}", retries);

                        if (retries == 0)
                        {
                            _logger.LogError("Could not connect to RabbitMQ after retries. Giving up.");
                            throw;
                        }

                        Thread.Sleep(3000);
                    }
                }
            }
        }

        public void Dispose()
        {
            try { _channel?.Close(); _channel?.Dispose(); } catch { }
            try { _connection?.Close(); _connection?.Dispose(); } catch { }
        }
    }
}