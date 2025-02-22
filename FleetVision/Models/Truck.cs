using System.ComponentModel.DataAnnotations;

namespace FleetVision.Models
{
    public class Truck
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string PlateNumber { get; set; }  // Unique license plate number

        [Required]
        [StringLength(50)]
        public string VIN { get; set; }  // Unique 17-character vehicle identifier

        [Required]
        [StringLength(50)]
        public string Make { get; set; }  // Manufacturer (Ford, Volvo, etc.)

        [Required]
        [StringLength(50)]
        public string Model { get; set; }  // Truck model (F-150, FH16, etc.)

        [Required]
        public int Year { get; set; }  // Manufacturing year

        [StringLength(30)]
        public string Color { get; set; }  // Truck color

        [Required]
        [StringLength(20)]
        public string FuelType { get; set; }  // Diesel, Gasoline, Electric, etc.

        [Required]
        [Range(0, double.MaxValue)]
        public decimal GrossWeight { get; set; }  // Maximum weight in kg/lbs

        [Required]
        [Range(0, double.MaxValue)]
        public decimal PayloadCapacity { get; set; }  // Maximum cargo weight capacity

        [Required]
        [Range(0, double.MaxValue)]
        public decimal FuelCapacity { get; set; }  // Fuel tank capacity (liters/gallons)

        public string? QRCode { get; set; }
        public string? ImagePath { get; set; }
    }
}
