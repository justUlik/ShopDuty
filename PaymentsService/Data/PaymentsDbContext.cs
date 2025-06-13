using Microsoft.EntityFrameworkCore;
using PaymentsService.Data.Entities;

namespace PaymentsService.Data
{
    public class PaymentsDbContext : DbContext
    {
        public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options) { }

        public DbSet<Account> Accounts { get; set; }

        public DbSet<InboxMessage> InboxMessages { get; set; }

        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}