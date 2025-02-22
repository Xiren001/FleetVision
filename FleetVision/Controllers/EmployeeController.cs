using FleetVision.DBContext;
using FleetVision.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Drawing.Imaging;
using ZXing;
using ZXing.Common;

namespace FleetVision.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly FleetVisionContext _context;
        // Inject Identity services – note that your Identity user type is "Users"
        private readonly UserManager<Users> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public EmployeeController(
            FleetVisionContext context,
            UserManager<Users> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Employee
        public async Task<IActionResult> Index()
        {
            return View(await _context.Employees.ToListAsync());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] Employee employee, IFormFile? imageFile) // Accept form data and file
        {
            Console.WriteLine("Create action triggered.");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage)
                                              .ToList();
                Console.WriteLine($"Validation Errors: {string.Join(", ", errors)}");
                return Json(new { success = false, message = "Invalid data. Please check the form.", errors });
            }

            try
            {
                // Handle Image Upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/EmployeeImages");
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    // Save only the relative path
                    employee.ImagePath = "/EmployeeImages/" + uniqueFileName;
                }

                // 1. Save the employee record in your custom table.
                _context.Add(employee);
                await _context.SaveChangesAsync();

                // 2. Generate a default password (make sure it meets your Identity password policy)
                string defaultPassword = $"{employee.Name.ToLower()}_{employee.Id}!Password";
                employee.Password = Employee.HashPassword(defaultPassword);

                // 3. Generate a QR code
                string qrCodeData = $"ID: {employee.Id}, Name: {employee.Name}, Position: {employee.Position}, Department: {employee.Department}, Email: {employee.Email}";
                employee.QRCode = GenerateQRCode(qrCodeData);
                if (employee.QRCode == null)
                {
                    return Json(new { success = false, message = "Failed to generate QR code. Please try again." });
                }

                _context.Update(employee);
                await _context.SaveChangesAsync();

                // 4. Create an Identity user for the employee.
                var employeeUser = new Users
                {
                    FullName = employee.Name,
                    UserName = employee.Email,
                    Email = employee.Email,
                };

                var identityResult = await _userManager.CreateAsync(employeeUser, defaultPassword);
                if (!identityResult.Succeeded)
                {
                    var errorMessages = identityResult.Errors.Select(e => e.Description);
                    Console.WriteLine($"Identity creation errors: {string.Join(", ", errorMessages)}");
                    return Json(new { success = false, message = "Failed to create identity user.", errors = errorMessages });
                }

                // 5. Ensure the "Employee" role exists.
                if (!await _roleManager.RoleExistsAsync("Employee"))
                {
                    var roleResult = await _roleManager.CreateAsync(new IdentityRole("Employee"));
                    if (!roleResult.Succeeded)
                    {
                        var roleErrors = roleResult.Errors.Select(e => e.Description);
                        Console.WriteLine($"Role creation errors: {string.Join(", ", roleErrors)}");
                        return Json(new { success = false, message = "Failed to create the Employee role.", errors = roleErrors });
                    }
                }

                // 6. Assign the Identity user to the "Employee" role.
                await _userManager.AddToRoleAsync(employeeUser, "Employee");

                return Json(new { success = true, message = "Employee created successfully.", defaultPassword = defaultPassword });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving employee: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while saving the employee. Please try again." });
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromForm] Employee employee, IFormFile? imageFile)
        {
            if (id != employee.Id)
            {
                return Json(new { success = false, message = "Employee ID mismatch." });
            }

            ModelState.Remove("Password");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                }

                return Json(new { success = false, message = "Invalid data. Please check the form." });
            }

            try
            {
                // Get the existing employee from the database
                var existingEmployee = await _context.Employees.FindAsync(id);
                if (existingEmployee == null)
                {
                    return Json(new { success = false, message = "Employee not found." });
                }

                // Update the employee fields (excluding Password)
                existingEmployee.Name = employee.Name;
                existingEmployee.Position = employee.Position;
                existingEmployee.Department = employee.Department;

                // Handle Image Upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/EmployeeImages");

                    // Delete old image if it exists
                    if (!string.IsNullOrEmpty(existingEmployee.ImagePath))
                    {
                        string oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingEmployee.ImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Save new image
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    // Update image path
                    existingEmployee.ImagePath = "/EmployeeImages/" + uniqueFileName;
                }

                // Generate new QR code data
                string qrCodeData = $"ID: {employee.Id}, Name: {employee.Name}, Position: {employee.Position}, Department: {employee.Department}, Email: {employee.Email}";
                string newQRCodePath = GenerateQRCode(qrCodeData);

                if (newQRCodePath == null)
                {
                    return Json(new { success = false, message = "Failed to generate new QR code. Please try again." });
                }

                // Delete the old QR code file
                if (!string.IsNullOrEmpty(existingEmployee.QRCode))
                {
                    string oldQRCodePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingEmployee.QRCode.TrimStart('/'));
                    if (System.IO.File.Exists(oldQRCodePath))
                    {
                        System.IO.File.Delete(oldQRCodePath);
                    }
                }

                // Update the QR code path
                existingEmployee.QRCode = newQRCodePath;

                _context.Update(existingEmployee);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Employee updated successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating employee: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while updating the employee. Please try again." });
            }
        }



        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return Json(new { success = false, message = "Employee not found." });
            }

            try
            {
                // Delete the QR code file
                if (!string.IsNullOrEmpty(employee.QRCode))
                {
                    string qrCodePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", employee.QRCode.TrimStart('/'));
                    if (System.IO.File.Exists(qrCodePath))
                    {
                        System.IO.File.Delete(qrCodePath);
                    }
                }

                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Employee deleted successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting employee: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while deleting the employee. Please try again." });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken] // 🚨 Disable validation for fetch requests, or handle it properly in headers
        public async Task<IActionResult> RecordAttendance([FromBody] AttendanceRequestModel data)
        {
            if (data == null || string.IsNullOrEmpty(data.QrCodeData))
            {
                return Json(new { success = false, message = "Invalid QR code." });
            }

            string[] qrParts = data.QrCodeData.Split(", ");
            string employeeEmail = qrParts.FirstOrDefault(e => e.StartsWith("Email:"))?.Split(": ")[1];

            if (string.IsNullOrEmpty(employeeEmail))
            {
                return Json(new { success = false, message = "Invalid QR Code format. Email missing." });
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == employeeEmail);
            if (employee == null)
            {
                return Json(new { success = false, message = "Employee not found." });
            }

            var today = DateTime.Today;
            var now = DateTime.Now;

            // Get all attendance records for today
            var todaysRecords = await _context.Attendances
                .Where(a => a.EmployeeId == employee.Id && a.TimeIn.Date == today)
                .OrderBy(a => a.TimeIn)
                .ToListAsync();

            // Determine the next action based on existing records
            if (todaysRecords.Count == 0)
            {
                // First Time In
                var newAttendance = new Attendance
                {
                    EmployeeId = employee.Id,
                    TimeIn = now
                };
                _context.Attendances.Add(newAttendance);

                _context.AttendanceLogs.Add(new AttendanceLog
                {
                    EmployeeId = employee.Id,
                    Timestamp = now,
                    Status = "Time In (Start of Shift)"
                });

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "✅ Time In recorded successfully!" });
            }
            else if (todaysRecords.Count == 1 && todaysRecords[0].TimeOut == null)
            {
                // First Time Out (Break)
                todaysRecords[0].TimeOut = now;
                _context.Update(todaysRecords[0]);

                _context.AttendanceLogs.Add(new AttendanceLog
                {
                    EmployeeId = employee.Id,
                    Timestamp = now,
                    Status = "Time Out (Break)"
                });

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "✅ Time Out for break recorded!" });
            }
            else if (todaysRecords.Count == 1 && todaysRecords[0].TimeOut != null)
            {
                // Second Time In (After Break)
                var secondAttendance = new Attendance
                {
                    EmployeeId = employee.Id,
                    TimeIn = now
                };
                _context.Attendances.Add(secondAttendance);

                _context.AttendanceLogs.Add(new AttendanceLog
                {
                    EmployeeId = employee.Id,
                    Timestamp = now,
                    Status = "Time In (After Break)"
                });

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "✅ Time In after break recorded!" });
            }
            else if (todaysRecords.Count == 2 && todaysRecords[1].TimeOut == null)
            {
                // Second Time Out (End of Shift)
                todaysRecords[1].TimeOut = now;
                _context.Update(todaysRecords[1]);

                _context.AttendanceLogs.Add(new AttendanceLog
                {
                    EmployeeId = employee.Id,
                    Timestamp = now,
                    Status = "Time Out (End of Shift)"
                });

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "✅ Time Out at end of shift recorded!" });
            }
            else
            {
                return Json(new { success = false, message = "⚠ You have already completed your attendance for today." });
            }
        }

        public async Task<IActionResult> TodayAttendance()
        {
            var today = DateTime.Today;

            var attendanceRecords = await _context.Attendances
                .Where(a => a.TimeIn.Date == today)
                .Include(a => a.Employee) // Include Employee details
                .OrderBy(a => a.TimeIn)
                .ToListAsync();

            // Group by employee to get 2 Time In & Time Out records
            var groupedRecords = attendanceRecords
                .GroupBy(a => a.EmployeeId)
                .Select(g => new
                {
                    Employee = g.First().Employee, // Get employee details
                    FirstTimeIn = g.ElementAtOrDefault(0)?.TimeIn,
                    FirstTimeOut = g.ElementAtOrDefault(0)?.TimeOut,
                    SecondTimeIn = g.ElementAtOrDefault(1)?.TimeIn,
                    SecondTimeOut = g.ElementAtOrDefault(1)?.TimeOut
                })
                .ToList();

            return View(groupedRecords);
        }

        private string? GenerateQRCode(string qrCodeData)
        {
            try
            {
                Console.WriteLine("Generating QR code...");
                var qrWriter = new BarcodeWriterPixelData
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new EncodingOptions { Height = 300, Width = 300, Margin = 1 }
                };

                var pixelData = qrWriter.Write(qrCodeData);

                using (var bitmap = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppRgb))
                using (var ms = new MemoryStream())
                {
                    var bitmapData = bitmap.LockBits(new Rectangle(0, 0, pixelData.Width, pixelData.Height),
                        ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
                    try
                    {
                        System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
                    }
                    finally
                    {
                        bitmap.UnlockBits(bitmapData);
                    }

                    string qrCodeFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "qrcodes");
                    if (!Directory.Exists(qrCodeFolderPath))
                    {
                        Directory.CreateDirectory(qrCodeFolderPath);
                    }

                    string qrCodeFileName = $"{Guid.NewGuid()}.png";
                    string qrCodeFilePath = Path.Combine(qrCodeFolderPath, qrCodeFileName);
                    bitmap.Save(qrCodeFilePath, ImageFormat.Png);

                    if (System.IO.File.Exists(qrCodeFilePath))
                    {
                        Console.WriteLine("QR code generated and saved successfully.");
                        return $"/qrcodes/{qrCodeFileName}";
                    }
                    else
                    {
                        Console.WriteLine("Failed to save QR code file.");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating QR code: {ex.Message}");
                return null;
            }
        }
    }
}
