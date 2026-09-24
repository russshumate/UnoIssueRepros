using System.Diagnostics;
using System.Reflection;

namespace UnoIssueRepros.Infrastructure;

/// <summary>Launches this app again as a child process and streams its output back.</summary>
public sealed class ChildProcessRunner : IDisposable
{
    private Process? _process;

    public static bool IsSupported =>
        !OperatingSystem.IsBrowser() && Environment.ProcessPath is not null;

    public event Action<string, bool>? LineReceived;

    public event Action<int>? Exited;

    public bool IsRunning => _process is { HasExited: false };

    public int Start(IEnumerable<string> arguments, IReadOnlyDictionary<string, string>? environment = null)
    {
        Kill();

        var processPath = Environment.ProcessPath!;
        var info = new ProcessStartInfo
        {
            FileName = processPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        // Launched as "dotnet UnoIssueRepros.dll": pass the dll again.
        if (Path.GetFileNameWithoutExtension(processPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            info.ArgumentList.Add(Assembly.GetEntryAssembly()!.Location);
        }

        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        if (environment is not null)
        {
            foreach (var (key, value) in environment)
            {
                info.Environment[key] = value;
            }
        }

        var process = new Process { StartInfo = info, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) LineReceived?.Invoke(e.Data, false); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) LineReceived?.Invoke(e.Data, true); };
        process.Exited += (_, _) =>
        {
            // Let the async readers drain before reporting the exit.
            process.WaitForExit();
            Exited?.Invoke(process.ExitCode);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        _process = process;
        return process.Id;
    }

    public void Kill()
    {
        try
        {
            if (_process is { HasExited: false })
            {
                _process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // Already gone.
        }
    }

    public void Dispose()
    {
        Kill();
        _process?.Dispose();
    }

    /// <summary>Turns well-known Windows and .NET exit codes into words.</summary>
    public static string DescribeExitCode(int code) => unchecked((uint)code) switch
    {
        0 => "exit code 0: the process survived and exited normally",
        ChildMode.HungExitCode => "exit code 3: the watchdog ended the process because the UI thread hung",
        0xC00000FD => "0xC00000FD STATUS_STACK_OVERFLOW: the process died from a stack overflow",
        0xE0434352 => "0xE0434352: the process died from an unhandled .NET exception",
        0xC0000005 => "0xC0000005 STATUS_ACCESS_VIOLATION: the process crashed",
        0xC0000409 => "0xC0000409 STATUS_STACK_BUFFER_OVERRUN / fail-fast: the process was terminated",
        0x80131623 => "0x80131623 COR_E_FAILFAST: Environment.FailFast",
        _ => $"exit code {code} (0x{unchecked((uint)code):X8})",
    };
}
