namespace FleetVision.Models
{
    public class AttendanceLog
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } // "Time In" or "Time Out"

        // Navigation property
        public Employee Employee { get; set; }
    }

}
