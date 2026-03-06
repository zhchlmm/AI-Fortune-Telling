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

    public DbSet<MiniappUserEntity> MiniappUsers => Set<MiniappUserEntity>();

    public DbSet<MiniappProfileAuditEntity> MiniappProfileAudits => Set<MiniappProfileAuditEntity>();

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
        modelBuilder.Entity<MiniappUserEntity>().ToTable("MiniappUsers");
        modelBuilder.Entity<MiniappProfileAuditEntity>().ToTable("MiniappProfileAudits");

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

        modelBuilder.Entity<MiniappUserEntity>()
            .HasIndex(x => x.OpenId)
            .IsUnique();

        modelBuilder.Entity<MiniappUserEntity>()
            .Property(x => x.OpenId)
            .HasMaxLength(128);

        modelBuilder.Entity<MiniappUserEntity>()
            .Property(x => x.SessionKey)
            .HasMaxLength(256);

        modelBuilder.Entity<MiniappUserEntity>()
            .Property(x => x.Nickname)
            .HasMaxLength(64);

        modelBuilder.Entity<MiniappUserEntity>()
            .Property(x => x.Avatar)
            .HasMaxLength(512);

        modelBuilder.Entity<MiniappUserEntity>()
            .Property(x => x.Email)
            .HasMaxLength(128);

        modelBuilder.Entity<MiniappUserEntity>()
            .Property(x => x.PhoneNumber)
            .HasMaxLength(32);

        modelBuilder.Entity<MiniappProfileAuditEntity>()
            .Property(x => x.OpenId)
            .HasMaxLength(128);

        modelBuilder.Entity<MiniappProfileAuditEntity>()
            .Property(x => x.Action)
            .HasMaxLength(64);

        modelBuilder.Entity<MiniappProfileAuditEntity>()
            .Property(x => x.ChangedFields)
            .HasMaxLength(1024);

        modelBuilder.Entity<MiniappProfileAuditEntity>()
            .HasIndex(x => x.OpenId);
    }
}
