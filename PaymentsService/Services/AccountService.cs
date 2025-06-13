using Microsoft.EntityFrameworkCore;
using PaymentsService.Data;
using PaymentsService.Data.Entities;
using PaymentsService.Messaging; 
using System.Text.Json;

namespace PaymentsService.Services
{
    public class AccountService
    {
        private readonly PaymentsDbContext _dbContext;
        private readonly IMessagePublisher _publisher;

        public AccountService(PaymentsDbContext dbContext, IMessagePublisher publisher)
        {
            _dbContext = dbContext;
            _publisher = publisher;
        }

        public async Task<Account> CreateAccountAsync(Guid userId)
        {
            var existingAccount = await _dbContext.Accounts.FindAsync(userId);
            if (existingAccount != null)
            {
                throw new InvalidOperationException("Account already exists for this user.");
            }

            var account = new Account
            {
                UserId = userId,
                Balance = 0m,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.Accounts.AddAsync(account);
            await _dbContext.SaveChangesAsync();

            return account;
        }

        public async Task<Account?> GetAccountAsync(Guid userId)
        {
            return await _dbContext.Accounts.FindAsync(userId);
        }

        public async Task<Account> TopUpAccountAsync(Guid userId, decimal amount)
        {
            var account = await _dbContext.Accounts.FindAsync(userId);
            if (account == null)
            {
                throw new InvalidOperationException("Account does not exist.");
            }

            account.Balance += amount;

            await _dbContext.SaveChangesAsync();

            return account;
        }

        public async Task<decimal> GetBalanceAsync(Guid userId)
        {
            var account = await _dbContext.Accounts.FindAsync(userId);
            if (account == null)
            {
                throw new InvalidOperationException("Account does not exist.");
            }

            return account.Balance;
        }

        public async Task<bool> TryDebitAsync(Guid userId, Guid orderId, decimal amount)
        {
            var account = await _dbContext.Accounts.FindAsync(userId);
            if (account == null)
            {
                await PublishPaymentEvent(orderId, "payment-failed");
                return false; 
            }

            if (account.Balance < amount)
            {
                await PublishPaymentEvent(orderId, "payment-failed");
                return false;
            }

            account.Balance -= amount;
            await _dbContext.SaveChangesAsync();

            await PublishPaymentEvent(orderId, "payment-completed");

            return true; 
        }

        private async Task PublishPaymentEvent(Guid orderId, string type)
        {
            var payload = JsonSerializer.Serialize(new
            {
                OrderId = orderId,
                Type = type
            });

            await _publisher.PublishAsync(type, payload);
        }
    }
}