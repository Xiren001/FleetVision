namespace FleetVision.Models
{
    public class Attendance
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime TimeIn { get; set; }
        public DateTime? TimeOut { get; set; }

        // Navigation property
        public Employee Employee { get; set; }
    }

}
