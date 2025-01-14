using Microsoft.EntityFrameworkCore;

namespace WebAppi.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // DbSety dla tabel
    public DbSet<CryptoModel> Cryptos { get; set; }
    public DbSet<CryptoNamesModel> CryptoNames { get; set; }
    public DbSet<ExchangeRates> ExchangeRates { get; set; }
    public DbSet<CurrencyNames> CurrencyNames { get; set; }

    // Konfiguracja modeli
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Konfiguracja dla tabeli Cryptos
        modelBuilder.Entity<CryptoModel>(entity =>
        {
            entity.HasKey(e => e.DateTime); // Klucz główny

            entity.Property(e => e.CryptoPrice)
                .HasColumnType("money"); // Typ money w SQL Server

            entity.Property(e => e.High24)
                .HasColumnType("money"); // Typ money w SQL Server

            entity.Property(e => e.Low24)
                .HasColumnType("money"); // Typ money w SQL Server

            entity.Property(e => e.PriceChange)
                .HasPrecision(6, 2); // Precyzja dla zmiany ceny

            // Relacja Crypto.Crypto_Id -> CryptoNames.Id
            entity.HasOne(e => e.CryptoNameNavigation)
                .WithMany()
                .HasForeignKey(e => e.Crypto_Id)
                .HasConstraintName("FK_Crypto_CryptoNames");
        });

        // Konfiguracja dla tabeli CryptoNames
        modelBuilder.Entity<CryptoNamesModel>(entity =>
        {
            entity.HasKey(e => e.Id); // Klucz główny

            entity.Property(e => e.CryptoName)
                .IsRequired()
                .HasMaxLength(100); // Maksymalna długość nazwy kryptowaluty

            entity.Property(e => e.Symbol)
                .IsRequired()
                .HasMaxLength(10); // Maksymalna długość symbolu kryptowaluty (np. BTC, ETH)
             entity.Property(e => e.Image)
                .IsRequired();
        });

        // Konfiguracja dla tabeli ExchangeRates
        modelBuilder.Entity<ExchangeRates>(entity =>
        {
            entity.HasKey(e => e.Id); // Klucz główny

            entity.Property(e => e.ExchangeRate)
                .IsRequired()
                .HasMaxLength(20); // Maksymalna długość ciągu kursu wymiany

            // Relacja ExchangeRates.Id -> CurrencyNames.Id
            entity.HasOne<CurrencyNames>()
                .WithMany()
                .HasForeignKey(e => e.Id)
                .HasConstraintName("FK_ExchangeRates_Id_CurrencyNames_Id");

            // Relacja ExchangeRates.Id2 -> CurrencyNames.Id
            entity.HasOne<CurrencyNames>()
                .WithMany()
                .HasForeignKey(e => e.Id2)
                .HasConstraintName("FK_ExchangeRates_Id2_CurrencyNames_Id2");
        });

        // Konfiguracja dla tabeli CurrencyNames
        modelBuilder.Entity<CurrencyNames>(entity =>
        {
            entity.HasKey(e => e.Id); // Klucz główny

            entity.Property(e => e.CurrencyName)
                .IsRequired()
                .HasMaxLength(100); // Maksymalna długość nazwy (np. United States Dollar)

            entity.Property(e => e.Symbol)
                .IsRequired()
                .HasMaxLength(10); // Maksymalna długość symbolu (np. USD, EUR)
        });
    }
}