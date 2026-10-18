using System.ComponentModel.DataAnnotations;
using ContosoDashboard.Models;

namespace ContosoDashboard.Tests.Models;

public class DocumentTests
{
    [Fact]
    public void ValidDocumentPassesValidation()
    {
        var document = CreateValidDocument();

        var results = Validate(document);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingTitleFailsValidation(string? title)
    {
        var document = CreateValidDocument();
        document.Title = title!;

        var results = Validate(document);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(Document.Title)));
    }

    [Fact]
    public void TitleLongerThan255CharactersFailsValidation()
    {
        var document = CreateValidDocument();
        document.Title = new string('a', 256);

        var results = Validate(document);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(Document.Title)));
    }

    [Fact]
    public void DescriptionLongerThan2000CharactersFailsValidation()
    {
        var document = CreateValidDocument();
        document.Description = new string('a', 2001);

        var results = Validate(document);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(Document.Description)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Unknown")]
    public void UnsupportedCategoryFailsValidation(string category)
    {
        var document = CreateValidDocument();
        document.Category = category;

        var results = Validate(document);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(Document.Category)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(26214401)]
    public void FileSizeOutsideAllowedRangeFailsValidation(long fileSize)
    {
        var document = CreateValidDocument();
        document.FileSize = fileSize;

        var results = Validate(document);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(Document.FileSize)));
    }

    [Fact]
    public void ContentTypeLongerThan255CharactersFailsValidation()
    {
        var document = CreateValidDocument();
        document.ContentType = new string('a', 256);

        var results = Validate(document);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(Document.ContentType)));
    }

    [Theory]
    [InlineData("../outside.pdf")]
    [InlineData("/etc/passwd")]
    [InlineData("C:\\private\\file.pdf")]
    public void UnsafeStorageKeyFailsValidation(string storageKey)
    {
        var document = CreateValidDocument();
        document.StorageKey = storageKey;

        var results = Validate(document);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(Document.StorageKey)));
    }

    private static Document CreateValidDocument() => new()
    {
        Title = "Quarterly report",
        Category = "Reports",
        OriginalFileName = "quarterly-report.pdf",
        StorageKey = "4/personal/8f3090c2-report.pdf",
        FileSize = 1024,
        ContentType = "application/pdf",
        UploadedAtUtc = DateTime.UtcNow,
        UploaderId = 4,
        SafetyStatus = DocumentSafetyStatus.Clean
    };

    private static List<ValidationResult> Validate(Document document)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(document, new ValidationContext(document), results, validateAllProperties: true);
        return results;
    }
}