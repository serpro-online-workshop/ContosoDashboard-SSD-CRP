using System.ComponentModel.DataAnnotations;
using System.Text;
using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Tests.Services;

public class DocumentUploadTests
{
    [Fact]
    public async Task CleanSupportedUploadPersistsMetadataAndSafeStorageKey()
    {
        await using var harness = await UploadHarness.CreateAsync(FileSafetyScanResult.Clean);
        var request = CreateRequest("report.pdf", "application/pdf");

        var document = await harness.Service.CreateAsync(request, 4);

        Assert.Equal("Quarterly report", document.Title);
        Assert.Equal(DocumentSafetyStatus.Clean, document.SafetyStatus);
        Assert.StartsWith("4/personal/", document.StorageKey, StringComparison.Ordinal);
        Assert.Equal(1, await harness.Context.Documents.CountAsync());
        Assert.Equal(1, await harness.Context.DocumentActivities.CountAsync(activity => activity.ActivityType == DocumentActivityType.Upload));
    }

    [Fact]
    public async Task PersonalDocumentsReturnsOnlyDocumentsUploadedByRequestingUser()
    {
        await using var harness = await UploadHarness.CreateAsync(FileSafetyScanResult.Clean);
        harness.Context.Documents.AddRange(
            CreateStoredDocument(4, DocumentSafetyStatus.Clean, "4/personal/own.pdf"),
            CreateStoredDocument(2, DocumentSafetyStatus.Clean, "2/personal/other.pdf"),
            CreateStoredDocument(4, DocumentSafetyStatus.Rejected, "4/personal/rejected.pdf"));
        await harness.Context.SaveChangesAsync();

        var documents = await harness.Service.GetPersonalDocumentsAsync(4);

        var document = Assert.Single(documents);
        Assert.Equal(4, document.UploaderId);
        Assert.Equal(DocumentSafetyStatus.Clean, document.SafetyStatus);
    }

    private static Document CreateStoredDocument(int uploaderId, DocumentSafetyStatus safetyStatus, string storageKey) => new()
    {
        Title = "Quarterly report",
        Category = "Reports",
        OriginalFileName = Path.GetFileName(storageKey),
        StorageKey = storageKey,
        FileSize = 12,
        ContentType = "application/pdf",
        UploadedAtUtc = DateTime.UtcNow,
        UploaderId = uploaderId,
        SafetyStatus = safetyStatus
    };

    [Theory]
    [InlineData("report.exe", "application/octet-stream")]
    [InlineData("report.pdf", "text/plain")]
    public async Task UnsupportedOrMismatchedFileTypeIsRejectedBeforeStaging(string fileName, string contentType)
    {
        await using var harness = await UploadHarness.CreateAsync(FileSafetyScanResult.Clean);

        await Assert.ThrowsAsync<ValidationException>(() => harness.Service.CreateAsync(CreateRequest(fileName, contentType), 4));

        Assert.Equal(0, harness.Storage.StageCount);
        Assert.Equal(0, await harness.Context.Documents.CountAsync());
    }

    [Fact]
    public async Task FileAbove25MbIsRejectedAndNeverStored()
    {
        await using var harness = await UploadHarness.CreateAsync(FileSafetyScanResult.Clean);
        var request = CreateRequest("large.pdf", "application/pdf");
        request.FileSize = 25L * 1024 * 1024 + 1;

        await Assert.ThrowsAsync<ValidationException>(() => harness.Service.CreateAsync(request, 4));

        Assert.Equal(0, harness.Storage.StageCount);
        Assert.Empty(harness.Storage.CommittedKeys);
    }

    [Fact]
    public async Task FileAt25MbLimitIsAccepted()
    {
        await using var harness = await UploadHarness.CreateAsync(FileSafetyScanResult.Clean);
        var content = new MemoryStream(new byte[(int)DocumentService.MaximumFileSize]);
        var request = new DocumentUploadRequest
        {
            Title = "Maximum-size PDF",
            Category = "Reports",
            OriginalFileName = "maximum.pdf",
            ContentType = "application/pdf",
            FileSize = DocumentService.MaximumFileSize,
            Content = content
        };

        var document = await harness.Service.CreateAsync(request, 4);

        Assert.Equal(DocumentService.MaximumFileSize, document.FileSize);
        Assert.Equal(DocumentSafetyStatus.Clean, document.SafetyStatus);
    }

