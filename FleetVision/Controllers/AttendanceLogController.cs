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
                .OrderBy(a => a.Timestamp) // Order logs chronologically
                .ToListAsync();

            var groupedLogs = logs
                .GroupBy(a => a.EmployeeId)
                .Select(g =>
                {
                    var logEntries = g.OrderBy(a => a.Timestamp).ToList();
                    var attendancePairs = new List<Attendance>();

                    for (int i = 0; i < logEntries.Count; i += 2)
                    {
                        var timeIn = logEntries[i].Timestamp;
                        DateTime? timeOut = (i + 1 < logEntries.Count) ? logEntries[i + 1].Timestamp : null;

                        attendancePairs.Add(new Attendance
                        {
                            EmployeeId = g.Key,
                            TimeIn = timeIn,
                            TimeOut = timeOut,
                            Employee = _context.Employees
                                .Where(e => e.Id == g.Key)
                                .Select(e => new Employee { Name = e.Name })
                                .FirstOrDefault()
                        });
                    }

                    return attendancePairs;
                })
                .SelectMany(a => a) // Flatten list
                .ToList();

            return View(groupedLogs);
        }

    }
}
