using Microsoft.EntityFrameworkCore;

namespace SaszetApp.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<PetFoodItemEntity> PetFoodItems { get; set; }
        public DbSet<LlmProviderEntity> LlmProviders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PetFoodItemEntity>(entity =>
            {
                entity.ToTable("PetFoodItems");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.EanCode, e.Language }).IsUnique();
                entity.HasIndex(e => e.EanCode);
                entity.HasIndex(e => e.ProductName);
                entity.HasIndex(e => e.Language);

                entity.Property(e => e.Pros).HasColumnType("jsonb");
                entity.Property(e => e.Cons).HasColumnType("jsonb");
            });

            modelBuilder.Entity<LlmProviderEntity>(entity =>
            {
                entity.ToTable("LlmProviders");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ProviderName).IsUnique();
                if (Database.IsNpgsql())
                {
                    entity.HasIndex(e => e.IsPrimary).IsUnique().HasFilter("\"IsPrimary\" = true");
                }
                else
                {
                    entity.HasIndex(e => e.IsPrimary).IsUnique().HasFilter("\"IsPrimary\" = 1");
                }
            });
        }
    }
}
