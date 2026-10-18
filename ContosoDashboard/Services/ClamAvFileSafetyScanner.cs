using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace ContosoDashboard.Services;

public sealed class ClamAvFileSafetyScanner : IFileSafetyScanner
{
    private readonly ClamAvOptions _options;
    private readonly IClamAvProcessRunner _processRunner;

    public ClamAvFileSafetyScanner(IOptions<ClamAvOptions> options, IClamAvProcessRunner processRunner)
    {
        _options = options.Value;
        _processRunner = processRunner;
    }

    public async Task<FileSafetyScanResult> ScanAsync(string stagedPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ExecutablePath) || !File.Exists(stagedPath))
        {
            return FileSafetyScanResult.Unavailable;
        }

        var timeout = TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 1, 300));
        var result = await _processRunner.RunAsync(_options.ExecutablePath, stagedPath, timeout, cancellationToken);
        if (!result.Started || result.TimedOut || !result.ExitCode.HasValue)
        {
            return FileSafetyScanResult.Unavailable;
        }

        return result.ExitCode.Value switch
        {
            0 => FileSafetyScanResult.Clean,
            1 => FileSafetyScanResult.Unsafe,
            _ => FileSafetyScanResult.Unavailable
        };
    }
}

public sealed class SystemClamAvProcessRunner : IClamAvProcessRunner
{
    public async Task<ClamAvProcessResult> RunAsync(string executablePath, string stagedPath, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("--no-summary");
        startInfo.ArgumentList.Add(stagedPath);

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
            {
                return new ClamAvProcessResult(false, false, null);
            }

            var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);

            try
            {
                await process.WaitForExitAsync(timeoutSource.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                process.Kill(entireProcessTree: true);
                return new ClamAvProcessResult(true, true, null);
            }

            await Task.WhenAll(standardOutputTask, standardErrorTask);
            return new ClamAvProcessResult(true, false, process.ExitCode);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or IOException)
        {
            return new ClamAvProcessResult(false, false, null);
        }
    }
}