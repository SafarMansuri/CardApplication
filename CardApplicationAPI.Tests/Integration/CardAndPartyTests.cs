using CardApplicationAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace CardApplicationAPI.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class CardAndPartyTests : ApiTestBase
{
    [Fact]
    public async Task UnequalSwipes_Settle_Reopen_AndProtectSettledRows()
    {
        var first = TestData.Card(amount: 325m, profit: 35m);
        first.Id = await Create("/api/CardInfo", first);
        var second = TestData.Card(amount: 675m, profit: 65m);
        second.Id = await Create("/api/CardInfo", second);
        Assert.Equal(2, Items(await Send(HttpMethod.Get, "/api/CardInfo")).Count);
        var summary = Items(await Send(HttpMethod.Get, "/api/PaymentInfo?partyName=Test%20Party&status=pending"))[0]!;
        var summaryId = summary["id"]!.GetValue<int>();
        Assert.Equal(1000m, Money(summary, "totalAmount"));
        Assert.Equal(100m, Money(summary, "totalProfit"));
        Assert.Equal(2, summary["swipeCount"]!.GetValue<int>());
        var before = DateTime.UtcNow.AddSeconds(-1);
        await Send(HttpMethod.Patch, $"/api/PaymentInfo/{summaryId}/mark-paid");
        var paid = Result(await Send(HttpMethod.Get, $"/api/CardInfo/{first.Id}"));
        Assert.True(paid["isSettled"]!.GetValue<bool>());
        Assert.InRange(DateTime.Parse(paid["settledAt"]!.GetValue<string>(), null,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal), before, DateTime.UtcNow.AddSeconds(1));
        await Send(HttpMethod.Put, $"/api/CardInfo/{first.Id}", first, HttpStatusCode.Conflict);
        await Send(HttpMethod.Delete, $"/api/CardInfo/{first.Id}", status: HttpStatusCode.Conflict);
        await Send(HttpMethod.Patch, $"/api/PaymentInfo/{summaryId}/mark-paid", status: HttpStatusCode.Conflict);
        var newSwipe = TestData.Card(amount: 225m, profit: 25m);
        await Create("/api/CardInfo", newSwipe);
        summary = Items(await Send(HttpMethod.Get, "/api/PaymentInfo?status=Pending"))[0]!;
        Assert.Equal(summaryId, summary["id"]!.GetValue<int>());
        Assert.Equal(1225m, Money(summary, "totalAmount"));
        Assert.Equal(1000m, Money(summary, "paidAmount"));
        Assert.Equal(225m, Money(summary, "pendingAmount"));
        Assert.Equal("admin", summary["lastPaidByUsername"]!.GetValue<string>());
        using var master = await AsRole("Master");
        await Send(HttpMethod.Patch, $"/api/PaymentInfo/{summaryId}/mark-paid", client: master);
        summary = Items(await Send(HttpMethod.Get, "/api/PaymentInfo?status=Paid"))[0]!;
        Assert.Equal(1225m, Money(summary, "paidAmount"));
        Assert.Equal(0m, Money(summary, "pendingAmount"));
        Assert.Equal("master", summary["lastPaidByUsername"]!.GetValue<string>());
        await using var db = Database.CreateContext();
        Assert.Equal(3, await db.CardInfo.CountAsync(x => x.IsSettled));
        Assert.Empty(await db.BankTransactions.ToListAsync());
        Assert.Empty(await db.CashLedger.ToListAsync());
    }

    [Fact]
    public async Task PendingCrud_ResynchronizesBothParties_AndIgnoresClientSettlement()
    {
        var card = TestData.Card("Original");
        card.IsSettled = true;
        card.SettledAt = DateTime.UtcNow;
        card.Id = await Create("/api/CardInfo", card);
        Assert.False(Result(await Send(HttpMethod.Get, $"/api/CardInfo/{card.Id}"))["isSettled"]!.GetValue<bool>());
        card.PartyName = " Changed ";
        card.Deduction = 250.25m;
        await Send(HttpMethod.Put, $"/api/CardInfo/{card.Id}", card);
        Assert.Empty(Items(await Send(HttpMethod.Get, "/api/PaymentInfo?partyName=Original")));
        var summary = Items(await Send(HttpMethod.Get, "/api/PaymentInfo?partyName=Changed"))[0]!;
        Assert.Equal(250.25m, Money(summary, "pendingAmount"));
        await Send(HttpMethod.Delete, $"/api/CardInfo/{card.Id}");
        Assert.Empty(Items(await Send(HttpMethod.Get, "/api/PaymentInfo")));
        await Send(HttpMethod.Get, $"/api/CardInfo/{card.Id}", status: HttpStatusCode.NotFound);
        await Send(HttpMethod.Delete, $"/api/CardInfo/{card.Id}", status: HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task InvalidRequests_DoNotCreateRows()
    {
        var card = TestData.Card();
        card.BankName = null;
        await Send(HttpMethod.Post, "/api/CardInfo", card, HttpStatusCode.BadRequest);
        card.BankName = new string('b', 51);
        await Send(HttpMethod.Post, "/api/CardInfo", card, HttpStatusCode.BadRequest);
        card.BankName = "Bank";
        card.Deduction = -1;
        await Send(HttpMethod.Post, "/api/CardInfo", card, HttpStatusCode.BadRequest);
        card.Deduction = 10;
        await Send(HttpMethod.Put, "/api/CardInfo/999", card, HttpStatusCode.BadRequest);
        card.Id = 999;
        await Send(HttpMethod.Put, "/api/CardInfo/999", card, HttpStatusCode.NotFound);
        await Send(HttpMethod.Get, "/api/PaymentInfo?status=Processing", status: HttpStatusCode.BadRequest);
        await Send(HttpMethod.Patch, "/api/PaymentInfo/999/mark-paid", status: HttpStatusCode.NotFound);
        Assert.Empty(Items(await Send(HttpMethod.Get, "/api/CardInfo")));
    }

    [Fact]
    public async Task ZeroAndNullAmounts_PreserveCurrentSummarySemantics()
    {
        var card = TestData.Card(amount: 0, profit: 0);
        card.Deduction = null;
        card.Profit = null;
        card.Id = await Create("/api/CardInfo", card);
        var summary = Items(await Send(HttpMethod.Get, "/api/PaymentInfo"))[0]!;
        Assert.Equal(0m, Money(summary, "totalAmount"));
        Assert.Equal("Paid", summary["paymentStatus"]!.GetValue<string>());
        Assert.False(Result(await Send(HttpMethod.Get, $"/api/CardInfo/{card.Id}"))["isSettled"]!.GetValue<bool>());
    }
}
