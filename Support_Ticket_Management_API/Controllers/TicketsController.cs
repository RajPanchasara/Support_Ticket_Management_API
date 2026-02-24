using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Support_Ticket_Management_API.Data;
using Support_Ticket_Management_API.DTOs;
using Support_Ticket_Management_API.Models;
using System.Security.Claims;
using System;
using System.Linq;

namespace Support_Ticket_Management_API.Controllers
{
    [ApiController]
    [Route("tickets")]
    public class TicketsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<TicketsController> _logger; // adding logger for debugging

        public TicketsController(AppDbContext context, IConfiguration config, ILogger<TicketsController> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        // POST api/tickets
        // endpoint to submit a new issue
        [HttpPost]
        [Authorize(Roles = "USER,MANAGER")]
        public async Task<IActionResult> SubmitNewIssue([FromBody] CreateTicketDto dto)
        {
            try {
                // get userid from token
                var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int userId = int.Parse(claim);
                
                Console.WriteLine("User " + userId + " is creating ticket: " + dto.Title);
                
                // Prof said we need to check priority
                if (dto.Priority == TicketPriority.HIGH && dto.Description.Length < 20) {
                     return BadRequest("High priority tickets need more description detail");
                }

                var tckt = new Ticket();
                tckt.Title = dto.Title;
                tckt.Description = dto.Description;
                tckt.Priority = dto.Priority;
                tckt.CreatedBy = userId;

                _context.Tickets.Add(tckt);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Ticket {tckt.Id} created successfully");

                return CreatedAtAction("GetIssueById", new { id = tckt.Id }, tckt);
            }
            catch(Exception ex) {
                _logger.LogError("Error making ticket: " + ex.Message);
                return StatusCode(500, "Internal Server Error");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> FetchMyTickets()
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            Console.WriteLine($"fetching tickets for role: {role}");

            // user gets only their own tickets
            if (role == "USER")
            {
                var userTickets = await _context.Tickets
                    .Include(t => t.Creator)
                    .Include(t => t.Assignee)
                    .Where(t => t.CreatedBy == userId)
                    .ToListAsync();
                    
                return Ok(userTickets);
            }
            
            // support gets assigned tickets
            if (role == "SUPPORT")
            {
                var assignedTickets = await _context.Tickets.Include(t => t.Creator).Include(t => t.Assignee).Where(t => t.AssignedTo == userId).ToListAsync();
                return Ok(assignedTickets);
            }

            // manager gets everything
            // var all = _context.Tickets.ToList(); // old sync code
            var all = await _context.Tickets.Include(t => t.Creator).Include(t => t.Assignee).ToListAsync(); 
            return Ok(all);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetIssueById(int id)
        {
            var tckt = await _context.Tickets.Include(t => t.Comments).Include(t => t.StatusLogs).FirstOrDefaultAsync(t => t.Id == id);
            
            if (tckt == null) {
                _logger.LogWarning("Ticket not found! id=" + id);
                return NotFound();
            }

            var role = User.FindFirstValue(ClaimTypes.Role);
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // check permissions before returning
            if (role == "MANAGER" || tckt.CreatedBy == userId || tckt.AssignedTo == userId) {
                return Ok(tckt);
            }

            return StatusCode(403); // Forbid
        }

        // assign ticket to a support staff member
        [HttpPatch("{id}/assign")]
        [Authorize(Roles = "MANAGER,SUPPORT")]
        public async Task<IActionResult> AssignTech(int id, [FromBody] AssignDto dto)
        {
            var tckt = await _context.Tickets.FindAsync(id);
            if (tckt == null) return NotFound();

            var u = await _context.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Id == dto.UserId);
            if (u == null) {
                return BadRequest("User not found");
            }
            
            // don't let regular users be assigned
            if (u.Role.Name == RoleName.USER) {
                Console.WriteLine("Tried to assign to user role");
                return BadRequest("Cannot assign ticket to USER role");
            }

            tckt.AssignedTo = u.Id;
            // tckt.Status = TicketStatus.IN_PROGRESS; // wait, requirements say just assign
            await _context.SaveChangesAsync();
            _logger.LogInformation("Assigned ticket " + id + " to user " + u.Id);

            return NoContent();
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "MANAGER,SUPPORT")]
        public async Task<IActionResult> ChangeTicketState(int id, [FromBody] UpdateStatusDto dto)
        {
            var t = await _context.Tickets.FindAsync(id);
            if (t == null) return NotFound();

            // we have strict rules for state changes
            bool isAllowed = false;
            
            if (t.Status == TicketStatus.OPEN && dto.NewStatus == TicketStatus.IN_PROGRESS) isAllowed = true;
            if (t.Status == TicketStatus.IN_PROGRESS && dto.NewStatus == TicketStatus.RESOLVED) isAllowed = true;
            if (t.Status == TicketStatus.RESOLVED && dto.NewStatus == TicketStatus.CLOSED) isAllowed = true;

            if (isAllowed == false) {
                 _logger.LogWarning("bad transition from " + t.Status + " to " + dto.NewStatus);
                 return BadRequest("Invalid status transition according to business rules.");
            }

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var historyLog = new TicketStatusLog();
            historyLog.TicketId = t.Id;
            historyLog.OldStatus = t.Status;
            historyLog.NewStatus = dto.NewStatus;
            historyLog.ChangedBy = userId;

            t.Status = dto.NewStatus; // update the parent record
            
            _context.TicketStatusLogs.Add(historyLog);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "MANAGER")]
        public async Task<IActionResult> RemoveTicket(int id)
        {
            var tckt = await _context.Tickets.FindAsync(id);
            if (tckt == null) {
                return NotFound();
            }

            // prevent deleting closed tickets?
            // if (tckt.Status == TicketStatus.CLOSED) return BadRequest("Can't delete closed tickets");

            _context.Tickets.Remove(tckt);
            await _context.SaveChangesAsync();
            
            Console.WriteLine("Deleted ticket " + id);
            return NoContent();
        }
    }
}
