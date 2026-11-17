using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

namespace TodoApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>(e =>
        {
            e.ToTable("Tasks");
            e.HasKey(t => t.Id);
            e.Property(t => t.Title).IsRequired().HasMaxLength(200);
            e.Property(t => t.Description).HasMaxLength(2000);
            e.Property(t => t.Assignee).IsRequired().HasMaxLength(100);
            e.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(t => t.Status);
            e.HasIndex(t => t.Assignee);
        });
    }
}
