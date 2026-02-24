using System.ComponentModel.DataAnnotations;
using Support_Ticket_Management_API.Models;

namespace Support_Ticket_Management_API.DTOs
{
    public class CreateTicketDto
    {
        [Required(ErrorMessage = "title is required")]
        [MinLength(5)]
        public string Title { get; set; }

        [Required]
        [MinLength(10)]
        public string Description { get; set; }

        public TicketPriority Priority { get; set; } = TicketPriority.MEDIUM;
    }
}
