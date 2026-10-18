using ContosoDashboard.Data;
using ContosoDashboard.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Tests.Data;

public class DocumentSchemaInitializerTests
{
    [Fact]
    public async Task InitializerAddsDocumentTablesIdempotentlyAndPreservesLegacyUsers()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "CREATE TABLE Users (UserId INTEGER PRIMARY KEY, DisplayName TEXT NOT NULL); INSERT INTO Users (UserId, DisplayName) VALUES (77, 'Existing User');";
            await command.ExecuteNonQueryAsync();
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new ApplicationDbContext(options);
        var initializer = new DocumentSchemaInitializer(context);

        await initializer.InitializeAsync();
        await initializer.InitializeAsync();

        Assert.Equal("Existing User", await context.Database.SqlQueryRaw<string>("SELECT DisplayName AS Value FROM Users WHERE UserId = 77").SingleAsync());
        Assert.True(await TableExistsAsync(connection, "Documents"));
        Assert.True(await TableExistsAsync(connection, "DocumentTags"));
        Assert.True(await TableExistsAsync(connection, "DocumentShares"));
        Assert.True(await TableExistsAsync(connection, "DocumentActivities"));
    }

    private static async Task<bool> TableExistsAsync(SqliteConnection connection, string tableName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";
        command.Parameters.AddWithValue("$name", tableName);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }
}