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
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public AdminController(AppDbContext context, UserManager<ApplicationUser> userManager,
            IEmailService emailService, IConfiguration config)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _config = config;
        }

        public async Task<IActionResult> Dashboard()
        {
            var events = await _context.Events
                .Include(e => e.ContactPerson)
                .Include(e => e.GuestRegistrations)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            var viewModels = events.Select(e => new EventSummaryViewModel
            {
                EventId = e.Id,
                EventName = e.EventName,
                HallName = e.HallName,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                MaxGuests = e.MaxGuests,
                ApprovedCount = e.GuestRegistrations.Count(g => g.Status == RegistrationStatus.Approved),
                PendingCount = e.GuestRegistrations.Count(g => g.Status == RegistrationStatus.Pending)
            }).ToList();

            return View(viewModels);
        }

        [HttpGet]
        public IActionResult CreateEvent()
        {
            return View(new CreateEventViewModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today,
                BuffetStartTime = new TimeSpan(12, 0, 0),
                BuffetEndTime = new TimeSpan(15, 0, 0)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(CreateEventViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // check if end date earlier than start date...
            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date cannot be before start date.");
                return View(model);
            }

            // create or find ContactPerson account...
            var contactPerson = await _userManager.FindByEmailAsync(model.ContactPersonEmail);
            string tempPassword = string.Empty;

            // generate password and create contact person profile, if email not found...
            if (contactPerson == null)
            {
                tempPassword = GenerateTempPassword();
                contactPerson = new ApplicationUser
                {
                    UserName = model.ContactPersonEmail,
                    Email = model.ContactPersonEmail,
                    FullName = model.ContactPersonName,
                    Role = AppRoles.ContactPerson,
                    EmailConfirmed = true
                };
                var result = await _userManager.CreateAsync(contactPerson, tempPassword);
                if (!result.Succeeded)
                {
                    ModelState.AddModelError("", "Could not create contact person account: " +
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                    return View(model);
                }
                await _userManager.AddToRoleAsync(contactPerson, AppRoles.ContactPerson);
            }

            // create event...
            var ev = new Event
            {
                EventName = model.EventName,
                HallName = model.HallName,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                BuffetStartTime = model.BuffetStartTime,
                BuffetEndTime = model.BuffetEndTime,
                MaxGuests = model.MaxGuests,
                ContactPersonId = contactPerson.Id,
                RegistrationToken = Guid.NewGuid().ToString()
            };

            _context.Events.Add(ev);
            await _context.SaveChangesAsync();

            // send email to contact person...
            // request scheme - type of server protocol, request host - domain name of the server...
            var baseUrl = _config["AppSettings:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
            var registrationLink = $"{baseUrl}/Guest/Register/{ev.RegistrationToken}";
            var dashboardLink = $"{baseUrl}/Account/Login";

            var emailBody = BuildContactPersonEmail(model.ContactPersonName, model.EventName,
                model.HallName, model.StartDate, model.EndDate, model.BuffetStartTime,
                model.BuffetEndTime, model.MaxGuests, registrationLink, dashboardLink,
                model.ContactPersonEmail, tempPassword);

            try
            {
                await _emailService.SendEmailAsync(model.ContactPersonEmail,
                    $"Event Access – {model.EventName}", emailBody);
            }
            catch
            {
                TempData["Error"] = "Event created but email could not be sent. Please check email settings.";
                return RedirectToAction("EventDetail", new { id = ev.Id });
            }

            TempData["Success"] = $"Event '{model.EventName}' created and contact person notified!";
            return RedirectToAction("EventDetail", new { id = ev.Id });
        }

        public async Task<IActionResult> EventDetail(int id)
        {
            var ev = await _context.Events
                .Include(e => e.ContactPerson)
                .Include(e => e.GuestRegistrations)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null) return NotFound();

            var baseUrl = _config["AppSettings:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";

            var vm = new EventDetailViewModel
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
                ContactPersonName = ev.ContactPerson?.FullName ?? "N/A",
                ContactPersonEmail = ev.ContactPerson?.Email ?? "N/A",
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
        public async Task<IActionResult> UpdateMaxGuests(int eventId, int maxGuests)
        {
            // check if event exists...
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound();

            if (maxGuests < 1)
            {
                TempData["Error"] = "Guest count must be at least 1.";
                return RedirectToAction("EventDetail", new { id = eventId });
            }

            // number of approved guests...
            var approvedCount = await _context.GuestRegistrations
                .CountAsync(g => g.EventId == eventId && g.Status == RegistrationStatus.Approved);

            // check if the number of approved guests have been exhausted...
            if (maxGuests < approvedCount)
            {
                TempData["Error"] = $"Cannot set max guests below the {approvedCount} already approved guests.";
                return RedirectToAction("EventDetail", new { id = eventId });
            }

            ev.MaxGuests = maxGuests;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Guest count updated to {maxGuests}.";
            return RedirectToAction("EventDetail", new { id = eventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleEventStatus(int eventId)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound();

            ev.IsActive = !ev.IsActive;
            await _context.SaveChangesAsync();
            TempData["Success"] = ev.IsActive ? "Event activated." : "Event deactivated.";
            return RedirectToAction("EventDetail", new { id = eventId });
        }

        // staff management...
        [HttpGet]
        public IActionResult CreateStaff()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStaff(CreateStaffViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError("Email", "An account with this email already exists.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Role = model.Role,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, model.Role);
            TempData["Success"] = $"Staff account created for {model.FullName}.";
            return RedirectToAction("Dashboard");
        }

        private string GenerateTempPassword()
        {
            // generate a new guid with no hypens and take the first 8 digits...
            return "Pass@" + Guid.NewGuid().ToString("N")[..8];
        }

        // build the contact person email to be sent...
        private string BuildContactPersonEmail(string name, string eventName, string hall,
            DateTime start, DateTime end, TimeSpan buffetStart, TimeSpan buffetEnd,
            int maxGuests, string registrationLink, string dashboardLink,
            string email, string tempPassword)
        {
            var credentialsSection = string.IsNullOrEmpty(tempPassword) ? "" : $@"
                <div style='background:#fff8e1;border-left:4px solid #b8960c;padding:16px;margin:20px 0;border-radius:4px;'>
                    <strong>Your Login Credentials</strong><br/>
                    Email: <code>{email}</code><br/>
                    Temporary Password: <code>{tempPassword}</code><br/>
                    <small style='color:#666;'>Please change your password after first login.</small>
                </div>";

            return $@"
            <div style='font-family:Segoe UI,sans-serif;max-width:600px;margin:auto;'>
                <div style='background:#1a1a2e;padding:24px;text-align:center;border-bottom:3px solid #b8960c;'>
                    <h2 style='color:#f0d060;margin:0;'>⭐ Hotel Buffet Pass</h2>
                </div>
                <div style='padding:32px;background:#fff;'>
                    <p>Dear <strong>{name}</strong>,</p>
                    <p>You have been assigned as the contact person for the following event:</p>
                    <table style='width:100%;border-collapse:collapse;margin:20px 0;'>
                        <tr><td style='padding:8px;background:#f8f6f0;font-weight:600;width:40%'>Event</td><td style='padding:8px;'>{eventName}</td></tr>
                        <tr><td style='padding:8px;background:#f8f6f0;font-weight:600;'>Hall</td><td style='padding:8px;'>{hall}</td></tr>
                        <tr><td style='padding:8px;background:#f8f6f0;font-weight:600;'>Dates</td><td style='padding:8px;'>{start:dd MMM yyyy} – {end:dd MMM yyyy}</td></tr>
                        <tr><td style='padding:8px;background:#f8f6f0;font-weight:600;'>Buffet Hours</td><td style='padding:8px;'>{@buffetStart:hh\:mm} – {@buffetEnd:hh\:mm}</td></tr>
                        <tr><td style='padding:8px;background:#f8f6f0;font-weight:600;'>Max Guests</td><td style='padding:8px;'>{maxGuests}</td></tr>
                    </table>
                    {credentialsSection}
                    <p><strong>Guest Registration Link</strong><br/>
                    Share this link with your event participants so they can register:</p>
                    <div style='text-align:center;margin:20px 0;'>
                        <a href='{registrationLink}' style='background:#b8960c;color:white;padding:14px 28px;border-radius:6px;text-decoration:none;font-weight:bold;'>
                            📋 Guest Registration Link
                        </a>
                    </div>
                    <p>You can also log into the dashboard to approve guests and manage your event:</p>
                    <div style='text-align:center;margin:20px 0;'>
                        <a href='{dashboardLink}' style='background:#1a1a2e;color:#f0d060;padding:14px 28px;border-radius:6px;text-decoration:none;font-weight:bold;'>
                            🔑 Go to Dashboard
                        </a>
                    </div>
                </div>
                <div style='background:#f8f6f0;padding:16px;text-align:center;color:#888;font-size:13px;'>
                    Hotel Events & Conferences Team
                </div>
            </div>";
        }
    }
}
