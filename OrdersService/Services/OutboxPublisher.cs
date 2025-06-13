using Microsoft.EntityFrameworkCore;
using OrdersService.Data;
using OrdersService.Data.Entities;
using OrdersService.Messaging;

namespace OrdersService.Services
{
    public class OutboxPublisher : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMessagePublisher _messagePublisher;
        private readonly ILogger<OutboxPublisher> _logger;

        public OutboxPublisher(IServiceScopeFactory serviceScopeFactory, IMessagePublisher messagePublisher, ILogger<OutboxPublisher> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _messagePublisher = messagePublisher;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OutboxPublisher started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

                var messages = await dbContext.OutboxMessages
                    .Where(m => m.ProcessedAt == null)
                    .OrderBy(m => m.CreatedAt)
                    .Take(10)
                    .ToListAsync(stoppingToken);

                foreach (var message in messages)
                {
                    try
                    {
                        await _messagePublisher.PublishAsync(message.Type, message.Payload);

                        message.ProcessedAt = DateTime.UtcNow;
                        await dbContext.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation("Published outbox message {MessageId}", message.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error publishing outbox message {MessageId}", message.Id);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

            _logger.LogInformation("OutboxPublisher stopped.");
        }
    }
}