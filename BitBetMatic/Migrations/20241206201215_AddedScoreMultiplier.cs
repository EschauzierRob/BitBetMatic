using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitBetMaticFunctions.Migrations
{
    public partial class AddedScoreMultiplier : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ScoreMultiplier",
                table: "IndicatorThresholds",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScoreMultiplier",
                table: "IndicatorThresholds");
        }
    }
}
