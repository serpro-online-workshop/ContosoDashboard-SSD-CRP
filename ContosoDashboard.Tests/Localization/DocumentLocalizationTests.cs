using System.Xml.Linq;

namespace ContosoDashboard.Tests.Localization;

public class DocumentLocalizationTests
{
    [Fact]
    public void EveryDocumentResourceKeyHasAValueInAllSupportedCultures()
    {
        var resourcesDirectory = Path.Combine(FindRepositoryRoot(), "ContosoDashboard", "Resources");
        var english = ReadResources(Path.Combine(resourcesDirectory, "SharedResource.resx"));
        var resourceFiles = new[]
        {
            "SharedResource.pt-BR.resx",
            "SharedResource.es.resx",
            "SharedResource.de.resx"
        };

        Assert.NotEmpty(english);
        foreach (var fileName in resourceFiles)
        {
            var translated = ReadResources(Path.Combine(resourcesDirectory, fileName));
            foreach (var (key, _) in english)
            {
                Assert.True(translated.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value),
                    $"Resource '{key}' is missing or empty in {fileName}.");
            }
        }
    }

    private static Dictionary<string, string> ReadResources(string path) => XDocument.Load(path)
        .Root!
        .Elements("data")
        .ToDictionary(
            element => (string)element.Attribute("name")!,
            element => (string?)element.Element("value") ?? string.Empty,
            StringComparer.Ordinal);

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ContosoDashboard", "Resources", "SharedResource.resx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("The repository root could not be located from the test output directory.");
    }
}