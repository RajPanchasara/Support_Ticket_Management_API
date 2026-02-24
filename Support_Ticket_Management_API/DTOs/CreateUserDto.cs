using Support_Ticket_Management_API.Models;

namespace Support_Ticket_Management_API.DTOs
{
    public class CreateUserDto
    {
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public RoleName Role { get; set; }
    }
}
