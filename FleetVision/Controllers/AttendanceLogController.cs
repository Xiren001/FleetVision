using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FleetVision.DBContext;
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

        public async Task<IActionResult> Attendance(string search, DateTime? startDate, DateTime? endDate)
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
        public async Task<IActionResult> TodayAttendance()
        {
            var today = DateTime.Today;

            var logs = await _context.AttendanceLogs
                .Where(a => a.Timestamp.Date == today)
                .GroupBy(a => a.EmployeeId) // Group by EmployeeId
                .Select(g => new Attendance
                {
                    Id = g.First().Id,
                    EmployeeId = g.Key,
                    TimeIn = g.Min(a => a.Timestamp), // Get the earliest time-in
                    TimeOut = g.Count() > 1 ? g.Max(a => a.Timestamp) : null, // If multiple records exist, get the latest timestamp as time-out
                    Employee = _context.Employees
                        .Where(e => e.Id == g.Key)
                        .Select(e => new Employee { Name = e.Name })
                        .FirstOrDefault()
                })
                .ToListAsync();

            return View(logs);
        }



    }
}
