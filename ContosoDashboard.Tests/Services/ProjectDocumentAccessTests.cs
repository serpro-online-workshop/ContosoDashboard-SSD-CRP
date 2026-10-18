using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Tests.Services;

public class ProjectDocumentAccessTests
{
    [Fact]
    public async Task ProjectMemberAndManagerCanViewButRemovedMemberLosesProjectAccess()
    {
        await using var harness = await AccessHarness.CreateAsync();
        var document = new Document
        {
            Title = "Project plan",
            Category = "Project Documents",
            OriginalFileName = "plan.pdf",
            StorageKey = "2/1/plan.pdf",
            FileSize = 12,
            ContentType = "application/pdf",
            UploadedAtUtc = DateTime.UtcNow,
            UploaderId = 2,
            ProjectId = 1,
            SafetyStatus = DocumentSafetyStatus.Clean
        };
        harness.Context.Documents.Add(document);
        await harness.Context.SaveChangesAsync();

        Assert.True(await harness.Authorization.CanViewAsync(document.DocumentId, 4));
        Assert.True(await harness.Authorization.CanViewAsync(document.DocumentId, 2));

        var membership = await harness.Context.ProjectMembers.SingleAsync(member => member.ProjectId == 1 && member.UserId == 4);
        harness.Context.ProjectMembers.Remove(membership);
        await harness.Context.SaveChangesAsync();

        Assert.False(await harness.Authorization.CanViewAsync(document.DocumentId, 4));
        Assert.True(await harness.Authorization.CanViewAsync(document.DocumentId, 2));
    }

    private sealed class AccessHarness : IAsyncDisposable
    {
        private AccessHarness(SqliteConnection connection, ApplicationDbContext context, DocumentAuthorizationService authorization)
        {
            Connection = connection;
            Context = context;
            Authorization = authorization;
        }

        public SqliteConnection Connection { get; }
        public ApplicationDbContext Context { get; }
        public DocumentAuthorizationService Authorization { get; }

        public static async Task<AccessHarness> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return new AccessHarness(connection, context, new DocumentAuthorizationService(context));
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}