using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TodoApi.Models;

namespace TodoApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    private static readonly ValueConverter<DateTime, DateTime> UtcConverter = new(
        v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> NullableUtcConverter = new(
        v => v.HasValue ? v.Value.ToUniversalTime() : v,
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

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
            // SQLite no guarda la zona horaria: al leer se marca la fecha como UTC para serializarla con "Z".
            e.Property(t => t.CreatedAt).HasConversion(UtcConverter);
            e.Property(t => t.UpdatedAt).HasConversion(UtcConverter);
            e.Property(t => t.DueDate).HasConversion(NullableUtcConverter);
            e.HasIndex(t => t.Status);
            e.HasIndex(t => t.Assignee);
        });
    }
}
