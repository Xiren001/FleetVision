using Microsoft.EntityFrameworkCore;

namespace FleetVision.Services
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions options) : base(options) 
        { 
        }
        //public DbSet<Info> infos { get; set; }
    } 
} 