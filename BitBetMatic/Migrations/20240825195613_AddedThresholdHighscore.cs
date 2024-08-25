using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitBetMaticFunctions.Migrations
{
    public partial class AddedThresholdHighscore : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Highscore",
                table: "IndicatorThresholds",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Highscore",
                table: "IndicatorThresholds");
        }
    }
}
