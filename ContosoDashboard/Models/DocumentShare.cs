using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class DocumentShare : IValidatableObject
{
    [Key]
    public int DocumentShareId { get; set; }

    [Required]
    public int DocumentId { get; set; }

    [Required]
    public int SharedByUserId { get; set; }

    public int? RecipientUserId { get; set; }

    [MaxLength(100)]
    public string? RecipientDepartment { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(DocumentId))]
    public virtual Document Document { get; set; } = null!;

    [ForeignKey(nameof(SharedByUserId))]
    public virtual User SharedByUser { get; set; } = null!;

    [ForeignKey(nameof(RecipientUserId))]
    public virtual User? RecipientUser { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hasUser = RecipientUserId.HasValue;
        var hasDepartment = !string.IsNullOrWhiteSpace(RecipientDepartment);
        if (hasUser == hasDepartment)
        {
            yield return new ValidationResult("Specify exactly one share recipient.", [nameof(RecipientUserId), nameof(RecipientDepartment)]);
        }

        if (CreatedAtUtc.Kind != DateTimeKind.Utc)
        {
            yield return new ValidationResult("The share timestamp must be UTC.", [nameof(CreatedAtUtc)]);
        }
    }
}