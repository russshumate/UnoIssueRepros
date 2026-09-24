using System.Reflection;
using System.Runtime.InteropServices;

namespace UnoIssueRepros.Infrastructure;

public static class EnvironmentInfo
{
    public static string Describe()
    {
        var uno = Version(typeof(Window).Assembly);
        var reactive = Version(typeof(Uno.Extensions.Reactive.Feed).Assembly);
        var head = OperatingSystem.IsBrowser() ? "WebAssembly" : "Skia desktop";
        return $"Uno.WinUI {uno} · Uno.Extensions.Reactive {reactive} · {head} · render backend {RenderBackend.Current}" +
               $"{Environment.NewLine}{RuntimeInformation.OSDescription} · {RuntimeInformation.FrameworkDescription}";
    }

    private static string Version(Assembly assembly)
    {
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return informational?.Split('+')[0] ?? assembly.GetName().Version?.ToString() ?? "?";
    }
}
