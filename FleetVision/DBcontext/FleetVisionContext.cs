using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FleetVision.Models;

namespace FleetVision.DBContext
{
    public class FleetVisionContext : DbContext
    {
        public FleetVisionContext(DbContextOptions<FleetVisionContext> options) : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<AttendanceLog> AttendanceLogs { get; set; }
        public DbSet<Truck> Trucks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Ensure base configurations are applied

            // Configure decimal precision
            modelBuilder.Entity<Truck>()
                .Property(t => t.FuelCapacity)
                .HasPrecision(18, 4);

            modelBuilder.Entity<Truck>()
                .Property(t => t.GrossWeight)
                .HasPrecision(18, 4);

            modelBuilder.Entity<Truck>()
                .Property(t => t.PayloadCapacity)
                .HasPrecision(18, 4);
        }
    }
}
