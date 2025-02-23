using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FleetVision.DBContext;
using FleetVision.Models;

namespace FleetVision.Controllers
{
    public class DispatchLogController : Controller
    {
        private readonly FleetVisionContext _context;

        public DispatchLogController(FleetVisionContext context)
        {
            _context = context;
        }

        // GET: Dispatch History with filters
        public async Task<IActionResult> Index(string search, DateTime? startDate, DateTime? endDate)
        {
            var logs = _context.DispatchHistories.Include(d => d.Truck).AsQueryable();

            // Filter by truck plate number
            if (!string.IsNullOrEmpty(search))
            {
                logs = logs.Where(d => d.Truck.PlateNumber.Contains(search));
            }

            // Filter by date range
            if (startDate.HasValue)
            {
                logs = logs.Where(d => d.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                logs = logs.Where(d => d.Timestamp <= endDate.Value);
            }

            return View(await logs.OrderByDescending(d => d.Timestamp).ToListAsync());
        }

        // GET: Today's Dispatches
        public async Task<IActionResult> TodayDispatches()
        {
            var today = DateTime.Today;

            var logs = await _context.DispatchHistories
                .Where(d => d.Timestamp.Date == today)
                .Include(d => d.Truck)
                .OrderBy(d => d.Timestamp)
                .ToListAsync();

            return View(logs);
        }

        // GET: Active Dispatches (Currently Out)
        public async Task<IActionResult> ActiveDispatches()
        {
            var activeDispatches = await _context.DispatchLogs
                .Where(d => d.ReturnTime == null) // Trucks that haven't returned yet
                .Include(d => d.Truck)
                .OrderBy(d => d.DispatchTime)
                .ToListAsync();

            return View(activeDispatches);
        }

        // GET: View Dispatch Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dispatchLog = await _context.DispatchLogs
                .Include(d => d.Truck)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (dispatchLog == null)
            {
                return NotFound();
            }

            return View(dispatchLog);
        }

        private bool DispatchLogExists(int id)
        {
            return _context.DispatchLogs.Any(e => e.Id == id);
        }
    }
}
