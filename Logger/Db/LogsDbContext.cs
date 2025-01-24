using Logger.Db;
using Microsoft.EntityFrameworkCore;

public class LogsDbContext : DbContext
{
    public DbSet<LogEntry> Logs { get; set; }

    public LogsDbContext(DbContextOptions<LogsDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LogEntry>(entity =>
        {
            entity.ToTable("Logs");
            
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        base.OnModelCreating(modelBuilder);
    }
}