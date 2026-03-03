using System.ComponentModel.DataAnnotations;

namespace HotelBuffetPass.ViewModels
{
    public class LoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public class CreateEventViewModel
    {
        [Required, Display(Name = "Event Name")]
        public string EventName { get; set; } = string.Empty;
        [Required, Display(Name = "Hall Name")]
        public string HallName { get; set; } = string.Empty;
        [Required, DataType(DataType.Date), Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;
        [Required, DataType(DataType.Date), Display(Name = "End Date")]
        public DateTime EndDate { get; set; } = DateTime.Today;
        [Required, DataType(DataType.Time), Display(Name = "Buffet Opens")]
        public TimeSpan BuffetStartTime { get; set; } = new TimeSpan(12, 0, 0);
        [Required, DataType(DataType.Time), Display(Name = "Buffet Closes")]
        public TimeSpan BuffetEndTime { get; set; } = new TimeSpan(15, 0, 0);
        [Required, Range(1, 10000), Display(Name = "Number of Guests")]
        public int MaxGuests { get; set; }
        [Required, EmailAddress, Display(Name = "Contact Person Email")]
        public string ContactPersonEmail { get; set; } = string.Empty;
        [Required, Display(Name = "Contact Person Full Name")]
        public string ContactPersonName { get; set; } = string.Empty;
    }

    public class EventDetailViewModel
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string HallName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan BuffetStartTime { get; set; }
        public TimeSpan BuffetEndTime { get; set; }
        public int MaxGuests { get; set; }
        public int ApprovedCount { get; set; }
        public int PendingCount { get; set; }
        public string RegistrationLink { get; set; } = string.Empty;
        public string ContactPersonName { get; set; } = string.Empty;
        public string ContactPersonEmail { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<GuestRowViewModel> Guests { get; set; } = new();
    }

    public class GuestRowViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
    }

    public class CreateStaffViewModel
    {
        [Required, Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required, DataType(DataType.Password), MinLength(6)]
        public string Password { get; set; } = string.Empty;
        [Required, Display(Name = "Role")]
        public string Role { get; set; } = string.Empty;
    }

    public class ContactPersonDashboardViewModel
    {
        public List<EventSummaryViewModel> Events { get; set; } = new();
    }

    public class EventSummaryViewModel
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string HallName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxGuests { get; set; }
        public int ApprovedCount { get; set; }
        public int PendingCount { get; set; }
    }

    public class InviteGuestsViewModel
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string RegistrationLink { get; set; } = string.Empty;
        [Display(Name = "Email Addresses (one per line or comma-separated)")]
        public string? EmailAddresses { get; set; }
    }

    public class ContactPersonEventViewModel
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string HallName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan BuffetStartTime { get; set; }
        public TimeSpan BuffetEndTime { get; set; }
        public int MaxGuests { get; set; }
        public int ApprovedCount { get; set; }
        public int PendingCount { get; set; }
        public string RegistrationLink { get; set; } = string.Empty;
        public List<GuestRowViewModel> Guests { get; set; } = new();
    }

    public class GuestRegisterViewModel
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string HallName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        [Required, Display(Name = "Your Full Name")]
        public string FullName { get; set; } = string.Empty;
    }

    public class GuestQRViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public string HallName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan BuffetStartTime { get; set; }
        public TimeSpan BuffetEndTime { get; set; }
        public string QRCodeBase64 { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class GuestLookupViewModel
    {
        [Required, Display(Name = "Your Full Name")]
        public string FullName { get; set; } = string.Empty;
        [Required, Display(Name = "Event Name")]
        public string EventName { get; set; } = string.Empty;
    }

    public class ScanResultViewModel
    {
        public bool IsValid { get; set; }
        public string GuestName { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public string HallName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string StatusColor { get; set; } = "danger";
    }
}
