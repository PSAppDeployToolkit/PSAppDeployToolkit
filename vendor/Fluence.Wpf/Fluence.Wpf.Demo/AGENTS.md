# Gallery guidance

Read this file when adding or changing a gallery page, gallery navigation, or its shared sample resources.

- MainWindow is a FluenceWindow and owns the navigation shell. Pages are selected by tag through MainWindow.NavigateTo; preserve that route contract and its shell Back history.
- Add sample pages under Pages/Gallery*.xaml and register them in the navigation catalog. Use the DemoSampleControl contract for control samples. Reference pages that mirror WinUI Gallery catalogs may render directly where the existing gallery does so.
- Keep window backdrop and application theme/accent controls on the Settings page. Preserve the existing DemoSharedStyles resource boundary.
- Use the in-repository demo-sample-page playbook at .claude/skills/demo-sample-page/SKILL.md. Its SPEC.md defines page skeletons, sample slots, catalog integration, and the completion checklist.
- Test the gallery shell and page contracts in Fluence.Wpf.Tests/Gallery and Gallery/Pages. Run the demo on both of its target frameworks when the change affects them.
