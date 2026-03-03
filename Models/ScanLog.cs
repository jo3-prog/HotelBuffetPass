using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBuffetPass.Models
{
    public class ScanLog
    {
        public int Id { get; set; }

        public int GuestRegistrationId { get; set; }

        [ForeignKey("GuestRegistrationId")]
        public GuestRegistration GuestRegistration { get; set; } = null!;

        public DateTime ScannedAt { get; set; } = DateTime.UtcNow;

        public string? ScannedByUserId { get; set; }

        [ForeignKey("ScannedByUserId")]
        public ApplicationUser? ScannedBy { get; set; }

        public bool WasValid { get; set; }

        public string? InvalidReason { get; set; } // e.g. "Event Expired", "Already Scanned Today", "Outside Buffet Hours"
    }
}
