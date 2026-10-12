# Uno Issue Repros

Minimal, self-contained reproductions of Uno Platform issues we hit while building a production
line-of-business app on Uno. There is no application code here, only one small Uno 6.7 app with
a page per issue.

- **Launch page:** lists every issue, grouped the way the issue table groups them.
- **One page per issue:** a **Repro** pane and a **Workaround** pane, side by side (stacked on narrow windows).
- **Crashing repros run in a child process.** The app starts a second copy of itself (`--repro N`) and shows
  what the child printed and how it exited, so the launcher survives a stack overflow or a hung UI thread.
- **Tooling issues come with written steps**, since they live in Visual Studio or other tools, not in app code.

## Running it

Requirements: .NET 10 SDK and the Uno Platform workloads (see [Uno's setup guide](https://aka.platform.uno/get-started)).
The Uno SDK version is pinned in [global.json](global.json) (`Uno.Sdk` 6.7.30, which brings in Uno.WinUI 6.7.135
and Uno.Extensions.Reactive 7.3.6; the verdicts below were identical on Uno.Sdk 6.6.42 / Uno.WinUI 6.6.184 / Reactive 7.2.3).

```bash
dotnet run --project UnoIssueRepros -f net10.0-desktop
```

```bash
dotnet run --project UnoIssueRepros -f net10.0-browserwasm
```

Most issues are about Skia desktop on Windows; the page for each one says where it applies. The
WebAssembly head runs everything that doesn't need a second window or a child process.

### Command-line modes

| Arguments | What it does |
|---|---|
| `--repro N` | Opens only issue N's crashing content, for trying it by hand. |
| `--repro N --auto` | Runs issue N's scripted repro, prints `[repro]` lines to stdout, and exits. A watchdog ends a hung child with exit code 3. |
| `--probe window-timing` | Times `new Window()` (issue 20). Pick the backend with `UNOREPRO_RENDER_BACKEND=opengl\|software\|vulkan`. |
| `--probe smoke` | Opens every issue page in turn, prints the verdicts pages compute on load, runs the headless MVUX probes, and exits with the failure count. |

## The issues

"Here" is what this app showed on Windows 11 with the pinned versions (Uno.Sdk 6.7.30 / Uno.WinUI 6.7.135,
Reactive 7.3.6). Two need a person: a drag (5) and a look at the taskbar (6). Everything else was checked by
`--probe smoke` or a scripted child run.

### Likely bugs

| # | Issue | Here | Workaround |
|---|---|---|---|
| 1 | Toolkit `TextBoxExtensions.Mask` breaks typing (throws `IndexOutOfRange` per keystroke on WASM) | Reproduced (desktop: digits never land, no exception) | Hand-rolled masked input |
| 2 | A `UserControl` that uses itself in its own `ItemTemplate` stack-overflows (template built eagerly) | Reproduced (stack overflow) | Build the tree in code-behind |
| 3 | `AppWindow.Hide()` silently does nothing on Skia Win32 | Reproduced (analyzer also says Uno0001 not implemented) | Close and recreate the window |
| 4 | `AppWindow.Id` isn't the HWND on Skia Win32 | Reproduced | Enumerate `UnoPlatformRegularWindow` windows |
| 5 | `ItemContainer` (so `SelectorBarItem`) captures the pointer on press, blocking per-item drag | Needs a drag on the page | Raw pointer handlers on the strip |
| 6 | `IsShownInSwitchers` not implemented: hidden windows still show in the taskbar and Alt+Tab | Check the taskbar (analyzer: Uno0001 not implemented) | Close windows instead of parking them |
| 7 | `TextDecorations="Underline"` renders nothing on Skia desktop | Reproduced by pixel comparison; Strikethrough is missing too | Border with a bottom edge bound to Foreground |
| 23 | `ClearValue` on a Foreground set by a Style `{ThemeResource}` re-resolves it against the application theme, not the element's ([unoplatform/uno#25313](https://github.com/unoplatform/uno/issues/25313)) | Reproduced | Swap Styles instead of set + clear; never `ClearValue` a theme-bound Foreground in a `RequestedTheme` subtree |

### Behaviours that surprised us (docs or guidance)

| # | Issue | Here | Workaround |
|---|---|---|---|
| 8 | A wrapped `TextBlock` in a `ScrollViewer` with an Auto scrollbar causes an endless layout cycle | Not reproduced in isolation yet | `Visible` or `Hidden`, never `Auto`, over wrapping content |
| 9 | A completed Storyboard keeps its animated values, which override local sets | Not reproduced in isolation (a local set restores the box; checked by pixels) | Pin the end value, then `Stop()` |
| 10 | `ThemeDictionaries` lose to `MergedDictionaries`; high contrast is app-driven | Reproduced (merged value wins) | Keep themed keys only in `ThemeDictionaries` |
| 11 | `DataContext ??=` in a child control never creates its own view model | Reproduced | Cache the VM in a field, assign unconditionally |
| 12 | `GetServiceProvider()` returns null inside MVUX template trees | Reproduced; cause: it reads only the element's own value | `FindServiceProvider()`, which walks up the tree |

### MVUX (Uno.Extensions.Reactive)

| # | Issue | Here | Workaround |
|---|---|---|---|
| 13 | A second concurrent subscriber never receives emissions (seen on 7.1.1) | Not reproduced on 7.2.3 or 7.3.6 | One subscription fanned out, or one `SourceContext` each |
| 14 | Commands run off the UI thread | Reproduced (command on a worker thread) | Marshal UI work through the `DispatcherQueue` |
| 15 | A selection used as a `Combine` input triggers a refetch | Reproduced (3 selections → 3 extra fetches) | Derive the selected item from the list and the id |
| 16 | No testing package for 7.x | Checked live against nuget.org | Drive feeds with `SourceContext` + `GetSource` |
| 22 | A `FeedView` bound to an `IFeed<string>` gets the string, not the feed (found while building 12) | Reproduced | Feed a record, or bind the value without a FeedView |

### Tooling (written steps)

| # | Issue |
|---|---|
| 17 | Visual Studio 2026 WASM Mono debugger: "fatal error loading metadata System.Private.CoreLib" |
| 18 | Hot Reload hangs the API at startup under Aspire, and breaks AppHost user-secrets |
| 19 | The Uno App MCP server times out when connecting |

### Solved, worth sharing

| # | Note |
|---|---|
| 20 | A 3.6 s `new Window()` freeze was the Intel Arc GPU driver, not Uno. Bisect by render backend first. The page times each backend in its own child process. |
| 21 | A `UIElement` can move between Windows. Detach it from the old parent first. Confirmed here: out and back with its state. |

## Layout

```
UnoIssueRepros/
  Infrastructure/   issue catalog, child-process runner, launch pane, log and steps views, Win32 helpers, smoke probe
  Pages/            MainPage (launch list) and IssuePage (Repro | Workaround)
  Issues/IssueNN/   one folder per issue; the XAML that matters for a repro stays in XAML
  Issues/Shared/    window helpers and the headless MVUX feed harness
```

To add an issue, create `Issues/IssueNN/`, add a Repro and a Workaround control, and add a row to
[IssueCatalog.cs](UnoIssueRepros/Infrastructure/IssueCatalog.cs). If the repro can take the process down,
set `ReproMode.ChildProcess`, give it a `CreateChild`, and implement `IScriptedRepro` for a `--auto` run.

## Licence

[MIT](LICENSE).
