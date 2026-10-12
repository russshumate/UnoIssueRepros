using UnoIssueRepros.Issues.Issue01;
using UnoIssueRepros.Issues.Issue02;
using UnoIssueRepros.Issues.Issue03;
using UnoIssueRepros.Issues.Issue04;
using UnoIssueRepros.Issues.Issue05;
using UnoIssueRepros.Issues.Issue06;
using UnoIssueRepros.Issues.Issue07;
using UnoIssueRepros.Issues.Issue08;
using UnoIssueRepros.Issues.Issue09;
using UnoIssueRepros.Issues.Issue10;
using UnoIssueRepros.Issues.Issue11;
using UnoIssueRepros.Issues.Issue12;
using UnoIssueRepros.Issues.Issue13;
using UnoIssueRepros.Issues.Issue14;
using UnoIssueRepros.Issues.Issue15;
using UnoIssueRepros.Issues.Issue16;
using UnoIssueRepros.Issues.Issue20;
using UnoIssueRepros.Issues.Issue21;
using UnoIssueRepros.Issues.Issue22;
using UnoIssueRepros.Issues.Issue23;
using UnoIssueRepros.Issues.Shared;

namespace UnoIssueRepros.Infrastructure;

/// <summary>The issue table. The numbers match the table shared with the Uno team.</summary>
public static partial class IssueCatalog
{
    private const string DesktopWindows = "Skia desktop on Windows (Win32)";

    public static IReadOnlyList<IssueInfo> All { get; } = Build();

    public static IssueInfo? Find(int number) => All.FirstOrDefault(i => i.Number == number);

    public static int IndexOf(IssueInfo issue) => All.ToList().IndexOf(issue);

