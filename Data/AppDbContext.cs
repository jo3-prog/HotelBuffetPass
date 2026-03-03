using HotelBuffetPass.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HotelBuffetPass.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Event> Events { get; set; }
        public DbSet<GuestRegistration> GuestRegistrations { get; set; }
        public DbSet<ScanLog> ScanLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Ensure RegistrationToken is unique
            builder.Entity<Event>()
                .HasIndex(e => e.RegistrationToken)
                .IsUnique();

            // Ensure QRCodeToken is unique
            builder.Entity<GuestRegistration>()
                .HasIndex(g => g.QRCodeToken)
                .IsUnique();

            // Prevent cascade delete conflicts
            builder.Entity<Event>()
                .HasOne(e => e.ContactPerson)
                .WithMany()
                .HasForeignKey(e => e.ContactPersonId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ScanLog>()
                .HasOne(s => s.ScannedBy)
                .WithMany()
                .HasForeignKey(s => s.ScannedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
