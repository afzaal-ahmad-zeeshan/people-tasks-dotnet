using Microsoft.EntityFrameworkCore;
using hello_dotnet.Models;

namespace hello_dotnet.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Person> People => Set<Person>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Person>()
            .Property(p => p.Hobbies)
            .HasConversion(
                v => string.Join("|||", v),
                v => v.Split("|||", StringSplitOptions.RemoveEmptyEntries).ToList());

        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.Person)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.PersonId);

        modelBuilder.Entity<TaskItem>()
            .Property(t => t.Status)
            .HasConversion<string>();
    }
}