    [Theory]
    [InlineData(FileSafetyScanResult.Unsafe)]
    [InlineData(FileSafetyScanResult.Unavailable)]
    public async Task UnsafeOrUnavailableScanFailsClosedAndDiscardsStagedFile(FileSafetyScanResult result)
    {
        await using var harness = await UploadHarness.CreateAsync(result);

        await Assert.ThrowsAsync<ValidationException>(() => harness.Service.CreateAsync(CreateRequest("report.pdf", "application/pdf"), 4));

        Assert.Equal(1, harness.Storage.DiscardCount);
        Assert.Empty(harness.Storage.CommittedKeys);
        Assert.Equal(0, await harness.Context.Documents.CountAsync());
    }

    [Fact]
    public async Task BatchUploadReportsSuccessAndFailureIndependently()
    {
        await using var harness = await UploadHarness.CreateAsync(FileSafetyScanResult.Clean);
        var requests = new[]
        {
            CreateRequest("report.pdf", "application/pdf"),
            CreateRequest("notes.txt", "text/plain"),
            CreateRequest("script.exe", "application/octet-stream")
        };

        var results = await harness.Service.CreateManyAsync(requests, 4);

        Assert.Equal(3, results.Count);
        Assert.NotNull(results[0].Document);
        Assert.Null(results[0].Error);
        Assert.NotNull(results[1].Document);
        Assert.Null(results[1].Error);
        Assert.Null(results[2].Document);
        Assert.False(string.IsNullOrWhiteSpace(results[2].Error));
        Assert.Equal(2, await harness.Context.Documents.CountAsync());
    }

    private static DocumentUploadRequest CreateRequest(string fileName, string contentType) => new()
    {
        Title = "Quarterly report",
        Category = "Reports",
        OriginalFileName = fileName,
        ContentType = contentType,
        FileSize = Encoding.UTF8.GetByteCount("sample file"),
        Content = new MemoryStream(Encoding.UTF8.GetBytes("sample file")),
        Tags = [" Finance "]
    };

    private sealed class UploadHarness : IAsyncDisposable
    {
        private UploadHarness(SqliteConnection connection, ApplicationDbContext context, FakeStorage storage, FakeScanner scanner, DocumentService service)
        {
            Connection = connection;
            Context = context;
            Storage = storage;
            Scanner = scanner;
            Service = service;
        }

        public SqliteConnection Connection { get; }
        public ApplicationDbContext Context { get; }
        public FakeStorage Storage { get; }
        public FakeScanner Scanner { get; }
        public DocumentService Service { get; }

        public static async Task<UploadHarness> CreateAsync(FileSafetyScanResult scanResult)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();
            var storage = new FakeStorage();
            var scanner = new FakeScanner(scanResult);
            var authorization = new DocumentAuthorizationService(context);
            var service = new DocumentService(context, storage, scanner, authorization);
            return new UploadHarness(connection, context, storage, scanner, service);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }

    private sealed class FakeStorage : IFileStorageService
    {
        public int StageCount { get; private set; }
        public int DiscardCount { get; private set; }
        public List<string> CommittedKeys { get; } = new();

        public async Task<StagedFile> StageAsync(Stream content, long maxFileSize, CancellationToken cancellationToken = default)
        {
            StageCount++;
            var memory = new MemoryStream();
            await content.CopyToAsync(memory, cancellationToken);
            return new StagedFile($"stage/{Guid.NewGuid():N}", memory.Length);
        }

        public Task<string> CommitAsync(StagedFile stagedFile, int uploaderId, int? projectId, string extension, CancellationToken cancellationToken = default)
        {
            var key = $"{uploaderId}/{projectId?.ToString() ?? "personal"}/{Guid.NewGuid():N}{extension}";
            CommittedKeys.Add(key);
            return Task.FromResult(key);
        }

        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream());

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DiscardAsync(StagedFile stagedFile, CancellationToken cancellationToken = default)
        {
            DiscardCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeScanner : IFileSafetyScanner
    {
        private readonly FileSafetyScanResult _result;

        public FakeScanner(FileSafetyScanResult result) => _result = result;

        public Task<FileSafetyScanResult> ScanAsync(string stagedPath, CancellationToken cancellationToken = default) => Task.FromResult(_result);
    }
}