    private static List<IssueInfo> Build() =>
    [
        // ── Likely bugs ─────────────────────────────────────────────────────────────
        new(1, IssueGroup.LikelyBug,
            "Toolkit TextBoxExtensions.Mask throws IndexOutOfRange on every keystroke",
            "A plain TextBox with CommunityToolkit's TextBoxExtensions.Mask=\"(999) 999-9999\". It looks fine until you type. " +
            "On WebAssembly every keystroke threw Arg_IndexOutOfRangeException inside the mask's input handler and the text was " +
            "never formatted. On Skia desktop no exception surfaces, but the typed digits never land: the box stays \"(___) ___-____\" " +
            "while the same keystrokes type normally into a plain TextBox.",
            "File the bug? Which repo owns it: Windows Community Toolkit or Uno?",
            ReproMode.ChildProcess,
            () => new ChildLaunchPane(1,
                "\"Run scripted repro\" types 5551234567 into the masked box by posting keyboard messages to the child's own " +
                "window (Windows only), then reports the text and any exceptions. \"Open child to try by hand\" to type yourself.",
                browserFallback: () => new MaskRepro()),
            () => new HandRolledMask())
        {
            CreateChild = () => new MaskRepro(),
            Platforms = "Skia desktop, WebAssembly (first seen on WebAssembly)",
        },

        new(2, IssueGroup.LikelyBug,
            "A UserControl that uses itself in its own ItemTemplate stack-overflows",
            "A tree control whose ItemsRepeater.ItemTemplate contains the same UserControl. WinUI builds the template lazily, " +
            "per realized item. Uno builds it eagerly when the template is assigned, during InitializeComponent, so one instance " +
            "constructs the next forever, before any data is bound.",
            "Is the eager template build known? Can it be made lazy?",
            ReproMode.ChildProcess,
            () => new ChildLaunchPane(2,
                "The child constructs one RecursiveNode bound to a three-level tree. Expected: the tree renders. " +
                "Reproduced: the child dies with 0xC00000FD (stack overflow) after the constructor count climbs."),
            Issue02Panes.Workaround)
        {
            CreateChild = () => new RecursiveRepro(),
        },

        new(3, IssueGroup.LikelyBug,
            "AppWindow.Hide() silently does nothing on Skia Win32",
            "Hide() returns normally, but the window stays on screen and AppWindow.IsVisible stays true. Code that trusts it " +
            "(a window pool, for example) keeps visible blank windows around. Uno's analyzer does flag it at build time " +
            "(Uno0001: not implemented in Uno), but nothing happens at run time.",
            "Bug? If Hide() is unsupported, could it throw or log?",
            ReproMode.InProcess,
            () => new HideRepro(),
            () => new CloseAndRecreatePane(
                "Don't hide. Close the window (about 18 ms) and create a new one at the same position when you need it " +
                "again. If you must keep a window, check AppWindow.IsVisible after Hide() and fall back to closing."))
        {
            Platforms = DesktopWindows,
        },

        new(4, IssueGroup.LikelyBug,
            "AppWindow.Id isn't the HWND on Skia Win32",
            "On WinUI, AppWindow.Id.Value is the window handle, and Win32 interop code (layered-window opacity, " +
            "positioning, taskbar tweaks) passes it straight to user32. On Uno Skia Win32 it is some other number.",
            "Is there a supported way to get the HWND of a Uno window?",
            ReproMode.InProcess,
            () => new WindowIdRepro(),
            () => new WindowIdWorkaround())
        {
            Platforms = DesktopWindows,
        },

        new(5, IssueGroup.LikelyBug,
            "ItemContainer (so SelectorBarItem) captures the pointer on press, blocking per-item drag",
            "Tabs you can drag to reorder: each SelectorBarItem gets ManipulationMode=TranslateX and manipulation handlers. " +
            "They never fire, because ItemContainer, SelectorBarItem's base class, captures the pointer on press and can mark " +
            "it handled.",
            "Is the capture by design? If so, what is the supported way to drag an item?",
            ReproMode.InProcess,
            () => new ItemDragRepro(),
            () => new StripDragWorkaround()),

        new(6, IssueGroup.LikelyBug,
            "IsShownInSwitchers not implemented: hidden windows still show in the taskbar and Alt+Tab",
            "Because Hide() does nothing (issue 3), we parked pooled windows off-screen with IsShownInSwitchers = false. " +
            "The property is not honoured, so every parked window stayed in the taskbar and in Alt+Tab as a ghost. " +
            "Uno's analyzer flags it at build time (Uno0001: not implemented in Uno).",
            "Is IsShownInSwitchers on the roadmap for Skia Win32?",
            ReproMode.InProcess,
            () => new SwitcherRepro(),
            () => new CloseAndRecreatePane(
                "We dropped window parking. Close windows you don't need (cheap) and recreate them at the remembered " +
                "position, so nothing is left in the taskbar or Alt+Tab."))
        {
            Platforms = DesktopWindows,
        },

        new(7, IssueGroup.LikelyBug,
            "TextDecorations=\"Underline\" renders nothing on Skia desktop (6.6.42)",
            "Both the <Underline><Run/></Underline> span and the TextDecorations property on the TextBlock render as plain text, " +
            "although the docs list them as supported. The pane's pixel check shows Strikethrough renders nothing either: both " +
            "decorated samples are pixel-identical to the plain one.",
            "Bug? Strikethrough is missing too.",
            ReproMode.InProcess,
            () => new UnderlineRepro(),
            () => new UnderlineWorkaround())
        {
            Platforms = "Skia desktop (compare with WebAssembly)",
        },

        new(23, IssueGroup.LikelyBug,
            "ClearValue on a Foreground set by a Style {ThemeResource} re-resolves it against the application theme, not the element's",
            "With the in-app theme switch done as documented (RequestedTheme on a root element; the application theme left at " +
            "its launch value), an element whose Foreground comes from a Style {ThemeResource} resolves correctly after the " +
            "switch. Give it a local Foreground and remove it with ClearValue, and the Style's {ThemeResource} comes back " +
            "resolved for the APPLICATION theme while the element's ActualTheme is unchanged. Row highlighting that sets and " +
            "clears a brush paints the wrong theme as soon as the element theme differs from the app theme " +
            "(whole WinUI.TableView grids, in our case). Reported as unoplatform/uno#25313.",
            "Should the re-application of a Style ThemeResource after a local clear resolve against the element's theme, " +
            "as ApplyVisualStateSetter does?",
            ReproMode.InProcess,
            () => new ClearValueThemeRepro(),
            () => new ClearValueThemeWorkaround())
        {
            Platforms = DesktopWindows,
        },

        // ── Behaviours that surprised us ────────────────────────────────────────────
        new(8, IssueGroup.Surprise,
            "A wrapped TextBlock inside a ScrollViewer with an Auto scrollbar causes an endless layout cycle",
            "When the container is squeezed, the Auto bar appears, takes width, the text re-wraps shorter, the bar is no longer " +
            "needed, the text re-wraps taller, and so on. In our app it ended in \"Layout cycle detected\" on resize, at two " +
            "nesting levels in the same view. This minimal version does NOT cycle on its own yet: the scrollbar toggles cleanly " +
            "because here it overlays the content instead of taking width. It is kept as the starting point; the production " +
            "layout had more around it.",
            "Worth a note in the ScrollViewer docs? Could the layout cycle message name the bistable element?",
            ReproMode.ChildProcess,
            () => new ChildLaunchPane(8,
                "The child sweeps the container width one pixel at a time through the band where the scrollbar toggles. " +
                "Reproduced: a layout-cycle exception, a crash, or the watchdog reporting a hung UI thread (exit code 3). " +
                "Not reproduced: the sweep finishes and the child exits with code 0. The exact band depends on fonts and DPI."),
            () => new LayoutCycleWorkaround())
        {
            CreateChild = () => new WrapInAutoScroll(),
        },

        new(9, IssueGroup.Surprise,
            "A completed Storyboard keeps its animated values, which override local sets",
            "After a fade-out Storyboard completed, element.Opacity = 1 did nothing: the default FillBehavior (HoldEnd) keeps " +
            "the animated value, and WinUI ranks it above a local value. It caused a phantom gap in a list whose recycled row " +
            "stayed invisible. In this minimal version on 6.6.184 the local set DOES bring the box back (checked by pixels), " +
            "so Uno differs from WinUI's documented precedence here, and the production case needs more of its setup.",
            "Which precedence should Uno follow here? Docs: make the HoldEnd rule easier to find.",
            ReproMode.InProcess,
            () => new StoryboardHold(stopBeforeRestore: false),
            () => new StoryboardHold(stopBeforeRestore: true)),

        new(10, IssueGroup.Surprise,
            "ThemeDictionaries lose to MergedDictionaries; high contrast is driven by the app, not the OS",
            "A key in both a ResourceDictionary's ThemeDictionaries and its MergedDictionaries resolves to the merged value, " +
            "so the themed one is never used. Separately, turning on Windows contrast themes does not switch the app to its " +
            "HighContrast theme dictionary; AccessibilitySettings.HighContrast is flagged by Uno's analyzer as not implemented.",
            "Does the lookup order match WinUI? Is OS high-contrast tracking planned?",
            ReproMode.InProcess,
            () => new ThemeLookupRepro(),
            () => new ThemeLookupWorkaround()),

        new(11, IssueGroup.Surprise,
            "DataContext ??= in a child control never creates its own view model, because DataContext is inherited",
            "DataContext ??= new MyViewModel() in a child's Loaded always sees the parent's inherited DataContext, so the " +
            "child's view model is never created and its bindings silently hit the wrong object. With MVUX, the child's cold " +
            "feeds never subscribe and a FeedView spins forever.",
            "Guidance: a docs note or an analyzer for ??= on DataContext.",
            ReproMode.InProcess,
            Issue11Panes.Repro,
            Issue11Panes.Workaround),

        new(12, IssueGroup.Surprise,
            "GetServiceProvider() returns null inside MVUX template trees",
            "A control inside an MVUX FeedView template resolves a service in Loaded with this.GetServiceProvider() and gets " +
            "null, while controls the app creates and attaches a provider to get one. Building this repro showed why: " +
            "GetServiceProvider() reads only the element's own Region.ServiceProvider value, while FindServiceProvider() walks up the tree.",
            "Is GetServiceProvider() meant to skip ancestors? If so, could the docs point to FindServiceProvider()?",
            ReproMode.InProcess,
            () => new ServiceProviderPanel(useWorkaround: false),
            () => new ServiceProviderPanel(useWorkaround: true)),

        // ── MVUX ────────────────────────────────────────────────────────────────────
        new(13, IssueGroup.Mvux,
            "A second concurrent subscriber never receives emissions",
            "Two subscribers enumerate the same feed on the same SourceContext at the same time. In Reactive 7.1.1 the second " +
            "never received anything. This app is on the Reactive version Uno.Sdk 6.6.42 brings in (7.2.3, see the environment " +
            "line), and there the probe's two subscribers both receive every value, so it may be fixed or our spike's setup differed.",
            "Bug, or a documented one-subscriber-per-context rule?",
            ReproMode.InProcess,
            () => new ConcurrentSubscribersPane(workaround: false),
            () => new ConcurrentSubscribersPane(workaround: true)),

        new(14, IssueGroup.Mvux,
            "Commands run off the UI thread",
            "MVUX-generated commands execute on a background thread. Code in a command that touches UI runs on the wrong " +
            "thread, nothing in the generated API hints at it, and on Skia desktop creating a control there does not even throw.",
            "Docs, or an opt-in to run a command on the UI thread?",
            ReproMode.InProcess,
            () => new CommandThreadView(workaround: false),
            () => new CommandThreadView(workaround: true)),

        new(15, IssueGroup.Mvux,
            "Using a selection as a Combine input triggers a refetch",
            "When the selected id is an input of the feed that fetches the list, every selection change re-runs the fetch: " +
            "a click re-queries the server and rebuilds the list under the user.",
            "Docs: show the derived-selection pattern next to Feed.Combine.",
            ReproMode.InProcess,
            () => new SelectionRefetchPane(workaround: false),
            () => new SelectionRefetchPane(workaround: true)),

        new(16, IssueGroup.Mvux,
            "There's no testing package for 7.x",
            "Uno.Extensions.Reactive.Testing stops at 2.x and depends on Reactive 2.x, so FeedTests and .Record() can't be used " +
            "with Reactive 7.x. Issues 13 and 15 were proven in a standalone headless spike instead.",
            "Ship a 7.x testing package, or document the supported test seam.",
            ReproMode.InProcess,
            () => new TestingPackagePane(),
            Issue16Panes.Workaround),

        new(22, IssueGroup.Mvux,
            "A FeedView bound to an IFeed<string> gets the string, not the feed",
            "Found while building issue 12. With [assembly: BindableGenerationTool(3)], which dotnet new unoapp generates, the " +
            "generated view model exposes an IFeed<string> as a plain string property. A FeedView whose Source binds to it " +
            "receives the string and shows \"An error occurred.\", with no exception and no log line. A feed of a record works.",
            "By design for primitives? If so, could FeedView report a non-feed Source clearly instead of its generic error?",
            ReproMode.InProcess,
            () => new StringFeedView(workaround: false),
            () => new StringFeedView(workaround: true)),

        // ── Tooling ─────────────────────────────────────────────────────────────────
        new(17, IssueGroup.Tooling,
            "Visual Studio 2026 WASM Mono debugger: \"fatal error loading metadata System.Private.CoreLib\"",
            "F5 ends the debug session with \"A fatal error has occurred loading the metadata for 'System.Private.CoreLib.dll'\". " +
            "In our loop an Aspire AppHost auto-attached the debugger to a Blazor WebAssembly project, and the WASM attach " +
            "killed the whole session. Clearing .vs, killing processes and rebooting did not help.",
            "Is this tracked on the Uno side too? Known-issues note for Uno WASM debugging in VS 2026?",
            ReproMode.WrittenSteps,
            () => new StepsView(Tooling.DebuggerRepro),
            () => new StepsView(Tooling.DebuggerWorkaround))
        {
            Platforms = "Visual Studio 2026 (seen on 18.7.0, runtime 10.0.9), WebAssembly debugging",
        },

        new(18, IssueGroup.Tooling,
            "Hot Reload hangs the API at startup under Aspire, and breaks AppHost user-secrets",
            "F5 of an Aspire AppHost from Visual Studio: the API resource sometimes never binds its port (its main thread is " +
            "stuck in the Hot Reload startup hook), and when it does start, user-secrets are silently missing.",
            "Is this on the Uno Hot Reload side, the VS side, or both? Any recommended settings for Uno + Aspire?",
            ReproMode.WrittenSteps,
            () => new StepsView(Tooling.AspireRepro),
            () => new StepsView(Tooling.AspireWorkaround))
        {
            Platforms = "Visual Studio 2026 + .NET Aspire AppHost + ASP.NET Core API",
        },

        new(19, IssueGroup.Tooling,
            "The Uno App MCP server times out when connecting",
            "Starting the App MCP (uno.devserver --mcp-app) for an AI agent: the client times out on connect, and when the " +
            "app does attach no frames come through. We traced it to a GetCurrentWindow() race and reported it on the Uno Discord.",
            "Status of the fix? Is there a supported setup for agents like Claude Code?",
            ReproMode.WrittenSteps,
            () => new StepsView(Tooling.McpRepro),
            () => new StepsView(Tooling.McpWorkaround))
        {
            Platforms = "Uno App MCP (uno.devserver --mcp-app) with an MCP client",
        },

        // ── Solved ──────────────────────────────────────────────────────────────────
        new(20, IssueGroup.Solved,
            "The 3.6 s new Window() freeze was the Intel Arc GPU driver, not Uno",
            "Every new Window() blocked the UI thread for about 3.6 s. Timings on the bad driver: OpenGL 3608 ms, software " +
            "17 ms, Vulkan 716 ms. A driver update brought it to about 12 ms.",
            "Nothing to fix. Worth a troubleshooting note: bisect by render backend first.",
            ReproMode.ChildProcess,
            () => new WindowTimingPane(),
            Issue20Panes.Lesson)
        {
            Platforms = DesktopWindows,
        },

        new(21, IssueGroup.Solved,
            "A UIElement can move between Windows",
            "We weren't sure a live element could be re-parented into another Window. It can (proven on 6.6.184): detach it " +
            "from the old parent first, then attach it in the new window. Its state comes along.",
            "Nothing to fix. Worth a sample for multi-window apps.",
            ReproMode.InProcess,
            () => new MoveBetweenWindows(),
            () => new StepsView("""
                ## The pattern
                $ oldParent.Children.Remove(element);   // detach first
                $ newWindow.Content = new Border { Child = element };
                $ newWindow.Activate();
                - Moving it back is the same in reverse: clear the other window's reference, close it, re-add the element.
                - Detach before attaching. An element with a parent cannot be added to a second parent.
                """))
        {
            Platforms = "Skia desktop",
        },
    ];
}

