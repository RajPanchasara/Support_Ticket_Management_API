using Microsoft.EntityFrameworkCore;
using Support_Ticket_Management_API.Models;

namespace Support_Ticket_Management_API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

        public DbSet<Role> Roles => Set<Role>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Ticket> Tickets => Set<Ticket>();
        public DbSet<TicketComment> TicketComments => Set<TicketComment>();
        public DbSet<TicketStatusLog> TicketStatusLogs => Set<TicketStatusLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>().HasIndex(r => r.Name).IsUnique();

            // Store enums as strings in the database for readability and to match API spec
            modelBuilder.Entity<Role>().Property(r => r.Name).HasConversion<string>();

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany()
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Creator)
                .WithMany()
                .HasForeignKey(t => t.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>().Property(t => t.Status).HasConversion<string>();
            modelBuilder.Entity<Ticket>().Property(t => t.Priority).HasConversion<string>();

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Assignee)
                .WithMany()
                .HasForeignKey(t => t.AssignedTo)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketComment>()
                .HasOne(c => c.Ticket)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TicketComment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketStatusLog>()
                .HasOne(l => l.Ticket)
                .WithMany(t => t.StatusLogs)
                .HasForeignKey(l => l.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TicketStatusLog>()
                .HasOne(l => l.Changer)
                .WithMany()
                .HasForeignKey(l => l.ChangedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketStatusLog>().Property(l => l.OldStatus).HasConversion<string>();
            modelBuilder.Entity<TicketStatusLog>().Property(l => l.NewStatus).HasConversion<string>();

            base.OnModelCreating(modelBuilder);
        }
    }
}
