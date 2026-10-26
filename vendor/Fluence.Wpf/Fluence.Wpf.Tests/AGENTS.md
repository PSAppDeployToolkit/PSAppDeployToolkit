# .NET test guidance

Read this file for changes to the xunit.v3 suite. The .NET 8 smoke suite has a separate project and intentionally narrower coverage.

## Test execution

Run the built Microsoft Testing Platform executable rather than dotnet test. The full test targets are net472 and net10.0-windows10.0.26100.0. Exclude Category=Screenshots in ordinary test runs.

The whole net472 assembly can abort after a long single-process run. For local verification, run net472 as two complementary lanes and run the net10.0-windows10.0.26100.0 assembly in one pass, as shown in CONTRIBUTING.md and the test project README. Lane A filters the seven costliest classes; lane B excludes those same classes. Compare their combined case count with the committed baseline. A new test class is included in lane B automatically. CI splits both target frameworks into the same two lanes.

Use the .NET 8 smoke project to validate behavior on the library's .NET 8 target. Keep smoke coverage targeted to the pipeline and controls already represented there.

## Isolation and layout

- Every new test class is sealed and owns its test state. Classes that apply a theme, change accent, or change reduced motion implement IAsyncLifetime and reset through TestApp on WpfTestSta's STA thread. Do this in InitializeAsync, not in each test body.
- A class that never changes theme state can use IClassFixture<LightThemeFixture>. Pure-logic tests need neither fixture.
- Run all WPF object creation, control interaction, and visual-tree assertions through WpfTestSta.
- Use the shared VisualTree, BrushAssert, ThemeTestHelpers, DemoTestHost, and condition-based dispatcher wait helpers. Do not copy private versions.
- Show a window to apply templates, drain its dispatcher, and close it through the shared cleanup helper in a finally block.
- Keep one sealed test class per subject in the folder that owns the concern. Folders map to namespace segments; use Control rather than Controls, Gallery rather than Demo, and Windowing rather than Window to avoid type-name shadowing.
- TestApp.EnsureLibraryTheme resets library resources. EnsureDemoTheme explicitly merges DemoSharedStyles and should be limited to tests that need demo resources.

The shared infrastructure and folder examples are in Fluence.Wpf.Tests/README.md. The committed test list under Baselines is the minimum count; update it when a test case is added or removed and explain the change in CHANGELOG.md.

## Screenshot tests and known failures

Screenshot tests are opt-in with FLUENCE_CAPTURE_SCREENSHOTS=1 and carry Category=Screenshots. RenderTargetBitmap does not capture native DWM pixels. Keep ordinary test runs from overwriting committed screenshots.

Read KNOWN_ISSUES.md before interpreting the known net472 whole-assembly abort, TimePicker flyout flake, or PowerShell dispatcher boundaries. A successful run requires passing assertions and a successful process exit. Do not add to the known failure count.
