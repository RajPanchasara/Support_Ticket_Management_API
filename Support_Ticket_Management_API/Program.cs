using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Support_Ticket_Management_API.Data;
using Support_Ticket_Management_API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(opts => opts.UseSqlServer(conn));

// Token service
builder.Services.AddSingleton<TokenService>();

// Authentication 
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (jwtSecret == null) {
    jwtSecret = "A_Very_Long_And_Secure_Secret_Key_1234567890!#@"; // default key
}
var issuer = builder.Configuration["Jwt:Issuer"] ?? "SupportTicketApi";
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = issuer,
            ValidAudience = issuer,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuerSigningKey = true
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Ensure database and seed roles
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (db.Roles.Count() == 0) // no roles found
    {
        // adding default roles
        db.Roles.Add(new Support_Ticket_Management_API.Models.Role { Name = Support_Ticket_Management_API.Models.RoleName.MANAGER });
        db.Roles.Add(new Support_Ticket_Management_API.Models.Role { Name = Support_Ticket_Management_API.Models.RoleName.SUPPORT });
        db.Roles.Add(new Support_Ticket_Management_API.Models.Role { Name = Support_Ticket_Management_API.Models.RoleName.USER });
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline. //
app.UseSwagger();
app.UseSwaggerUI(c => 
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Support Ticket API v1");
    // set /docs as swagger ui url
    c.RoutePrefix = "docs";
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
