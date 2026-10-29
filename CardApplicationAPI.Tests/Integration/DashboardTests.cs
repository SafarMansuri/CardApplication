using CardApplicationAPI.Tests.Infrastructure;
using System.Net;

namespace CardApplicationAPI.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class DashboardTests : ApiTestBase
{
    [Fact]
    public async Task EmptyDashboardHasAllZeroTotalsAndEmptyAccounts()
    {
        var result = Result(await Send(HttpMethod.Get, "/api/Dashboard/summary")).AsObject();
        Assert.Equal(16, result.Count);
        foreach (var field in result.Where(x => x.Key != "accounts"))
            Assert.Equal(0m, field.Value!.GetValue<decimal>());
        Assert.Empty(result["accounts"]!.AsArray());
    }

    [Fact]
    public async Task MixedWorkflow_HasIndependentExactTotalsAndChartSeriesBeforeAndAfterSettlement()
    {
        var bank = await Create("/api/BankAccount", TestData.Account("Main HDFC"));
        await Create("/api/BankTransaction", TestData.Transaction(bank, "OPENING", 1000));
        await Create("/api/BankTransaction", TestData.Transaction(bank, "IN", 200));
        await Create("/api/BankTransaction", TestData.Transaction(bank, "OUT", 100));
        await Create("/api/CashLedger", TestData.Cash("OPENING", 500));
        await Create("/api/CashLedger", TestData.Cash("IN", 50));
        await Create("/api/CashLedger", TestData.Cash("OUT", 20));
        await Create("/api/CardInfo", TestData.Card(party: "Alpha Card Party", amount: 325, profit: 35));
        await Create("/api/CardInfo", TestData.Card(party: "Alpha Card Party", amount: 675, profit: 65));
        var stock = await Create("/api/StockItem", TestData.Stock());
        await Create("/api/StockItem", TestData.Stock("OUT", 950));
        var unavailable = TestData.Stock(amount: 200);
        unavailable.Status = "Sold";
        await Create("/api/StockItem", unavailable);
        var before = Result(await Send(HttpMethod.Get, "/api/Dashboard/summary"));
        var expected = new Dictionary<string, decimal>
        {
            ["totalBankBalance"] = 1100, ["totalAvailableCash"] = 530, ["totalSwipes"] = 2,
            ["totalPartyProfit"] = 100, ["totalStockItems"] = 3, ["totalPendingPartyPayment"] = 1000,
            ["availableStockCount"] = 1, ["availableStockValue"] = 700,
            ["pendingStockPayable"] = 900, ["pendingStockReceivable"] = 950,
            ["totalCapital"] = 1630, ["totalOwedToYou"] = 950, ["totalYouOwe"] = 1900,
            ["netPosition"] = -950, ["stockAvailablePct"] = 33.3m
        };
        foreach (var field in expected) Assert.Equal(field.Value, Money(before, field.Key));
        var accountSummary = Assert.Single(before["accounts"]!.AsArray())!;
        Assert.Equal(bank, accountSummary["accountId"]!.GetValue<int>());
        Assert.Equal("Main HDFC", accountSummary["accountName"]!.GetValue<string>());
        Assert.Equal(1100m, Money(accountSummary, "currentBalance"));

        var monthly = Result(await Send(HttpMethod.Get, "/api/Dashboard/monthly-flow?months=12")).AsArray();
        Assert.Equal(12, monthly.Count);
        var latestMonth = monthly[^1]!;
        Assert.Equal(200m, Money(latestMonth, "bankIn"));
        Assert.Equal(100m, Money(latestMonth, "bankOut"));
        Assert.Equal(50m, Money(latestMonth, "cashIn"));
        Assert.Equal(20m, Money(latestMonth, "cashOut"));

        var daily = Result(await Send(HttpMethod.Get, "/api/Dashboard/swipes-daily?days=7")).AsArray();
        Assert.Equal(7, daily.Count);
        var todayPoint = daily[^1]!;
        Assert.Equal(2, todayPoint["count"]!.GetValue<int>());
        Assert.Equal(1000m, Money(todayPoint, "amount"));
        Assert.Equal(100m, Money(todayPoint, "profit"));

        var topParties = Result(await Send(HttpMethod.Get, "/api/Dashboard/top-parties?limit=5")).AsArray();
        Assert.Equal(2, topParties.Count);
        Assert.Equal("Alpha Card Party", topParties[0]!["partyName"]!.GetValue<string>());
        Assert.Equal("CardParty", topParties[0]!["type"]!.GetValue<string>());
        Assert.Equal(1000m, Money(topParties[0]!, "pending"));
        Assert.Equal("Supplier", topParties[1]!["partyName"]!.GetValue<string>());
        Assert.Equal("Supplier", topParties[1]!["type"]!.GetValue<string>());
        Assert.Equal(900m, Money(topParties[1]!, "pending"));

        await Send(HttpMethod.Get, "/api/Dashboard/monthly-flow?months=0", status: HttpStatusCode.BadRequest);
        await Send(HttpMethod.Get, "/api/Dashboard/swipes-daily?days=0", status: HttpStatusCode.BadRequest);
        await Send(HttpMethod.Get, "/api/Dashboard/top-parties?limit=0", status: HttpStatusCode.BadRequest);

        var partyId = Items(await Send(HttpMethod.Get, "/api/PaymentInfo"))[0]!["id"]!.GetValue<int>();
        var stockPaymentId = Items(await Send(HttpMethod.Get, $"/api/StockPaymentInfo/stock/{stock}"))[0]!["id"]!.GetValue<int>();
        await Send(HttpMethod.Patch, $"/api/PaymentInfo/{partyId}/mark-paid");
        await Send(HttpMethod.Patch, $"/api/StockPaymentInfo/{stockPaymentId}/settle", TestData.Settlement(700));
        expected["totalPendingPartyPayment"] = 0;
        expected["pendingStockPayable"] = 200;
        expected["totalYouOwe"] = 200;
        expected["netPosition"] = 750;
        var after = Result(await Send(HttpMethod.Get, "/api/Dashboard/summary"));
        foreach (var field in expected) Assert.Equal(field.Value, Money(after, field.Key));

        var topAfter = Result(await Send(HttpMethod.Get, "/api/Dashboard/top-parties?limit=5")).AsArray();
        var remainingParty = Assert.Single(topAfter)!;
        Assert.Equal("Supplier", remainingParty["type"]!.GetValue<string>());
        Assert.Equal(200m, Money(remainingParty, "pending"));
    }
}
