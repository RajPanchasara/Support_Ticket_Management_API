using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Support_Ticket_Management_API.Models
{
    public class TicketStatusLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TicketId { get; set; }
        public Ticket Ticket { get; set; }

        [Required]
        public TicketStatus OldStatus { get; set; }

        [Required]
        public TicketStatus NewStatus { get; set; }

        public int ChangedBy { get; set; }
        public User Changer { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
