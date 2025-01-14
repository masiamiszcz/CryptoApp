using Microsoft.EntityFrameworkCore;

namespace CryptoDbDockerService.AppDb;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Crypto> Cryptos { get; set; }
    public DbSet<CryptoNames> CryptoNames { get; set; }
    public DbSet<ExchangeRates> ExchangeRates { get; set; }
    public DbSet<CurrencyNames> CurrencyNames { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Konfiguracja dla tabeli Crypto
        modelBuilder.Entity<Crypto>(entity =>
        {
            entity.HasKey(e => e.DateTime); // Klucz główny na DateTime
            
            entity.Property(e => e.CryptoPrice)
                .HasColumnType("money");

            entity.Property(e => e.High24)
                .HasColumnType("money");

            entity.Property(e => e.Low24)
                .HasColumnType("money");

            entity.Property(e => e.PriceChange)
                .HasPrecision(6, 2);

            // Relacja Crypto.Crypto_Id -> CryptoNames.Id
            entity.HasOne<CryptoNames>()
                .WithMany()
                .HasForeignKey(e => e.Crypto_Id)
                .HasConstraintName("FK_Crypto_CryptoNames");
        });

        // Konfiguracja dla tabeli CryptoNames
        modelBuilder.Entity<CryptoNames>(entity =>
        {
            entity.HasKey(e => e.Id); // Klucz główny
            
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd(); // Automatyczne inkrementowanie

            entity.Property(e => e.CryptoName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Symbol)
                .IsRequired()
                .HasMaxLength(10);
            
            entity.Property(e => e.Image)
                .IsRequired();
        });

        // Konfiguracja dla tabeli ExchangeRates
        modelBuilder.Entity<ExchangeRates>(entity =>
        {
            entity.HasKey(e => e.Id); // Klucz główny
            
            entity.Property(e => e.ExchangeRate)
                .IsRequired()
                .HasMaxLength(20);
            

            // Relacja ExchangeRates.Id -> CurrencyNames.Id
            entity.HasOne<CurrencyNames>()
                .WithMany()
                .HasForeignKey(e => e.Id)
                .HasConstraintName("FK_ExchangeRates_Id_CurrencyNames_Id")
                .OnDelete(DeleteBehavior.NoAction);

            // Relacja ExchangeRates.Id2 -> CurrencyNames.Id
            entity.HasOne<CurrencyNames>()
                .WithMany()
                .HasForeignKey(e => e.Id2)
                .HasConstraintName("FK_ExchangeRates_Id2_CurrencyNames_Id2")
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Konfiguracja dla tabeli CurrencyNames
        modelBuilder.Entity<CurrencyNames>(entity =>
        {
            entity.HasKey(e => e.Id); // Klucz główny

            entity.Property(e => e.CurrencyName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Symbol)
                .IsRequired()
                .HasMaxLength(10);
        });
    }
}