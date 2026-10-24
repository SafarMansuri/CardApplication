using CardApplicationAPI.Helpers;
using CardApplicationAPI.Tests.Infrastructure;
using CardApplicationAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using System.Net;

namespace CardApplicationAPI.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class AuthenticationAndUsersTests : ApiTestBase
{
    [Fact]
    public async Task UserServiceRedactsPasswordsAcrossRepeatedEnumeration()
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserService>();
        var result = await service.GetAllAsync(new());
        Assert.All(result.ResultObject!.Items, user => Assert.Equal(string.Empty, user.Password));
        Assert.All(result.ResultObject.Items, user => Assert.Equal(string.Empty, user.Password));
    }

    [Fact]
    public async Task Login_Rotate_RejectReplay_Logout_RejectRevokedToken()
    {
        var login = await Send(HttpMethod.Post, "/api/Auth/login", new { username = "admin", password = Password }, client: Anonymous);
        var access = new JwtSecurityTokenHandler().ReadJwtToken(login!["accessToken"]!.GetValue<string>());
        Assert.Equal(CardApplicationFactory.Issuer, access.Issuer);
        Assert.Contains(CardApplicationFactory.Audience, access.Audiences);
        var old = login["refreshToken"]!.GetValue<string>();
        var rotated = await Send(HttpMethod.Post, "/api/Auth/refresh", new { refreshToken = old }, client: Anonymous);
        var current = rotated!["refreshToken"]!.GetValue<string>();
        Assert.NotEqual(old, current);
        await Send(HttpMethod.Post, "/api/Auth/refresh", new { refreshToken = old }, HttpStatusCode.Unauthorized, Anonymous);
        await Send(HttpMethod.Post, "/api/Auth/logout", new { refreshToken = current }, client: Anonymous);
        await Send(HttpMethod.Post, "/api/Auth/logout", new { refreshToken = current }, HttpStatusCode.BadRequest, Anonymous);
        await Send(HttpMethod.Post, "/api/Auth/refresh", new { refreshToken = current }, HttpStatusCode.Unauthorized, Anonymous);
        await using var db = Database.CreateContext();
        var stored = await db.RefreshTokens.SingleAsync(x => x.Token == old);
        Assert.True(stored.IsRevoked);
        Assert.Equal(current, stored.ReplacedByToken);
        Assert.True((await db.RefreshTokens.SingleAsync(x => x.Token == current)).IsRevoked);
    }

    [Fact]
    public async Task InvalidLoginAndExpiredOrDeletedUserTokens_AreRejected()
    {
        await Send(HttpMethod.Post, "/api/Auth/login", new { username = "missing", password = Password }, HttpStatusCode.Unauthorized, Anonymous);
        await Send(HttpMethod.Post, "/api/Auth/login", new { username = "admin", password = "wrong" }, HttpStatusCode.Unauthorized, Anonymous);
        await Send(HttpMethod.Post, "/api/Auth/login", new { }, HttpStatusCode.Unauthorized, Anonymous);
        await Send(HttpMethod.Post, "/api/Auth/refresh", new { refreshToken = "unknown" }, HttpStatusCode.Unauthorized, Anonymous);
        var login = await Send(HttpMethod.Post, "/api/Auth/login", new { username = "user", password = Password }, client: Anonymous);
        var token = login!["refreshToken"]!.GetValue<string>();
        await using (var db = Database.CreateContext())
        {
            var row = await db.RefreshTokens.SingleAsync(x => x.Token == token);
            row.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }
        await Send(HttpMethod.Post, "/api/Auth/refresh", new { refreshToken = token }, HttpStatusCode.Unauthorized, Anonymous);
        login = await Send(HttpMethod.Post, "/api/Auth/login", new { username = "user", password = Password }, client: Anonymous);
        token = login!["refreshToken"]!.GetValue<string>();
        var id = login["user"]!["id"]!.GetValue<int>();
        await Send(HttpMethod.Delete, $"/api/Users/{id}");
        await Send(HttpMethod.Post, "/api/Auth/refresh", new { refreshToken = token }, HttpStatusCode.Unauthorized, Anonymous);
        await Send(HttpMethod.Post, "/api/Auth/seed", status: HttpStatusCode.BadRequest, client: Anonymous);
    }

    [Fact]
    public async Task UsersCrud_ProtectsPasswords_AndValidatesDuplicates()
    {
        var id = await Create("/api/Users", new { username = "newuser", password = Password, role = "User", fullName = "Synthetic Person" });
        var created = Result(await Send(HttpMethod.Get, $"/api/Users/{id}")).AsObject();
        Assert.False(created.ContainsKey("password"));
        Assert.All(Items(await Send(HttpMethod.Get, "/api/Users")), user => Assert.False(user!.AsObject().ContainsKey("password")));
        await using (var db = Database.CreateContext())
        {
            var user = await db.Users.SingleAsync(x => x.Id == id);
            Assert.NotEqual(Password, user.Password);
            Assert.True(new PasswordHasherHelper().VerifyPassword(user.Password, Password));
        }
        await Send(HttpMethod.Post, "/api/Users", new { username = " NEWUSER ", password = Password, role = "User" }, HttpStatusCode.Conflict);
        await Send(HttpMethod.Post, "/api/Users", new { username = "xx", password = "short", role = "Invalid", phoneNumber = "not-phone" }, HttpStatusCode.BadRequest);
        await Send(HttpMethod.Put, $"/api/Users/{id}", new { id, username = "admin", password = Password, role = "User" }, HttpStatusCode.Conflict);
        await Send(HttpMethod.Put, $"/api/Users/{id}", new { id, username = "renamed", password = Password + "changed", role = "User" });
        await Send(HttpMethod.Post, "/api/Auth/login", new { username = "renamed", password = Password + "changed" }, client: Anonymous);
        await Send(HttpMethod.Delete, $"/api/Users/{id}");
        await Send(HttpMethod.Get, $"/api/Users/{id}", status: HttpStatusCode.NotFound);
        await Send(HttpMethod.Delete, $"/api/Users/{id}", status: HttpStatusCode.NotFound);
        await Send(HttpMethod.Put, $"/api/Users/{id}", new { id, username = "renamed", password = Password, role = "User" }, HttpStatusCode.NotFound);
        await Send(HttpMethod.Put, "/api/Users/999", new { id = 998, username = "renamed", password = Password, role = "User" }, HttpStatusCode.BadRequest);
    }
}
