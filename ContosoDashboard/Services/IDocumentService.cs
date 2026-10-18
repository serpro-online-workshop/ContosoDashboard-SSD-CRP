using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public sealed class DocumentUploadRequest
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Category { get; init; } = string.Empty;
    public string OriginalFileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long FileSize { get; set; }
    public Stream Content { get; init; } = Stream.Null;
    public int? ProjectId { get; init; }
    public int? TaskId { get; init; }
    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();
}

public sealed class DocumentMetadataUpdate
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Category { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();
}

public sealed class DocumentReplacementRequest
{
    public string OriginalFileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public Stream Content { get; init; } = Stream.Null;
}

public sealed class DocumentShareRequest
{
    public int? RecipientUserId { get; init; }
    public string? RecipientDepartment { get; init; }
}

public sealed record DocumentUploadResult(Document? Document, string? Error);

public enum DocumentSortField
{
    Title,
    UploadedAtUtc,
    Category,
    FileSize
}

public sealed record DocumentSearchQuery
{
    public string? SearchText { get; init; }
    public string? Category { get; init; }
    public int? ProjectId { get; init; }
    public string? Tag { get; init; }
    public DateTime? UploadedFromUtc { get; init; }
    public DateTime? UploadedToUtc { get; init; }
    public DocumentSortField SortBy { get; init; } = DocumentSortField.UploadedAtUtc;
    public bool Descending { get; init; } = true;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public sealed record DocumentSearchPage(IReadOnlyList<Document> Items, int TotalCount, int Page, int PageSize)
{
    public int PageCount => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public interface IDocumentService
{
    Task<Document> CreateAsync(DocumentUploadRequest request, int requestingUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentUploadResult>> CreateManyAsync(IEnumerable<DocumentUploadRequest> requests, int requestingUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Document>> GetPersonalDocumentsAsync(int requestingUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Document>> GetSharedWithMeAsync(int requestingUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Document>> GetTaskDocumentsAsync(int taskId, int requestingUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentShare>> GetManageableSharesAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default);
    Task<DocumentSearchPage> SearchAsync(DocumentSearchQuery query, int requestingUserId, CancellationToken cancellationToken = default);
    Task<Document?> GetAccessibleAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default);
    Task<Document?> UpdateMetadataAsync(int documentId, DocumentMetadataUpdate update, int requestingUserId, CancellationToken cancellationToken = default);
    Task<Document?> ReplaceFileAsync(int documentId, DocumentReplacementRequest replacement, int requestingUserId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int documentId, int requestingUserId, bool confirmed, CancellationToken cancellationToken = default);
    Task<DocumentShare> ShareAsync(int documentId, DocumentShareRequest request, int requestingUserId, CancellationToken cancellationToken = default);
    Task<bool> RevokeShareAsync(int documentShareId, int requestingUserId, CancellationToken cancellationToken = default);
    Task RecordActivityAsync(int documentId, int requestingUserId, DocumentActivityType activityType, CancellationToken cancellationToken = default);
}