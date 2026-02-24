using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Support_Ticket_Management_API.Models
{
    public class Ticket
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        public TicketStatus Status { get; set; } = TicketStatus.OPEN;
        public TicketPriority Priority { get; set; } = TicketPriority.MEDIUM;

        [Required]
        public int CreatedBy { get; set; }
        public User Creator { get; set; }

        public int? AssignedTo { get; set; }
        public User Assignee { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<TicketComment> Comments { get; set; } = new List<TicketComment>();
        public List<TicketStatusLog> StatusLogs { get; set; } = new List<TicketStatusLog>();
    }
}
