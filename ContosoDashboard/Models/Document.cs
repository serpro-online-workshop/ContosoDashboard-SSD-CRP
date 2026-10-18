using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class Document : IValidatableObject
{
    private static readonly HashSet<string> AllowedCategories = new(StringComparer.Ordinal)
    {
        "Project Documents",
        "Team Resources",
        "Personal Files",
        "Reports",
        "Presentations",
        "Other"
    };

    [Key]
    public int DocumentId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    public string StorageKey { get; set; } = string.Empty;

    [Range(1, 25L * 1024 * 1024)]
    public long FileSize { get; set; }

    [Required]
    [MaxLength(255)]
    public string ContentType { get; set; } = string.Empty;

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;

    [Required]
    public int UploaderId { get; set; }

    public int? ProjectId { get; set; }

    public int? TaskId { get; set; }

    [Required]
    public DocumentSafetyStatus SafetyStatus { get; set; } = DocumentSafetyStatus.Pending;

    [ForeignKey(nameof(UploaderId))]
    public virtual User Uploader { get; set; } = null!;

    [ForeignKey(nameof(ProjectId))]
    public virtual Project? Project { get; set; }

    [ForeignKey(nameof(TaskId))]
    public virtual TaskItem? Task { get; set; }

    public virtual ICollection<DocumentTag> Tags { get; set; } = new List<DocumentTag>();
    public virtual ICollection<DocumentShare> Shares { get; set; } = new List<DocumentShare>();
    public virtual ICollection<DocumentActivity> Activities { get; set; } = new List<DocumentActivity>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            yield return new ValidationResult("A title is required.", [nameof(Title)]);
        }

        if (!AllowedCategories.Contains(Category))
        {
            yield return new ValidationResult("The category is not supported.", [nameof(Category)]);
        }

        if (!IsSafeStorageKey(StorageKey))
        {
            yield return new ValidationResult("The storage key must be a generated relative path.", [nameof(StorageKey)]);
        }

        if (UploadedAtUtc.Kind != DateTimeKind.Utc)
        {
            yield return new ValidationResult("The upload timestamp must be UTC.", [nameof(UploadedAtUtc)]);
        }

        if (!Enum.IsDefined(SafetyStatus))
        {
            yield return new ValidationResult("The safety status is not supported.", [nameof(SafetyStatus)]);
        }
    }

    private static bool IsSafeStorageKey(string? storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || storageKey.Contains(':') || storageKey.Contains('\0'))
        {
            return false;
        }

        var normalized = storageKey.Replace('\\', '/');
        return !normalized.StartsWith('/') &&
            !normalized.Split('/').Any(segment => segment is ".." or "." or "");
    }
}

public enum DocumentSafetyStatus
{
    Pending,
    Clean,
    Rejected
}