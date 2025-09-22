using Microsoft.EntityFrameworkCore;
namespace Server.Models;

public class ServerDbContext : DbContext
{
    public DbSet<File> Files { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase("Repload");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var fileEntity = modelBuilder.Entity<File>();
        fileEntity.HasKey(f => f.Uuid);
        fileEntity.HasIndex(f => f.Uuid);
        fileEntity.Property(f => f.Name)
            .HasMaxLength(255);
    }
}