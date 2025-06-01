using Microsoft.EntityFrameworkCore;

namespace Azunt.DepotManagement
{
    public class DepotAppDbContext : DbContext
    {
        public DepotAppDbContext(DbContextOptions<DepotAppDbContext> options)
            : base(options)
        {
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Depot>()
                .Property(m => m.CreatedAt)
                .HasDefaultValueSql("GetDate()");
        }

        public DbSet<Depot> Depots { get; set; } = null!;
    }
}