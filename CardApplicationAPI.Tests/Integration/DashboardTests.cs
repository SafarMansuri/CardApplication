using CardApplicationAPI.Tests.Infrastructure;

namespace CardApplicationAPI.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class DashboardTests : ApiTestBase
{
    [Fact]
    public async Task EmptyDashboardHasAllTenZeroTotals()
    {
        var result = Result(await Send(HttpMethod.Get, "/api/Dashboard/summary")).AsObject();
        Assert.Equal(10, result.Count);
        foreach (var field in result) Assert.Equal(0m, field.Value!.GetValue<decimal>());
    }

    [Fact]
    public async Task MixedWorkflow_HasIndependentExactTotalsBeforeAndAfterSettlement()
    {
        var bank = await Create("/api/BankAccount", TestData.Account());
        await Create("/api/BankTransaction", TestData.Transaction(bank, "OPENING", 1000));
        await Create("/api/BankTransaction", TestData.Transaction(bank, "OUT", 100));
        await Create("/api/CashLedger", TestData.Cash("OPENING", 500));
        await Create("/api/CashLedger", TestData.Cash("IN", 50));
        await Create("/api/CashLedger", TestData.Cash("OUT", 20));
        await Create("/api/CardInfo", TestData.Card(amount: 325, profit: 35));
        await Create("/api/CardInfo", TestData.Card(amount: 675, profit: 65));
        var stock = await Create("/api/StockItem", TestData.Stock());
        await Create("/api/StockItem", TestData.Stock("OUT", 950));
        var unavailable = TestData.Stock(amount: 200);
        unavailable.Status = "Sold";
        await Create("/api/StockItem", unavailable);
        var before = Result(await Send(HttpMethod.Get, "/api/Dashboard/summary"));
        var expected = new Dictionary<string, decimal>
        {
            ["totalBankBalance"] = 900, ["totalAvailableCash"] = 530, ["totalSwipes"] = 2,
            ["totalPartyProfit"] = 100, ["totalStockItems"] = 2, ["totalPendingPartyPayment"] = 1000,
            ["availableStockCount"] = 1, ["availableStockValue"] = 700,
            ["pendingStockPayable"] = 900, ["pendingStockReceivable"] = 950
        };
        foreach (var field in expected) Assert.Equal(field.Value, Money(before, field.Key));
        var partyId = Items(await Send(HttpMethod.Get, "/api/PaymentInfo"))[0]!["id"]!.GetValue<int>();
        var stockPaymentId = Items(await Send(HttpMethod.Get, $"/api/StockPaymentInfo/stock/{stock}"))[0]!["id"]!.GetValue<int>();
        await Send(HttpMethod.Patch, $"/api/PaymentInfo/{partyId}/mark-paid");
        await Send(HttpMethod.Patch, $"/api/StockPaymentInfo/{stockPaymentId}/settle", TestData.Settlement(700));
        expected["totalPendingPartyPayment"] = 0;
        expected["pendingStockPayable"] = 200;
        var after = Result(await Send(HttpMethod.Get, "/api/Dashboard/summary"));
        foreach (var field in expected) Assert.Equal(field.Value, Money(after, field.Key));
    }
}
