using HotelBuffetPass.Data;
using HotelBuffetPass.Models;
using HotelBuffetPass.Services;
using HotelBuffetPass.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBuffetPass.Controllers
{
    [Authorize(Roles = AppRoles.ContactPerson)]
    public class ContactPersonController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public ContactPersonController(AppDbContext context, UserManager<ApplicationUser> userManager,
            IEmailService emailService, IConfiguration config)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _config = config;
        }

        public async Task<IActionResult> Dashboard()
        {
            // get events linked with the currently logged in contact person...
            var userId = _userManager.GetUserId(User);
            var events = await _context.Events
                .Include(e => e.GuestRegistrations)
                .Where(e => e.ContactPersonId == userId)
                .OrderByDescending(e => e.StartDate)
                .ToListAsync();

            var vm = new ContactPersonDashboardViewModel
            {
                Events = events.Select(e => new EventSummaryViewModel
                {
                    EventId = e.Id,
                    EventName = e.EventName,
                    HallName = e.HallName,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    MaxGuests = e.MaxGuests,
                    ApprovedCount = e.GuestRegistrations.Count(g => g.Status == RegistrationStatus.Approved),
                    PendingCount = e.GuestRegistrations.Count(g => g.Status == RegistrationStatus.Pending)
                }).ToList()
            };

            return View(vm);
        }

        // get event details for currently logged in contact person...
        public async Task<IActionResult> EventDetail(int id)
        {
            var userId = _userManager.GetUserId(User);
            var ev = await _context.Events
                .Include(e => e.GuestRegistrations)
                .FirstOrDefaultAsync(e => e.Id == id && e.ContactPersonId == userId);

            if (ev == null) return NotFound();

            var baseUrl = _config["AppSettings:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";

            var vm = new ContactPersonEventViewModel
            {
                EventId = ev.Id,
                EventName = ev.EventName,
                HallName = ev.HallName,
                StartDate = ev.StartDate,
                EndDate = ev.EndDate,
                BuffetStartTime = ev.BuffetStartTime,
                BuffetEndTime = ev.BuffetEndTime,
                MaxGuests = ev.MaxGuests,
                ApprovedCount = ev.GuestRegistrations.Count(g => g.Status == RegistrationStatus.Approved),
                PendingCount = ev.GuestRegistrations.Count(g => g.Status == RegistrationStatus.Pending),
                RegistrationLink = $"{baseUrl}/Guest/Register/{ev.RegistrationToken}",
                Guests = ev.GuestRegistrations.OrderByDescending(g => g.RegisteredAt).Select(g => new GuestRowViewModel
                {
                    Id = g.Id,
                    FullName = g.FullName,
                    Status = g.Status.ToString(),
                    RegisteredAt = g.RegisteredAt
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveGuest(int guestId, int eventId)
        {
            // check if event belongs to the logged in contact person...
            var userId = _userManager.GetUserId(User);
            var ev = await _context.Events
                .Include(e => e.GuestRegistrations)
                .FirstOrDefaultAsync(e => e.Id == eventId && e.ContactPersonId == userId);

            if (ev == null) return NotFound();

            // check if the guest is registered...
            var guest = ev.GuestRegistrations.FirstOrDefault(g => g.Id == guestId);
            if (guest == null) return NotFound();

            // check if approved count exceeds or equals max count...
            var approvedCount = ev.GuestRegistrations.Count(g => g.Status == RegistrationStatus.Approved);
            if (approvedCount >= ev.MaxGuests)
            {
                TempData["Error"] = $"Maximum guest limit ({ev.MaxGuests}) reached. Please ask the hotel to increase the limit.";
                return RedirectToAction("EventDetail", new { id = eventId });
            }

            // update guest registration status...
            guest.Status = RegistrationStatus.Approved;
            guest.ApprovedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{guest.FullName} approved successfully.";
            return RedirectToAction("EventDetail", new { id = eventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectGuest(int guestId, int eventId)
        {
            var userId = _userManager.GetUserId(User);
            var ev = await _context.Events
                .Include(e => e.GuestRegistrations)
                .FirstOrDefaultAsync(e => e.Id == eventId && e.ContactPersonId == userId);

            if (ev == null) return NotFound();

            var guest = ev.GuestRegistrations.FirstOrDefault(g => g.Id == guestId);
            if (guest == null) return NotFound();

            // update guest registration status...
            guest.Status = RegistrationStatus.Rejected;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{guest.FullName} has been rejected.";
            return RedirectToAction("EventDetail", new { id = eventId });
        }

        [HttpGet]
        public async Task<IActionResult> InviteGuests(int id)
        {
            var userId = _userManager.GetUserId(User);
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == id && e.ContactPersonId == userId);
            if (ev == null) return NotFound();

            var baseUrl = _config["AppSettings:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
            var vm = new InviteGuestsViewModel
            {
                EventId = ev.Id,
                EventName = ev.EventName,
                RegistrationLink = $"{baseUrl}/Guest/Register/{ev.RegistrationToken}"
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteGuests(InviteGuestsViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == model.EventId && e.ContactPersonId == userId);
            if (ev == null) return NotFound();

            var baseUrl = _config["AppSettings:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
            model.RegistrationLink = $"{baseUrl}/Guest/Register/{ev.RegistrationToken}";
            model.EventName = ev.EventName;

            if (!string.IsNullOrWhiteSpace(model.EmailAddresses))
            {
                // parse comma or newline to separate emails...
                var emails = model.EmailAddresses
                    .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries) // splits the string into an array and discards empty results...
                    .Select(e => e.Trim()) // removes white spaces...
                    .Where(e => !string.IsNullOrEmpty(e)) // filters empty strings after the trim...
                    .Distinct() // removes duplicate emails...
                    .ToList();

                int sent = 0, failed = 0;
                foreach (var email in emails)
                {
                    try
                    {
                        var body = BuildGuestInviteEmail(email, ev.EventName, ev.HallName,
                            ev.StartDate, ev.EndDate, ev.BuffetStartTime, ev.BuffetEndTime, model.RegistrationLink);
                        // call email service...
                        await _emailService.SendEmailAsync(email, $"You're invited – {ev.EventName} Buffet Pass", body);
                        sent++;
                    }
                    catch { failed++; }
                }

                TempData["Success"] = $"Invitations sent to {sent} guest(s).";
                if (failed > 0) TempData["Error"] = $"{failed} email(s) could not be delivered.";
            }

            return RedirectToAction("EventDetail", new { id = model.EventId });
        }

        private string BuildGuestInviteEmail(string toEmail, string eventName, string hall,
            DateTime start, DateTime end, TimeSpan buffetStart, TimeSpan buffetEnd, string registrationLink)
        {
            return $@"
            <div style='font-family:Segoe UI,sans-serif;max-width:600px;margin:auto;'>
                <div style='background:#1a1a2e;padding:24px;text-align:center;border-bottom:3px solid #b8960c;'>
                    <h2 style='color:#f0d060;margin:0;'>⭐ Hotel Buffet Pass</h2>
                </div>
                <div style='padding:32px;background:#fff;'>
                    <p>You have been invited to join the buffet for:</p>
                    <table style='width:100%;border-collapse:collapse;margin:20px 0;'>
                        <tr><td style='padding:8px;background:#f8f6f0;font-weight:600;width:40%'>Event</td><td style='padding:8px;'>{eventName}</td></tr>
                        <tr><td style='padding:8px;background:#f8f6f0;font-weight:600;'>Hall</td><td style='padding:8px;'>{hall}</td></tr>
                        <tr><td style='padding:8px;background:#f8f6f0;font-weight:600;'>Dates</td><td style='padding:8px;'>{start:dd MMM yyyy} – {end:dd MMM yyyy}</td></tr>
                        <tr><td style='padding:8px;background:#f8f6f0;font-weight:600;'>Buffet Hours</td><td style='padding:8px;'>{@buffetStart:hh\:mm} – {@buffetEnd:hh\:mm}</td></tr>
                    </table>
                    <p>Click the button below to register your name and receive your digital buffet pass (QR code):</p>
                    <div style='text-align:center;margin:28px 0;'>
                        <a href='{registrationLink}' style='background:#b8960c;color:white;padding:16px 32px;border-radius:6px;text-decoration:none;font-weight:bold;font-size:16px;'>
                            Register for My Buffet Pass
                        </a>
                    </div>
                    <p style='color:#888;font-size:13px;'>
                        Once your registration is approved by the contact person, you will be able to retrieve your QR code by visiting the link and entering your name.
                    </p>
                </div>
                <div style='background:#f8f6f0;padding:16px;text-align:center;color:#888;font-size:13px;'>
                    Hotel Events & Conferences Team
                </div>
            </div>";
        }
    }
}
