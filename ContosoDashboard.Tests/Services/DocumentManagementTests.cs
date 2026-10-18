using System.ComponentModel.DataAnnotations;
using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Tests.Services;

public class DocumentManagementTests
{
    [Fact]
    public async Task OwnerCanUpdateMetadataAndMustConfirmPermanentDeletion()
    {
        await using var harness = await ManagementHarness.CreateAsync(FileSafetyScanResult.Clean);
        var document = await harness.AddDocumentAsync(4);

        Assert.Null(await harness.Service.UpdateMetadataAsync(document.DocumentId, new DocumentMetadataUpdate
        {
            Title = "Unauthorized title",
            Category = "Reports"
        }, 2));
        Assert.Null(await harness.Service.ReplaceFileAsync(document.DocumentId, new DocumentReplacementRequest
        {
            OriginalFileName = "unauthorized.pdf",
            ContentType = "application/pdf",
            FileSize = 1,
            Content = new MemoryStream([7])
        }, 2));
        Assert.False(await harness.Service.DeleteAsync(document.DocumentId, 2, confirmed: true));

        var updated = await harness.Service.UpdateMetadataAsync(document.DocumentId, new DocumentMetadataUpdate
        {
            Title = "Updated title",
            Description = "Updated description",
            Category = "Reports",
            Tags = ["finance", "quarterly"]
        }, 4);

        Assert.Equal("Updated title", updated!.Title);
        Assert.Equal(2, updated.Tags.Count);
        Assert.False(await harness.Service.DeleteAsync(document.DocumentId, 4, confirmed: false));
        Assert.True(await harness.Service.DeleteAsync(document.DocumentId, 4, confirmed: true));
        Assert.Null(await harness.Context.Documents.FindAsync(document.DocumentId));
        Assert.False(harness.Storage.Files.ContainsKey(document.StorageKey));
        Assert.Contains(await harness.Context.DocumentActivities.ToListAsync(), activity => activity.ActivityType == DocumentActivityType.Delete && activity.ActorUserId == 4);
    }

    [Fact]
    public async Task UnsafeReplacementPreservesThePreviousCleanFileAndMetadata()
    {
        await using var harness = await ManagementHarness.CreateAsync(FileSafetyScanResult.Unsafe);
        var document = await harness.AddDocumentAsync(4);

        await Assert.ThrowsAsync<ValidationException>(() => harness.Service.ReplaceFileAsync(document.DocumentId, new DocumentReplacementRequest
        {
            OriginalFileName = "replacement.pdf",
            ContentType = "application/pdf",
            FileSize = 3,
            Content = new MemoryStream([9, 8, 7])
        }, 4));

        Assert.Equal(document.StorageKey, (await harness.Context.Documents.FindAsync(document.DocumentId))!.StorageKey);
        Assert.Equal(DocumentSafetyStatus.Clean, document.SafetyStatus);
        Assert.True(harness.Storage.Files.ContainsKey(document.StorageKey));
        Assert.Empty(harness.Storage.CommittedKeys);
    }

    private sealed class ManagementHarness : IAsyncDisposable
    {
        private ManagementHarness(SqliteConnection connection, ApplicationDbContext context, ManagementStorage storage, DocumentService service)
        {
            Connection = connection;
            Context = context;
            Storage = storage;
            Service = service;
        }

        public SqliteConnection Connection { get; }
        public ApplicationDbContext Context { get; }
        public ManagementStorage Storage { get; }
        public DocumentService Service { get; }

        public static async Task<ManagementHarness> CreateAsync(FileSafetyScanResult scanResult)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();
            var storage = new ManagementStorage();
            var service = new DocumentService(context, storage, new ManagementScanner(scanResult), new DocumentAuthorizationService(context));
            return new ManagementHarness(connection, context, storage, service);
        }

        public async Task<Document> AddDocumentAsync(int uploaderId)
        {
            var document = new Document
            {
                Title = "Original title",
                Category = "Project Documents",
                OriginalFileName = "original.pdf",
                StorageKey = $"{uploaderId}/personal/original.pdf",
                FileSize = 3,
                ContentType = "application/pdf",
                UploadedAtUtc = DateTime.UtcNow,
                UploaderId = uploaderId,
                SafetyStatus = DocumentSafetyStatus.Clean
            };
            Context.Documents.Add(document);
            await Context.SaveChangesAsync();
            Storage.Files[document.StorageKey] = [1, 2, 3];
            return document;
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }

    private sealed class ManagementStorage : IFileStorageService
    {
        private readonly Dictionary<string, byte[]> _stagedFiles = new();
        public Dictionary<string, byte[]> Files { get; } = new();
        public List<string> CommittedKeys { get; } = new();

        public async Task<StagedFile> StageAsync(Stream content, long maxFileSize, CancellationToken cancellationToken = default)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            var path = $"stage/{Guid.NewGuid():N}";
            _stagedFiles[path] = buffer.ToArray();
            return new StagedFile(path, buffer.Length);
        }

        public Task<string> CommitAsync(StagedFile stagedFile, int uploaderId, int? projectId, string extension, CancellationToken cancellationToken = default)
        {
            var key = $"{uploaderId}/{projectId?.ToString() ?? "personal"}/{Guid.NewGuid():N}{extension}";
            Files[key] = _stagedFiles[stagedFile.StagingPath];
            _stagedFiles.Remove(stagedFile.StagingPath);
            CommittedKeys.Add(key);
            return Task.FromResult(key);
        }

        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream(Files[storageKey]));

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            Files.Remove(storageKey);
            return Task.CompletedTask;
        }

        public Task DiscardAsync(StagedFile stagedFile, CancellationToken cancellationToken = default)
        {
            _stagedFiles.Remove(stagedFile.StagingPath);
            return Task.CompletedTask;
        }
    }

    private sealed class ManagementScanner(FileSafetyScanResult scanResult) : IFileSafetyScanner
    {
        public Task<FileSafetyScanResult> ScanAsync(string stagedPath, CancellationToken cancellationToken = default) => Task.FromResult(scanResult);
    }
}