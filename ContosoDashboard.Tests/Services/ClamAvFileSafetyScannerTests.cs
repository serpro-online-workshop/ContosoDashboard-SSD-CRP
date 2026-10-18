using ContosoDashboard.Services;
using Microsoft.Extensions.Options;

namespace ContosoDashboard.Tests.Services;

public class ClamAvFileSafetyScannerTests
{
    [Theory]
    [InlineData(0, FileSafetyScanResult.Clean)]
    [InlineData(1, FileSafetyScanResult.Unsafe)]
    [InlineData(2, FileSafetyScanResult.Unavailable)]
    public async Task ScannerMapsClamAvExitCodes(int exitCode, FileSafetyScanResult expected)
    {
        using var file = new TemporaryFile();
        var runner = new FakeProcessRunner(new ClamAvProcessResult(true, false, exitCode));
        var scanner = CreateScanner(runner);

        var result = await scanner.ScanAsync(file.Path);

        Assert.Equal(expected, result);
        Assert.Equal("clamscan", runner.ExecutablePath);
        Assert.Equal(file.Path, runner.StagedPath);
    }

    [Fact]
    public async Task MissingExecutableFailsClosed()
    {
        using var file = new TemporaryFile();
        var scanner = CreateScanner(new FakeProcessRunner(new ClamAvProcessResult(false, false, null)));

        var result = await scanner.ScanAsync(file.Path);

        Assert.Equal(FileSafetyScanResult.Unavailable, result);
    }

    [Fact]
    public async Task TimeoutFailsClosed()
    {
        using var file = new TemporaryFile();
        var scanner = CreateScanner(new FakeProcessRunner(new ClamAvProcessResult(true, true, null)));

        var result = await scanner.ScanAsync(file.Path);

        Assert.Equal(FileSafetyScanResult.Unavailable, result);
    }

    [Fact]
    public async Task MissingStagedFileFailsClosedWithoutStartingScanner()
    {
        var runner = new FakeProcessRunner(new ClamAvProcessResult(true, false, 0));
        var scanner = CreateScanner(runner);

        var result = await scanner.ScanAsync(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));

        Assert.Equal(FileSafetyScanResult.Unavailable, result);
        Assert.Null(runner.StagedPath);
    }

    private static ClamAvFileSafetyScanner CreateScanner(FakeProcessRunner runner) =>
        new(Options.Create(new ClamAvOptions { ExecutablePath = "clamscan", TimeoutSeconds = 2 }), runner);

    private sealed class FakeProcessRunner : IClamAvProcessRunner
    {
        private readonly ClamAvProcessResult _result;

        public FakeProcessRunner(ClamAvProcessResult result) => _result = result;

        public string? ExecutablePath { get; private set; }
        public string? StagedPath { get; private set; }

        public Task<ClamAvProcessResult> RunAsync(string executablePath, string stagedPath, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            ExecutablePath = executablePath;
            StagedPath = stagedPath;
            Assert.Equal(TimeSpan.FromSeconds(2), timeout);
            return Task.FromResult(_result);
        }
    }

    private sealed class TemporaryFile : IDisposable
    {
        public TemporaryFile()
        {
            Path = System.IO.Path.GetTempFileName();
        }

        public string Path { get; }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}