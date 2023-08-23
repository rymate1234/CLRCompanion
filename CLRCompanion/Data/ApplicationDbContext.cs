using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Hosting;
using System.Reflection.Metadata;
using System.Text.Json;

namespace CLRCompanion.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public DbSet<Bot> Bots { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<OpenAIFunction> OpenAIFunctions { get; set; }
        public DbSet<UserPreferences> UserPreferences { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<OpenAIFunctionParam>()
                .Property(e => e.Enum)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null),
                    new ValueComparer<ICollection<string>>(
                        (c1, c2) => c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => (ICollection<string>)c.ToList()));
        }
    }
}