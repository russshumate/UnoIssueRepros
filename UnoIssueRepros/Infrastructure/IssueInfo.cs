namespace UnoIssueRepros.Infrastructure;

public enum IssueGroup
{
    LikelyBug,
    Surprise,
    Mvux,
    Tooling,
    Solved,
}

public enum ReproMode
{
    /// <summary>The repro runs live inside the Repro pane.</summary>
    InProcess,

    /// <summary>The repro crashes or hangs, so it runs in a child copy of this app.</summary>
    ChildProcess,

    /// <summary>The issue is in the tooling, so the panes hold written steps.</summary>
    WrittenSteps,
}

/// <summary>
/// One row of the issue table. Panes are created fresh each time the issue page is shown.
/// </summary>
public sealed record IssueInfo(
    int Number,
    IssueGroup Group,
    string Title,
    string Summary,
    string Ask,
    ReproMode Mode,
    Func<UIElement> CreateRepro,
    Func<UIElement> CreateWorkaround)
{
    /// <summary>Content shown in the child process window (ChildProcess mode only).</summary>
    public Func<UIElement>? CreateChild { get; init; }

    /// <summary>Where the repro is meaningful, e.g. "Skia desktop (Windows)".</summary>
    public string Platforms { get; init; } = "Skia desktop, WebAssembly";

    public string ModeLabel => Mode switch
    {
        ReproMode.InProcess => "live",
        ReproMode.ChildProcess => "child process",
        _ => "written steps",
    };

    public static string GroupTitle(IssueGroup group) => group switch
    {
        IssueGroup.LikelyBug => "Likely bugs",
        IssueGroup.Surprise => "Behaviours that surprised us (docs or guidance)",
        IssueGroup.Mvux => "MVUX (Uno.Extensions.Reactive)",
        IssueGroup.Tooling => "Tooling",
        _ => "Solved, worth sharing",
    };
}
