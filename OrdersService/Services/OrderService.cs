using Microsoft.EntityFrameworkCore;
using OrdersService.Data;
using OrdersService.Data.Entities;
using System.Text.Json;

namespace OrdersService.Services
{
    public class OrderService
    {
        private readonly OrdersDbContext _dbContext;

        public OrderService(OrdersDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Order> CreateOrderAsync(Guid userId, decimal amount, string description)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Amount = amount,
                Description = description,
                Status = "NEW",
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.Orders.AddAsync(order);

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                AggregateId = order.Id,
                Type = "OrderPaymentRequested",
                Payload = JsonSerializer.Serialize(new
                {
                    OrderId = order.Id,
                    UserId = order.UserId,
                    Amount = order.Amount
                }),
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.OutboxMessages.AddAsync(outboxMessage);

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return order;
        }

        public async Task<List<Order>> GetOrdersAsync(Guid userId)
        {
            return await _dbContext.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<Order?> GetOrderByIdAsync(Guid orderId)
        {
            return await _dbContext.Orders.FindAsync(orderId);
        }
        
        public async Task MarkOrderAsPaidAsync(Guid orderId)
        {
            var order = await _dbContext.Orders.FindAsync(orderId);
            if (order != null && order.Status == "NEW") 
            {
                order.Status = "FINISHED";
                await _dbContext.SaveChangesAsync();
            }
        }
        
        public async Task MarkOrderAsCancelledAsync(Guid orderId)
        {
            var order = await _dbContext.Orders.FindAsync(orderId);
            if (order != null && order.Status == "NEW")
            {
                order.Status = "CANCELLED";
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}