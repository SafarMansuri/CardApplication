using CardApplicationAPI.Data.DBContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardApplicationAPI.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261010160000_AddStockPaymentClassificationAndHistory")]
    public class AddStockPaymentClassificationAndHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(name: "CompletedAt", table: "StockPaymentInfo", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<int>(name: "CompletedByUserId", table: "StockPaymentInfo", type: "int", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CompletedByUsername", table: "StockPaymentInfo", type: "nvarchar(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "OutstandingAmount", table: "StockPaymentInfo", type: "decimal(18,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<string>(name: "PaymentStatus", table: "StockPaymentInfo", type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending");
            migrationBuilder.AddColumn<string>(name: "PaymentType", table: "StockPaymentInfo", type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Payable");

            migrationBuilder.Sql("""
                UPDATE payment
                SET payment.PaymentType = CASE WHEN UPPER(COALESCE(stock.[IN/OUT], '')) = 'OUT' THEN 'Receivable' ELSE 'Payable' END,
                    payment.PaymentStatus = 'Pending',
                    payment.OutstandingAmount = COALESCE(stock.[AMOUNT], 0),
                    payment.[PARTY NAME] = CASE WHEN UPPER(COALESCE(stock.[IN/OUT], '')) = 'OUT' THEN NULLIF(LTRIM(RTRIM(stock.[SOLD TO])), '') ELSE stock.[PARTY NAME] END,
                    payment.[ACCOUNT NAME] = CASE WHEN UPPER(COALESCE(stock.[IN/OUT], '')) = 'OUT' THEN NULLIF(LTRIM(RTRIM(stock.[SOLD TO])), '') ELSE stock.[PARTY NAME] END,
                    payment.UpdatedAt = SYSUTCDATETIME()
                FROM StockPaymentInfo payment
                INNER JOIN StockItems stock ON stock.Id = payment.StockItemId;
                """);

            migrationBuilder.CreateTable(
                name: "StockPaymentHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    StockPaymentInfoId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReferenceNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PerformedByUserId = table.Column<int>(type: "int", nullable: false),
                    PerformedByUsername = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockPaymentHistory", x => x.Id);
                    table.CheckConstraint("CK_StockPaymentHistory_EventType", "[EventType] IN ('Settlement', 'Reset')");
                    table.ForeignKey("FK_StockPaymentHistory_StockPaymentInfo_StockPaymentInfoId", x => x.StockPaymentInfoId, "StockPaymentInfo", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(name: "CK_StockPaymentInfo_OutstandingAmount", table: "StockPaymentInfo", sql: "[OutstandingAmount] >= 0");
            migrationBuilder.AddCheckConstraint(name: "CK_StockPaymentInfo_PaymentStatus", table: "StockPaymentInfo", sql: "[PaymentStatus] IN ('Pending', 'Completed')");
            migrationBuilder.AddCheckConstraint(name: "CK_StockPaymentInfo_PaymentType", table: "StockPaymentInfo", sql: "[PaymentType] IN ('Payable', 'Receivable')");
            migrationBuilder.CreateIndex(name: "IX_StockPaymentHistory_StockPaymentInfoId_CreatedAt", table: "StockPaymentHistory", columns: new[] { "StockPaymentInfoId", "CreatedAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "StockPaymentHistory");
            migrationBuilder.DropCheckConstraint(name: "CK_StockPaymentInfo_OutstandingAmount", table: "StockPaymentInfo");
            migrationBuilder.DropCheckConstraint(name: "CK_StockPaymentInfo_PaymentStatus", table: "StockPaymentInfo");
            migrationBuilder.DropCheckConstraint(name: "CK_StockPaymentInfo_PaymentType", table: "StockPaymentInfo");
            migrationBuilder.DropColumn(name: "CompletedAt", table: "StockPaymentInfo");
            migrationBuilder.DropColumn(name: "CompletedByUserId", table: "StockPaymentInfo");
            migrationBuilder.DropColumn(name: "CompletedByUsername", table: "StockPaymentInfo");
            migrationBuilder.DropColumn(name: "OutstandingAmount", table: "StockPaymentInfo");
            migrationBuilder.DropColumn(name: "PaymentStatus", table: "StockPaymentInfo");
            migrationBuilder.DropColumn(name: "PaymentType", table: "StockPaymentInfo");
        }
    }
}
