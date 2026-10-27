# Documentation guidance

Read this file when changing repository documentation.

## Ownership and source

- This repository owns the C# API and PowerShell function reference generators and their ignored output under artifacts/docs/. The website repository owns tracked published generated pages and images.
- The authored product guides are docs/controls.md, docs/theming.md, and docs/winui-parity.md. Keep public behavior and examples aligned with the implementation.
- The standalone Fluence.Wpf.Website repository owns Docusaurus authoring, navigation, styling, browser review, publishing, and runnable walkthroughs. Website-only work follows that repository's instructions and checks.
- Keep operational release instructions and private release notes outside the tracked public documentation. CONTRIBUTING.md and the workflow files are the public contributor references.

## Editing

- Update the generator, XML comments, or PowerShell help when those own generated content. Generate into ignored artifacts/docs/, then check the same output before syncing the website copy.
- Public API or behavior changes update CHANGELOG.md, the relevant XML documentation, and the corresponding website control page. Keep the control catalog focused on choosing controls. Sync managed pages to the website repository when a change affects its published documentation. Published screenshots require separate visual review.
- Use .editorconfig and the repository text-policy check for encoding, line endings, and whitespace. Do not use em dashes or en dashes in Markdown.
