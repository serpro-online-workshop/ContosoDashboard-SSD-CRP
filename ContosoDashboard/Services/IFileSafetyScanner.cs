namespace ContosoDashboard.Services;

public enum FileSafetyScanResult
{
    Clean,
    Unsafe,
    Unavailable
}

public interface IFileSafetyScanner
{
    Task<FileSafetyScanResult> ScanAsync(string stagedPath, CancellationToken cancellationToken = default);
}

public sealed class ClamAvOptions
{
    public string ExecutablePath { get; set; } = "clamscan";
    public int TimeoutSeconds { get; set; } = 30;
}

public sealed record ClamAvProcessResult(bool Started, bool TimedOut, int? ExitCode);

public interface IClamAvProcessRunner
{
    Task<ClamAvProcessResult> RunAsync(string executablePath, string stagedPath, TimeSpan timeout, CancellationToken cancellationToken = default);
}