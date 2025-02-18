using FleetVision.DBContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using FleetVision.Models;

namespace FleetVision.Controllers
{
    public class AttendanceLogController : Controller
    {
        private readonly FleetVisionContext _context;

        public AttendanceLogController(FleetVisionContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search, DateTime? startDate, DateTime? endDate)
        {
            var logs = _context.AttendanceLogs.Include(a => a.Employee).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                logs = logs.Where(a => a.Employee.Name.Contains(search));
            }

            if (startDate.HasValue)
            {
                logs = logs.Where(a => a.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                logs = logs.Where(a => a.Timestamp <= endDate.Value);
            }

            return View(await logs.ToListAsync());
        }
    }
}
