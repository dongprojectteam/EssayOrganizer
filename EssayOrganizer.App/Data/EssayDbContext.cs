using EssayOrganizer.App.Models;
using Microsoft.EntityFrameworkCore;

namespace EssayOrganizer.App.Data;

public class EssayDbContext : DbContext
{
    public EssayDbContext(DbContextOptions<EssayDbContext> options) : base(options)
    {
    }

    public DbSet<Essay> Essays => Set<Essay>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var essay = modelBuilder.Entity<Essay>();
        essay.HasKey(x => x.Id);
        essay.Property(x => x.Title).HasMaxLength(200).IsRequired();
        essay.Property(x => x.Body).IsRequired();
        essay.Property(x => x.FormattedBody).IsRequired();
        essay.HasIndex(x => x.ProfileId).HasDatabaseName("IX_Essays_ProfileId");
        essay.HasIndex(x => x.UpdatedAt).HasDatabaseName("IX_Essays_UpdatedAt");
        essay.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_Essays_CreatedAt");

        var appSetting = modelBuilder.Entity<AppSetting>();
        appSetting.HasKey(x => x.Key);
        appSetting.Property(x => x.Key).HasMaxLength(100);
        appSetting.Property(x => x.Value).IsRequired();

        var profile = modelBuilder.Entity<Profile>();
        profile.HasKey(x => x.Id);
        profile.Property(x => x.Name).HasMaxLength(80).IsRequired();
        profile.Property(x => x.Avatar).HasMaxLength(500).IsRequired();
    }
}

