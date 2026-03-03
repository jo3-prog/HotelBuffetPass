using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBuffetPass.Models
{
    public enum RegistrationStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class GuestRegistration
    {
        public int Id { get; set; }

        [Required]
        public int EventId { get; set; }

        [ForeignKey("EventId")]
        public Event Event { get; set; } = null!;

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        public RegistrationStatus Status { get; set; } = RegistrationStatus.Pending;

        // unique token used to generate QR code...
        public string QRCodeToken { get; set; } = Guid.NewGuid().ToString();

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        public DateTime? ApprovedAt { get; set; }

        // Navigation
        public ICollection<ScanLog> ScanLogs { get; set; } = new List<ScanLog>();
    }
}
