using Microsoft.EntityFrameworkCore;

namespace CoinGeckoDockerService
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        
        // DbSet dla tabel w bazie danych
        public DbSet<Crypto> Cryptos { get; set; }
        public DbSet<CryptoNames> CryptoNames { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Konfiguracja dla tabeli Crypto
            modelBuilder.Entity<Crypto>(entity =>
            {
                entity.HasKey(e => e.DateTime); // Klucz główny ustawiony na DateTime
                
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
                    .HasForeignKey(e => e.Crypto_Id) // Mapowanie przez Crypto_Id
                    .HasPrincipalKey(e => e.Id) // Mapowanie do Id w CryptoNames
                    .HasConstraintName("FK_Crypto_CryptoNames");
            });

            // Konfiguracja dla tabeli CryptoNames
            modelBuilder.Entity<CryptoNames>(entity =>
            {
                entity.HasKey(e => e.Id); // Klucz główny na Id
                
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
        }
    }
}
