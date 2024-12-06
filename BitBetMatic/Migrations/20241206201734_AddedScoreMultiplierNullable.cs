using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitBetMaticFunctions.Migrations
{
    public partial class AddedScoreMultiplierNullable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "ScoreMultiplier",
                table: "IndicatorThresholds",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "ScoreMultiplier",
                table: "IndicatorThresholds",
                type: "float",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);
        }
    }
}
