using HotelBuffetPass.Data;
using HotelBuffetPass.Models;
using HotelBuffetPass.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBuffetPass.Controllers
{
    [Authorize(Roles = $"{AppRoles.RestaurantStaff},{AppRoles.Admin}")]
    public class ScannerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ScannerController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Scan()
        {
            return View();
        }

        // called via AJAX when QR is scanned — returns JSON...  
        [HttpPost]
        public async Task<IActionResult> ValidateScan([FromBody] ScanRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.QRToken))
                return Json(new ScanResultViewModel
                {
                    IsValid = false,
                    Message = "Invalid QR code.",
                    StatusColor = "danger"
                });

            // check if registration with QRCodeToken exits...
            var registration = await _context.GuestRegistrations
                .Include(g => g.Event)
                .Include(g => g.ScanLogs)
                .FirstOrDefaultAsync(g => g.QRCodeToken == request.QRToken);

            if (registration == null)
                return Json(new ScanResultViewModel
                {
                    IsValid = false,
                    Message = "QR code not recognised.",
                    StatusColor = "danger"
                });

            var ev = registration.Event;
            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            var currentTime = now.TimeOfDay;

            // Rule 1: Guest must be approved
            if (registration.Status != RegistrationStatus.Approved)
                return Json(new ScanResultViewModel
                {
                    IsValid = false,
                    GuestName = registration.FullName,
                    EventName = ev.EventName,
                    Message = registration.Status == RegistrationStatus.Pending
                        ? "This guest has not been approved yet."
                        : "This registration has been rejected.",
                    StatusColor = "warning"
                });

            // Rule 2: Event must not be expired
            var eventEnd = DateOnly.FromDateTime(ev.EndDate);
            var eventStart = DateOnly.FromDateTime(ev.StartDate);

            if (today > eventEnd)
                return Json(new ScanResultViewModel
                {
                    IsValid = false,
                    GuestName = registration.FullName,
                    EventName = ev.EventName,
                    HallName = ev.HallName,
                    Message = "Event Expired. This buffet pass is no longer valid.",
                    StatusColor = "secondary"
                });

            if (today < eventStart)
                return Json(new ScanResultViewModel
                {
                    IsValid = false,
                    GuestName = registration.FullName,
                    EventName = ev.EventName,
                    HallName = ev.HallName,
                    Message = "Event has not started yet.",
                    StatusColor = "warning"
                });

            // Rule 3: Must be within buffet hours
            if (currentTime < ev.BuffetStartTime || currentTime > ev.BuffetEndTime)
                return Json(new ScanResultViewModel
                {
                    IsValid = false,
                    GuestName = registration.FullName,
                    EventName = ev.EventName,
                    HallName = ev.HallName,
                    Message = $"Outside buffet hours. Buffet is open {ev.BuffetStartTime:hh\\:mm} – {ev.BuffetEndTime:hh\\:mm}.",
                    StatusColor = "warning"
                });

            // Rule 4: Cannot scan twice on the same day
            var alreadyScannedToday = registration.ScanLogs
                .Any(s => s.WasValid && DateOnly.FromDateTime(s.ScannedAt) == today);

            if (alreadyScannedToday)
                return Json(new ScanResultViewModel
                {
                    IsValid = false,
                    GuestName = registration.FullName,
                    EventName = ev.EventName,
                    HallName = ev.HallName,
                    Message = "This pass has already been used today.",
                    StatusColor = "warning"
                });

            // All checks passed — log the scan
            var userId = _userManager.GetUserId(User);
            var scanLog = new ScanLog
            {
                GuestRegistrationId = registration.Id,
                ScannedAt = now,
                ScannedByUserId = userId,
                WasValid = true
            };
            _context.ScanLogs.Add(scanLog);
            await _context.SaveChangesAsync();

            return Json(new ScanResultViewModel
            {
                IsValid = true,
                GuestName = registration.FullName,
                EventName = ev.EventName,
                HallName = ev.HallName,
                Message = "Access Granted ✓",
                StatusColor = "success"
            });
        }
    }

    public class ScanRequest
    {
        public string? QRToken { get; set; }
    }
}
