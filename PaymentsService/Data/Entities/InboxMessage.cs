using System.ComponentModel.DataAnnotations;

namespace PaymentsService.Data.Entities
{
    public class InboxMessage
    {
        [Key]
        public Guid Id { get; set; }

        public Guid AggregateId { get; set; } 

        public string Type { get; set; } = string.Empty;

        public string Payload { get; set; } = string.Empty;

        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }
    }
}