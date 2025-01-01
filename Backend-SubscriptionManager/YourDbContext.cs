using Backend_SubscriptionManager.Model;
using Microsoft.EntityFrameworkCore;
namespace YourProjectNamespace.Data
{
    public class YourDbContext : DbContext
    {
        public YourDbContext(DbContextOptions<YourDbContext> options)
            : base(options)
        {
        }

        // DbSet for Subscriptions Table
        public DbSet<Subscription> Subscriptions { get; set; }
    }
}
