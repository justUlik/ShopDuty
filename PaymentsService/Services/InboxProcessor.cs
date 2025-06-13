using Microsoft.EntityFrameworkCore;
using PaymentsService.Data;
using PaymentsService.Data.Entities;
using System.Text.Json;

namespace PaymentsService.Services
{
    public class InboxProcessor : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<InboxProcessor> _logger;

        public InboxProcessor(IServiceScopeFactory scopeFactory, ILogger<InboxProcessor> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("InboxProcessor started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
                var accountService = scope.ServiceProvider.GetRequiredService<AccountService>();

                var messages = await dbContext.InboxMessages
                    .Where(m => m.ProcessedAt == null)
                    .OrderBy(m => m.ReceivedAt)
                    .Take(10)
                    .ToListAsync(stoppingToken);

                foreach (var message in messages)
                {
                    try
                    {
                        var paymentRequest = JsonSerializer.Deserialize<OrderPaymentRequested>(message.Payload);

                        if (paymentRequest == null)
                        {
                            _logger.LogWarning("Invalid message payload.");
                            message.ProcessedAt = DateTime.UtcNow;
                            await dbContext.SaveChangesAsync(stoppingToken);
                            continue;
                        }

                        bool success = await accountService.TryDebitAsync(
                            paymentRequest.UserId,
                            paymentRequest.OrderId,
                            paymentRequest.Amount
                        );
                        
                        var outboxMessage = new OutboxMessage
                        {
                            Id = Guid.NewGuid(),
                            AggregateId = paymentRequest.OrderId,
                            Type = success ? "OrderPaymentSucceeded" : "OrderPaymentFailed",
                            Payload = JsonSerializer.Serialize(new
                            {
                                OrderId = paymentRequest.OrderId
                            }),
                            CreatedAt = DateTime.UtcNow
                        };

                        await dbContext.OutboxMessages.AddAsync(outboxMessage);

                        message.ProcessedAt = DateTime.UtcNow;

                        await dbContext.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation("Processed InboxMessage {MessageId}, Success: {Success}", message.Id, success);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing InboxMessage {MessageId}", message.Id);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

            _logger.LogInformation("InboxProcessor stopped.");
        }

        private class OrderPaymentRequested
        {
            public Guid OrderId { get; set; }
            public Guid UserId { get; set; }
            public decimal Amount { get; set; }
        }
    }
}