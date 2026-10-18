namespace ContosoDashboard.Services;

public sealed record StagedFile(string StagingPath, long Size);

public interface IFileStorageService
{
    Task<StagedFile> StageAsync(Stream content, long maxFileSize, CancellationToken cancellationToken = default);
    Task<string> CommitAsync(StagedFile stagedFile, int uploaderId, int? projectId, string extension, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DiscardAsync(StagedFile stagedFile, CancellationToken cancellationToken = default);
}

public sealed class DocumentStorageOptions
{
    public string RootPath { get; set; } = "AppData/uploads";
}