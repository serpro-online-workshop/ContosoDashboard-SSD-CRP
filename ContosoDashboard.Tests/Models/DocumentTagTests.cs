using System.ComponentModel.DataAnnotations;
using ContosoDashboard.Models;

namespace ContosoDashboard.Tests.Models;

public class DocumentTagTests
{
    [Fact]
    public void TagValueIsTrimmedAndNormalized()
    {
        var tag = new DocumentTag { Value = "  Finance  " };

        tag.Normalize();

        Assert.Equal("Finance", tag.Value);
        Assert.Equal("finance", tag.NormalizedValue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyTagValueFailsValidation(string? value)
    {
        var tag = new DocumentTag { Value = value! };
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(tag, new ValidationContext(tag), results, validateAllProperties: true);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(DocumentTag.Value)));
    }

    [Fact]
    public void TagValueLongerThan100CharactersFailsValidation()
    {
        var tag = new DocumentTag { Value = new string('a', 101) };
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(tag, new ValidationContext(tag), results, validateAllProperties: true);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(DocumentTag.Value)));
    }
}