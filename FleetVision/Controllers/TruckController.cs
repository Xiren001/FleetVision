using FleetVision.DBContext;
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
                if (imageFile != null && imageFile.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await imageFile.CopyToAsync(memoryStream);
                        truck.Image = memoryStream.ToArray(); // Store image as byte array
                    }
                }

                _context.Add(truck);
                await _context.SaveChangesAsync();

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
                return Json(new { success = false, message = "Invalid data." });
            }

            try
            {
                var existingTruck = await _context.Trucks.FindAsync(id);
                if (existingTruck == null)
                {
                    return Json(new { success = false, message = "Truck not found." });
                }

                existingTruck.PlateNumber = truck.PlateNumber;
                existingTruck.Model = truck.Model;
                existingTruck.PayloadCapacity = truck.PayloadCapacity;

                if (imageFile != null && imageFile.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await imageFile.CopyToAsync(memoryStream);
                        existingTruck.Image = memoryStream.ToArray(); // Update image
                    }
                }

                string qrCodeData = $"ID: {truck.Id}, Plate: {truck.PlateNumber}, Model: {truck.Model}, Capacity: {truck.PayloadCapacity}";
                existingTruck.QRCode = GenerateQRCode(qrCodeData);

                _context.Update(existingTruck);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Truck updated successfully." });
            }
            catch (Exception ex)
            {
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
