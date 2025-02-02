using Microsoft.AspNetCore.Mvc;
using FleetVision.Models;
using FleetVision.DBContext;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace FleetVision.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly FleetVisionContext _context;

        public EmployeeController(FleetVisionContext context)
        {
            _context = context;
        }

        // GET: Employee
        public async Task<IActionResult> Index()
        {
            return View(await _context.Employees.ToListAsync());
        }

        // GET: Employee/Create
        public IActionResult Create()
        {
            return View();
        }

        // GET: create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee)
        {
            Console.WriteLine("Create action triggered."); // Log to confirm the action is being called

            // Generate QR code data
            string qrCodeData = $"Name: {employee.Name}, Position: {employee.Position}, Department: {employee.Department}";

            // Generate QR code and save it
            employee.QRCode = GenerateQRCode(qrCodeData);

            // Check if QR code generation failed
            if (employee.QRCode == null)
            {
                ModelState.AddModelError(string.Empty, "Failed to generate QR code. Please try again.");
                return View(employee);
            }

            // Validate the model after setting the QRCode property
            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState is invalid."); // Log if ModelState is invalid
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                }
                return View(employee);
            }

            try
            {
                Console.WriteLine("Saving employee to database..."); // Log database save attempt
                _context.Add(employee);
                await _context.SaveChangesAsync();

                Console.WriteLine("Employee saved successfully."); // Log success
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving employee: {ex.Message}"); // Log any exceptions
                ModelState.AddModelError(string.Empty, "An error occurred while saving the employee. Please try again.");
                return View(employee);
            }
        }

        private string? GenerateQRCode(string qrCodeData)
        {
            try
            {
                Console.WriteLine("Generating QR code..."); // Log QR code generation

                // Create a QR code writer
                var qrWriter = new BarcodeWriterPixelData
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new EncodingOptions
                    {
                        Height = 300,
                        Width = 300,
                        Margin = 1
                    }
                };

                // Generate the QR code pixel data
                var pixelData = qrWriter.Write(qrCodeData);

                // Convert pixel data to a bitmap
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

                    // Ensure the "qrcodes" folder exists
                    string qrCodeFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "qrcodes");
                    if (!Directory.Exists(qrCodeFolderPath))
                    {
                        Directory.CreateDirectory(qrCodeFolderPath);
                    }

                    // Save the QR code image to a file
                    string qrCodeFileName = $"{Guid.NewGuid()}.png";
                    string qrCodeFilePath = Path.Combine(qrCodeFolderPath, qrCodeFileName);
                    bitmap.Save(qrCodeFilePath, ImageFormat.Png);

                    // Verify that the file was created successfully
                    if (System.IO.File.Exists(qrCodeFilePath))
                    {
                        Console.WriteLine("QR code generated and saved successfully."); // Log success
                        return $"/qrcodes/{qrCodeFileName}";
                    }
                    else
                    {
                        Console.WriteLine("Failed to save QR code file."); // Log failure
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error generating QR code: {ex.Message}");
                return null;
            }
        }

        // GET: Employee/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Employee employee)
        {
            if (id != employee.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Generate new QR code data
                    string qrCodeData = $"Name: {employee.Name}, Position: {employee.Position}, Department: {employee.Department}";

                    // Generate new QR code and save it
                    string newQRCodePath = GenerateQRCode(qrCodeData);

                    // Check if QR code generation failed
                    if (newQRCodePath == null)
                    {
                        ModelState.AddModelError(string.Empty, "Failed to generate new QR code. Please try again.");
                        return View(employee);
                    }

                    // Get the existing employee from the database
                    var existingEmployee = await _context.Employees.FindAsync(id);
                    if (existingEmployee == null)
                    {
                        return NotFound();
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

                    // Update the employee with the new QR code path
                    existingEmployee.Name = employee.Name;
                    existingEmployee.Position = employee.Position;
                    existingEmployee.Department = employee.Department;
                    existingEmployee.QRCode = newQRCodePath;

                    // Save changes to the database
                    _context.Update(existingEmployee);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating employee: {ex.Message}");
                    ModelState.AddModelError(string.Empty, "An error occurred while updating the employee. Please try again.");
                    return View(employee);
                }
            }
            return View(employee);
        }

        // GET: Employee/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(m => m.Id == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // POST: Employee/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            return PartialView("_DetailsPartial", employee);
        }
    }
}