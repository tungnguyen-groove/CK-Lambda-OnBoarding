using Microsoft.EntityFrameworkCore;
using SnowflakeItemMaster.Domain.Common;
using SnowflakeItemMaster.Domain.Entities;

namespace SnowflakeItemMaster.Infrastructure
{
    public class RepositoryContext : DbContext
    {
        public RepositoryContext(DbContextOptions<RepositoryContext> options)
            : base(options)
        {
        }

        public DbSet<ItemMasterSourceLog> ItemMasterSourceLogs { get; set; }

        //public DbSet<ItemMaster> ItemMasters { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure indexes
            modelBuilder.Entity<ItemMasterSourceLog>()
                .HasIndex(x => x.Sku);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedDate = DateTime.Now;
                        break;
                    case EntityState.Modified:
                        entry.Entity.LastModifiedDate = DateTime.Now;
                        break;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}