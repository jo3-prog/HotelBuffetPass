using HotelBuffetPass.Data;
using HotelBuffetPass.Models;
using HotelBuffetPass.Services;
using HotelBuffetPass.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBuffetPass.Controllers
{
    public class GuestController : Controller
    {
        private readonly AppDbContext _context;
        private readonly QRCodeService _qrService;

        public GuestController(AppDbContext context, QRCodeService qrService)
        {
            _context = context;
            _qrService = qrService;
        }

        // GET: /Guest/Register/{token}
        [HttpGet("Guest/Register/{token}")]
        public async Task<IActionResult> Register(string token)
        {
            // check if token is valid and active...
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.RegistrationToken == token && e.IsActive);
            if (ev == null)
                return View("InvalidLink");

            // check if event has expired...
            if (ev.EndDate < DateTime.Today)
                return View("EventExpired", ev.EventName);

            var vm = new GuestRegisterViewModel
            {
                EventId = ev.Id,
                EventName = ev.EventName,
                HallName = ev.HallName,
                StartDate = ev.StartDate,
                EndDate = ev.EndDate
            };

            return View(vm);
        }

        [HttpPost("Guest/Register/{token}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string token, GuestRegisterViewModel model)
        {
            // check if token is valid and active, return events with linked guest regs...
            var ev = await _context.Events
                .Include(e => e.GuestRegistrations)
                .FirstOrDefaultAsync(e => e.RegistrationToken == token && e.IsActive);

            if (ev == null) return View("InvalidLink");

            if (!ModelState.IsValid)
            {
                model.EventName = ev.EventName;
                model.HallName = ev.HallName;
                model.StartDate = ev.StartDate;
                model.EndDate = ev.EndDate;
                return View(model);
            }

            // check for duplicate registration...
            var alreadyRegistered = await _context.GuestRegistrations
                .AnyAsync(g => g.EventId == ev.Id &&
                               g.FullName.ToLower() == model.FullName.ToLower());

            if (alreadyRegistered)
            {
                ModelState.AddModelError("FullName", "This name is already registered for this event.");
                model.EventName = ev.EventName;
                model.HallName = ev.HallName;
                model.StartDate = ev.StartDate;
                model.EndDate = ev.EndDate;
                return View(model);
            }

            var registration = new GuestRegistration
            {
                EventId = ev.Id,
                FullName = model.FullName.Trim(),
                Status = RegistrationStatus.Pending,
                QRCodeToken = Guid.NewGuid().ToString()
            };

            _context.GuestRegistrations.Add(registration);
            await _context.SaveChangesAsync();

            return RedirectToAction("Registered", new { id = registration.Id });
        }

        // confirmation page after registration...
        public async Task<IActionResult> Registered(int id)
        {
            var reg = await _context.GuestRegistrations
                .Include(g => g.Event)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (reg == null) return NotFound();

            var vm = new GuestQRViewModel
            {
                FullName = reg.FullName,
                EventName = reg.Event.EventName,
                HallName = reg.Event.HallName,
                StartDate = reg.Event.StartDate,
                EndDate = reg.Event.EndDate,
                BuffetStartTime = reg.Event.BuffetStartTime,
                BuffetEndTime = reg.Event.BuffetEndTime,
                Status = reg.Status.ToString()
            };

            // if reg status is approved, generate QR code...
            if (reg.Status == RegistrationStatus.Approved)
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var qrContent = $"{baseUrl}/Guest/Pass/{reg.QRCodeToken}";
                vm.QRCodeBase64 = _qrService.GenerateQRCodeBase64(qrContent);
            }

            return View("QRPass", vm);
        }

        // direct QR pass page...
        [HttpGet("Guest/Pass/{qrToken}")]
        public async Task<IActionResult> Pass(string qrToken)
        {
            // confirm qrToken...
            var reg = await _context.GuestRegistrations
                .Include(g => g.Event)
                .FirstOrDefaultAsync(g => g.QRCodeToken == qrToken);

            if (reg == null) return NotFound();

            var vm = new GuestQRViewModel
            {
                FullName = reg.FullName,
                EventName = reg.Event.EventName,
                HallName = reg.Event.HallName,
                StartDate = reg.Event.StartDate,
                EndDate = reg.Event.EndDate,
                BuffetStartTime = reg.Event.BuffetStartTime,
                BuffetEndTime = reg.Event.BuffetEndTime,
                Status = reg.Status.ToString()
            };

            if (reg.Status == RegistrationStatus.Approved)
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var qrContent = $"{baseUrl}/Guest/Pass/{reg.QRCodeToken}";
                vm.QRCodeBase64 = _qrService.GenerateQRCodeBase64(qrContent);
            }

            return View("QRPass", vm);
        }

        // guest lookup page (find your QR by name + event)...
        [HttpGet]
        public IActionResult Lookup()
        {
            return View(new GuestLookupViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Lookup(GuestLookupViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // checks for event name and full name and gets it from db if it exists...
            var registration = await _context.GuestRegistrations
                .Include(g => g.Event)
                .Where(g => g.FullName.ToLower() == model.FullName.ToLower() &&
                            g.Event.EventName.ToLower() == model.EventName.ToLower())
                .OrderByDescending(g => g.RegisteredAt)
                .FirstOrDefaultAsync();

            if (registration == null)
            {
                ModelState.AddModelError("", "No registration found with that name and event. Please check your details.");
                return View(model);
            }

            return RedirectToAction("Registered", new { id = registration.Id });
        }
    }
}
