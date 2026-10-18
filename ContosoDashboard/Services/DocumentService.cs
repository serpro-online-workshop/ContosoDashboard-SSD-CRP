using System.ComponentModel.DataAnnotations;
using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContosoDashboard.Services;

public sealed class DocumentService : IDocumentService
{
    public const long MaximumFileSize = 25L * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, string[]> SupportedTypes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = ["application/pdf"],
        [".doc"] = ["application/msword"],
        [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
        [".xls"] = ["application/vnd.ms-excel"],
        [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
        [".ppt"] = ["application/vnd.ms-powerpoint"],
        [".pptx"] = ["application/vnd.openxmlformats-officedocument.presentationml.presentation"],
        [".txt"] = ["text/plain"],
        [".csv"] = ["text/csv", "application/csv"],
        [".jpg"] = ["image/jpeg"],
        [".jpeg"] = ["image/jpeg"],
        [".png"] = ["image/png"]
    };

    private static readonly HashSet<string> AllowedCategories = new(StringComparer.Ordinal)
    {
        "Project Documents",
        "Team Resources",
        "Personal Files",
        "Reports",
        "Presentations",
        "Other"
    };

    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly IFileSafetyScanner _fileSafetyScanner;
    private readonly IDocumentAuthorizationService _authorization;
    private readonly ILogger<DocumentService> _logger;
    private readonly INotificationService? _notificationService;

    public DocumentService(
        ApplicationDbContext context,
        IFileStorageService fileStorage,
        IFileSafetyScanner fileSafetyScanner,
        IDocumentAuthorizationService authorization,
        ILogger<DocumentService>? logger = null,
        INotificationService? notificationService = null)
    {
        _context = context;
        _fileStorage = fileStorage;
        _fileSafetyScanner = fileSafetyScanner;
        _authorization = authorization;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DocumentService>.Instance;
        _notificationService = notificationService;
    }

    public async Task<Document> CreateAsync(DocumentUploadRequest request, int requestingUserId, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request, requestingUserId);

        var uploaderExists = await _context.Users.AnyAsync(user => user.UserId == requestingUserId, cancellationToken);
        if (!uploaderExists)
        {
            throw new ValidationException("The uploading user was not found.");
        }

        var projectId = request.ProjectId;
        if (request.TaskId.HasValue)
        {
            if (!await _authorization.CanAccessTaskAsync(request.TaskId.Value, requestingUserId))
            {
                throw new ValidationException("The user cannot attach a document to this task.");
            }

            var taskProjectId = await _context.Tasks
                .Where(task => task.TaskId == request.TaskId.Value)
                .Select(task => task.ProjectId)
                .SingleOrDefaultAsync(cancellationToken);
            if (projectId.HasValue && projectId != taskProjectId)
            {
                throw new ValidationException("The task and document project must match.");
            }

            projectId = taskProjectId;
        }

        if (projectId.HasValue && !await _authorization.CanUploadToProjectAsync(projectId.Value, requestingUserId))
        {
            throw new ValidationException("The user cannot upload documents to this project.");
        }

        var extension = Path.GetExtension(request.OriginalFileName).ToLowerInvariant();
        var contentType = request.ContentType.Split(';', 2)[0].Trim();
        if (!SupportedTypes.TryGetValue(extension, out var allowedContentTypes) ||
            !allowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationException("The file type is not supported.");
        }

        var document = new Document
        {
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Category = request.Category,
            OriginalFileName = request.OriginalFileName,
            StorageKey = $"{requestingUserId}/personal/{Guid.NewGuid():N}{extension}",
            FileSize = request.FileSize,
            ContentType = contentType,
            UploadedAtUtc = DateTime.UtcNow,
            UploaderId = requestingUserId,
            ProjectId = projectId,
            TaskId = request.TaskId,
            SafetyStatus = DocumentSafetyStatus.Pending
        };

        ValidateDocument(document);
        AddTags(document, request.Tags);

        StagedFile? stagedFile = null;
        string? committedStorageKey = null;
        try
        {
            stagedFile = await _fileStorage.StageAsync(request.Content, MaximumFileSize, cancellationToken);
            if (stagedFile.Size != request.FileSize)
            {
                throw new ValidationException("The uploaded file size does not match the selected file size.");
            }

            var scanResult = await _fileSafetyScanner.ScanAsync(stagedFile.StagingPath, cancellationToken);
            if (scanResult != FileSafetyScanResult.Clean)
            {
                throw new ValidationException(scanResult == FileSafetyScanResult.Unsafe
                    ? "The file did not pass the safety scan."
                    : "The file could not be scanned and was not stored.");
            }

            committedStorageKey = await _fileStorage.CommitAsync(stagedFile, requestingUserId, projectId, extension, cancellationToken);
            document.StorageKey = committedStorageKey;
            document.FileSize = stagedFile.Size;
            document.SafetyStatus = DocumentSafetyStatus.Clean;
            ValidateDocument(document);

            _context.Documents.Add(document);
            _context.DocumentActivities.Add(new DocumentActivity
            {
                Document = document,
                ActorUserId = requestingUserId,
                ActivityType = DocumentActivityType.Upload,
                OccurredAtUtc = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
            await NotifyProjectMembersAsync(document, requestingUserId, cancellationToken);
            return document;
        }
        catch
        {
            _context.ChangeTracker.Clear();
            try
            {
                if (committedStorageKey != null)
                {
                    await _fileStorage.DeleteAsync(committedStorageKey, cancellationToken);
                }
                else if (stagedFile != null)
                {
                    await _fileStorage.DiscardAsync(stagedFile, cancellationToken);
                }
            }
            catch (Exception cleanupException)
            {
                _logger.LogError(cleanupException, "Failed to clean up staged document data after an upload failure.");
            }

            throw;
        }
    }

    public async Task<IReadOnlyList<DocumentUploadResult>> CreateManyAsync(IEnumerable<DocumentUploadRequest> requests, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var results = new List<DocumentUploadResult>();
        foreach (var request in requests)
        {
            try
            {
                var document = await CreateAsync(request, requestingUserId, cancellationToken);
                results.Add(new DocumentUploadResult(document, null));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception) when (exception is ValidationException or IOException or UnauthorizedAccessException)
            {
                results.Add(new DocumentUploadResult(null, exception.Message));
            }
        }

        return results;
    }

    public async Task<IReadOnlyList<Document>> GetPersonalDocumentsAsync(int requestingUserId, CancellationToken cancellationToken = default)
    {
        if (requestingUserId <= 0)
        {
            return Array.Empty<Document>();
        }

        return await _context.Documents
            .AsNoTracking()
            .Where(document => document.UploaderId == requestingUserId && document.SafetyStatus == DocumentSafetyStatus.Clean)
            .Include(document => document.Project)
            .Include(document => document.Tags)
            .OrderByDescending(document => document.UploadedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetSharedWithMeAsync(int requestingUserId, CancellationToken cancellationToken = default)
    {
        if (requestingUserId <= 0)
        {
            return Array.Empty<Document>();
        }

        var department = await _context.Users
            .Where(user => user.UserId == requestingUserId)
            .Select(user => user.Department)
            .SingleOrDefaultAsync(cancellationToken);
        if (department is null && !await _context.Users.AnyAsync(user => user.UserId == requestingUserId, cancellationToken))
        {
            return Array.Empty<Document>();
        }

        var query = _context.Documents
            .AsNoTracking()
            .Where(document => document.SafetyStatus == DocumentSafetyStatus.Clean && document.Shares.Any(share =>
                share.RecipientUserId == requestingUserId ||
                (department != null && share.RecipientDepartment != null && share.RecipientDepartment.ToLower() == department.ToLower())))
            .Include(document => document.Project)
            .Include(document => document.Tags)
            .OrderByDescending(document => document.UploadedAtUtc);
        var candidates = await query.ToListAsync(cancellationToken);
        var accessibleDocuments = new List<Document>(candidates.Count);
        foreach (var document in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await _authorization.CanViewAsync(document.DocumentId, requestingUserId))
            {
                accessibleDocuments.Add(document);
            }
        }

        return accessibleDocuments;
    }

    public async Task<IReadOnlyList<Document>> GetTaskDocumentsAsync(int taskId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        if (taskId <= 0 || !await _authorization.CanAccessTaskAsync(taskId, requestingUserId))
        {
            return Array.Empty<Document>();
        }

        var candidates = await _context.Documents
            .AsNoTracking()
            .Where(document => document.TaskId == taskId && document.SafetyStatus == DocumentSafetyStatus.Clean)
            .Include(document => document.Project)
            .Include(document => document.Tags)
            .OrderByDescending(document => document.UploadedAtUtc)
            .ToListAsync(cancellationToken);
        var accessibleDocuments = new List<Document>(candidates.Count);
        foreach (var document in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await _authorization.CanViewAsync(document.DocumentId, requestingUserId))
            {
                accessibleDocuments.Add(document);
            }
        }

        return accessibleDocuments;
    }

    public async Task<IReadOnlyList<DocumentShare>> GetManageableSharesAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        if (!await _authorization.CanManageAsync(documentId, requestingUserId))
        {
            return Array.Empty<DocumentShare>();
        }

        return await _context.DocumentShares
            .AsNoTracking()
            .Include(share => share.RecipientUser)
            .Where(share => share.DocumentId == documentId)
            .OrderBy(share => share.RecipientDepartment)
            .ThenBy(share => share.RecipientUser!.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<DocumentSearchPage> SearchAsync(DocumentSearchQuery query, int requestingUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 500);
        if (requestingUserId <= 0)
        {
            return new DocumentSearchPage(Array.Empty<Document>(), 0, page, pageSize);
        }

        var documentsQuery = _context.Documents
            .AsNoTracking()
            .Where(document => document.SafetyStatus == DocumentSafetyStatus.Clean);

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var searchText = query.SearchText.Trim().ToLowerInvariant();
            documentsQuery = documentsQuery.Where(document =>
                document.Title.ToLower().Contains(searchText) ||
                document.OriginalFileName.ToLower().Contains(searchText) ||
                (document.Description != null && document.Description.ToLower().Contains(searchText)) ||
                document.Uploader.DisplayName.ToLower().Contains(searchText) ||
                document.Uploader.Email.ToLower().Contains(searchText) ||
                (document.Project != null && document.Project.Name.ToLower().Contains(searchText)) ||
                document.Tags.Any(tag => tag.Value.ToLower().Contains(searchText)));
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            documentsQuery = documentsQuery.Where(document => document.Category == query.Category.Trim());
        }

        if (query.ProjectId.HasValue)
        {
            documentsQuery = documentsQuery.Where(document => document.ProjectId == query.ProjectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Tag))
        {
            var tag = query.Tag.Trim().ToLowerInvariant();
            documentsQuery = documentsQuery.Where(document => document.Tags.Any(item => item.NormalizedValue == tag));
        }

        if (query.UploadedFromUtc.HasValue)
        {
            documentsQuery = documentsQuery.Where(document => document.UploadedAtUtc >= query.UploadedFromUtc.Value);
        }

        if (query.UploadedToUtc.HasValue)
        {
            documentsQuery = documentsQuery.Where(document => document.UploadedAtUtc <= query.UploadedToUtc.Value);
        }

        var candidates = await documentsQuery
            .Include(document => document.Project)
            .Include(document => document.Tags)
            .ToListAsync(cancellationToken);
        var accessibleDocuments = new List<Document>(candidates.Count);
        foreach (var document in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await _authorization.CanViewAsync(document.DocumentId, requestingUserId))
            {
                accessibleDocuments.Add(document);
            }
        }

        IOrderedEnumerable<Document> orderedDocuments = query.SortBy switch
        {
            DocumentSortField.Title => query.Descending
                ? accessibleDocuments.OrderByDescending(document => document.Title, StringComparer.CurrentCultureIgnoreCase).ThenByDescending(document => document.DocumentId)
                : accessibleDocuments.OrderBy(document => document.Title, StringComparer.CurrentCultureIgnoreCase).ThenBy(document => document.DocumentId),
            DocumentSortField.Category => query.Descending
                ? accessibleDocuments.OrderByDescending(document => document.Category, StringComparer.CurrentCultureIgnoreCase).ThenByDescending(document => document.DocumentId)
                : accessibleDocuments.OrderBy(document => document.Category, StringComparer.CurrentCultureIgnoreCase).ThenBy(document => document.DocumentId),
            DocumentSortField.FileSize => query.Descending
                ? accessibleDocuments.OrderByDescending(document => document.FileSize).ThenByDescending(document => document.DocumentId)
                : accessibleDocuments.OrderBy(document => document.FileSize).ThenBy(document => document.DocumentId),
            _ => query.Descending
                ? accessibleDocuments.OrderByDescending(document => document.UploadedAtUtc).ThenByDescending(document => document.DocumentId)
                : accessibleDocuments.OrderBy(document => document.UploadedAtUtc).ThenBy(document => document.DocumentId)
        };

        var totalCount = accessibleDocuments.Count;
        var items = orderedDocuments.Skip((page - 1) * pageSize).Take(pageSize).ToArray();
        return new DocumentSearchPage(items, totalCount, page, pageSize);
    }

