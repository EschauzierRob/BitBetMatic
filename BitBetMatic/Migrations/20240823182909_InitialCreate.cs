using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitBetMaticFunctions.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IndicatorThresholds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Strategy = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Market = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RsiOverbought = table.Column<int>(type: "int", nullable: false),
                    RsiOversold = table.Column<int>(type: "int", nullable: false),
                    MacdSignalLine = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AtrMultiplier = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SmaShortTerm = table.Column<int>(type: "int", nullable: false),
                    SmaLongTerm = table.Column<int>(type: "int", nullable: false),
                    ParabolicSarStep = table.Column<double>(type: "float", nullable: false),
                    ParabolicSarMax = table.Column<double>(type: "float", nullable: false),
                    BollingerBandsPeriod = table.Column<int>(type: "int", nullable: false),
                    BollingerBandsDeviation = table.Column<double>(type: "float", nullable: false),
                    AdxStrongTrend = table.Column<double>(type: "float", nullable: false),
                    StochasticOverbought = table.Column<double>(type: "float", nullable: true),
                    StochasticOversold = table.Column<double>(type: "float", nullable: true),
                    BuyThreshold = table.Column<int>(type: "int", nullable: false),
                    SellThreshold = table.Column<int>(type: "int", nullable: false),
                    RsiPeriod = table.Column<int>(type: "int", nullable: false),
                    AtrPeriod = table.Column<int>(type: "int", nullable: false),
                    StochasticPeriod = table.Column<int>(type: "int", nullable: false),
                    StochasticSignalPeriod = table.Column<int>(type: "int", nullable: false),
                    MacdFastPeriod = table.Column<int>(type: "int", nullable: false),
                    MacdSlowPeriod = table.Column<int>(type: "int", nullable: false),
                    MacdSignalPeriod = table.Column<int>(type: "int", nullable: false),
                    AdxPeriod = table.Column<int>(type: "int", nullable: false),
                    RocPeriod = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndicatorThresholds", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IndicatorThresholds_Strategy_Market_CreatedAt",
                table: "IndicatorThresholds",
                columns: new[] { "Strategy", "Market", "CreatedAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IndicatorThresholds");
        }
    }
}
