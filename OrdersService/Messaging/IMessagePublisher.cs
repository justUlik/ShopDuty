namespace OrdersService.Messaging
{
    public interface IMessagePublisher
    {
        Task PublishAsync(string messageType, string payload);
    }
}