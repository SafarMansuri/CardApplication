using CardApplicationAPI.Data.DBContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardApplicationAPI.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260928120000_AddPartyPaymentSummaries")]
    public partial class AddPartyPaymentSummaries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IMEI", table: "CardInfo", type: "nvarchar(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<decimal>(
                name: "Profit", table: "CardInfo", type: "decimal(18,2)", nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "Model", table: "CardInfo", type: "nvarchar(200)", maxLength: 200, nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "Merchant", table: "CardInfo", type: "nvarchar(200)", maxLength: 200, nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "SwipePerson", table: "CardInfo", type: "nvarchar(150)", maxLength: 150, nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CardName", table: "CardInfo", type: "nvarchar(100)", maxLength: 100, nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(
                name: "CardNumber", table: "CardInfo", type: "nvarchar(100)", maxLength: 100, nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(max)");

            migrationBuilder.DropTable(name: "PaymentInfo");

            migrationBuilder.CreateTable(
                name: "PaymentInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PartyName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SwipeCount = table.Column<int>(type: "int", nullable: false),
                    SwipeAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Profit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalProfit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    LimitUsed = table.Column<bool>(type: "bit", nullable: false),
                    CardName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Merchant = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SwipePerson = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Createdat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Updatedat = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_PaymentInfo", x => x.Id));

            migrationBuilder.Sql("""
                WITH PartyRows AS
                (
                    SELECT *,
                        COUNT(*) OVER (PARTITION BY PartyName) AS PartySwipeCount,
                        ROW_NUMBER() OVER (PARTITION BY PartyName ORDER BY [Date] DESC, Id DESC) AS RowNumber
                    FROM CardInfo
                )
                INSERT INTO PaymentInfo
                    ([Date], PartyName, SwipeCount, SwipeAmount, TotalAmount, Profit, TotalProfit,
                     LimitUsed, CardName, Merchant, SwipePerson, Remarks, Createdat, Updatedat)
                SELECT [Date], PartyName, PartySwipeCount, Deduction, Deduction * PartySwipeCount,
                       Profit, Profit * PartySwipeCount, limitUsed, CardName, Merchant, SwipePerson,
                       Remarks, Createdat, SYSUTCDATETIME()
                FROM PartyRows
                WHERE RowNumber = 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentInfo_PartyName", table: "PaymentInfo", column: "PartyName", unique: true);
            migrationBuilder.CreateIndex(
                name: "IX_Users_Username", table: "Users", column: "Username", unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_PaymentInfo_PartyName", table: "PaymentInfo");
            migrationBuilder.DropIndex(name: "IX_Users_Username", table: "Users");
            migrationBuilder.DropTable(name: "PaymentInfo");

            migrationBuilder.CreateTable(
                name: "PaymentInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Card = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SwipeCount = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreditAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Profit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OutletName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Createdat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Updatedat = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_PaymentInfo", x => x.Id));

            migrationBuilder.DropColumn(name: "IMEI", table: "CardInfo");
            migrationBuilder.DropColumn(name: "Profit", table: "CardInfo");
            migrationBuilder.DropColumn(name: "Model", table: "CardInfo");
            migrationBuilder.DropColumn(name: "Merchant", table: "CardInfo");
            migrationBuilder.DropColumn(name: "SwipePerson", table: "CardInfo");
            migrationBuilder.AlterColumn<string>(
                name: "CardName", table: "CardInfo", type: "nvarchar(max)", nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(100)", oldMaxLength: 100);
            migrationBuilder.AlterColumn<string>(
                name: "CardNumber", table: "CardInfo", type: "nvarchar(max)", nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(100)", oldMaxLength: 100);
        }
    }
}