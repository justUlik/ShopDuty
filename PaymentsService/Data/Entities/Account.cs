using System.ComponentModel.DataAnnotations;

namespace PaymentsService.Data.Entities
{
    public class Account
    {
        [Key]
        public Guid UserId { get; set; }

        public decimal Balance { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}