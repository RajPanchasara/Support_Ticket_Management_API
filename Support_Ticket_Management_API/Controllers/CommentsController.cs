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
    [Route("tickets/{ticketId}/comments")]
    public class CommentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<CommentsController> _logger;

        public CommentsController(AppDbContext context, IConfiguration config, ILogger<CommentsController> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        private int CurrentUserId() {
            var val = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(val);
        }

        // add a new reply to the ticket
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PostReply(int ticketId, [FromBody] CommentDto dto)
        {
            var t = await _context.Tickets.FindAsync(ticketId);
            if (t == null) {
                _logger.LogWarning("ticket not found " + ticketId);
                return NotFound("No ticket found with that ID");
            }

            int uid = CurrentUserId();
            var r = User.FindFirstValue(ClaimTypes.Role);

            // only managers or people assigned/created the ticket can comment
            if (r == "MANAGER" || t.CreatedBy == uid || t.AssignedTo == uid)
            {
                var newComm = new TicketComment();
                newComm.TicketId = ticketId;
                newComm.UserId = uid;
                newComm.Comment = dto.Comment;
                
                try {
                    _context.TicketComments.Add(newComm);
                    await _context.SaveChangesAsync();
                    
                    Console.WriteLine("Comment posted on ticket: " + ticketId);
                    return CreatedAtAction(null, new { id = newComm.Id });
                } catch(Exception ex) {
                    Console.WriteLine("error posting comment: " + ex.Message);
                    return StatusCode(500, "Server error saving comment.");
                }
            }
            
            _logger.LogWarning("User " + uid + " tried to comment on ticket " + ticketId + " without permission");
            return StatusCode(403, "You don't have permission to comment on this ticket.");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> FetchTicketThread(int ticketId)
        {
            var t = await _context.Tickets.FindAsync(ticketId);
            if (t == null) {
                return NotFound("Ticket missing");
            }

            int currentUsr = CurrentUserId();
            var roleStr = User.FindFirstValue(ClaimTypes.Role);

            if (roleStr == "MANAGER" || t.CreatedBy == currentUsr || t.AssignedTo == currentUsr)
            {
                var commentList = await _context.TicketComments.Where(c => c.TicketId == ticketId).ToListAsync();
                return Ok(commentList);
            }

            return StatusCode(403);
        }

        [HttpPatch("../comments/{id}")]
        [Authorize]
        public async Task<IActionResult> ModifyReply(int ticketId, int id, [FromBody] CommentDto dto)
        {
            var threadCommand = await _context.TicketComments.Include(c => c.Ticket).FirstOrDefaultAsync(c => c.Id == id && c.TicketId == ticketId);
            if (threadCommand == null) return NotFound();

            int userId = CurrentUserId();
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole == "MANAGER" || threadCommand.UserId == userId)
            {
                threadCommand.Comment = dto.Comment;
                // _context.Update(threadCommand);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Comment updated: " + id);
                return NoContent();
            }

            return Unauthorized("You can't edit someone else's comment");
        }

        [HttpDelete("../comments/{id}")]
        [Authorize]
        public async Task<IActionResult> RemoveComment(int ticketId, int id)
        {
            var c = await _context.TicketComments.Include(c => c.Ticket).FirstOrDefaultAsync(c => c.Id == id && c.TicketId == ticketId);
            if (c == null) return NotFound();

            int uid = CurrentUserId();
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (role == "MANAGER" || c.UserId == uid)
            {
                // perform delete
                 Console.WriteLine("About to delete comment " + id);
                _context.TicketComments.Remove(c);
                await _context.SaveChangesAsync();
                
                return NoContent();
            }

            return Forbid();
        }
    }
}
