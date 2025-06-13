using System.ComponentModel.DataAnnotations;

namespace OrdersService.Data.Entities
{
    public class Order
    {
        [Key]
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public decimal Amount { get; set; }

        public string Description { get; set; } = string.Empty;

        public string Status { get; set; } = "NEW";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}