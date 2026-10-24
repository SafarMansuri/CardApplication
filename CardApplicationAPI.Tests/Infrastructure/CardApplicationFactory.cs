using CardApplicationAPI.Data.DBContext;
using CardApplicationAPI.Models.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace CardApplicationAPI.Tests.Infrastructure;

public sealed class CardApplicationFactory(LocalDbTestDatabase database) : WebApplicationFactory<Program>
{
    public TestErrorLog Errors { get; } = new();
    public string SigningKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    public const string Issuer = "CardApplication.Tests";
    public const string Audience = "CardApplication.Tests.Client";

    private Dictionary<string, string?> Settings => new()
    {
        ["Jwt:Key"] = SigningKey,
        ["Jwt:Issuer"] = Issuer,
        ["Jwt:Audience"] = Audience,
        ["Jwt:AccessExpiryMinutes"] = "15",
        ["Jwt:RefreshExpiryDays"] = "1",
        ["ConnectionStrings:DefaultConnection"] = database.ConnectionString
    };

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(Settings));
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(Settings));
        builder.ConfigureTestServices(services =>
        {
            services.AddLogging(logging => logging.AddProvider(Errors));
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(database.ConnectionString));
            services.PostConfigure<JwtSettings>(settings =>
            {
                settings.Key = SigningKey;
                settings.Issuer = Issuer;
                settings.Audience = Audience;
                settings.AccessExpiryMinutes = 15;
                settings.RefreshExpiryDays = 1;
            });
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.ValidIssuer = Issuer;
                options.TokenValidationParameters.ValidAudience = Audience;
                options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
            });
        });
    }
}
