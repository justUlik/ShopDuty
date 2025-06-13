using Microsoft.EntityFrameworkCore;
using PaymentsService.Data;
using PaymentsService.Data.Entities;
using PaymentsService.Messaging;

namespace PaymentsService.Services
{
    public class OutboxPublisher : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMessagePublisher _messagePublisher;
        private readonly ILogger<OutboxPublisher> _logger;

        public OutboxPublisher(IServiceScopeFactory scopeFactory, IMessagePublisher messagePublisher, ILogger<OutboxPublisher> logger)
        {
            _scopeFactory = scopeFactory;
            _messagePublisher = messagePublisher;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OutboxPublisher started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

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

                        _logger.LogInformation("Published OutboxMessage {MessageId}", message.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error publishing OutboxMessage {MessageId}", message.Id);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

            _logger.LogInformation("OutboxPublisher stopped.");
        }
    }
}