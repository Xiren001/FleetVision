using Microsoft.AspNetCore.Identity;

namespace FleetVision.Models
{
    public class Users : IdentityUser
    {
        public string FullName { get; set; }
    }
}
