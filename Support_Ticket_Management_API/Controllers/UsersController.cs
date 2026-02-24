using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Support_Ticket_Management_API.Data;
using Support_Ticket_Management_API.DTOs;
using Support_Ticket_Management_API.Models;
using BCrypt.Net;
using System.Linq;
using System;

namespace Support_Ticket_Management_API.Controllers
{
    [ApiController]
    [Route("users")]
    [Authorize(Roles = "MANAGER")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<UsersController> _logger;

        public UsersController(AppDbContext db, IConfiguration config, ILogger<UsersController> logger)
        {
            _context = db; // map db context
            _config = config;
            _logger = logger;
        }

        // add a new user to the system
        [HttpPost]
        public async Task<IActionResult> RegisterNewEmployee([FromBody] CreateUserDto dto)
        {
            _logger.LogInformation("manager is registering new user: " + dto.Email);
            
            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6) {
                // simple validation
                return BadRequest("Password must be at least 6 characters long.");
            }

            var duplicateEmail = await _context.Users.AnyAsync(u => u.Email == dto.Email);
            if (duplicateEmail == true) {
                _logger.LogWarning("Email conflict for " + dto.Email);
                return BadRequest("Email already exists in our system.");
            }

            // try to get the role they asked for
            var r = await _context.Roles.FirstOrDefaultAsync(x => x.Name.ToString() == dto.Role.ToString());
            if (r == null) {
                return BadRequest("Invalid role specified.");
            }

            var newUser = new User();
            newUser.Name = dto.Name;
            newUser.Email = dto.Email;
            
            // hash the password using bcrypt config
            newUser.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password); 
            newUser.RoleId = r.Id;

            try {
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                
                Console.WriteLine("user added to DB with id " + newUser.Id);
                return CreatedAtAction(null, new { id = newUser.Id });
            } 
            catch(Exception e) {
                 _logger.LogError("Db save failed: " + e.Message);
                 return StatusCode(500, "Oops, something broke in the db");
            }
        }

        // list all users for manager dashboard
        [HttpGet]
        public async Task<IActionResult> ListCompanyUsers()
        {
            _logger.LogInformation("getting all users for manager view");
            
            // select just what we need to send so password hash isn't exposed
            var userList = await _context.Users.Include(u => u.Role)
                 .Select(u => new { 
                     u.Id, 
                     u.Name, 
                     u.Email, 
                     Role = u.Role.Name 
                 })
                 .ToListAsync();
                 
            return Ok(userList);
        }
    }
}
