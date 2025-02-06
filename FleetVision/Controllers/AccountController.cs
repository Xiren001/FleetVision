using Microsoft.AspNetCore.Mvc;

namespace FleetVision.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