/// <summary>Written steps for the tooling rows. They reference this repo so they can be followed as-is.</summary>
internal static class Tooling
{
    public const string DebuggerRepro = """
        ## Setup
        - Visual Studio 2026 (seen on 18.7.0, .NET runtime 10.0.9).
        - Tools → Options → Debugging → General: "Enable new .NET 9+ Mono debugger for WASM apps" is checked (the default).
        ## Steps: this repo's WebAssembly head
        - Open UnoIssueRepros.sln, pick the "UnoIssueRepros (WebAssembly)" launch profile, press F5.
        ## Steps: the path that failed for us every time
        - An Aspire AppHost that references a Blazor WebAssembly project. VS auto-attaches the WASM debugger to it.
        $ dotnet new aspire-apphost -o DebugRepro.AppHost
        $ dotnet new blazorwasm -o DebugRepro.Web
        $ dotnet add DebugRepro.AppHost reference DebugRepro.Web
        - In the AppHost's Program.cs: builder.AddProject<Projects.DebugRepro_Web>("web");
        - Set the AppHost as the startup project and press F5.
        ## Result
        The debug session ends with: "A fatal error has occurred loading the metadata for 'System.Private.CoreLib.dll' … the debugger could not get the metadata." It looks like AppHost or API debugging is broken, but they are fine; the WASM attach takes the session down.
        ## References
        - developercommunity.microsoft.com/t/11015765
        - github.com/dotnet/aspnetcore/issues/65122
        """;

