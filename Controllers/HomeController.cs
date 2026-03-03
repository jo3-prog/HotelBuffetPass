using Microsoft.AspNetCore.Mvc;

namespace HotelBuffetPass.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Dashboard", "Admin");
                if (User.IsInRole("ContactPerson"))
                    return RedirectToAction("Dashboard", "ContactPerson");
                if (User.IsInRole("RestaurantStaff"))
                    return RedirectToAction("Scan", "Scanner");
            }
            return View();
        }
    }
}
