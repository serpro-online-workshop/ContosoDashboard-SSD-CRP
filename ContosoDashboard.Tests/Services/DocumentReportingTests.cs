using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Tests.Services;

public class DocumentReportingTests
{
    [Fact]
    public async Task DashboardShowsOnlyCurrentUsersCleanDocumentsAndFiveMostRecent()
    {
        await using var harness = await ReportingHarness.CreateAsync();
        var now = DateTime.UtcNow;
        for (var index = 0; index < 6; index++)
        {
            harness.Context.Documents.Add(CreateDocument(4, $"Personal {index}", $"4/personal/{index}.pdf", DocumentSafetyStatus.Clean, now.AddMinutes(index)));
        }
        harness.Context.Documents.Add(CreateDocument(2, "Other user", "2/personal/other.pdf", DocumentSafetyStatus.Clean, now));
        harness.Context.Documents.Add(CreateDocument(4, "Rejected", "4/personal/rejected.pdf", DocumentSafetyStatus.Rejected, now));
        await harness.Context.SaveChangesAsync();

        var summary = await new DashboardService(harness.Context).GetDashboardSummaryAsync(4);

        Assert.Equal(6, summary.DocumentCount);
        Assert.Equal(5, summary.RecentDocuments.Count);
        Assert.Equal("Personal 5", summary.RecentDocuments[0].Title);
        Assert.All(summary.RecentDocuments, document => Assert.Equal(4, document.UploaderId));
    }

    [Fact]
    public async Task AdministratorReportGroupsUploadsAndAccessAndDeniesOtherRoles()
    {
        await using var harness = await ReportingHarness.CreateAsync();
        var document = CreateDocument(4, "A report", "4/personal/report.pdf", DocumentSafetyStatus.Clean, DateTime.UtcNow);
        harness.Context.Documents.Add(document);
        harness.Context.DocumentActivities.Add(new DocumentActivity
        {
            Document = document,
            ActorUserId = 4,
            ActivityType = DocumentActivityType.Download,
            OccurredAtUtc = DateTime.UtcNow
        });
        await harness.Context.SaveChangesAsync();
        var service = new DocumentReportService(harness.Context);

        var report = await service.GetAdminReportAsync(1);

        Assert.Equal(1, report.FileTypeCounts["pdf"]);
        Assert.Equal(1, report.UploadsByUploader[4]);
        Assert.Equal(1, report.ActivityCounts[DocumentActivityType.Download]);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetAdminReportAsync(4));
    }

    private static Document CreateDocument(int uploaderId, string title, string key, DocumentSafetyStatus status, DateTime uploadedAtUtc) => new()
    {
        Title = title,
        Category = "Reports",
        OriginalFileName = Path.GetFileName(key),
        StorageKey = key,
        FileSize = 12,
        ContentType = "application/pdf",
        UploadedAtUtc = uploadedAtUtc,
        UploaderId = uploaderId,
        SafetyStatus = status
    };

    private sealed class ReportingHarness : IAsyncDisposable
    {
        private ReportingHarness(SqliteConnection connection, ApplicationDbContext context)
        {
            Connection = connection;
            Context = context;
        }

        public SqliteConnection Connection { get; }
        public ApplicationDbContext Context { get; }

        public static async Task<ReportingHarness> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return new ReportingHarness(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}