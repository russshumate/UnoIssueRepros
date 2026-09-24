namespace UnoIssueRepros.Infrastructure;

/// <summary>
/// Opt-in render backend override for the desktop head, driven by an environment variable
/// so the parent app can launch child processes on each backend (issue 20).
/// The flags themselves are applied in Platforms/Desktop/Program.cs.
/// </summary>
public static class RenderBackend
{
    public const string EnvironmentVariable = "UNOREPRO_RENDER_BACKEND";

    public static readonly string[] Choices = ["opengl", "software", "vulkan"];

    /// <summary>The backend this process was asked to use, or "default".</summary>
    public static string Current { get; set; } = "default";
}
