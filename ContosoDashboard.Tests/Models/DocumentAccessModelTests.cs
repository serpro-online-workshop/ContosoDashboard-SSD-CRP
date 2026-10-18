using System.ComponentModel.DataAnnotations;
using ContosoDashboard.Models;

namespace ContosoDashboard.Tests.Models;

public class DocumentAccessModelTests
{
    [Fact]
    public void ShareRequiresExactlyOneRecipientKind()
    {
        var withoutRecipient = Validate(new DocumentShare { DocumentId = 1, SharedByUserId = 1 });
        var withBothRecipients = Validate(new DocumentShare
        {
            DocumentId = 1,
            SharedByUserId = 1,
            RecipientUserId = 2,
            RecipientDepartment = "Engineering"
        });

        Assert.Contains(withoutRecipient, result => result.MemberNames.Contains(nameof(DocumentShare.RecipientUserId)));
        Assert.Contains(withBothRecipients, result => result.MemberNames.Contains(nameof(DocumentShare.RecipientUserId)));
    }

    [Fact]
    public void ShareAcceptsSingleUserOrDepartmentRecipient()
    {
        var userShare = Validate(new DocumentShare { DocumentId = 1, SharedByUserId = 1, RecipientUserId = 2 });
        var departmentShare = Validate(new DocumentShare { DocumentId = 1, SharedByUserId = 1, RecipientDepartment = "Engineering" });

        Assert.Empty(userShare);
        Assert.Empty(departmentShare);
    }

    [Fact]
    public void ActivityRequiresUtcTimestampAndKnownActivityType()
    {
        var activity = new DocumentActivity
        {
            ActorUserId = 1,
            ActivityType = DocumentActivityType.Upload,
            OccurredAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Local),
            Details = "Uploaded PDF"
        };
        var results = Validate(activity);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(DocumentActivity.OccurredAtUtc)));
    }

    private static List<ValidationResult> Validate(object value)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(value, new ValidationContext(value), results, validateAllProperties: true);
        return results;
    }
}