using CardApplicationAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using System.Text;

namespace CardApplicationAPI.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class HttpContractTests : ApiTestBase
{
    public static readonly string[] Routes =
    [
        "POST /api/Auth/login", "POST /api/Auth/refresh", "POST /api/Auth/logout", "POST /api/Auth/seed",
        "GET /api/Users", "GET /api/Users/{id}", "POST /api/Users", "PUT /api/Users/{id}", "DELETE /api/Users/{id}",
        "GET /api/CardInfo", "GET /api/CardInfo/{id}", "POST /api/CardInfo", "PUT /api/CardInfo/{id}", "DELETE /api/CardInfo/{id}",
        "GET /api/PaymentInfo", "PATCH /api/PaymentInfo/{id}/mark-paid",
        "GET /api/StockItem", "GET /api/StockItem/{id}", "GET /api/StockItem/by-party/{partyName}",
        "GET /api/StockItem/by-status/{status}", "GET /api/StockItem/by-inout/{inOut}",
        "POST /api/StockItem", "PUT /api/StockItem/{id}", "DELETE /api/StockItem/{id}",
        "GET /api/StockPaymentInfo", "GET /api/StockPaymentInfo/{id}", "GET /api/StockPaymentInfo/stock/{stockItemId}",
        "GET /api/StockPaymentInfo/{id}/history", "GET /api/StockPaymentInfo/summary/parties", "GET /api/StockPaymentInfo/summary/totals",
        "PATCH /api/StockPaymentInfo/{id}/settle", "PATCH /api/StockPaymentInfo/{id}/reset",
        "GET /api/CashLedger", "GET /api/CashLedger/{id:long}", "GET /api/CashLedger/type/{transactionType}",
        "GET /api/CashLedger/summary/opening", "GET /api/CashLedger/summary/in", "GET /api/CashLedger/summary/out", "GET /api/CashLedger/summary/current",
        "POST /api/CashLedger", "PUT /api/CashLedger/{id:long}", "DELETE /api/CashLedger/{id:long}",
        "GET /api/BankAccount", "GET /api/BankAccount/{id:int}", "GET /api/BankAccount/summary/total", "GET /api/BankAccount/summary/accounts",
        "POST /api/BankAccount", "PUT /api/BankAccount/{id:int}", "DELETE /api/BankAccount/{id:int}",
        "GET /api/BankTransaction", "GET /api/BankTransaction/{id:int}", "GET /api/BankTransaction/account/{accountId:int}",
        "POST /api/BankTransaction", "POST /api/BankTransaction/transfer", "PUT /api/BankTransaction/{id:int}", "DELETE /api/BankTransaction/{id:int}",
        "GET /api/Dashboard/summary", "GET /WeatherForecast"
    ];

    [Fact]
    public async Task RouteInventoryAndAnonymousAuthorization_CoverEveryControllerAction()
    {
        var actual = typeof(Program).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(ControllerBase)))
            .SelectMany(type => type.GetMethods().SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>()
                .SelectMany(http => http.HttpMethods.Select(verb =>
                {
                    var root = type.GetCustomAttribute<RouteAttribute>()!.Template.Replace("[controller]", type.Name.Replace("Controller", ""));
                    return $"{verb} /{root}{(http.Template == null ? "" : "/" + http.Template)}";
                })))).OrderBy(x => x).ToArray();
        Assert.Equal(Routes.OrderBy(x => x), actual);
        foreach (var route in Routes.Where(x => !x.Contains("/api/Auth")))
        {
            var split = route.Split(' ', 2);
            var path = System.Text.RegularExpressions.Regex.Replace(split[1], "\\{[^}]+\\}", "1");
            await Send(new HttpMethod(split[0]), path, status: HttpStatusCode.Unauthorized, client: Anonymous);
        }
    }

    [Fact]
    public async Task UserCannotManageUsersOrSettlePayments_ButCanReadBusinessData()
    {
        using var user = await AsRole("User");
        foreach (var route in Routes.Where(x => x.Contains("/api/Users") || x.Contains("/mark-paid") || x.Contains("/settle") || x.Contains("/reset")))
        {
            var split = route.Split(' ', 2);
            await Send(new HttpMethod(split[0]), split[1].Replace("{id}", "1"), status: HttpStatusCode.Forbidden, client: user);
        }
        await Send(HttpMethod.Get, "/api/CardInfo", client: user);
        using var master = await AsRole("Master");
        await Send(HttpMethod.Get, "/api/Users", client: master);
    }

    [Fact]
    public async Task InvalidTokens_AreRejectedByRealJwtValidation()
    {
        foreach (var defect in new[] { "expired", "issuer", "audience", "signature", "malformed" })
        {
            using var client = NewClient();
            var key = defect == "signature" ? new string('z', 64) : Factory.SigningKey;
            var token = new JwtSecurityToken(
                defect == "issuer" ? "wrong" : CardApplicationFactory.Issuer,
                defect == "audience" ? "wrong" : CardApplicationFactory.Audience,
                [new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(ClaimTypes.Name, "admin"), new Claim(ClaimTypes.Role, "Admin")],
                expires: defect == "expired" ? DateTime.UtcNow.AddMinutes(-5) : DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                defect == "malformed" ? "not-a-jwt" : new JwtSecurityTokenHandler().WriteToken(token));
            await Send(HttpMethod.Get, "/api/Dashboard/summary", status: HttpStatusCode.Unauthorized, client: client);
        }
    }

    [Fact]
    public async Task PaginationBoundsMalformedJsonAndWeather_AreHandled()
    {
        foreach (var route in new[] { "Users", "CardInfo", "PaymentInfo", "StockItem", "StockPaymentInfo", "CashLedger", "BankAccount", "BankTransaction" })
        {
            foreach (var query in new[] { "pageNumber=0", "pageSize=0", "pageSize=101", "pageNumber=abc" })
                await Send(HttpMethod.Get, $"/api/{route}?{query}", status: HttpStatusCode.BadRequest);
            var page = Result(await Send(HttpMethod.Get, $"/api/{route}?pageNumber=999&pageSize=100"));
            Assert.Empty(page["items"]!.AsArray());
            Assert.Equal(999, page["pagination"]!["currentPage"]!.GetValue<int>());
        }
        var defaultPage = Result(await Send(HttpMethod.Get, "/api/Users"));
        Assert.Equal(10, defaultPage["pagination"]!["pageSize"]!.GetValue<int>());
        var first = Items(await Send(HttpMethod.Get, "/api/Users?pageNumber=1&pageSize=1"))[0]!["id"]!.GetValue<int>();
        var second = Items(await Send(HttpMethod.Get, "/api/Users?pageNumber=2&pageSize=1"))[0]!["id"]!.GetValue<int>();
        Assert.NotEqual(first, second);
        using var response = await Client.PostAsync("/api/CardInfo", new StringContent("{broken", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var weather = (await Send(HttpMethod.Get, "/WeatherForecast"))!.AsArray();
        Assert.Equal(5, weather.Count);
        Assert.All(weather, row => Assert.InRange(row!["temperatureC"]!.GetValue<int>(), -20, 54));
        await Send(HttpMethod.Get, "/no-such-route", status: HttpStatusCode.NotFound);
    }
}
