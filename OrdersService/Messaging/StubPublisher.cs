using Microsoft.Extensions.Logging;

namespace OrdersService.Messaging
{
    public class StubPublisher : IMessagePublisher
    {
        private readonly ILogger<StubPublisher> _logger;

        public StubPublisher(ILogger<StubPublisher> logger)
        {
            _logger = logger;
        }

        public Task PublishAsync(string messageType, string payload)
        {
            _logger.LogInformation("Publishing message: {Type}, Payload: {Payload}", messageType, payload);
            return Task.CompletedTask;
        }
    }
}