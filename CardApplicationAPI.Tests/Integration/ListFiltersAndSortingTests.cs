using CardApplicationAPI.Tests.Infrastructure;
using System.Net;

namespace CardApplicationAPI.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class ListFiltersAndSortingTests : ApiTestBase
{
    [Fact]
    public async Task CardInfo_SearchFiltersAndSorting_WorkAndValidate()
    {
        var first = TestData.Card(party: "Zeta Party", amount: 200m, profit: 20m);
        first.CardName = "Alpha Visa";
        first.CardNumber = "4111-AAA";
        first.BankName = "HDFC";
        first.limitUsed = true;
        first.Id = await Create("/api/CardInfo", first);

        var second = TestData.Card(party: "Alpha Party", amount: 500m, profit: 50m);
        second.CardName = "Beta Master";
        second.CardNumber = "5222-BBB";
        second.BankName = "ICICI";
        second.limitUsed = false;
        second.Id = await Create("/api/CardInfo", second);

        var summaryId = Items(await Send(HttpMethod.Get, "/api/PaymentInfo?partyName=Zeta%20Party"))[0]!["id"]!.GetValue<int>();
        await Send(HttpMethod.Patch, $"/api/PaymentInfo/{summaryId}/mark-paid");

        var searchParty = Items(await Send(HttpMethod.Get, "/api/CardInfo?search=zeta"));
        Assert.Single(searchParty);
        Assert.Equal(first.Id, searchParty[0]!["id"]!.GetValue<int>());

        var searchBank = Items(await Send(HttpMethod.Get, "/api/CardInfo?search=icici"));
        Assert.Single(searchBank);
        Assert.Equal(second.Id, searchBank[0]!["id"]!.GetValue<int>());

        var settledOnly = Items(await Send(HttpMethod.Get, "/api/CardInfo?isSettled=true&limitUsed=true"));
        Assert.Single(settledOnly);
        Assert.Equal(first.Id, settledOnly[0]!["id"]!.GetValue<int>());

        var sortedAsc = Items(await Send(HttpMethod.Get, "/api/CardInfo?sortBy=partyName&sortDir=asc"));
        Assert.Equal(new[] { second.Id, first.Id }, sortedAsc.Select(x => x!["id"]!.GetValue<int>()));

        var sortedDeductionDesc = Items(await Send(HttpMethod.Get, "/api/CardInfo?sortBy=deduction&sortDir=desc"));
        Assert.Equal(new[] { second.Id, first.Id }, sortedDeductionDesc.Select(x => x!["id"]!.GetValue<int>()));

        await Send(HttpMethod.Get, "/api/CardInfo?sortBy=unknownField", status: HttpStatusCode.BadRequest);
        await Send(HttpMethod.Get, "/api/CardInfo?sortDir=sideways", status: HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Users_SearchRoleFilterSortingAndSummary_WorkAndValidate()
    {
        await Create("/api/Users", new
        {
            username = "rahul.emp",
            password = Password,
            fullName = "Rahul Sharma",
            nickName = "Rocky",
            role = "User"
        });

        var summary = Result(await Send(HttpMethod.Get, "/api/Users/summary"));
        Assert.Equal(4, summary["total"]!.GetValue<int>());
        Assert.Equal(1, summary["masters"]!.GetValue<int>());
        Assert.Equal(1, summary["admins"]!.GetValue<int>());
        Assert.Equal(2, summary["users"]!.GetValue<int>());

        var nickSearch = Items(await Send(HttpMethod.Get, "/api/Users?search=rocky&role=user"));
        Assert.Single(nickSearch);
        Assert.Equal("rahul.emp", nickSearch[0]!["username"]!.GetValue<string>());

        var sortedDesc = Items(await Send(HttpMethod.Get, "/api/Users?sortBy=username&sortDir=desc"));
        var usernames = sortedDesc.Select(x => x!["username"]!.GetValue<string>()).ToArray();
        Assert.Equal(usernames.OrderByDescending(x => x, StringComparer.OrdinalIgnoreCase), usernames);

        await Send(HttpMethod.Get, "/api/Users?role=SuperAdmin", status: HttpStatusCode.BadRequest);
        await Send(HttpMethod.Get, "/api/Users?sortBy=password", status: HttpStatusCode.BadRequest);
        await Send(HttpMethod.Get, "/api/Users?sortDir=up", status: HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StockItem_SearchFiltersSortingAndPaymentStatus_WorkAndValidate()
    {
        var inStock = TestData.Stock("IN", 700m);
        inStock.No = "STK-101";
        inStock.Model = "Galaxy S26";
        inStock.Imei = "IMEI-AAA-111";
        inStock.PartyName = "Vendor One";
        inStock.Status = "Available";
        inStock.Id = await Create("/api/StockItem", inStock);

        var outStock = TestData.Stock("OUT", 950m);
        outStock.No = "STK-202";
        outStock.Model = "iPhone 18";
        outStock.Imei = "IMEI-BBB-222";
        outStock.PartyName = "Vendor One";
        outStock.SoldTo = "Customer Alpha";
        outStock.Status = "Sold";
        outStock.Id = await Create("/api/StockItem", outStock);

        var createdItem = Result(await Send(HttpMethod.Get, $"/api/StockItem/{inStock.Id}"));
        Assert.Equal("Pending", createdItem["paymentStatus"]!.GetValue<string>());

        var paymentId = Items(await Send(HttpMethod.Get, $"/api/StockPaymentInfo/stock/{inStock.Id}"))[0]!["id"]!.GetValue<int>();
        await Send(HttpMethod.Patch, $"/api/StockPaymentInfo/{paymentId}/settle", TestData.Settlement(700m));

        var afterSettle = Result(await Send(HttpMethod.Get, $"/api/StockItem/{inStock.Id}"));
        Assert.Equal("Completed", afterSettle["paymentStatus"]!.GetValue<string>());

        var filtered = Items(await Send(HttpMethod.Get, "/api/StockItem?search=customer%20alpha&status=sold&inOut=out"));
        Assert.Single(filtered);
        Assert.Equal(outStock.Id, filtered[0]!["id"]!.GetValue<int>());
        Assert.Equal("Pending", filtered[0]!["paymentStatus"]!.GetValue<string>());

        var sortedByAmountDesc = Items(await Send(HttpMethod.Get, "/api/StockItem?sortBy=amount&sortDir=desc"));
        Assert.Equal(new[] { outStock.Id, inStock.Id }, sortedByAmountDesc.Select(x => x!["id"]!.GetValue<int>()));

        var sortedByPaymentStatusAsc = Items(await Send(HttpMethod.Get, "/api/StockItem?sortBy=paymentStatus&sortDir=asc"));
        Assert.Equal(new[] { inStock.Id, outStock.Id }, sortedByPaymentStatusAsc.Select(x => x!["id"]!.GetValue<int>()));

        await Send(HttpMethod.Get, "/api/StockItem?status=Archived", status: HttpStatusCode.BadRequest);
        await Send(HttpMethod.Get, "/api/StockItem?inOut=BOTH", status: HttpStatusCode.BadRequest);
        await Send(HttpMethod.Get, "/api/StockItem?sortBy=invalid", status: HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CashLedger_BankAccount_AndBankTransaction_FiltersSortingAndStatusSummary_Work()
    {
        var cash1 = TestData.Cash("OPENING", 1000m);
        cash1.Name = "Opening Vault";
        cash1.Remarks = "Initial cash";
        await Create("/api/CashLedger", cash1);

        var cash2 = TestData.Cash("OUT", 150m);
        cash2.Name = "Courier Charge";
        cash2.Remarks = "BlueDart parcel";
        await Create("/api/CashLedger", cash2);

        var filteredCash = Items(await Send(HttpMethod.Get, "/api/CashLedger?search=bluedart&transactionType=out"));
        Assert.Single(filteredCash);
        Assert.Equal("Courier Charge", filteredCash[0]!["name"]!.GetValue<string>());

        var cashSortedAsc = Items(await Send(HttpMethod.Get, "/api/CashLedger?sortBy=amount&sortDir=asc"));
        Assert.Equal(150m, Money(cashSortedAsc[0]!, "amount"));

        await Send(HttpMethod.Get, "/api/CashLedger?transactionType=TRANSFER", status: HttpStatusCode.BadRequest);
        await Send(HttpMethod.Get, "/api/CashLedger?sortBy=unknown", status: HttpStatusCode.BadRequest);

        var accActive = TestData.Account("HDFC Primary");
        accActive.BankName = "HDFC Bank";
        accActive.AccountNo = "11112222";
        accActive.IsActive = true;
        accActive.Id = await Create("/api/BankAccount", accActive);

        var accInactive = TestData.Account("Axis Dormant");
        accInactive.BankName = "Axis Bank";
        accInactive.AccountNo = "99998888";
        accInactive.IsActive = false;
        accInactive.Id = await Create("/api/BankAccount", accInactive);

        var statusSummary = Result(await Send(HttpMethod.Get, "/api/BankAccount/summary/status"));
        Assert.Equal(2, statusSummary["total"]!.GetValue<int>());
        Assert.Equal(1, statusSummary["active"]!.GetValue<int>());
        Assert.Equal(1, statusSummary["inactive"]!.GetValue<int>());

        var inactiveFiltered = Items(await Send(HttpMethod.Get, "/api/BankAccount?search=9999&isActive=false"));
        Assert.Single(inactiveFiltered);
        Assert.Equal(accInactive.Id, inactiveFiltered[0]!["id"]!.GetValue<int>());

        await Send(HttpMethod.Get, "/api/BankAccount?sortBy=balance", status: HttpStatusCode.BadRequest);

        var openingTxn = TestData.Transaction(accActive.Id, "OPENING", 5000m);
        openingTxn.ReferenceNo = "REF-OPEN-1";
        openingTxn.Remarks = "Seed balance";
        await Create("/api/BankTransaction", openingTxn);

        var inTxn = TestData.Transaction(accActive.Id, "IN", 1200m);
        inTxn.ReferenceNo = "REF-IN-2";
        inTxn.Remarks = "Customer NEFT";
        await Create("/api/BankTransaction", inTxn);

        await Send(HttpMethod.Post, "/api/BankTransaction/transfer", new
        {
            fromAccountId = accActive.Id,
            toAccountId = accInactive.Id,
            amount = 800m,
            transactionDate = DateTime.UtcNow,
            referenceNo = "TRF-99",
            remarks = "Move to Axis"
        });

        var moneyIn = Items(await Send(HttpMethod.Get, "/api/BankTransaction?transactionType=IN,OPENING&sortBy=amount&sortDir=asc"));
        Assert.Equal(2, moneyIn.Count);
        Assert.Equal(1200m, Money(moneyIn[0]!, "amount"));
        Assert.Equal(5000m, Money(moneyIn[1]!, "amount"));

        var transferSearch = Items(await Send(HttpMethod.Get, $"/api/BankTransaction/account/{accActive.Id}?search=Axis&transactionType=TRANSFER_IN,TRANSFER_OUT"));
        Assert.Single(transferSearch);
        Assert.Equal("TRANSFER_OUT", transferSearch[0]!["transactionType"]!.GetValue<string>());

        await Send(HttpMethod.Get, "/api/BankTransaction?transactionType=IN,INVALID", status: HttpStatusCode.BadRequest);
        await Send(HttpMethod.Get, $"/api/BankTransaction/account/{accActive.Id}?sortBy=invalidField", status: HttpStatusCode.BadRequest);
    }
}
