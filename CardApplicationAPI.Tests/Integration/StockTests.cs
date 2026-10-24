using CardApplicationAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace CardApplicationAPI.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class StockTests : ApiTestBase
{
    private async Task<int> PaymentId(int stockId) =>
        Items(await Send(HttpMethod.Get, $"/api/StockPaymentInfo/stock/{stockId}"))[0]!["id"]!.GetValue<int>();

    [Fact]
    public async Task Classification_Filters_PartySummaries_AndInventory()
    {
        var stock = TestData.Stock();
        stock.InOut = " in ";
        stock.Status = " available ";
        stock.Id = await Create("/api/StockItem", stock);
        var sold = TestData.Stock("OUT", 950);
        sold.Id = await Create("/api/StockItem", sold);
        Assert.Equal(2, Items(await Send(HttpMethod.Get, "/api/StockItem")).Count);
        Assert.Equal("IN", Result(await Send(HttpMethod.Get, $"/api/StockItem/{stock.Id}"))["inOut"]!.GetValue<string>());
        foreach (var route in new[] { "by-party/Supplier", "by-status/Available", "by-inout/IN" })
            Assert.NotEmpty(Items(await Send(HttpMethod.Get, $"/api/StockItem/{route}")));
        Assert.Equal(2, Items(await Send(HttpMethod.Get, "/api/StockPaymentInfo")).Count);
        var payable = Items(await Send(HttpMethod.Get, "/api/StockPaymentInfo?partyName=Supplier&inOut=in&paymentType=payable&status=pending"))[0]!;
        Assert.Equal(700m, Money(payable, "outstandingAmount"));
        var receivable = Items(await Send(HttpMethod.Get, "/api/StockPaymentInfo?partyName=Buyer&paymentType=Receivable"))[0]!;
        Assert.Equal("Buyer", receivable["partyName"]!.GetValue<string>());
        Assert.Equal(950m, Money(receivable, "outstandingAmount"));
        Assert.Equal(2, Items(await Send(HttpMethod.Get, "/api/StockPaymentInfo/summary/parties")).Count);
        var totals = Result(await Send(HttpMethod.Get, "/api/StockPaymentInfo/summary/totals"));
        Assert.Equal(700m, Money(totals, "pendingPayable"));
        Assert.Equal(950m, Money(totals, "pendingReceivable"));
        stock.InOut = "IN";
        stock.Amount = 800.25m;
        await Send(HttpMethod.Put, $"/api/StockItem/{stock.Id}", stock);
        Assert.Equal(800.25m, Money(Result(await Send(HttpMethod.Get, $"/api/StockPaymentInfo/{await PaymentId(stock.Id)}")), "outstandingAmount"));
        await Send(HttpMethod.Delete, $"/api/StockItem/{stock.Id}");
        Assert.Empty(Items(await Send(HttpMethod.Get, $"/api/StockPaymentInfo/stock/{stock.Id}")));
        await Send(HttpMethod.Get, $"/api/StockItem/{stock.Id}", status: HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Settlement_Protection_Reset_AndAuditAreConsistent()
    {
        var stock = TestData.Stock();
        stock.Id = await Create("/api/StockItem", stock);
        var payment = await PaymentId(stock.Id);
        await Send(HttpMethod.Patch, $"/api/StockPaymentInfo/{payment}/settle", TestData.Settlement(1), HttpStatusCode.BadRequest);
        Assert.Empty(Items(await Send(HttpMethod.Get, $"/api/StockPaymentInfo/{payment}/history")));
        await Send(HttpMethod.Patch, $"/api/StockPaymentInfo/{payment}/settle", TestData.Settlement(700));
        var completed = Result(await Send(HttpMethod.Get, $"/api/StockPaymentInfo/{payment}"));
        Assert.Equal("Completed", completed["paymentStatus"]!.GetValue<string>());
        Assert.Equal(0m, Money(completed, "outstandingAmount"));
        Assert.Equal("admin", completed["completedByUsername"]!.GetValue<string>());
        await Send(HttpMethod.Patch, $"/api/StockPaymentInfo/{payment}/settle", TestData.Settlement(700), HttpStatusCode.Conflict);
        stock.Amount = 701;
        await Send(HttpMethod.Put, $"/api/StockItem/{stock.Id}", stock, HttpStatusCode.Conflict);
        stock.Amount = 700;
        stock.PartyName = "Different";
        await Send(HttpMethod.Put, $"/api/StockItem/{stock.Id}", stock, HttpStatusCode.Conflict);
        stock.PartyName = "Supplier";
        stock.SoldTo = "Unexpected buyer";
        await Send(HttpMethod.Put, $"/api/StockItem/{stock.Id}", stock, HttpStatusCode.Conflict);
        stock.InOut = "OUT";
        await Send(HttpMethod.Put, $"/api/StockItem/{stock.Id}", stock, HttpStatusCode.Conflict);
        stock.InOut = "IN";
        stock.SoldTo = null;
        stock.Colour = "Grey";
        await Send(HttpMethod.Put, $"/api/StockItem/{stock.Id}", stock);
        await Send(HttpMethod.Delete, $"/api/StockItem/{stock.Id}", status: HttpStatusCode.Conflict);
        await Send(HttpMethod.Patch, $"/api/StockPaymentInfo/{payment}/reset", new { reason = "" }, HttpStatusCode.BadRequest);
        await Send(HttpMethod.Patch, $"/api/StockPaymentInfo/{payment}/reset", new { reason = "Correction" });
        await Send(HttpMethod.Patch, $"/api/StockPaymentInfo/{payment}/reset", new { reason = "Again" }, HttpStatusCode.Conflict);
        Assert.Equal(700m, Money(Result(await Send(HttpMethod.Get, $"/api/StockPaymentInfo/{payment}")), "outstandingAmount"));
        using var master = await AsRole("Master");
        await Send(HttpMethod.Patch, $"/api/StockPaymentInfo/{payment}/settle", TestData.Settlement(700), client: master);
        var history = Items(await Send(HttpMethod.Get, $"/api/StockPaymentInfo/{payment}/history"));
        Assert.Equal(new[] { "Settlement", "Reset", "Settlement" }, history.Select(x => x!["eventType"]!.GetValue<string>()));
        Assert.Equal("master", history[0]!["performedByUsername"]!.GetValue<string>());
        Assert.Equal("Correction", history[1]!["remarks"]!.GetValue<string>());
        Assert.Equal("TEST-REFERENCE", history[2]!["referenceNo"]!.GetValue<string>());
        await using var db = Database.CreateContext();
        Assert.Single(await db.StockPaymentInfo.ToListAsync());
        Assert.Equal(3, await db.StockPaymentHistory.CountAsync());
        Assert.Empty(await db.BankTransactions.ToListAsync());
    }

    [Fact]
    public async Task SameParty_HasSeparatePayableAndReceivableGroups()
    {
        var incoming = TestData.Stock();
        incoming.Id = await Create("/api/StockItem", incoming);
        var outgoing = TestData.Stock("OUT", 950);
        outgoing.SoldTo = "Supplier";
        outgoing.Id = await Create("/api/StockItem", outgoing);
        await Send(HttpMethod.Patch, $"/api/StockPaymentInfo/{await PaymentId(outgoing.Id)}/settle", TestData.Settlement(950));
        var groups = Items(await Send(HttpMethod.Get, "/api/StockPaymentInfo/summary/parties?partyName=Supplier"));
        Assert.Equal(2, groups.Count);
        var received = Items(await Send(HttpMethod.Get, "/api/StockPaymentInfo/summary/parties?partyName=Supplier&paymentType=Receivable&status=Completed"))[0]!;
        Assert.Equal(950m, Money(received, "completedAmount"));
        Assert.Equal(0m, Money(received, "outstandingAmount"));
        Assert.Equal(1, received["itemCount"]!.GetValue<int>());
    }

    [Fact]
    public async Task InvalidStockAndPayments_ReturnClientErrorsWithoutWrites()
    {
        foreach (var stock in new[] { TestData.Stock("INVALID"), TestData.Stock(amount: 0) })
            await Send(HttpMethod.Post, "/api/StockItem", stock, HttpStatusCode.BadRequest);
        var missing = TestData.Stock("OUT");
        missing.SoldTo = null;
        await Send(HttpMethod.Post, "/api/StockItem", missing, HttpStatusCode.BadRequest);
        missing = TestData.Stock();
        missing.PartyName = " ";
        await Send(HttpMethod.Post, "/api/StockItem", missing, HttpStatusCode.BadRequest);
        missing = TestData.Stock();
        missing.Id = 998;
        await Send(HttpMethod.Put, "/api/StockItem/999", missing, HttpStatusCode.BadRequest);
        missing.Id = 999;
        await Send(HttpMethod.Put, "/api/StockItem/999", missing, HttpStatusCode.NotFound);
        await Send(HttpMethod.Delete, "/api/StockItem/999", status: HttpStatusCode.NotFound);
        await Send(HttpMethod.Get, "/api/StockPaymentInfo?status=Paid", status: HttpStatusCode.BadRequest);
        await Send(HttpMethod.Get, "/api/StockPaymentInfo?paymentType=Other", status: HttpStatusCode.BadRequest);
        await Send(HttpMethod.Get, "/api/StockPaymentInfo?inOut=Other", status: HttpStatusCode.BadRequest);
        await Send(HttpMethod.Get, "/api/StockPaymentInfo/999", status: HttpStatusCode.NotFound);
        await Send(HttpMethod.Get, "/api/StockPaymentInfo/999/history", status: HttpStatusCode.NotFound);
        await Send(HttpMethod.Patch, "/api/StockPaymentInfo/999/settle", TestData.Settlement(10), HttpStatusCode.NotFound);
        await Send(HttpMethod.Patch, "/api/StockPaymentInfo/999/reset", new { reason = "Test" }, HttpStatusCode.NotFound);
        var invalid = TestData.Settlement(0);
        await Send(HttpMethod.Patch, "/api/StockPaymentInfo/999/settle", invalid, HttpStatusCode.BadRequest);
        invalid.Amount = 10;
        invalid.TransactionDate = default;
        await Send(HttpMethod.Patch, "/api/StockPaymentInfo/999/settle", invalid, HttpStatusCode.BadRequest);
        Assert.Empty(Items(await Send(HttpMethod.Get, "/api/StockItem")));
    }
}
