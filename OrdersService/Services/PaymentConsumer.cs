using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrdersService.Services;

public class PaymentConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public PaymentConsumer(IServiceScopeFactory scopeFactory, ILogger<PaymentConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_connection == null || !_connection.IsOpen)
                {
                    var factory = new ConnectionFactory
                    {
                        HostName = "rabbitmq",
                        UserName = "guest",
                        Password = "guest",
                        DispatchConsumersAsync = true
                    };

                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();
                    _channel.ExchangeDeclare(exchange: "order-status", type: ExchangeType.Fanout, durable: true);
                    _channel.QueueDeclare(queue: "payment-events", 
                                          durable: true,
                                          exclusive: false,
                                          autoDelete: false,
                                          arguments: null);
                    
                    _channel.QueueBind(queue: "payment-events", exchange: "order-status", routingKey: "");
                    var consumer = new AsyncEventingBasicConsumer(_channel);
                    consumer.Received += async (model, ea) =>
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();

                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);

                        _logger.LogInformation("Received payment message: {Message}", message);

                        var paymentMessage = JsonSerializer.Deserialize<PaymentEventMessage>(message);

                        if (paymentMessage != null)
                        {
                            if (paymentMessage.Type == "payment-completed")
                            {
                                await orderService.MarkOrderAsPaidAsync(paymentMessage.OrderId);
                            }
                            else if (paymentMessage.Type == "payment-failed")
                            {
                                await orderService.MarkOrderAsCancelledAsync(paymentMessage.OrderId);
                            }
                        }

                        _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    };

                    _channel.BasicConsume(queue: "payment-events", autoAck: false, consumer: consumer);

                    _logger.LogInformation("Started consuming payment-events queue.");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PaymentConsumer. Will retry in 5 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}

public class PaymentEventMessage
{
    public Guid OrderId { get; set; }
    public string Type { get; set; } = string.Empty; 
}