    public async Task<Document?> GetAccessibleAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        if (!await _authorization.CanViewAsync(documentId, requestingUserId))
        {
            return null;
        }

        return await _context.Documents
            .Include(document => document.Uploader)
            .Include(document => document.Project)
            .Include(document => document.Tags)
            .FirstOrDefaultAsync(document => document.DocumentId == documentId && document.SafetyStatus == DocumentSafetyStatus.Clean, cancellationToken);
    }

    public async Task RecordActivityAsync(int documentId, int requestingUserId, DocumentActivityType activityType, CancellationToken cancellationToken = default)
    {
        if (requestingUserId <= 0 || !Enum.IsDefined(activityType))
        {
            throw new ValidationException("The document activity is invalid.");
        }

        if (!await _context.Users.AnyAsync(user => user.UserId == requestingUserId, cancellationToken) ||
            !await _context.Documents.AnyAsync(document => document.DocumentId == documentId, cancellationToken))
        {
            throw new ValidationException("The activity actor or document was not found.");
        }

        _context.DocumentActivities.Add(new DocumentActivity
        {
            DocumentId = documentId,
            ActorUserId = requestingUserId,
            ActivityType = activityType,
            OccurredAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Document?> UpdateMetadataAsync(int documentId, DocumentMetadataUpdate update, int requestingUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        if (!await _authorization.CanManageAsync(documentId, requestingUserId))
        {
            return null;
        }

        var document = await _context.Documents
            .Include(item => item.Tags)
            .FirstOrDefaultAsync(item => item.DocumentId == documentId && item.SafetyStatus == DocumentSafetyStatus.Clean, cancellationToken);
        if (document is null)
        {
            return null;
        }

        document.Title = update.Title.Trim();
        document.Description = string.IsNullOrWhiteSpace(update.Description) ? null : update.Description.Trim();
        document.Category = update.Category;
        document.Tags.Clear();
        AddTags(document, update.Tags);
        ValidateDocument(document);
        await _context.SaveChangesAsync(cancellationToken);
        return document;
    }

    public async Task<Document?> ReplaceFileAsync(int documentId, DocumentReplacementRequest replacement, int requestingUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        if (!await _authorization.CanManageAsync(documentId, requestingUserId))
        {
            return null;
        }

        var document = await _context.Documents
            .FirstOrDefaultAsync(item => item.DocumentId == documentId && item.SafetyStatus == DocumentSafetyStatus.Clean, cancellationToken);
        if (document is null)
        {
            return null;
        }

        var extension = GetValidatedFileExtension(replacement.OriginalFileName, replacement.ContentType, replacement.FileSize, replacement.Content);
        var oldStorageKey = document.StorageKey;
        var oldFileName = document.OriginalFileName;
        var oldContentType = document.ContentType;
        var oldFileSize = document.FileSize;
        StagedFile? stagedFile = null;
        string? replacementStorageKey = null;
        var persisted = false;

        try
        {
            stagedFile = await _fileStorage.StageAsync(replacement.Content, MaximumFileSize, cancellationToken);
            if (stagedFile.Size != replacement.FileSize)
            {
                throw new ValidationException("The uploaded file size does not match the selected file size.");
            }

            var scanResult = await _fileSafetyScanner.ScanAsync(stagedFile.StagingPath, cancellationToken);
            if (scanResult != FileSafetyScanResult.Clean)
            {
                throw new ValidationException(scanResult == FileSafetyScanResult.Unsafe
                    ? "The file did not pass the safety scan."
                    : "The file could not be scanned and was not stored.");
            }

            replacementStorageKey = await _fileStorage.CommitAsync(stagedFile, document.UploaderId, document.ProjectId, extension, cancellationToken);
            document.StorageKey = replacementStorageKey;
            document.OriginalFileName = Path.GetFileName(replacement.OriginalFileName.Replace('\\', '/'));
            document.ContentType = replacement.ContentType.Split(';', 2)[0].Trim();
            document.FileSize = stagedFile.Size;
            document.SafetyStatus = DocumentSafetyStatus.Clean;
            ValidateDocument(document);
            await _context.SaveChangesAsync(cancellationToken);
            persisted = true;
        }
        catch
        {
            if (!persisted && replacementStorageKey is not null)
            {
                document.StorageKey = oldStorageKey;
                document.OriginalFileName = oldFileName;
                document.ContentType = oldContentType;
                document.FileSize = oldFileSize;
                document.SafetyStatus = DocumentSafetyStatus.Clean;
                _context.Entry(document).State = EntityState.Unchanged;
            }

            try
            {
                if (replacementStorageKey is not null)
                {
                    await _fileStorage.DeleteAsync(replacementStorageKey, cancellationToken);
                }
                else if (stagedFile is not null)
                {
                    await _fileStorage.DiscardAsync(stagedFile, cancellationToken);
                }
            }
            catch (Exception cleanupException)
            {
                _logger.LogError(cleanupException, "Failed to clean up replacement document data after a replacement failure.");
            }

            throw;
        }

        try
        {
            await _fileStorage.DeleteAsync(oldStorageKey, cancellationToken);
        }
        catch (Exception cleanupException)
        {
            _logger.LogError(cleanupException, "Failed to remove the previous document file after a successful replacement.");
        }

        return document;
    }

    public async Task<bool> DeleteAsync(int documentId, int requestingUserId, bool confirmed, CancellationToken cancellationToken = default)
    {
        if (!confirmed || !await _authorization.CanManageAsync(documentId, requestingUserId))
        {
            return false;
        }

        var document = await _context.Documents
            .FirstOrDefaultAsync(item => item.DocumentId == documentId && item.SafetyStatus == DocumentSafetyStatus.Clean, cancellationToken);
        if (document is null)
        {
            return false;
        }

        var storageKey = document.StorageKey;
        _context.DocumentActivities.Add(new DocumentActivity
        {
            DocumentId = document.DocumentId,
            ActorUserId = requestingUserId,
            ActivityType = DocumentActivityType.Delete,
            OccurredAtUtc = DateTime.UtcNow
        });
        _context.Documents.Remove(document);
        await _context.SaveChangesAsync(cancellationToken);
        try
        {
            await _fileStorage.DeleteAsync(storageKey, cancellationToken);
        }
        catch (Exception cleanupException)
        {
            _logger.LogError(cleanupException, "Failed to remove a document file after deleting its metadata.");
        }

        return true;
    }

    public async Task<DocumentShare> ShareAsync(int documentId, DocumentShareRequest request, int requestingUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!await _authorization.CanManageAsync(documentId, requestingUserId))
        {
            throw new UnauthorizedAccessException("The user cannot share this document.");
        }

        var documentExists = await _context.Documents.AnyAsync(document =>
            document.DocumentId == documentId && document.SafetyStatus == DocumentSafetyStatus.Clean, cancellationToken);
        if (!documentExists)
        {
            throw new KeyNotFoundException("The document was not found.");
        }

        var recipientDepartment = string.IsNullOrWhiteSpace(request.RecipientDepartment) ? null : request.RecipientDepartment.Trim();
        if (request.RecipientUserId.HasValue == (recipientDepartment is not null))
        {
            throw new ValidationException("Specify exactly one share recipient.");
        }

        if (request.RecipientUserId is <= 0 || (request.RecipientUserId.HasValue && !await _context.Users.AnyAsync(user => user.UserId == request.RecipientUserId.Value, cancellationToken)))
        {
            throw new ValidationException("The share recipient was not found.");
        }

        if (recipientDepartment is not null)
        {
            if (recipientDepartment.Length > 100 || !await _context.Users.AnyAsync(user => user.Department != null && user.Department.ToLower() == recipientDepartment.ToLower(), cancellationToken))
            {
                throw new ValidationException("The share department was not found or is invalid.");
            }
        }

        var duplicateShare = await _context.DocumentShares.AnyAsync(share =>
            share.DocumentId == documentId &&
            (request.RecipientUserId.HasValue
                ? share.RecipientUserId == request.RecipientUserId
                : share.RecipientDepartment != null && share.RecipientDepartment.ToLower() == recipientDepartment!.ToLower()), cancellationToken);
        if (duplicateShare)
        {
            throw new ValidationException("This document is already shared with the recipient.");
        }

        var share = new DocumentShare
        {
            DocumentId = documentId,
            SharedByUserId = requestingUserId,
            RecipientUserId = request.RecipientUserId,
            RecipientDepartment = recipientDepartment,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.DocumentShares.Add(share);
        await _context.SaveChangesAsync(cancellationToken);
        _context.DocumentActivities.Add(new DocumentActivity
        {
            DocumentId = documentId,
            ActorUserId = requestingUserId,
            ActivityType = DocumentActivityType.Share,
            OccurredAtUtc = DateTime.UtcNow,
            Details = request.RecipientUserId.HasValue ? "Direct share" : "Department share"
        });
        await _context.SaveChangesAsync(cancellationToken);
        if (_notificationService is not null)
        {
            var recipientIds = request.RecipientUserId.HasValue
                ? [request.RecipientUserId.Value]
                : await _context.Users
                    .Where(user => user.Department != null && user.Department.ToLower() == recipientDepartment!.ToLower())
                    .Select(user => user.UserId)
                    .ToListAsync(cancellationToken);
            var documentTitle = await _context.Documents
                .Where(item => item.DocumentId == documentId)
                .Select(item => item.Title)
                .SingleAsync(cancellationToken);
            foreach (var recipientId in recipientIds.Distinct().Where(id => id != requestingUserId))
            {
                try
                {
                    await _notificationService.CreateDocumentShareNotificationAsync(recipientId, documentTitle);
                }
                catch (Exception notificationException)
                {
                    _logger.LogWarning(notificationException, "Failed to create a document share notification.");
                }
            }
        }

        return share;
    }

    public async Task<bool> RevokeShareAsync(int documentShareId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var share = await _context.DocumentShares.FirstOrDefaultAsync(item => item.DocumentShareId == documentShareId, cancellationToken);
        if (share is null || !await _authorization.CanManageAsync(share.DocumentId, requestingUserId))
        {
            return false;
        }

        _context.DocumentShares.Remove(share);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ValidateRequest(DocumentUploadRequest request, int requestingUserId)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (requestingUserId <= 0 || request.Content == Stream.Null || !request.Content.CanRead)
        {
            throw new ValidationException("The upload request is invalid.");
        }

        if (request.FileSize <= 0 || request.FileSize > MaximumFileSize)
        {
            throw new ValidationException("The file exceeds the 25 MB limit or is empty.");
        }

        if (string.IsNullOrWhiteSpace(request.OriginalFileName) || request.OriginalFileName.Length > 255)
        {
            throw new ValidationException("The original file name is invalid.");
        }
    }

    private static void ValidateDocument(Document document)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(document, new ValidationContext(document), results, validateAllProperties: true))
        {
            throw new ValidationException(results[0].ErrorMessage ?? "The document metadata is invalid.");
        }
    }

    private static string GetValidatedFileExtension(string fileName, string contentType, long fileSize, Stream content)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 255 || content == Stream.Null || !content.CanRead || fileSize <= 0 || fileSize > MaximumFileSize)
        {
            throw new ValidationException("The replacement file metadata is invalid.");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var normalizedContentType = contentType.Split(';', 2)[0].Trim();
        if (!SupportedTypes.TryGetValue(extension, out var allowedTypes) || !allowedTypes.Contains(normalizedContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationException("The file type is not supported.");
        }

        return extension;
    }

    private static void AddTags(Document document, IEnumerable<string> tags)
    {
        var normalizedValues = new HashSet<string>(StringComparer.Ordinal);
        foreach (var rawTag in tags)
        {
            var tag = new DocumentTag { Value = rawTag };
            tag.Normalize();
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(tag, new ValidationContext(tag), results, validateAllProperties: true))
            {
                throw new ValidationException(results[0].ErrorMessage ?? "A document tag is invalid.");
            }

            if (!normalizedValues.Add(tag.NormalizedValue))
            {
                throw new ValidationException("Document tags must be unique.");
            }

            document.Tags.Add(tag);
        }
    }

    private async Task NotifyProjectMembersAsync(Document document, int uploaderId, CancellationToken cancellationToken)
    {
        if (_notificationService is null || !document.ProjectId.HasValue)
        {
            return;
        }

        var projectId = document.ProjectId.Value;
        var recipientIds = await _context.ProjectMembers
            .Where(member => member.ProjectId == projectId)
            .Select(member => member.UserId)
            .ToListAsync(cancellationToken);
        var managerId = await _context.Projects
            .Where(project => project.ProjectId == projectId)
            .Select(project => project.ProjectManagerId)
            .SingleOrDefaultAsync(cancellationToken);
        if (managerId > 0)
        {
            recipientIds.Add(managerId);
        }

        foreach (var recipientId in recipientIds.Distinct().Where(id => id != uploaderId))
        {
            try
            {
                await _notificationService.CreateProjectDocumentUploadNotificationAsync(recipientId, document.Title);
            }
            catch (Exception notificationException)
            {
                _logger.LogWarning(notificationException, "Failed to create a project document notification.");
            }
        }
    }
}