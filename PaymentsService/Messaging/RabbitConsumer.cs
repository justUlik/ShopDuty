using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentsService.Data;
using PaymentsService.Data.Entities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PaymentsService.Messaging
{
    public class RabbitConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RabbitConsumer> _logger;
        private IModel _channel;
        private IConnection _connection;

        private const string ExchangeName = "order-payments";
        private const string QueueName = "payments-service-inbox";

        public RabbitConsumer(IServiceScopeFactory scopeFactory, ILogger<RabbitConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = "rabbitmq",
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            };

            int retries = 5;
            while (retries > 0 && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();

                    _channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Fanout, durable: true);
                    _channel.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false);
                    _channel.QueueBind(queue: QueueName, exchange: ExchangeName, routingKey: "");

                    _logger.LogInformation("Connected to RabbitMQ and declared exchange/queue.");
                    break;
                }
                catch (Exception ex)
                {
                    retries--;
                    _logger.LogWarning(ex, "Failed to connect to RabbitMQ. Retries left: {Retries}", retries);

                    if (retries == 0)
                    {
                        _logger.LogError("Could not connect to RabbitMQ after retries. Exiting.");
                        throw;
                    }

                    await Task.Delay(3000, stoppingToken); 
                }
            }

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var payload = Encoding.UTF8.GetString(body);
                var type = ea.BasicProperties.Type ?? "Unknown";

                _logger.LogInformation("Received RabbitMQ message: {Type}, Payload: {Payload}", type, payload);

                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

                var inboxMessage = new InboxMessage
                {
                    Id = Guid.NewGuid(),
                    AggregateId = Guid.NewGuid(), 
                    Type = type,
                    Payload = payload,
                    ReceivedAt = DateTime.UtcNow
                };

                await dbContext.InboxMessages.AddAsync(inboxMessage);
                await dbContext.SaveChangesAsync(stoppingToken);
            };

            _channel.BasicConsume(queue: QueueName, autoAck: true, consumer: consumer);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}