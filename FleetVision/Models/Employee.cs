namespace FleetVision.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Position { get; set; }
        public string Department { get; set; }
        public string? QRCode { get; set; } // This will store the QR code image path or data
    }
}