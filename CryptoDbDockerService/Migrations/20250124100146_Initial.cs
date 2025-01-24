using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoDbDockerService.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CryptoNames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CryptoName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Image = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CryptoNames", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CurrencyNames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CurrencyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Symbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyNames", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cryptos",
                columns: table => new
                {
                    DateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    High24 = table.Column<decimal>(type: "money", nullable: false),
                    Low24 = table.Column<decimal>(type: "money", nullable: false),
                    Crypto_Id = table.Column<int>(type: "int", nullable: false),
                    CryptoPrice = table.Column<decimal>(type: "money", nullable: false),
                    PriceChange = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cryptos", x => x.DateTime);
                    table.ForeignKey(
                        name: "FK_Crypto_CryptoNames",
                        column: x => x.Crypto_Id,
                        principalTable: "CryptoNames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                columns: table => new
                {
                    DateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false),
                    Id2 = table.Column<int>(type: "int", nullable: false),
                    ApiId = table.Column<int>(type: "int", nullable: false),
                    ExchangeRate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.DateTime);
                    table.ForeignKey(
                        name: "FK_ExchangeRates_Id2_CurrencyNames_Id2",
                        column: x => x.Id2,
                        principalTable: "CurrencyNames",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExchangeRates_Id_CurrencyNames_Id",
                        column: x => x.Id,
                        principalTable: "CurrencyNames",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cryptos_Crypto_Id",
                table: "Cryptos",
                column: "Crypto_Id");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_Id",
                table: "ExchangeRates",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_Id2",
                table: "ExchangeRates",
                column: "Id2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cryptos");

            migrationBuilder.DropTable(
                name: "ExchangeRates");

            migrationBuilder.DropTable(
                name: "CryptoNames");

            migrationBuilder.DropTable(
                name: "CurrencyNames");
        }
    }
}
