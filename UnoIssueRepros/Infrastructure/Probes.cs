using UnoIssueRepros.Issues.Issue20;

namespace UnoIssueRepros.Infrastructure;

/// <summary>Child-process probes started with <c>--probe name</c>.</summary>
public static class Probes
{
    public static UIElement Create(string name) => name switch
    {
        WindowTimingProbe.ProbeName => new WindowTimingProbe(),
        SmokeProbe.ProbeName => new SmokeProbe(),
        _ => Unknown(name),
    };

    private static UIElement Unknown(string name)
    {
        ChildMode.Report($"unknown probe '{name}'");
        ChildMode.Exit(2);
        return new TextBlock();
    }
}
