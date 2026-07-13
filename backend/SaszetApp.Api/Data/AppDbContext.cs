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
        public DbSet<SystemSettingEntity> SystemSettings { get; set; }
        public DbSet<UserScanLimitEntity> UserScanLimits { get; set; }
        public DbSet<UserScanUsageEntity> UserScanUsages { get; set; }

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
                if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
                {
                    entity.HasIndex(e => e.IsPrimary).IsUnique().HasFilter("\"IsPrimary\" = true");
                }
                else
                {
                    entity.HasIndex(e => e.IsPrimary).IsUnique().HasFilter("\"IsPrimary\" = 1");
                }
            });

            modelBuilder.Entity<SystemSettingEntity>(entity =>
            {
                entity.ToTable("SystemSettings");
                entity.HasKey(e => e.Key);
            });

            modelBuilder.Entity<UserScanLimitEntity>(entity =>
            {
                entity.ToTable("UserScanLimits");
                entity.HasKey(e => e.UserId);
            });

            modelBuilder.Entity<UserScanUsageEntity>(entity =>
            {
                entity.ToTable("UserScanUsages");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ScannedAt);
            });
        }
    }
}
