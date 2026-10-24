using CardApplicationAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace CardApplicationAPI.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class ConcurrencyTests : ApiTestBase
{
    [Fact]
    public async Task ConcurrentRefresh_CanRotateAnOldTokenOnlyOnce()
    {
        var login = await Send(HttpMethod.Post, "/api/Auth/login", new { username = "user", password = Password }, client: Anonymous);
        var token = login!["refreshToken"]!.GetValue<string>();
        var statuses = await Together(
            () => Anonymous.PostAsJsonAsync("/api/Auth/refresh", new { refreshToken = token }),
            () => Anonymous.PostAsJsonAsync("/api/Auth/refresh", new { refreshToken = token }));
        Assert.Equal(new[] { HttpStatusCode.OK, HttpStatusCode.Unauthorized }, statuses.OrderBy(x => x));
        await using var db = Database.CreateContext();
        var original = await db.RefreshTokens.SingleAsync(x => x.Token == token);
        Assert.True(original.IsRevoked);
        Assert.Equal(1, await db.RefreshTokens.CountAsync(x => x.UserId == original.UserId && x.RevokedAt == null));
    }

    private async Task<HttpStatusCode[]> Together(params Func<Task<HttpResponseMessage>>[] operations)
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = operations.Select(async operation =>
        {
            await gate.Task;
            using var response = await operation();
            Assert.True((int)response.StatusCode < 500, $"Unexpected server error: {Factory.Errors.Details}");
            return response.StatusCode;
        }).ToArray();
        gate.SetResult();
        return await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(45));
    }

    [Fact]
    public async Task DuplicateStockSettlement_OnlyOneSucceedsAndOneAuditIsWritten()
    {
        var id = await Create("/api/StockItem", TestData.Stock());
        var payment = Items(await Send(HttpMethod.Get, $"/api/StockPaymentInfo/stock/{id}"))[0]!["id"]!.GetValue<int>();
        var path = $"/api/StockPaymentInfo/{payment}/settle";
        var statuses = await Together(
            () => Client.PatchAsJsonAsync(path, TestData.Settlement(700)),
            () => Client.PatchAsJsonAsync(path, TestData.Settlement(700)));
        Assert.Equal(new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }, statuses.OrderBy(x => x));
        await using var db = Database.CreateContext();
        Assert.Single(await db.StockPaymentHistory.ToListAsync());
        Assert.Equal(0m, (await db.StockPaymentInfo.SingleAsync()).OutstandingAmount);
    }

    [Fact]
    public async Task DuplicatePartySettlement_OnlyOneSucceeds()
    {
        await Create("/api/CardInfo", TestData.Card());
        var payment = Items(await Send(HttpMethod.Get, "/api/PaymentInfo"))[0]!["id"]!.GetValue<int>();
        var path = $"/api/PaymentInfo/{payment}/mark-paid";
        var statuses = await Together(() => Client.PatchAsync(path, null), () => Client.PatchAsync(path, null));
        Assert.Equal(new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }, statuses.OrderBy(x => x));
        await using var db = Database.CreateContext();
        Assert.True((await db.CardInfo.SingleAsync()).IsSettled);
        Assert.Equal(100m, (await db.PaymentInfo.SingleAsync()).PaidAmount);
    }

    [Fact]
    public async Task CompetingTransfers_CannotOverspendOrLeaveHalfAPair()
    {
        var source = await Create("/api/BankAccount", TestData.Account("Source"));
        var destination = await Create("/api/BankAccount", TestData.Account("Destination"));
        await Create("/api/BankTransaction", TestData.Transaction(source));
        var body = new { fromAccountId = source, toAccountId = destination, amount = 700m, transactionDate = DateTime.UtcNow };
        var statuses = await Together(
            () => Client.PostAsJsonAsync("/api/BankTransaction/transfer", body),
            () => Client.PostAsJsonAsync("/api/BankTransaction/transfer", body));
        Assert.Equal(new[] { HttpStatusCode.OK, HttpStatusCode.BadRequest }, statuses.OrderBy(x => x));
        await using var db = Database.CreateContext();
        Assert.Equal(3, await db.BankTransactions.CountAsync());
        Assert.Equal(1000m, Result(await Send(HttpMethod.Get, "/api/BankAccount/summary/total")).GetValue<decimal>());
    }

    [Fact]
    public async Task FinancialEditRacingSettlement_CannotChangeACompletedObligation()
    {
        var stock = TestData.Stock();
        stock.Id = await Create("/api/StockItem", stock);
        var payment = Items(await Send(HttpMethod.Get, $"/api/StockPaymentInfo/stock/{stock.Id}"))[0]!["id"]!.GetValue<int>();
        stock.Amount = 800;
        var statuses = await Together(
            () => Client.PutAsJsonAsync($"/api/StockItem/{stock.Id}", stock),
            () => Client.PatchAsJsonAsync($"/api/StockPaymentInfo/{payment}/settle", TestData.Settlement(700)));
        Assert.True(
            statuses.SequenceEqual(new[] { HttpStatusCode.OK, HttpStatusCode.BadRequest }) ||
            statuses.SequenceEqual(new[] { HttpStatusCode.Conflict, HttpStatusCode.OK }),
            $"Unexpected concurrent outcomes: {string.Join(", ", statuses)}");
        await using var db = Database.CreateContext();
        var source = await db.StockItems.SingleAsync();
        var obligation = await db.StockPaymentInfo.SingleAsync();
        Assert.Equal(source.Amount, obligation.Amount);
        if (obligation.PaymentStatus == "Completed")
        {
            Assert.Equal(700m, source.Amount);
            Assert.Equal(700m, (await db.StockPaymentHistory.SingleAsync()).Amount);
            Assert.Equal(0m, obligation.OutstandingAmount);
        }
        else
        {
            Assert.Equal(800m, obligation.OutstandingAmount);
            Assert.Empty(await db.StockPaymentHistory.ToListAsync());
        }
    }
}
