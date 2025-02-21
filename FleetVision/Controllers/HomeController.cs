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


        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Home()
        {
            return View();
        }
        public IActionResult About()
        {
            return View();
        }
        public IActionResult Services()
        {
            return View();
        }
        public IActionResult Blog()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult ScannerWindow()
        {
            return View();
        }




        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }




        [Authorize(Roles = "Admin")]
        public IActionResult Admin()
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
