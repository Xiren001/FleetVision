namespace FleetVision.Models
{
    public class DispatchHistory
    {
        public int Id { get; set; }
        public int TruckId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } // "Dispatched" or "Returned"

        // Navigation property
        public Truck Truck { get; set; }
    }
}
