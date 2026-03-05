using Microsoft.EntityFrameworkCore;

namespace AdminApi.Host.Data;

public class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options)
{
    public DbSet<FortuneSessionEntity> FortuneSessions => Set<FortuneSessionEntity>();

    public DbSet<ContentItemEntity> Contents => Set<ContentItemEntity>();

    public DbSet<ContentCategoryEntity> ContentCategories => Set<ContentCategoryEntity>();

    public DbSet<OrderEntity> Orders => Set<OrderEntity>();

    public DbSet<FortuneTemplateEntity> Templates => Set<FortuneTemplateEntity>();

    public DbSet<AdminUserEntity> AdminUsers => Set<AdminUserEntity>();

    public DbSet<LoginAuditEntity> LoginAudits => Set<LoginAuditEntity>();

    public DbSet<AiAuditEntity> AiAudits => Set<AiAuditEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FortuneSessionEntity>().ToTable("FortuneSessions");
        modelBuilder.Entity<ContentItemEntity>().ToTable("Contents");
        modelBuilder.Entity<ContentCategoryEntity>().ToTable("ContentCategories");
        modelBuilder.Entity<OrderEntity>().ToTable("Orders");
        modelBuilder.Entity<FortuneTemplateEntity>().ToTable("Templates");
        modelBuilder.Entity<AdminUserEntity>().ToTable("AdminUsers");
        modelBuilder.Entity<LoginAuditEntity>().ToTable("LoginAudits");
        modelBuilder.Entity<AiAuditEntity>().ToTable("AiAudits");

        modelBuilder.Entity<ContentCategoryEntity>()
            .HasIndex(x => x.Name)
            .IsUnique();

        modelBuilder.Entity<ContentItemEntity>()
            .HasOne(x => x.Category)
            .WithMany(x => x.Contents)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<OrderEntity>()
            .Property(x => x.Amount)
            .HasPrecision(18, 2);
    }
}
