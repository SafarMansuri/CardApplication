using CardApplicationAPI.Data.DBContext;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace CardApplicationAPI.Tests.Infrastructure;

public sealed class LocalDbTestDatabase : IAsyncDisposable
{
    private const string Server = @"(localdb)\MSSQLLocalDB";
    private readonly string name = $"CardApplication_Test_{Guid.NewGuid():N}";
    private bool owned;

    public string Name => name;
    public string ConnectionString => ConnectionFor(name);
    public static string BootstrapSql => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "CreateCardApplicationDatabase.sql"));

    private static string ConnectionFor(string database) => new SqlConnectionStringBuilder
    {
        DataSource = Server,
        InitialCatalog = database,
        IntegratedSecurity = true,
        TrustServerCertificate = true,
        Pooling = database != "master",
        MaxPoolSize = 10,
        ConnectTimeout = 15
    }.ConnectionString;

    public async Task CreateAsync(bool bootstrap = true)
    {
        if (owned) throw new InvalidOperationException("Database already owned.");
        ValidateName();
        await ExecuteOnAsync(ConnectionFor("master"), $"CREATE DATABASE [{name}];");
        owned = true;
        if (bootstrap) await ExecuteAsync(BootstrapSql);
    }

    public AppDbContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlServer(ConnectionString, sql => sql.CommandTimeout(30)).Options);

    public Task ExecuteAsync(string sql) => ExecuteOnAsync(ConnectionString, sql);

    private static async Task ExecuteOnAsync(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
        await command.ExecuteNonQueryAsync();
    }

    public async Task<T> ScalarAsync<T>(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private void ValidateName()
    {
        if (!Regex.IsMatch(name, "^CardApplication_Test_[0-9a-f]{32}$"))
            throw new InvalidOperationException("Unsafe test database name.");
    }

    public async ValueTask DisposeAsync()
    {
        if (!owned) return;
        ValidateName();
        try
        {
            using var pool = new SqlConnection(ConnectionString);
            SqlConnection.ClearPool(pool);
            await ExecuteOnAsync(ConnectionFor("master"),
                $"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}];");
            owned = false;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Cleanup failed for owned database {name}. No other databases were targeted.", exception);
        }
    }
}
