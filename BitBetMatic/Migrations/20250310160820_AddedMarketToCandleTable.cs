using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitBetMaticFunctions.Migrations
{
    public partial class AddedMarketToCandleTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Market",
                table: "Candles",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Market",
                table: "Candles");
        }
    }
}
