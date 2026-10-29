using CardApplicationAPI.Helpers;
using CardApplicationAPI.Middleware;
using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Models.Settings;
using CardApplicationAPI.Services.Implementations;
using CardApplicationAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace CardApplicationAPI.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class ValidationAndHelpersTests
{
    [Fact]
    public async Task LedgerValidationUsesClientErrorCodes()
    {
        var cash = new CashLedgerService(null!);
        foreach (var model in new[] { TestData.Cash("OTHER"), TestData.Cash(amount: 0), TestData.Cash(amount: -1) })
            Assert.Equal(ApplicationActionStatusCodes.ValidationFailed, (await cash.AddAsync(model)).StatusCode);
        var bank = new BankTransactionService(null!);
        foreach (var model in new[] { TestData.Transaction(0), TestData.Transaction(1, "OTHER"), TestData.Transaction(1, amount: 0) })
            Assert.Equal(ApplicationActionStatusCodes.ValidationFailed, (await bank.AddAsync(model)).StatusCode);
        Assert.Equal(ApplicationActionStatusCodes.ValidationFailed,
            (await new BankAccountService(null!).AddAsync(TestData.Account(" "))).StatusCode);
    }

    [Fact]
    public async Task ListSortAndFilterValidation_RejectsInvalidValuesBeforeRepositoryCall()
    {
        Assert.Equal(ApplicationActionStatusCodes.ValidationFailed,
            (await new CardInfoService(null!).GetAllAsync(new(), sortBy: "bad")).StatusCode);
        Assert.Equal(ApplicationActionStatusCodes.ValidationFailed,
            (await new CardInfoService(null!).GetAllAsync(new(), sortDir: "bad")).StatusCode);
        Assert.Equal(ApplicationActionStatusCodes.ValidationFailed,
            (await new UserService(null!).GetAllAsync(new(), role: "InvalidRole")).StatusCode);
        Assert.Equal(ApplicationActionStatusCodes.ValidationFailed,
            (await new StockItemService(null!).GetAllAsync(new(), status: "Archived")).StatusCode);
        Assert.Equal(ApplicationActionStatusCodes.ValidationFailed,
            (await new StockItemService(null!).GetAllAsync(new(), inOut: "SIDEWAYS")).StatusCode);
        Assert.Equal(ApplicationActionStatusCodes.ValidationFailed,
            (await new CashLedgerService(null!).GetAllAsync(new(), transactionType: "TRANSFER")).StatusCode);
        Assert.Equal(ApplicationActionStatusCodes.ValidationFailed,
            (await new BankAccountService(null!).GetAllAsync(new(), sortBy: "unknown")).StatusCode);
        Assert.Equal(ApplicationActionStatusCodes.ValidationFailed,
            (await new BankTransactionService(null!).GetAllAsync(new(), transactionType: "IN,UNKNOWN")).StatusCode);
        Assert.Equal(ApplicationActionStatusCodes.ValidationFailed,
            (await new DashboardService(null!).GetMonthlyFlowAsync(0)).StatusCode);
        Assert.Equal(ApplicationActionStatusCodes.ValidationFailed,
            (await new DashboardService(null!).GetSwipesDailyAsync(366)).StatusCode);
        Assert.Equal(ApplicationActionStatusCodes.ValidationFailed,
            (await new DashboardService(null!).GetTopPartiesAsync(51)).StatusCode);
    }

    [Theory]
    [InlineData("", "card", "party")]
    [InlineData("card", " ", "party")]
    [InlineData("card", "number", " ")]
    public async Task CardRequiredFieldsFailBeforePersistence(string name, string number, string party)
    {
        var result = await new CardInfoService(null!).AddAsync(new CardInfo { CardName = name, CardNumber = number, PartyName = party });
        Assert.Equal(ApplicationActionStatusCodes.ValidationFailed, result.StatusCode);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(1, -1)]
    public async Task NegativeCardValuesAreRejected(decimal deduction, decimal profit)
    {
        var result = await new CardInfoService(null!).AddAsync(TestData.Card(amount: deduction, profit: profit));
        Assert.Equal(ApplicationActionStatusCodes.ValidationFailed, result.StatusCode);
    }

    [Theory]
    [InlineData("OTHER", 100, "supplier", "buyer")]
    [InlineData("IN", 0, "supplier", "buyer")]
    [InlineData("IN", -1, "supplier", "buyer")]
    [InlineData("IN", 100, "", "buyer")]
    [InlineData("OUT", 100, "supplier", "")]
    public async Task InvalidStockFailsBeforePersistence(string direction, decimal amount, string party, string buyer)
    {
        var stock = TestData.Stock(direction, amount);
        stock.PartyName = party;
        stock.SoldTo = buyer;
        var result = await new StockItemService(null!).AddAsync(stock);
        Assert.Equal(ApplicationActionStatusCodes.ValidationFailed, result.StatusCode);
    }

    [Theory]
    [InlineData("Paid", null, null)]
    [InlineData(null, "Invalid", null)]
    [InlineData(null, null, "Invalid")]
    public async Task InvalidStockFiltersAreRejected(string? status, string? type, string? direction)
    {
        var result = await new StockPaymentInfoService(null!).GetAllAsync(new(), null, direction, type, status);
        Assert.Equal(ApplicationActionStatusCodes.ValidationFailed, result.StatusCode);
    }

    [Theory]
    [InlineData(0, 1, "admin")]
    [InlineData(1, 0, "admin")]
    [InlineData(1, 1, "")]
    public async Task InvalidPartySettlementIdentityIsRejected(int id, int userId, string username)
    {
        Assert.Equal(ApplicationActionStatusCodes.ValidationFailed,
            (await new PaymentInfoService(null!).MarkPaidAsync(id, userId, username)).StatusCode);
    }

    [Fact]
    public void PasswordHashing_AndJsonDirection_AreCorrect()
    {
        var hasher = new PasswordHasherHelper();
        var hash = hasher.HashPassword("synthetic-password");
        Assert.NotEqual("synthetic-password", hash);
        Assert.True(hasher.VerifyPassword(hash, "synthetic-password"));
        Assert.False(hasher.VerifyPassword(hash, "wrong"));
        Assert.False(hasher.VerifyPassword("", "wrong"));
        Assert.False(hasher.VerifyPassword("not-a-valid-hash", "wrong"));
        var user = JsonSerializer.Deserialize<Users>("""{"Username":"synthetic","Password":"synthetic-password","Role":"User"}""")!;
        Assert.Equal("synthetic-password", user.Password);
        Assert.DoesNotContain("Password", JsonSerializer.Serialize(user));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public void JwtRequiresStrongSigningKey(string key)
    {
        var service = new JwtTokenService(Options.Create(new JwtSettings { Key = key }));
        Assert.Throws<InvalidOperationException>(() => service.GenerateAccessToken(new Users { Id = 1, Username = "test", Role = "User" }));
    }

    [Theory]
    [InlineData("User", "validuser", "long-password", true)]
    [InlineData("Owner", "validuser", "long-password", false)]
    [InlineData("User", "ab", "long-password", false)]
    [InlineData("User", "validuser", "short", false)]
    public void UserAttributesValidateContracts(string role, string username, string password, bool valid)
    {
        var user = new Users { Role = role, Username = username, Password = password };
        Assert.Equal(valid, Validator.TryValidateObject(user, new ValidationContext(user), [], true));
    }

    [Theory]
    [InlineData("argument", 400)]
    [InlineData("missing", 404)]
    [InlineData("unauthorized", 401)]
    [InlineData("unexpected", 500)]
    public async Task MiddlewareMapsErrorsAndDoesNotLeakUnexpectedDetails(string kind, int status)
    {
        Exception error = kind switch
        {
            "argument" => new ArgumentException("invalid input"),
            "missing" => new KeyNotFoundException("missing"),
            "unauthorized" => new UnauthorizedAccessException("denied"),
            _ => new Exception("PRIVATE_INTERNAL_DETAIL")
        };
        var middleware = new GlobalExceptionMiddleware(_ => throw error, NullLogger<GlobalExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(context);
        Assert.Equal(status, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        var text = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.DoesNotContain("PRIVATE_INTERNAL_DETAIL", text);
        using var json = JsonDocument.Parse(text);
        Assert.Equal(status, json.RootElement.GetProperty("statusCode").GetInt32());
    }
}
