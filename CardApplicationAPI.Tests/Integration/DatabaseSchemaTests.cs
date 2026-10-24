using CardApplicationAPI.Data.Entities;
using CardApplicationAPI.Tests.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Net;
using System.Net.Http.Json;

namespace CardApplicationAPI.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class DatabaseSchemaTests
{
    [Fact]
    public async Task Bootstrap_CreatesCurrentModel_AndRejectsRerunWithoutDataLoss()
    {
        await using var database = new LocalDbTestDatabase();
        await database.CreateAsync();
        await using var db = database.CreateContext();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Equal(11, await database.ScalarAsync<int>("SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo')"));
        Assert.Equal(7, await database.ScalarAsync<int>("SELECT COUNT(*) FROM sys.indexes WHERE index_id > 0 AND is_primary_key = 0 AND object_id IN (SELECT object_id FROM sys.tables)"));
        Assert.Equal(10, await database.ScalarAsync<int>("SELECT COUNT(*) FROM sys.check_constraints"));
        Assert.Equal(4, await database.ScalarAsync<int>("SELECT COUNT(*) FROM sys.foreign_keys"));
        var model = db.GetService<IDesignTimeModel>().Model.GetRelationalModel();
        await using var connection = new SqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        foreach (var table in model.Tables)
        {
            await using var command = new SqlCommand("""
                SELECT c.name, t.name, c.max_length, c.precision, c.scale, c.is_nullable, c.is_identity
                FROM sys.columns c JOIN sys.types t ON c.user_type_id = t.user_type_id
                WHERE c.object_id = OBJECT_ID(@table)
                """, connection);
            command.Parameters.AddWithValue("@table", $"dbo.{table.Name}");
            await using var reader = await command.ExecuteReaderAsync();
            var count = 0;
            while (await reader.ReadAsync())
            {
                count++;
                var expected = table.Columns.Single(x => x.Name == reader.GetString(0));
                var type = reader.GetString(1);
                var storeType = type switch
                {
                    "nvarchar" => $"nvarchar({(reader.GetInt16(2) == -1 ? "max" : (reader.GetInt16(2) / 2).ToString())})",
                    "decimal" => $"decimal({reader.GetByte(3)},{reader.GetByte(4)})",
                    _ => type
                };
                Assert.Equal(expected.StoreType, storeType);
                Assert.Equal(expected.IsNullable, reader.GetBoolean(5));
                Assert.Equal(expected.Name == "Id", reader.GetBoolean(6));
            }
            Assert.Equal(table.Columns.Count(), count);
        }
        db.BankAccounts.Add(new BankAccountEntity { AccountName = "Retain me", BankName = "Synthetic" });
        await db.SaveChangesAsync();
        var exception = await Assert.ThrowsAsync<SqlException>(() => database.ExecuteAsync(LocalDbTestDatabase.BootstrapSql));
        Assert.Equal(50003, exception.Number);
        Assert.Equal(1, await db.BankAccounts.CountAsync());
    }

    [Fact]
    public async Task Bootstrap_GuardsCollisionsAndAmbientTransactions_AndRollsBackFailures()
    {
        await using var collision = new LocalDbTestDatabase();
        await collision.CreateAsync(false);
        await collision.ExecuteAsync("CREATE TABLE dbo.Users (Id int); INSERT INTO dbo.Users VALUES (42);");
        var error = await Assert.ThrowsAsync<SqlException>(() => collision.ExecuteAsync(LocalDbTestDatabase.BootstrapSql));
        Assert.Equal(50003, error.Number);
        Assert.Equal(42, await collision.ScalarAsync<int>("SELECT Id FROM dbo.Users"));
        await using var rollback = new LocalDbTestDatabase();
        await rollback.CreateAsync(false);
        error = await Assert.ThrowsAsync<SqlException>(() => rollback.ExecuteAsync("BEGIN TRANSACTION;\n" + LocalDbTestDatabase.BootstrapSql));
        Assert.Equal(50002, error.Number);
        var faultedScript = LocalDbTestDatabase.BootstrapSql.Replace("    COMMIT TRANSACTION;", "    THROW 50099, 'Synthetic failure', 1;");
        error = await Assert.ThrowsAsync<SqlException>(() => rollback.ExecuteAsync(faultedScript));
        Assert.Equal(50099, error.Number);
        Assert.Equal(0, await rollback.ScalarAsync<int>("SELECT COUNT(*) FROM sys.tables"));
    }

    [Fact]
    public async Task ConstraintsAndCascades_AreEnforcedBySqlServer()
    {
        await using var database = new LocalDbTestDatabase();
        await database.CreateAsync();
        await using (var db = database.CreateContext())
        {
            db.CashLedger.Add(new CashLedgerEntity { TransactionType = "INVALID", Amount = 10 });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
        await using (var db = database.CreateContext())
        {
            db.BankAccounts.AddRange(
                new BankAccountEntity { AccountName = "duplicate", BankName = "Bank" },
                new BankAccountEntity { AccountName = "duplicate", BankName = "Bank" });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
        await using (var db = database.CreateContext())
        {
            db.StockItems.Add(new StockItemEntity
            {
                No = "Cascade", Amount = 12.34m,
                PaymentInfo = [new StockPaymentInfoEntity
                {
                    No = "Cascade", Amount = 12.34m, OutstandingAmount = 12.34m,
                    History = [new StockPaymentHistoryEntity { EventType = "Reset", Amount = 12.34m, PerformedByUsername = "test" }]
                }]
            });
            await db.SaveChangesAsync();
        }
        await using (var db = database.CreateContext())
        {
            var stock = await db.StockItems.SingleAsync();
            Assert.Equal(12.34m, stock.Amount);
            db.StockItems.Remove(stock);
            await db.SaveChangesAsync();
            Assert.Empty(await db.StockPaymentInfo.ToListAsync());
            Assert.Empty(await db.StockPaymentHistory.ToListAsync());
        }
    }

    [Fact]
    public async Task Seed_OnlyCreatesInitialUserOnEmptyDatabase()
    {
        await using var database = new LocalDbTestDatabase();
        await database.CreateAsync();
        await using var factory = new CardApplicationFactory(database);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var response = await client.PostAsync("/api/Auth/seed", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var seed = (await response.Content.ReadFromJsonAsync<System.Text.Json.Nodes.JsonObject>())!;
        using var login = await client.PostAsJsonAsync("/api/Auth/login",
            new { username = seed["username"]!.GetValue<string>(), password = seed["defaultPassword"]!.GetValue<string>() });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var repeat = await client.PostAsync("/api/Auth/seed", null);
        Assert.Equal(HttpStatusCode.BadRequest, repeat.StatusCode);
        await using var db = database.CreateContext();
        Assert.Single(await db.Users.ToListAsync());
    }
}
