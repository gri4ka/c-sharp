using Microsoft.EntityFrameworkCore;
using tianzi.Models;

namespace tianzi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<SharedFile> SharedFiles => Set<SharedFile>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SharedFile>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<SharedFile>()
            .HasIndex(x => x.DeleteToken)
            .IsUnique();

        modelBuilder.Entity<AdminUser>()
            .HasIndex(x => x.Username)
            .IsUnique();
    }
}
