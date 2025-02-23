using System.ComponentModel.DataAnnotations;

namespace FleetVision.Models
{
    public class Truck
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string PlateNumber { get; set; }

        [Required]
        [StringLength(50)]
        public string VIN { get; set; }

        [Required]
        [StringLength(50)]
        public string Make { get; set; }

        [Required]
        [StringLength(50)]
        public string Model { get; set; }

        [Required]
        public int Year { get; set; }

        [StringLength(30)]
        public string Color { get; set; }

        [Required]
        [StringLength(20)]
        public string FuelType { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal GrossWeight { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal PayloadCapacity { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal FuelCapacity { get; set; }

        public string? QRCode { get; set; }
        public string? ImagePath { get; set; }

        // Truck Status: Available or Not Available
        [Required]
        public string Status { get; set; } = "Available";
    }
}