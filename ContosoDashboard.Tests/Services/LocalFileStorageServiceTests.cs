using System.ComponentModel.DataAnnotations;
using System.Text;
using ContosoDashboard.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace ContosoDashboard.Tests.Services;

public class LocalFileStorageServiceTests
{
    [Fact]
    public async Task CommitUsesGeneratedKeyAndStoresOutsideWebRoot()
    {
        using var fixture = new StorageFixture();
        var service = fixture.CreateService();
        var content = new MemoryStream(Encoding.UTF8.GetBytes("clean sample"));
        var stagedFile = await service.StageAsync(content, 1024);

        var key = await service.CommitAsync(stagedFile, 4, null, ".pdf");
        var storedPath = Path.Combine(fixture.StorageRoot, key.Replace('/', Path.DirectorySeparatorChar));
        var webRoot = Path.GetFullPath(fixture.Environment.WebRootPath) + Path.DirectorySeparatorChar;

        Assert.StartsWith("4/personal/", key, StringComparison.Ordinal);
        Assert.DoesNotContain("original-name", key, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(storedPath));
        Assert.False(Path.GetFullPath(storedPath).StartsWith(webRoot, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));

        await using var storedContent = await service.OpenReadAsync(key);
        using var reader = new StreamReader(storedContent);
        Assert.Equal("clean sample", await reader.ReadToEndAsync());

        await service.DeleteAsync(key);
        Assert.False(File.Exists(storedPath));
    }

    [Fact]
    public async Task OpenReadRejectsPathTraversalKeys()
    {
        using var fixture = new StorageFixture();
        var service = fixture.CreateService();

        await Assert.ThrowsAsync<ValidationException>(() => service.OpenReadAsync("../outside.pdf"));
        await Assert.ThrowsAsync<ValidationException>(() => service.OpenReadAsync("C:\\private\\file.pdf"));
    }

    [Fact]
    public async Task OversizedStageIsRejectedAndTemporaryFileIsRemoved()
    {
        using var fixture = new StorageFixture();
        var service = fixture.CreateService();
        var content = new MemoryStream(Encoding.UTF8.GetBytes("this is larger than the limit"));

        await Assert.ThrowsAsync<ValidationException>(() => service.StageAsync(content, 5));

        Assert.Empty(Directory.GetFiles(Path.Combine(fixture.StorageRoot, ".staging")));
    }

    [Fact]
    public async Task DiscardRemovesStagedFile()
    {
        using var fixture = new StorageFixture();
        var service = fixture.CreateService();
        var stagedFile = await service.StageAsync(new MemoryStream([1, 2, 3]), 100);

        Assert.True(File.Exists(stagedFile.StagingPath));
        await service.DiscardAsync(stagedFile);
        Assert.False(File.Exists(stagedFile.StagingPath));
    }

    [Fact]
    public void StorageRootInsideWebRootIsRejected()
    {
        using var fixture = new StorageFixture();

        Assert.Throws<ArgumentException>(() => fixture.CreateService(Path.Combine(fixture.Environment.WebRootPath, "uploads")));
    }

    private sealed class StorageFixture : IDisposable
    {
        public StorageFixture()
        {
            ContentRoot = Path.Combine(Path.GetTempPath(), $"contoso-storage-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(ContentRoot);
            Environment.ContentRootPath = ContentRoot;
            Environment.WebRootPath = Path.Combine(ContentRoot, "wwwroot");
            Directory.CreateDirectory(Environment.WebRootPath);
        }

        public string ContentRoot { get; }
        public string StorageRoot => Path.Combine(ContentRoot, "AppData", "uploads");
        public TestWebHostEnvironment Environment { get; } = new();

        public LocalFileStorageService CreateService(string? rootPath = null) => new(
            Options.Create(new DocumentStorageOptions { RootPath = rootPath ?? "AppData/uploads" }),
            Environment);

        public void Dispose()
        {
            if (Directory.Exists(ContentRoot))
            {
                Directory.Delete(ContentRoot, recursive: true);
            }
        }
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "ContosoDashboard.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}