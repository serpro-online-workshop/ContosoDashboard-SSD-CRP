using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class DocumentActivity : IValidatableObject
{
    [Key]
    public int DocumentActivityId { get; set; }

    public int? DocumentId { get; set; }

    [Required]
    public int ActorUserId { get; set; }

    [Required]
    public DocumentActivityType ActivityType { get; set; }

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(256)]
    public string? Details { get; set; }

    [ForeignKey(nameof(DocumentId))]
    public virtual Document? Document { get; set; }

    [ForeignKey(nameof(ActorUserId))]
    public virtual User Actor { get; set; } = null!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (OccurredAtUtc.Kind != DateTimeKind.Utc)
        {
            yield return new ValidationResult("The activity timestamp must be UTC.", [nameof(OccurredAtUtc)]);
        }

        if (!Enum.IsDefined(ActivityType))
        {
            yield return new ValidationResult("The activity type is not supported.", [nameof(ActivityType)]);
        }
    }
}

public enum DocumentActivityType
{
    Upload,
    Download,
    Delete,
    Share,
    RejectedUpload
}