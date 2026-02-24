using Support_Ticket_Management_API.Models;

namespace Support_Ticket_Management_API.DTOs
{
    public class UpdateStatusDto
    {
        public TicketStatus NewStatus { get; set; }
    }
}
