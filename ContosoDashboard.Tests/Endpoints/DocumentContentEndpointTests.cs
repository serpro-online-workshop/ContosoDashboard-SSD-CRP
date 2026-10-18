using System.Diagnostics;
using System.Security.Claims;
using ContosoDashboard.Endpoints;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ContosoDashboard.Tests.Endpoints;

public class DocumentContentEndpointTests
{
    [Fact]
    public async Task AuthorizedCleanPdfCanBeStreamedInline()
    {
        var document = CreateDocument("application/pdf");
        var documentService = new FakeDocumentService(document);
        var storage = new FakeStorage();
        var result = await DocumentContentEndpoints.GetContentAsync(CreateUser(4), document.DocumentId, "inline", documentService, storage);
        var streamResult = Assert.IsType<FileStreamHttpResult>(result);

        Assert.Equal("application/pdf", streamResult.ContentType);
        Assert.Equal(3, streamResult.FileStream.Length);
        Assert.Equal(1, storage.OpenCount);
        Assert.Empty(documentService.RecordedActivities);
    }

    [Fact]
    public async Task AuthorizedAttachmentRecordsDownloadActivity()
    {
        var document = CreateDocument("application/pdf");
        var documentService = new FakeDocumentService(document);
        var storage = new FakeStorage();

        var result = await DocumentContentEndpoints.GetContentAsync(CreateUser(4), document.DocumentId, "attachment", documentService, storage);

        Assert.IsType<FileStreamHttpResult>(result);
        Assert.Equal([DocumentActivityType.Download], documentService.RecordedActivities);
    }

    [Fact]
    public async Task AuthorizedLocalPdfPreviewStreamsWithinThreeSeconds()
    {
        var storageRoot = Path.Combine(Path.GetTempPath(), $"document-preview-{Guid.NewGuid():N}");
        var filePath = Path.Combine(storageRoot, "4", "personal", "preview.pdf");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllBytesAsync(filePath, new byte[1024 * 1024]);
        try
        {
            var document = CreateDocument("application/pdf");
            document.StorageKey = "4/personal/preview.pdf";
            var documentService = new FakeDocumentService(document);
            var storage = new LocalPreviewStorage(storageRoot);
            var timer = Stopwatch.StartNew();

            var result = await DocumentContentEndpoints.GetContentAsync(CreateUser(4), document.DocumentId, "inline", documentService, storage);

            timer.Stop();
            var streamResult = Assert.IsType<FileStreamHttpResult>(result);
            Assert.Equal(1024 * 1024, streamResult.FileStream.Length);
            Assert.True(timer.Elapsed < TimeSpan.FromSeconds(3), $"Local PDF preview stream took {timer.Elapsed.TotalMilliseconds:N0} ms.");
            await streamResult.FileStream.DisposeAsync();
        }
        finally
        {
            if (Directory.Exists(storageRoot))
            {
                Directory.Delete(storageRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task InaccessibleDocumentReturnsNotFoundWithoutOpeningStorage()
    {
        var service = new FakeDocumentService(null);
        var storage = new FakeStorage();

        var result = await DocumentContentEndpoints.GetContentAsync(CreateUser(99), 123, "attachment", service, storage);

        Assert.IsType<NotFound<ProblemDetails>>(result);
        Assert.Equal(0, storage.OpenCount);
    }

    [Fact]
    public async Task NonCleanDocumentIsNotStreamed()
    {
        var document = CreateDocument("application/pdf");
        document.SafetyStatus = DocumentSafetyStatus.Pending;
        var service = new FakeDocumentService(document);
        var storage = new FakeStorage();

        var result = await DocumentContentEndpoints.GetContentAsync(CreateUser(4), document.DocumentId, "attachment", service, storage);

        Assert.IsType<NotFound<ProblemDetails>>(result);
        Assert.Equal(0, storage.OpenCount);
    }

    [Fact]
    public async Task UnsupportedInlinePreviewReturns415WithoutOpeningStorage()
    {
        var document = CreateDocument("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        var service = new FakeDocumentService(document);
        var storage = new FakeStorage();

        var result = await DocumentContentEndpoints.GetContentAsync(CreateUser(4), document.DocumentId, "inline", service, storage);

        Assert.IsType<StatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status415UnsupportedMediaType, ((StatusCodeHttpResult)result).StatusCode);
        Assert.Equal(0, storage.OpenCount);
    }

    private static ClaimsPrincipal CreateUser(int userId) => new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Test"));

    private static Document CreateDocument(string contentType) => new()
    {
        DocumentId = 11,
        Title = "Test document",
        Category = "Reports",
        OriginalFileName = "test.pdf",
        StorageKey = "4/personal/test.pdf",
        FileSize = 100,
        ContentType = contentType,
        UploadedAtUtc = DateTime.UtcNow,
        UploaderId = 4,
        SafetyStatus = DocumentSafetyStatus.Clean
    };

    private sealed class FakeDocumentService : IDocumentService
    {
        private readonly Document? _document;
        public List<DocumentActivityType> RecordedActivities { get; } = [];

        public FakeDocumentService(Document? document) => _document = document;

        public Task<Document?> GetAccessibleAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_document?.DocumentId == documentId ? _document : null);

        public Task<Document> CreateAsync(DocumentUploadRequest request, int requestingUserId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<DocumentUploadResult>> CreateManyAsync(IEnumerable<DocumentUploadRequest> requests, int requestingUserId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<Document>> GetPersonalDocumentsAsync(int requestingUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Document>>(Array.Empty<Document>());

        public Task<IReadOnlyList<Document>> GetSharedWithMeAsync(int requestingUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Document>>(Array.Empty<Document>());

        public Task<IReadOnlyList<Document>> GetTaskDocumentsAsync(int taskId, int requestingUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Document>>(Array.Empty<Document>());

        public Task<IReadOnlyList<DocumentShare>> GetManageableSharesAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DocumentShare>>(Array.Empty<DocumentShare>());

        public Task<DocumentSearchPage> SearchAsync(DocumentSearchQuery query, int requestingUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DocumentSearchPage(Array.Empty<Document>(), 0, query.Page, query.PageSize));

        public Task<Document?> UpdateMetadataAsync(int documentId, DocumentMetadataUpdate update, int requestingUserId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Document?> ReplaceFileAsync(int documentId, DocumentReplacementRequest replacement, int requestingUserId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> DeleteAsync(int documentId, int requestingUserId, bool confirmed, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<DocumentShare> ShareAsync(int documentId, DocumentShareRequest request, int requestingUserId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> RevokeShareAsync(int documentShareId, int requestingUserId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task RecordActivityAsync(int documentId, int requestingUserId, DocumentActivityType activityType, CancellationToken cancellationToken = default)
        {
            RecordedActivities.Add(activityType);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeStorage : IFileStorageService
    {
        public int OpenCount { get; private set; }

        public Task<StagedFile> StageAsync(Stream content, long maxFileSize, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<string> CommitAsync(StagedFile stagedFile, int uploaderId, int? projectId, string extension, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            OpenCount++;
            return Task.FromResult<Stream>(new MemoryStream([1, 2, 3]));
        }

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task DiscardAsync(StagedFile stagedFile, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class LocalPreviewStorage(string rootPath) : IFileStorageService
    {
        public Task<StagedFile> StageAsync(Stream content, long maxFileSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string> CommitAsync(StagedFile stagedFile, int uploaderId, int? projectId, string extension, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new FileStream(Path.Combine(rootPath, storageKey.Replace('/', Path.DirectorySeparatorChar)), FileMode.Open, FileAccess.Read, FileShare.Read));
        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DiscardAsync(StagedFile stagedFile, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}