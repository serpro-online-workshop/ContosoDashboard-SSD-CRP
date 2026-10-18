using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class DocumentTag : IValidatableObject
{
    [Key]
    public int DocumentTagId { get; set; }

    [Required]
    public int DocumentId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Value { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string NormalizedValue { get; set; } = string.Empty;

    [ForeignKey(nameof(DocumentId))]
    public virtual Document Document { get; set; } = null!;

    public void Normalize()
    {
        Value = Value?.Trim() ?? string.Empty;
        NormalizedValue = Value.ToLowerInvariant();
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Value))
        {
            yield return new ValidationResult("A tag value is required.", [nameof(Value)]);
        }

        if (Value.Length > 100)
        {
            yield return new ValidationResult("A tag cannot exceed 100 characters.", [nameof(Value)]);
        }

        if (!string.Equals(NormalizedValue, Value.Trim().ToLowerInvariant(), StringComparison.Ordinal))
        {
            yield return new ValidationResult("The normalized tag value is inconsistent.", [nameof(NormalizedValue)]);
        }
    }
}