# Generate the C# API reference

This tool renders the public and protected `Fluence.Wpf` surface from the compiled .NET 10 assembly and its compiler XML comments. It writes generated Markdown to ignored `artifacts/docs/api/` and a coverage report to `artifacts/docs/coverage.md`. The separate [website repository](https://github.com/sintaxasn/Fluence.Wpf.Website/tree/main/docs/api) owns the tracked published pages. The website can build on Ubuntu without .NET or WPF.

From the repository root on Windows:

```powershell
dotnet build Fluence.Wpf/Fluence.Wpf.csproj -c Release -f net10.0-windows10.0.26100.0
pwsh -NoProfile -File tools/ApiDocs/Generate-ApiDocs.ps1 -Configuration Release -OutputRoot artifacts/docs/api -CoveragePath artifacts/docs/coverage.md
pwsh -NoProfile -File tools/ApiDocs/Generate-ApiDocs.ps1 -Configuration Release -OutputRoot artifacts/docs/api -CoveragePath artifacts/docs/coverage.md -Check
```

Use `-Configuration Debug` if the library was built in Debug. `-OutputRoot` and `-CoveragePath` can select other dedicated output paths. The default paths are the ignored artifact paths shown above. `-Check` compares every expected byte, the coverage report, and the complete generated page set at those same paths; it fails on missing, changed, or extra generated pages and does not edit files. Write the output before checking it in a fresh checkout. Keep the output root dedicated to generated pages rather than authored files.

The generator excludes the generated XAML helper and internal types, displays declared public and protected members, and links inherited WPF behavior through the base type. The local coverage report at `artifacts/docs/coverage.md` reports reflected member counts and missing compiler XML comments.

The published pages use stable website paths: `docs/api/<CLR namespace>/<type>.md`. Control guides can link to those pages without depending on heading anchors. Member anchors derive from compiler XML IDs, so adding an overload does not change existing anchors. See the [documentation tooling guide](../Docs/README.md) for the reviewable website sync.
