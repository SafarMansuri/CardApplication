using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardApplicationAPI.Migrations
{
    public partial class AddPartyPaymentSettlement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastPaidAt",
                table: "PaymentInfo",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastPaidByUserId",
                table: "PaymentInfo",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastPaidByUsername",
                table: "PaymentInfo",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "PaymentInfo",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "PaymentInfo",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<decimal>(
                name: "PendingAmount",
                table: "PaymentInfo",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsSettled",
                table: "CardInfo",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SettledAt",
                table: "CardInfo",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE payment
                SET payment.SwipeCount = totals.SwipeCount,
                    payment.TotalAmount = totals.TotalAmount,
                    payment.TotalProfit = totals.TotalProfit,
                    payment.PaidAmount = 0,
                    payment.PendingAmount = totals.TotalAmount,
                    payment.PaymentStatus = 'Pending',
                    payment.Updatedat = SYSUTCDATETIME()
                FROM PaymentInfo payment
                INNER JOIN
                (
                    SELECT PartyName,
                           COUNT(*) AS SwipeCount,
                           SUM(COALESCE(Deduction, 0)) AS TotalAmount,
                           SUM(COALESCE(Profit, 0)) AS TotalProfit
                    FROM CardInfo
                    GROUP BY PartyName
                ) totals ON totals.PartyName = payment.PartyName;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_PaymentInfo_Amounts",
                table: "PaymentInfo",
                sql: "[PaidAmount] >= 0 AND [PendingAmount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PaymentInfo_PaymentStatus",
                table: "PaymentInfo",
                sql: "[PaymentStatus] IN ('Pending', 'Paid')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PaymentInfo_Amounts",
                table: "PaymentInfo");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PaymentInfo_PaymentStatus",
                table: "PaymentInfo");

            migrationBuilder.DropColumn(name: "LastPaidAt", table: "PaymentInfo");
            migrationBuilder.DropColumn(name: "LastPaidByUserId", table: "PaymentInfo");
            migrationBuilder.DropColumn(name: "LastPaidByUsername", table: "PaymentInfo");
            migrationBuilder.DropColumn(name: "PaidAmount", table: "PaymentInfo");
            migrationBuilder.DropColumn(name: "PaymentStatus", table: "PaymentInfo");
            migrationBuilder.DropColumn(name: "PendingAmount", table: "PaymentInfo");
            migrationBuilder.DropColumn(name: "IsSettled", table: "CardInfo");
            migrationBuilder.DropColumn(name: "SettledAt", table: "CardInfo");
        }
    }
}
