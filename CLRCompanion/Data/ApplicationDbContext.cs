using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;

namespace CLRCompanion.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public DbSet<Bot> Bots { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<UserPreferences> UserPreferences { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
            => base.OnModelCreating(builder);
    }
}