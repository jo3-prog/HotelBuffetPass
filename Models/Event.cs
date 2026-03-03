using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBuffetPass.Models
{
    public class Event
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Event Name")]
        public string EventName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Hall Name")]
        public string HallName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required]
        [Display(Name = "Buffet Start Time")]
        [DataType(DataType.Time)]
        public TimeSpan BuffetStartTime { get; set; }

        [Required]
        [Display(Name = "Buffet End Time")]
        [DataType(DataType.Time)]
        public TimeSpan BuffetEndTime { get; set; }

        [Required]
        [Display(Name = "Max Guests")]
        public int MaxGuests { get; set; }

        [Display(Name = "Contact Person")]
        public string? ContactPersonId { get; set; }

        [ForeignKey("ContactPersonId")]
        public ApplicationUser? ContactPerson { get; set; }

        // unique GUID used as the registration URL token...
        public string RegistrationToken { get; set; } = Guid.NewGuid().ToString();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<GuestRegistration> GuestRegistrations { get; set; } = new List<GuestRegistration>();
    }
}
