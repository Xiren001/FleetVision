namespace FleetVision.Models
{
    public class DispatchLog
    {
        public int Id { get; set; }
        public int TruckId { get; set; }
        public DateTime DispatchTime { get; set; }
        public DateTime? ReturnTime { get; set; }

        // Navigation property
        public Truck Truck { get; set; }
    }
}