using System.Diagnostics;
using FleetVision.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetVision.Controllers
{
    public class HomeController : Controller
    {

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Home()
        {
            return View();
        }


        [Authorize]
        public IActionResult About()
        {
            return View();
        }
        [Authorize]
        public IActionResult Services()
        {
            return View();
        }
        [Authorize]
        public IActionResult Blog()
        {
            return View();
        }
        [Authorize]
        public IActionResult Contact()
        {
            return View();
        }
        [Authorize]
        public IActionResult ScannerWindow()
        {
            return View();
        }
        [Authorize]
        public IActionResult TruckScannerWindow()
        {
            return View("~/Views/TruckQR/TruckScannerWindow.cshtml");
        }




        [Authorize(Roles = "Admin")]
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Employee()
        {
            return View();
        }
        public IActionResult QR()
        {
            return View();
        }
        public IActionResult Attendance()
        {
            return View();
        }

        public IActionResult TodayAttendance()
        {
            return View();
        }
        public IActionResult Truck()
        {
            return View();
        }

        public IActionResult DispatchLogs()
        {
            return RedirectToAction("Index", "DispatchLog");
        }

        public IActionResult TodayDispatches()
        {
            return RedirectToAction("TodayDispatches", "DispatchLog");
        }

        public IActionResult TruckQR()
        {
            return View("~/Views/TruckQR/QR.cshtml"); 
        }




        [Authorize(Roles = "User")]
        public IActionResult User()
        {
            return View();
        }


        [Authorize(Roles = "Employee")]
        



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
