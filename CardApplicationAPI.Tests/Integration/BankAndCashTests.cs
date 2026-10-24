using CardApplicationAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace CardApplicationAPI.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class BankAndCashTests : ApiTestBase
{
    [Fact]
    public async Task CashCrudAndSummaries_RecalculateFromRows()
    {
        await Create("/api/CashLedger", TestData.Cash("OPENING", 1000));
        var cash = TestData.Cash("IN", 250.25m);
        cash.Id = await Create("/api/CashLedger", cash);
        var outgoing = await Create("/api/CashLedger", TestData.Cash("OUT", 50));
        Assert.Equal(3, Items(await Send(HttpMethod.Get, "/api/CashLedger")).Count);
        Assert.Single(Items(await Send(HttpMethod.Get, "/api/CashLedger/type/IN")));
        Assert.Equal(250.25m, Money(Result(await Send(HttpMethod.Get, $"/api/CashLedger/{cash.Id}")), "amount"));
        Assert.Equal(1000m, Result(await Send(HttpMethod.Get, "/api/CashLedger/summary/opening")).GetValue<decimal>());
        Assert.Equal(250.25m, Result(await Send(HttpMethod.Get, "/api/CashLedger/summary/in")).GetValue<decimal>());
        Assert.Equal(50m, Result(await Send(HttpMethod.Get, "/api/CashLedger/summary/out")).GetValue<decimal>());
        Assert.Equal(1200.25m, Result(await Send(HttpMethod.Get, "/api/CashLedger/summary/current")).GetValue<decimal>());
        cash.Amount = 300;
        await Send(HttpMethod.Put, $"/api/CashLedger/{cash.Id}", cash);
        await Send(HttpMethod.Delete, $"/api/CashLedger/{outgoing}");
        Assert.Equal(1300m, Result(await Send(HttpMethod.Get, "/api/CashLedger/summary/current")).GetValue<decimal>());
    }

    [Fact]
    public async Task BankCrudAndTransfers_KeepAccountAndGlobalBalancesConsistent()
    {
        var a = TestData.Account("A");
        a.Id = await Create("/api/BankAccount", a);
        var b = await Create("/api/BankAccount", TestData.Account("B"));
        Assert.Equal(2, Items(await Send(HttpMethod.Get, "/api/BankAccount")).Count);
        a.IsActive = false;
        await Send(HttpMethod.Put, $"/api/BankAccount/{a.Id}", a);
        Assert.False(Result(await Send(HttpMethod.Get, $"/api/BankAccount/{a.Id}"))["isActive"]!.GetValue<bool>());
        a.IsActive = true;
        await Send(HttpMethod.Put, $"/api/BankAccount/{a.Id}", a);
        var deposit = TestData.Transaction(a.Id);
        deposit.Id = await Create("/api/BankTransaction", deposit);
        var withdrawal = await Create("/api/BankTransaction", TestData.Transaction(a.Id, "OUT", 100));
        await Send(HttpMethod.Post, "/api/BankTransaction/transfer",
            new { fromAccountId = a.Id, toAccountId = b, amount = 250m, transactionDate = DateTime.UtcNow });
        Assert.Equal(4, Items(await Send(HttpMethod.Get, "/api/BankTransaction")).Count);
        Assert.Equal(3, Items(await Send(HttpMethod.Get, $"/api/BankTransaction/account/{a.Id}")).Count);
        Assert.Equal(1000m, Money(Result(await Send(HttpMethod.Get, $"/api/BankTransaction/{deposit.Id}")), "amount"));
        var balances = Items(await Send(HttpMethod.Get, "/api/BankAccount/summary/accounts"));
        Assert.Equal(650m, Money(balances[0]!, "currentBalance"));
        Assert.Equal(250m, Money(balances[1]!, "currentBalance"));
        Assert.Equal(900m, Result(await Send(HttpMethod.Get, "/api/BankAccount/summary/total")).GetValue<decimal>());
        deposit.Amount = 1200;
        await Send(HttpMethod.Put, $"/api/BankTransaction/{deposit.Id}", deposit);
        await Send(HttpMethod.Delete, $"/api/BankTransaction/{withdrawal}");
        Assert.Equal(1200m, Result(await Send(HttpMethod.Get, "/api/BankAccount/summary/total")).GetValue<decimal>());
        await using var db = Database.CreateContext();
        var pair = await db.BankTransactions.Where(x => x.TransactionType.StartsWith("TRANSFER")).ToListAsync();
        Assert.Equal(2, pair.Count);
        Assert.Equal(pair[0].Amount, pair[1].Amount);
    }

    [Fact]
    public async Task FailedTransfers_AreAtomic_AndEmptyAccountBalanceIsZero()
    {
        var a = await Create("/api/BankAccount", TestData.Account("A"));
        var b = await Create("/api/BankAccount", TestData.Account("B"));
        Assert.Equal(0m, Result(await Send(HttpMethod.Get, "/api/BankAccount/summary/total")).GetValue<decimal>());
        await Send(HttpMethod.Post, "/api/BankTransaction/transfer",
            new { fromAccountId = a, toAccountId = b, amount = 1m, transactionDate = DateTime.UtcNow }, HttpStatusCode.BadRequest);
        await Create("/api/BankTransaction", TestData.Transaction(a, amount: 100));
        foreach (var destination in new[] { a, b })
            await Send(HttpMethod.Post, "/api/BankTransaction/transfer",
                new { fromAccountId = a, toAccountId = destination, amount = 101m, transactionDate = DateTime.UtcNow }, HttpStatusCode.BadRequest);
        await Send(HttpMethod.Post, "/api/BankTransaction/transfer",
            new { fromAccountId = a, toAccountId = 999, amount = 1m, transactionDate = DateTime.UtcNow }, HttpStatusCode.NotFound);
        await using var db = Database.CreateContext();
        Assert.Single(await db.BankTransactions.ToListAsync());
        Assert.Equal(100m, Result(await Send(HttpMethod.Get, "/api/BankAccount/summary/total")).GetValue<decimal>());
    }

    [Fact]
    public async Task AccountDeletion_CurrentBehaviorRemovesOwnRowsAndUnlinksRelatedRows()
    {
        var a = await Create("/api/BankAccount", TestData.Account("A"));
        var b = await Create("/api/BankAccount", TestData.Account("B"));
        await Create("/api/BankTransaction", TestData.Transaction(a, amount: 100));
        await Send(HttpMethod.Post, "/api/BankTransaction/transfer",
            new { fromAccountId = a, toAccountId = b, amount = 20m, transactionDate = DateTime.UtcNow });
        await Send(HttpMethod.Delete, $"/api/BankAccount/{a}");
        await using var db = Database.CreateContext();
        var remaining = Assert.Single(await db.BankTransactions.ToListAsync());
        Assert.Equal(b, remaining.AccountId);
        Assert.Null(remaining.RelatedAccountId);
        Assert.Equal(20m, remaining.Amount);
    }

    [Fact]
    public async Task MissingRecordsAndValidation_UseCorrectHttpStatuses()
    {
        foreach (var route in new[] { "CashLedger", "BankAccount", "BankTransaction" })
        {
            await Send(HttpMethod.Get, $"/api/{route}/999", status: HttpStatusCode.NotFound);
            await Send(HttpMethod.Delete, $"/api/{route}/999", status: HttpStatusCode.NotFound);
        }
        var cash = TestData.Cash();
        cash.Id = 999;
        await Send(HttpMethod.Put, "/api/CashLedger/999", cash, HttpStatusCode.NotFound);
        await Send(HttpMethod.Put, "/api/CashLedger/998", cash, HttpStatusCode.BadRequest);
        var account = TestData.Account();
        account.Id = 999;
        await Send(HttpMethod.Put, "/api/BankAccount/999", account, HttpStatusCode.NotFound);
        await Send(HttpMethod.Put, "/api/BankAccount/998", account, HttpStatusCode.BadRequest);
        var transaction = TestData.Transaction(999);
        transaction.Id = 999;
        await Send(HttpMethod.Put, "/api/BankTransaction/999", transaction, HttpStatusCode.NotFound);
        await Send(HttpMethod.Put, "/api/BankTransaction/998", transaction, HttpStatusCode.BadRequest);
        await Send(HttpMethod.Post, "/api/BankTransaction", TestData.Transaction(999), HttpStatusCode.NotFound);
        await Send(HttpMethod.Post, "/api/BankTransaction", TestData.Transaction(0), HttpStatusCode.BadRequest);
        await Send(HttpMethod.Get, "/api/BankTransaction/account/0", status: HttpStatusCode.BadRequest);
        await Send(HttpMethod.Post, "/api/CashLedger", TestData.Cash(amount: 0), HttpStatusCode.BadRequest);
        await Send(HttpMethod.Post, "/api/CashLedger", TestData.Cash("OTHER"), HttpStatusCode.BadRequest);
        await Send(HttpMethod.Post, "/api/BankAccount", TestData.Account(" "), HttpStatusCode.BadRequest);
        await Create("/api/BankAccount", TestData.Account("Unique"));
        await Send(HttpMethod.Post, "/api/BankAccount", TestData.Account(" Unique "), HttpStatusCode.Conflict);
        var id = await Create("/api/BankAccount", TestData.Account("Other"));
        account = TestData.Account("Unique");
        account.Id = id;
        await Send(HttpMethod.Put, $"/api/BankAccount/{id}", account, HttpStatusCode.Conflict);
    }
}
