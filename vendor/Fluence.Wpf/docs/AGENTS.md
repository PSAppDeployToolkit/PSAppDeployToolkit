# Documentation guidance

Read this file when changing repository documentation.

## Ownership and source

- This repository owns generated C# API pages in docs/api/ and generated PowerShell function references in docs/powershell/reference/.
- The authored product guides are docs/controls.md, docs/theming.md, and docs/winui-parity.md. Keep public behavior and examples aligned with the implementation.
- The standalone Fluence.Wpf.Website repository owns Docusaurus authoring, navigation, styling, browser review, publishing, and runnable walkthroughs. Website-only work follows that repository's instructions and checks.
- Keep operational release instructions and private release notes outside the tracked public documentation. CONTRIBUTING.md and the workflow files are the public contributor references.

## Editing

- Check the generated-page source before editing generated output. Update the generator or source comments when those own the content.
- Public API or behavior changes update CHANGELOG.md and the relevant authored guide. Keep copies synchronized with the website repository when the change affects its published documentation.
- Use .editorconfig and the repository text-policy check for encoding, line endings, and whitespace. Do not use em dashes or en dashes in Markdown.
