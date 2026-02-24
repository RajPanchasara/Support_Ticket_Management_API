using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Support_Ticket_Management_API.Data;
using Support_Ticket_Management_API.DTOs;
using Support_Ticket_Management_API.Models;
using Support_Ticket_Management_API.Services;
using BCrypt.Net;
using System;

namespace Support_Ticket_Management_API.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TokenService _tokens;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger; // need logging for auth failures

        public AuthController(AppDbContext db, TokenService tokens, IConfiguration config, ILogger<AuthController> logger)
        {
            _context = db;
            _tokens = tokens;
            _config = config;
            _logger = logger;
        }

        // endpoint for logging in
        // POST api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> AuthenticateUser([FromBody] AuthRequest req)
        {
            _logger.LogInformation("Login attempt for email: " + req.Email);

            var usr = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == req.Email);
            
            if (usr == null) {
                // don't tell them the email was wrong, just generic error
                Console.WriteLine("User not found!");
                return Unauthorized("Invalid credentials provided.");
            }

            bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(req.Password, usr.Password);
            if (isPasswordCorrect == false) {
                 _logger.LogWarning("Wrong password entered for " + req.Email);
                 return Unauthorized("Invalid credentials provided.");
            }

            try {
                // generate jwt token via service
                string jwt = _tokens.GenerateToken(usr, usr.Role);
                Console.WriteLine("JWT generated successfully for user " + usr.Id);
                
                return Ok(new AuthResponse { 
                    Token = jwt, 
                    Role = usr.Role.Name.ToString() 
                });
            } catch (Exception ex) {
                _logger.LogError("Failed to make token: " + ex);
                return StatusCode(500, "Token generation failed");
            }
        }
    }
}
