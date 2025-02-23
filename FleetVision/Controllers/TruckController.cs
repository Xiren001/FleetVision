﻿using FleetVision.DBContext;
using FleetVision.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Drawing.Imaging;
using ZXing;
using ZXing.Common;

namespace FleetVision.Controllers
{
    public class TruckController : Controller
    {
        private readonly FleetVisionContext _context;

        public TruckController(FleetVisionContext context)
        {
            _context = context;
        }

        // GET: Trucks
        public async Task<IActionResult> Index()
        {
            return View(await _context.Trucks.ToListAsync());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] Truck truck, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data." });
            }

            try
            {
                // Handle Image Upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/TrucksImages");
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    // Save only the relative path
                    truck.ImagePath = "/TrucksImages/" + uniqueFileName;
                }

                // Save the truck record
                _context.Add(truck);
                await _context.SaveChangesAsync();

                // Generate QR code
                string qrCodeData = $"ID: {truck.Id}, Plate: {truck.PlateNumber}, Model: {truck.Model}, Capacity: {truck.PayloadCapacity}";
                truck.QRCode = GenerateQRCode(qrCodeData);
                if (truck.QRCode == null)
                {
                    return Json(new { success = false, message = "Failed to generate QR code." });
                }

                _context.Update(truck);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Truck created successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error saving truck.", error = ex.Message });
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromForm] Truck truck, IFormFile? imageFile)
        {
            if (id != truck.Id)
            {
                return Json(new { success = false, message = "Truck ID mismatch." });
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            try
            {
                var existingTruck = await _context.Trucks.FindAsync(id);
                if (existingTruck == null)
                {
                    return Json(new { success = false, message = "Truck not found." });
                }

                // Log incoming truck details for debugging
                Console.WriteLine($"Updating Truck ID: {id}, Plate: {truck.PlateNumber}, Model: {truck.Model}, Capacity: {truck.PayloadCapacity}");

                // Update non-file fields
                existingTruck.PlateNumber = truck.PlateNumber;
                existingTruck.Model = truck.Model;
                existingTruck.PayloadCapacity = truck.PayloadCapacity;

                // Handle Image Upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/TrucksImages");

                    // Delete old image if it exists
                    if (!string.IsNullOrEmpty(existingTruck.ImagePath))
                    {
                        string oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingTruck.ImagePath.TrimStart('/'));
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

                    existingTruck.ImagePath = "/TrucksImages/" + uniqueFileName;
                }

                // Generate new QR Code
                string qrCodeData = $"ID: {existingTruck.Id}, Plate: {existingTruck.PlateNumber}, Model: {existingTruck.Model}, Capacity: {existingTruck.PayloadCapacity}";
                existingTruck.QRCode = GenerateQRCode(qrCodeData);

                // Save changes
                _context.Update(existingTruck);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Truck updated successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating truck: " + ex.Message);
                return Json(new { success = false, message = "Error updating truck.", error = ex.Message });
            }
        }





        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var truck = await _context.Trucks.FindAsync(id);
            if (truck == null)
            {
                return Json(new { success = false, message = "Truck not found." });
            }

            try
            {
                _context.Trucks.Remove(truck);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Truck deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting truck.", error = ex.Message });
            }
        }


        public async Task<IActionResult> TodayDispatches()
        {
            var today = DateTime.Today;

            var dispatchRecords = await _context.DispatchLogs
                .Where(d => d.DispatchTime.Date == today || (d.ReturnTime.HasValue && d.ReturnTime.Value.Date == today))
                .Include(d => d.Truck)
                .OrderBy(d => d.DispatchTime)
                .ToListAsync();

            var formattedRecords = dispatchRecords.Select(d => new
            {
                Truck = d.Truck.PlateNumber,
                DispatchTime = d.DispatchTime.ToString("hh:mm tt"),
                ReturnTime = d.ReturnTime?.ToString("hh:mm tt") ?? "In Transit",
                Status = d.ReturnTime.HasValue ? "Returned" : "Dispatched"
            }).ToList();

            return View(formattedRecords);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RecordDispatch([FromBody] DispatchRequestModel data)
        {
            if (data == null || string.IsNullOrEmpty(data.QrCodeData))
            {
                return Json(new { success = false, message = "Invalid QR code." });
            }

            string[] qrParts = data.QrCodeData.Split(", ");
            string truckPlate = qrParts.FirstOrDefault(e => e.StartsWith("Plate:"))?.Split(": ")[1];

            if (string.IsNullOrEmpty(truckPlate))
            {
                return Json(new { success = false, message = "Invalid QR Code format. Plate Number missing." });
            }

            var truck = await _context.Trucks.FirstOrDefaultAsync(e => e.PlateNumber == truckPlate);
            if (truck == null)
            {
                return Json(new { success = false, message = "Truck not found." });
            }

            var now = DateTime.Now;

            // Get the latest dispatch log for this truck
            var lastDispatch = await _context.DispatchLogs
                .Where(d => d.TruckId == truck.Id)
                .OrderByDescending(d => d.DispatchTime)
                .FirstOrDefaultAsync();

            if (lastDispatch != null && lastDispatch.ReturnTime == null)
            {
                // This means the truck is currently dispatched and should be returned
                lastDispatch.ReturnTime = now;
                _context.Update(lastDispatch);

                _context.DispatchHistories.Add(new DispatchHistory
                {
                    TruckId = truck.Id,
                    Timestamp = now,
                    Status = "Returned"
                });

                truck.Status = "Available"; // Truck is now free

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "✅ Truck returned!" });
            }
            else if (truck.Status == "Available")
            {
                // Ensure we only dispatch if the truck was actually returned before
                var newDispatch = new DispatchLog
                {
                    TruckId = truck.Id,
                    DispatchTime = now
                };
                _context.DispatchLogs.Add(newDispatch);

                _context.DispatchHistories.Add(new DispatchHistory
                {
                    TruckId = truck.Id,
                    Timestamp = now,
                    Status = "Dispatched"
                });

                truck.Status = "Not Available"; // Truck is now in use

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "✅ Truck dispatched!" });
            }
            else
            {
                return Json(new { success = false, message = "🚫 Truck cannot be dispatched or returned at this moment." });
            }
        }


        private string? GenerateQRCode(string qrCodeData)
        {
            try
            {
                var qrWriter = new BarcodeWriterPixelData
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new EncodingOptions { Height = 300, Width = 300, Margin = 1 }
                };

                var pixelData = qrWriter.Write(qrCodeData);

                using (var bitmap = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppRgb))
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

                    return $"/qrcodes/{qrCodeFileName}";
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
