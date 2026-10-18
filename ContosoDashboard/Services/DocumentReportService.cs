using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public sealed record DocumentReportSnapshot(
    IReadOnlyDictionary<string, int> FileTypeCounts,
    IReadOnlyDictionary<int, int> UploadsByUploader,
    IReadOnlyDictionary<DocumentActivityType, int> ActivityCounts);

public sealed class DocumentReportService
{
    private readonly ApplicationDbContext _context;

    public DocumentReportService(ApplicationDbContext context) => _context = context;

    public async Task<DocumentReportSnapshot> GetAdminReportAsync(
        int requestingUserId,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var role = await _context.Users
            .Where(user => user.UserId == requestingUserId)
            .Select(user => (UserRole?)user.Role)
            .SingleOrDefaultAsync(cancellationToken);
        if (role != UserRole.Administrator)
        {
            throw new UnauthorizedAccessException("Administrator access is required for document reports.");
        }

        if (fromUtc.HasValue && toUtc.HasValue && fromUtc.Value > toUtc.Value)
        {
            throw new ArgumentException("The report start date must not be after its end date.");
        }

        var documentsQuery = _context.Documents
            .AsNoTracking()
            .Where(document => document.SafetyStatus == DocumentSafetyStatus.Clean);
        if (fromUtc.HasValue)
        {
            documentsQuery = documentsQuery.Where(document => document.UploadedAtUtc >= fromUtc.Value);
        }
        if (toUtc.HasValue)
        {
            documentsQuery = documentsQuery.Where(document => document.UploadedAtUtc <= toUtc.Value);
        }

        var documents = await documentsQuery
            .Select(document => new { document.UploaderId, document.OriginalFileName })
            .ToListAsync(cancellationToken);
        var fileTypeCounts = documents
            .GroupBy(document => Path.GetExtension(document.OriginalFileName).TrimStart('.').ToLowerInvariant() is { Length: > 0 } extension ? extension : "unknown")
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var uploadsByUploader = documents
            .GroupBy(document => document.UploaderId)
            .ToDictionary(group => group.Key, group => group.Count());

        var activitiesQuery = _context.DocumentActivities.AsNoTracking();
        if (fromUtc.HasValue)
        {
            activitiesQuery = activitiesQuery.Where(activity => activity.OccurredAtUtc >= fromUtc.Value);
        }
        if (toUtc.HasValue)
        {
            activitiesQuery = activitiesQuery.Where(activity => activity.OccurredAtUtc <= toUtc.Value);
        }

        var activities = await activitiesQuery
            .Select(activity => activity.ActivityType)
            .ToListAsync(cancellationToken);
        var activityCounts = activities
            .GroupBy(activityType => activityType)
            .ToDictionary(group => group.Key, group => group.Count());

        return new DocumentReportSnapshot(fileTypeCounts, uploadsByUploader, activityCounts);
    }
}