    public const string DebuggerWorkaround = """
        - Tools → Options → Debugging → General → uncheck "Enable new .NET 9+ Mono debugger for WASM apps" (search "Mono" in the Options box).
        - CoreCLR debugging (the API, the desktop head) is unaffected.
        - Or run without the debugger: Ctrl+F5.
        - Re-test after VS servicing updates and turn the option back on once the fix ships.
        ## Things that do not help
        - Deleting .vs, killing processes, rebooting, repairing the runtime.
        """;

    public const string AspireRepro = """
        ## Setup
        $ dotnet new aspire-apphost -o HotReloadRepro.AppHost
        $ dotnet new webapi -o HotReloadRepro.Api
        $ dotnet add HotReloadRepro.AppHost reference HotReloadRepro.Api
        $ dotnet user-secrets init --project HotReloadRepro.Api
        $ dotnet user-secrets set Demo:Secret hello --project HotReloadRepro.Api
        - AppHost Program.cs: builder.AddProject<Projects.HotReloadRepro_Api>("api");
        - API Program.cs: app.MapGet("/secret", (IConfiguration c) => c["Demo:Secret"] ?? "(missing)");
        ## Steps
        - In Visual Studio 2026, with Hot Reload on, set the AppHost as the startup project and press F5.
        ## Result 1: the API hangs (intermittent)
        The dashboard shows the api resource starting, then nothing. The port never binds, the process sits at ~0% CPU, and a managed stack dump shows the main thread blocked in the Hot Reload startup hook before Main runs:
        $ Microsoft.Extensions.DotNetDeltaApplier!HotReload.Listener.Listen()  (Task.Wait)
        $   StartupHook.Initialize() ← System.StartupHookProvider.ProcessStartupHooks()
        $ dotnet-stack report -p <api pid>
        $ Get-NetTCPConnection -State Listen -LocalPort <api port>
        ## Result 2: user-secrets are missing
        When the API does start, GET /secret returns "(missing)". VS injects ASPNETCORE_HOSTINGSTARTUPASSEMBLIES (BrowserRefresh, BrowserLink) into the child process. A hosting-startup assembly changes the host's ApplicationName, so WebApplication.CreateBuilder looks for the UserSecretsId on the wrong assembly and silently skips user-secrets. The same API run with dotnet run returns "hello".
        """;

