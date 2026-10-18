using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace ContosoDashboard.Tests.Services;

public class DocumentSearchTests
{
    [Fact]
    public async Task SearchFiltersAndPaginatesOnlyDocumentsTheUserCanAccess()
    {
        await using var harness = await SearchHarness.CreateAsync();
        var now = DateTime.UtcNow;
        harness.Context.Documents.AddRange(
            CreateDocument(4, "Budget Alpha", "4/personal/alpha.pdf", DocumentSafetyStatus.Clean, "Reports", now.AddDays(-2), "finance"),
            CreateDocument(4, "Budget Beta", "4/personal/beta.pdf", DocumentSafetyStatus.Clean, "Reports", now.AddDays(-1), "finance"),
            CreateDocument(2, "Budget Private", "2/personal/private.pdf", DocumentSafetyStatus.Clean, "Reports", now, "finance"),
            CreateDocument(4, "Budget Rejected", "4/personal/rejected.pdf", DocumentSafetyStatus.Rejected, "Reports", now, "finance"),
            CreateDocument(4, "Budget Other", "4/personal/other.pdf", DocumentSafetyStatus.Clean, "Other", now, "finance"),
            CreateDocument(4, "Budget Untagged", "4/personal/untagged.pdf", DocumentSafetyStatus.Clean, "Reports", now, "operations"));
        await harness.Context.SaveChangesAsync();

        var query = new DocumentSearchQuery
        {
            SearchText = "budget",
            Category = "Reports",
            Tag = "finance",
            UploadedFromUtc = now.AddDays(-3),
            UploadedToUtc = now,
            SortBy = DocumentSortField.Title,
            Descending = false,
            Page = 1,
            PageSize = 1
        };

        var firstPage = await harness.Service.SearchAsync(query, 4);
        var secondPage = await harness.Service.SearchAsync(query with { Page = 2 }, 4);

        Assert.Equal(2, firstPage.TotalCount);
        Assert.Equal("Budget Alpha", Assert.Single(firstPage.Items).Title);
        Assert.Equal(2, firstPage.PageCount);
        Assert.Equal("Budget Beta", Assert.Single(secondPage.Items).Title);
        Assert.Equal(2, secondPage.TotalCount);
    }

    [Fact]
    public async Task SearchForFiveHundredAccessibleDocumentsCompletesWithinTwoSeconds()
    {
        await using var harness = await SearchHarness.CreateAsync();
        var uploadedAtUtc = DateTime.UtcNow;
        for (var index = 0; index < 500; index++)
        {
            harness.Context.Documents.Add(CreateDocument(4, $"Document {index:D3}", $"4/personal/{index:D3}.pdf", DocumentSafetyStatus.Clean, "Reports", uploadedAtUtc, "benchmark"));
        }
        await harness.Context.SaveChangesAsync();

        var timer = Stopwatch.StartNew();
        var result = await harness.Service.SearchAsync(new DocumentSearchQuery { PageSize = 500 }, 4);
        timer.Stop();

        Assert.Equal(500, result.TotalCount);
        Assert.Equal(500, result.Items.Count);
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(2), $"500-document search took {timer.Elapsed.TotalMilliseconds:N0} ms.");
    }

    private static Document CreateDocument(int uploaderId, string title, string key, DocumentSafetyStatus safetyStatus, string category, DateTime uploadedAtUtc, string tag)
    {
        var document = new Document
        {
            Title = title,
            Category = category,
            OriginalFileName = Path.GetFileName(key),
            StorageKey = key,
            FileSize = 12,
            ContentType = "application/pdf",
            UploadedAtUtc = uploadedAtUtc,
            UploaderId = uploaderId,
            SafetyStatus = safetyStatus
        };
        var documentTag = new DocumentTag { Value = tag };
        documentTag.Normalize();
        document.Tags.Add(documentTag);
        return document;
    }

    private sealed class SearchHarness : IAsyncDisposable
    {
        private SearchHarness(SqliteConnection connection, ApplicationDbContext context, DocumentService service)
        {
            Connection = connection;
            Context = context;
            Service = service;
        }

        public SqliteConnection Connection { get; }
        public ApplicationDbContext Context { get; }
        public DocumentService Service { get; }

        public static async Task<SearchHarness> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();
            var authorization = new DocumentAuthorizationService(context);
            var service = new DocumentService(context, new SearchStorage(), new SearchScanner(), authorization);
            return new SearchHarness(connection, context, service);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }

    private sealed class SearchStorage : IFileStorageService
    {
        public Task<StagedFile> StageAsync(Stream content, long maxFileSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string> CommitAsync(StagedFile stagedFile, int uploaderId, int? projectId, string extension, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DiscardAsync(StagedFile stagedFile, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class SearchScanner : IFileSafetyScanner
    {
        public Task<FileSafetyScanResult> ScanAsync(string stagedPath, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}