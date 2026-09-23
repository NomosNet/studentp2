using Microsoft.EntityFrameworkCore;
using StudentPass.Contracts;
using ServiceUsers.Data.Entities;

namespace ServiceUsers.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<EmailVerification> EmailVerifications => Set<EmailVerification>();
    public DbSet<PartnerRequest> PartnerRequests => Set<PartnerRequest>();
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Ad> Ads => Set<Ad>();
    public DbSet<AdCategory> AdCategories => Set<AdCategory>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<ManagerAssignment> ManagerAssignments => Set<ManagerAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Email).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.Property(x => x.FullName).IsRequired();
            entity.Property(x => x.Role)
                .HasConversion(v => v.ToApi(), v => UserRoleExtensions.FromApi(v));
            entity.HasOne(x => x.Partner)
                .WithOne(x => x.User)
                .HasForeignKey<Partner>(x => x.UserEmail)
                .HasPrincipalKey<User>(x => x.Email)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmailVerification>(entity =>
        {
            entity.ToTable("email_verifications");
            entity.Property(x => x.Code).HasMaxLength(6).IsRequired();
        });

        modelBuilder.Entity<PartnerRequest>(entity =>
        {
            entity.ToTable("partner_requests");
            entity.Property(x => x.Status)
                .HasConversion(
                    v => v.ToString().ToLowerInvariant(),
                    v => Enum.Parse<PartnerRequestStatus>(v, true));
        });

        modelBuilder.Entity<Partner>(entity =>
        {
            entity.ToTable("partners");
            entity.HasIndex(x => x.UserEmail).IsUnique();
            entity.HasMany(x => x.Ads)
                .WithOne(x => x.Partner)
                .HasForeignKey(x => x.PartnerEmail)
                .HasPrincipalKey(x => x.UserEmail)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Ad>(entity =>
        {
            entity.ToTable("ads");
            entity.HasMany(x => x.Categories)
                .WithMany(x => x.Ads)
                .UsingEntity<AdCategory>(
                    right => right.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade),
                    left => left.HasOne<Ad>().WithMany().HasForeignKey(x => x.AdId).OnDelete(DeleteBehavior.Cascade),
                    join =>
                    {
                        join.ToTable("ad_categories");
                        join.HasKey(x => new { x.AdId, x.CategoryId });
                    });
        });

        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.ToTable("favorites");
            entity.HasOne(x => x.User)
                .WithMany(x => x.Favorites)
                .HasForeignKey(x => x.UserEmail)
                .HasPrincipalKey(x => x.Email)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Ad)
                .WithMany(x => x.Favorites)
                .HasForeignKey(x => x.AdId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ManagerAssignment>(entity =>
        {
            entity.ToTable("manager_assignments");
            entity.HasIndex(x => new { x.ManagerEmail, x.PartnerId }).IsUnique();
            entity.Property(x => x.ManagerEmail).IsRequired();
            entity.HasOne(x => x.Manager)
                .WithMany(x => x.Assignments)
                .HasForeignKey(x => x.ManagerEmail)
                .HasPrincipalKey(x => x.Email)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Partner)
                .WithMany(x => x.Managers)
                .HasForeignKey(x => x.PartnerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
