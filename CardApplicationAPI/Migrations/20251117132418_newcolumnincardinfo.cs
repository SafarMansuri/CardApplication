using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardApplicationAPI.Migrations
{
    /// <inheritdoc />
    public partial class newcolumnincardinfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "CardInfo",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankName",
                table: "CardInfo");
        }
    }
}
