using CardApplicationAPI.Data.DBContext;
using CardApplicationAPI.Data.Entities;
using CardApplicationAPI.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CardApplicationAPI.Tests.Infrastructure;

public abstract class ApiTestBase : IAsyncLifetime
{
    protected const string Password = "Synthetic-Test-Password!42";
    protected LocalDbTestDatabase Database { get; } = new();
    protected CardApplicationFactory Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;
    protected HttpClient Anonymous { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        try
        {
            await Database.CreateAsync();
            await using var db = Database.CreateContext();
            var hasher = new PasswordHasherHelper();
            db.Users.AddRange(new[] { "Admin", "Master", "User" }.Select(role => new UserEntity
            {
                Username = role.ToLowerInvariant(), Password = hasher.HashPassword(Password), Role = role
            }));
            await db.SaveChangesAsync();
            Factory = new CardApplicationFactory(Database);
            Anonymous = NewClient();
            using var scope = Factory.Services.CreateScope();
            Assert.Equal(Database.Name, scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.GetDbConnection().Database);
            Client = NewClient();
            var login = await Send(HttpMethod.Post, "/api/Auth/login", new { username = "admin", password = Password }, client: Anonymous);
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!["accessToken"]!.GetValue<string>());
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    protected HttpClient NewClient() => Factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false
    });

    protected async Task<HttpClient> AsRole(string role)
    {
        var client = NewClient();
        var login = await Send(HttpMethod.Post, "/api/Auth/login", new { username = role.ToLowerInvariant(), password = Password }, client: Anonymous);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!["accessToken"]!.GetValue<string>());
        return client;
    }

    protected async Task<JsonNode?> Send(HttpMethod method, string path, object? body = null,
        HttpStatusCode status = HttpStatusCode.OK, HttpClient? client = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body != null) request.Content = JsonContent.Create(body);
        using var response = await (client ?? Client).SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status, $"{method} {path}: expected {(int)status}, got {(int)response.StatusCode}: {text}\n{Factory.Errors.Details}");
        var json = string.IsNullOrWhiteSpace(text) ? null : JsonNode.Parse(text);
        if ((int)status >= 400 && json is JsonObject result && result["statusCode"] != null)
            Assert.Equal((int)status, result["statusCode"]!.GetValue<int>());
        return json;
    }

    protected async Task<int> Create(string path, object body)
    {
        using var response = await Client.PostAsJsonAsync(path, body);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, $"POST {path}: {response.StatusCode}: {text}\n{Factory.Errors.Details}");
        var json = JsonNode.Parse(text)!;
        Assert.Equal(200, json["statusCode"]!.GetValue<int>());
        var id = json["resultObject"]!["id"]!.GetValue<int>();
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/{id}", response.Headers.Location.ToString());
        return id;
    }

    protected static JsonNode Result(JsonNode? node) => node!["resultObject"]!;
    protected static JsonArray Items(JsonNode? node) => Result(node)["items"]!.AsArray();
    protected static decimal Money(JsonNode node, string field) => node[field]!.GetValue<decimal>();

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        Anonymous?.Dispose();
        if (Factory != null) await Factory.DisposeAsync();
        await Database.DisposeAsync();
    }
}
