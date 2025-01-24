using Microsoft.EntityFrameworkCore;

namespace CurrencyService.CurrencyModels;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) 
        : base(options)
    {
    }
    public DbSet<ExchangeRates> ExchangeRates { get; set; }
    public DbSet<CurrencyNames> CurrencyNames { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Konfiguracja autoinkrementacji dla `CurrencyNames.Id`
        modelBuilder.Entity<CurrencyNames>()
            .Property(c => c.Id)
            .ValueGeneratedOnAdd(); // Klucz autoinkrementowany

        // Konfiguracja relacji i złożonego klucza w `ExchangeRates`
        modelBuilder.Entity<ExchangeRates>(entity =>
        {
            // Definiowanie złożonego klucza podstawowego
            entity.HasKey(e => e.DateTime); // Klucz główny

            // Relacja: ExchangeRates.Id → CurrencyNames.Id
            entity.HasOne<CurrencyNames>()
                .WithMany()
                .HasForeignKey(e => e.Id)
                .OnDelete(DeleteBehavior.Cascade); // Usuwanie w kaskadzie dla Id
            
            // Relacja: ExchangeRates.Id2 → CurrencyNames.Id
            entity.HasOne<CurrencyNames>()
                .WithMany()
                .HasForeignKey(e => e.Id2)
                .OnDelete(DeleteBehavior.Cascade); // Usuwanie w kaskadzie dla Id2
        });

        // Konfiguracja precyzji dla ExchangeRate
        modelBuilder.Entity<ExchangeRates>()
            .Property(e => e.ExchangeRate)
            .HasPrecision(12, 6);

        base.OnModelCreating(modelBuilder);
    }
}