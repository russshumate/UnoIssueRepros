using Uno.UI.Hosting;
using UnoIssueRepros.Infrastructure;

namespace UnoIssueRepros;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Issue 20: the render backend has to be chosen before the host is built,
        // because the feature flags are read when the renderer is created.
        ApplyRenderBackendFromEnvironment();

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWin32()
            .Build();

        host.Run();
    }

    private static void ApplyRenderBackendFromEnvironment()
    {
        var requested = Environment.GetEnvironmentVariable(RenderBackend.EnvironmentVariable)?.Trim().ToLowerInvariant();
        switch (requested)
        {
            case "software":
                Uno.UI.FeatureConfiguration.Rendering.UseOpenGLOnWin32 = false;
                Uno.UI.FeatureConfiguration.Rendering.UseVulkanOnWin32 = false;
                break;
            case "vulkan":
                Uno.UI.FeatureConfiguration.Rendering.UseOpenGLOnWin32 = false;
                Uno.UI.FeatureConfiguration.Rendering.UseVulkanOnWin32 = true;
                break;
            case "opengl":
                Uno.UI.FeatureConfiguration.Rendering.UseOpenGLOnWin32 = true;
                Uno.UI.FeatureConfiguration.Rendering.UseVulkanOnWin32 = false;
                break;
            default:
                return;
        }

        RenderBackend.Current = requested;
    }
}
