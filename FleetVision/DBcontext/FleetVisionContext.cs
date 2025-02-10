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
    }
}