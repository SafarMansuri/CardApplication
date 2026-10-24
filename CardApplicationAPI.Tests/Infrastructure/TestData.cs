using CardApplicationAPI.Models;

namespace CardApplicationAPI.Tests.Infrastructure;

public static class TestData
{
    public static CardInfo Card(string party = "Test Party", decimal amount = 100m, decimal profit = 10m) => new()
    {
        CardName = "Synthetic card", CardNumber = $"TEST-{Guid.NewGuid():N}", PartyName = party,
        Deduction = amount, Profit = profit, BankName = "Test Bank", Date = DateTime.UtcNow,
        Merchant = "Test Merchant", SwipePerson = "Test Staff"
    };

    public static StockItem Stock(string direction = "IN", decimal amount = 700m) => new()
    {
        No = $"ST-{Guid.NewGuid():N}", Amount = amount, PartyName = "Supplier", SoldTo = direction == "OUT" ? "Buyer" : null,
        InOut = direction, Status = direction == "IN" ? "Available" : "Sold", Model = "Test device", Colour = "Black"
    };

    public static BankAccount Account(string name = "Account") => new() { AccountName = name, BankName = "Test Bank" };
    public static BankTransaction Transaction(int account, string type = "IN", decimal amount = 1000m) => new()
    {
        AccountId = account, TransactionType = type, Amount = amount, TransactionDate = DateTime.UtcNow
    };

    public static CashLedger Cash(string type = "IN", decimal amount = 100m) => new()
    {
        TransactionType = type, Amount = amount, TransactionDate = DateTime.UtcNow, Name = "Synthetic cash"
    };

    public static StockPaymentSettlementRequest Settlement(decimal amount) => new()
    {
        Amount = amount, TransactionDate = DateTime.UtcNow, PaymentMethod = "Bank", ReferenceNo = "TEST-REFERENCE", Remarks = "Synthetic settlement"
    };
}
