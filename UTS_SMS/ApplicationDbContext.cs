using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace UTS_SMS
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    }
}