    public const string AspireWorkaround = """
        - Run the AppHost (or just the API) from the command line. No Hot Reload hook is injected, the port binds, and user-secrets load.
        $ dotnet run --project HotReloadRepro.AppHost
        - Turning Hot Reload off in Tools → Options did not reliably take for us.
        - A possible app-side fix for the secrets (not yet verified cleanly): after CreateBuilder, load secrets from an explicit assembly.
        $ if (builder.Environment.IsDevelopment())
        $     builder.Configuration.AddUserSecrets(typeof(Program).Assembly);
        """;

    public const string McpRepro = """
        ## Setup
        - Add the Uno App MCP to an MCP client. For Claude Code, in the repo root:
        $ claude mcp add uno-app -- dotnet dnx -y uno.devserver -- --mcp-app
        - Start this app's desktop head with the dev server (Debug build, from Visual Studio or dotnet run).
        ## Steps
        - Start the MCP client and let it connect to uno-app.
        ## Result
        - The connection times out. With a stdio proxy that logs traffic, the server starts, but the handshake does not finish in time.
        - When it does connect, no frames are delivered. We traced this to a race in GetCurrentWindow() that drops every frame. Reported on the Uno Discord on 2026-05-06.
        - The --force-roots-fallback flag got us past a roots negotiation problem with the client, but not past the frame drop.
        """;

    public const string McpWorkaround = """
        Drive the WebAssembly head with Playwright instead of the App MCP.
        - Skia renderer: pixel-accurate screenshots (the page is a canvas, so there is no DOM to click).
        - Native renderer: real DOM elements, so the agent can click, type and assert.
        $ dotnet run -f net10.0-browserwasm
        - For DOM access, build the WebAssembly head with the native renderer (swap SkiaRenderer for NativeRenderer in UnoFeatures).
        - The Uno docs MCP (https://mcp.platform.uno/v1) is unaffected and stays useful for API questions.
        """;
}
