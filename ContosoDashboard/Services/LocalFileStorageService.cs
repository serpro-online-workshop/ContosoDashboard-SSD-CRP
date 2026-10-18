using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace ContosoDashboard.Services;

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _storageRoot;
    private readonly string _stagingRoot;

    public LocalFileStorageService(IOptions<DocumentStorageOptions> options, IWebHostEnvironment environment)
    {
        var configuredRoot = options.Value.RootPath;
        _storageRoot = Path.GetFullPath(Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.Combine(environment.ContentRootPath, configuredRoot));
        if (!string.IsNullOrWhiteSpace(environment.WebRootPath))
        {
            var webRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(environment.WebRootPath));
            var storageRoot = Path.TrimEndingDirectorySeparator(_storageRoot);
            var webRootPrefix = Path.EndsInDirectorySeparator(webRoot)
                ? webRoot
                : webRoot + Path.DirectorySeparatorChar;
            if (storageRoot.Equals(webRoot, PathComparison) || storageRoot.StartsWith(webRootPrefix, PathComparison))
            {
                throw new ArgumentException("Document storage must be outside the web root.", nameof(options));
            }
        }

        _stagingRoot = Path.Combine(_storageRoot, ".staging");
    }

    public async Task<StagedFile> StageAsync(Stream content, long maxFileSize, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!content.CanRead || maxFileSize <= 0)
        {
            throw new ValidationException("The upload stream or size limit is invalid.");
        }

        Directory.CreateDirectory(_stagingRoot);
        var stagingPath = Path.Combine(_stagingRoot, $"{Guid.NewGuid():N}.stage");
        var buffer = new byte[81920];
        long totalBytes = 0;

        try
        {
            await using var output = new FileStream(stagingPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, buffer.Length, FileOptions.Asynchronous | FileOptions.SequentialScan);
            int bytesRead;
            while ((bytesRead = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                totalBytes += bytesRead;
                if (totalBytes > maxFileSize)
                {
                    throw new ValidationException("The file exceeds the maximum allowed size.");
                }

                await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            }

            if (totalBytes == 0)
            {
                throw new ValidationException("The file is empty.");
            }

            return new StagedFile(stagingPath, totalBytes);
        }
        catch
        {
            if (File.Exists(stagingPath))
            {
                File.Delete(stagingPath);
            }

            throw;
        }
    }

    public Task<string> CommitAsync(StagedFile stagedFile, int uploaderId, int? projectId, string extension, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (uploaderId <= 0 || (projectId.HasValue && projectId.Value <= 0) ||
            string.IsNullOrWhiteSpace(extension) || extension.Length > 12 ||
            extension[0] != '.' || extension.Skip(1).Any(character => !char.IsAsciiLetterOrDigit(character)))
        {
            throw new ValidationException("The generated file location is invalid.");
        }

        var stagingPath = ResolveStagingPath(stagedFile.StagingPath);
        if (!File.Exists(stagingPath))
        {
            throw new FileNotFoundException("The staged upload no longer exists.");
        }

        var storageKey = $"{uploaderId}/{projectId?.ToString() ?? "personal"}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var destinationPath = ResolveStoragePath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.Move(stagingPath, destinationPath, overwrite: false);
        return Task.FromResult(storageKey);
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolveStoragePath(storageKey);
        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolveStoragePath(storageKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public Task DiscardAsync(StagedFile stagedFile, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolveStagingPath(stagedFile.StagingPath);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string ResolveStoragePath(string storageKey) => ResolveContainedPath(_storageRoot, storageKey);

    private string ResolveStagingPath(string stagingPath)
    {
        var fullPath = Path.GetFullPath(stagingPath);
        var stagingPrefix = Path.TrimEndingDirectorySeparator(_stagingRoot) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(stagingPrefix, PathComparison))
        {
            throw new ValidationException("The staged file path is outside the staging directory.");
        }

        return fullPath;
    }

    private static string ResolveContainedPath(string root, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath) ||
            relativePath.Contains(':') || relativePath.Contains('\0'))
        {
            throw new ValidationException("The storage key is not a safe relative path.");
        }

        var normalized = relativePath.Replace('\\', '/');
        if (normalized.Split('/').Any(segment => segment is "" or "." or ".."))
        {
            throw new ValidationException("The storage key is not a safe relative path.");
        }

        var rootPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var fullPath = Path.GetFullPath(Path.Combine(rootPath, normalized.Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = rootPath + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootPrefix, PathComparison))
        {
            throw new ValidationException("The storage key resolves outside the storage directory.");
        }

        return fullPath;
    }

    private static StringComparison PathComparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;
}