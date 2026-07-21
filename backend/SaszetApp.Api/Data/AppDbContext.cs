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
        public DbSet<UserEntity> Users { get; set; }
        public DbSet<CatEntity> Cats { get; set; }

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
                // ProviderName is no longer unique, as we can have multiple keys per provider
                entity.HasIndex(e => e.ProviderName);
                
                // If we still want exactly one global Primary route, we can keep the IsPrimary index, 
                // but since the fallback plan uses PriorityOrder per provider, we might not need the IsPrimary unique index at all. 
                // Wait, PRD says "Exactly ONE provider MUST be marked as IsPrimary = true at any given time."
                // So the global IsPrimary index is still valid. The globally active provider category is selected by IsPrimary = true.
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
            modelBuilder.Entity<UserEntity>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);
                entity.HasMany(e => e.Cats).WithOne(c => c.User).HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CatEntity>(entity =>
            {
                entity.ToTable("Cats");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.Property(e => e.Allergies).HasColumnType("jsonb");
                entity.Property(e => e.Name).HasMaxLength(100);
                entity.Property(e => e.Breed).HasMaxLength(100);
                entity.Property(e => e.Weight).HasPrecision(5, 2);
            });
        }
    }
}
