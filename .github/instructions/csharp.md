---
applyTo: "**/*.cs"
---

# C# Coding Conventions for PSAppDeployToolkit

These conventions define how C# source files should be written in this repository. Keep them focused on PSAppDeployToolkit-specific expectations rather than generic C# style advice.

## Core Rules

### Multi-targeting and build rules

- Most first-party C# projects target both .NET Framework 4.7.2 and the current modern .NET Windows target. Check the specific `.csproj` before assuming dual-targeting.
- Nullable reference types are enabled globally.
- Warnings are treated as errors.
- XML documentation files are generated.
- Analyzers run at `latest-all` with code style enforced in build.
- Banned APIs from `src/BannedSymbols.txt` are enforced.
- Do not relax nullable behaviour, analyzer severity, or banned API enforcement as part of a normal feature change.
- When using APIs unavailable on .NET Framework, use the repo's existing polyfills or the same conditional compilation symbols already used in the project.

### PowerShell interop

- C# code that receives values from PowerShell must defensively unwrap `PSObject` wrappers before processing.
- Apply this especially in transformation attributes, validation attributes, and other PowerShell-facing entry points.

### Code organization

- One primary type per file. File name matches the type name.
- Follow the namespace style already used in the file or project. Do not perform namespace-style rewrites as style-only churn.
- Prefer `sealed` by default unless a type is intentionally designed for inheritance.
- Prefer `internal` by default unless the type is consumed by PowerShell or another assembly.
- Preserve existing assembly boundaries and `InternalsVisibleTo` usage rather than widening APIs to `public` just to satisfy another project or test.
- Avoid large style-only refactors.

## Native Interop

### CsWin32 rules

- Prefer the repository's CsWin32-generated interop over ad-hoc `[DllImport]` declarations.
- Add new Win32 symbols to the appropriate `NativeMethods.txt` file instead of creating local P/Invoke definitions.
- Wrap raw CsWin32 calls in the repository's managed `NativeMethods` layer with proper validation, error handling, and SafeHandle usage.
- When the user confirms a symbol exists in their local CsWin32 setup, treat that as the source of truth.

### SafeHandle and unsafe usage

- Prefer the repository's custom SafeHandle types over raw `IntPtr`.
- Use the existing `DangerousAddRef` / `DangerousRelease` pattern when required for P/Invoke scenarios.
- Inline `ThrowIfNullOrInvalid` or `ThrowIfNullOrClosed` into subsequent SafeHandle usage rather than discarding the return value.
- Use `unsafe` on the method declaration only when the signature itself requires it; otherwise prefer an internal `unsafe` block.

### Specific interop constraints

- Do not pre-validate `RemoveFontResource` input with file-existence checks.
- Preserve strict named pipe security. Create named pipes with explicit `PipeSecurity`; do not weaken ACLs.

## Preferred Modern C# Patterns

- Use target-typed `new()` when the type is clear from context.
- Use collection expressions `[]` for empty or inline collections where the existing project style supports them.
- Use `_ =` to explicitly discard unused return values.
- Use `.ConfigureAwait(false)` on awaited calls in library code.
- Support `CancellationToken` in long-running operations.
- Prefer `ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrWhiteSpace`, and related guard APIs at method entry.
- For optional nullable parameters, check `is not null` before validating.
- Prefer pattern matching with `is` when it improves scoping and clarity.
- Prefer strong typing such as enums over primitive values when the domain is well-defined.

## Resource Management and Error Handling

- Prefer `using` declarations for disposable resources.
- Use the repository's nested cleanup and exception-handling patterns for COM, pipe, and native resource management where those patterns already exist.
- Prefer structured, repository-consistent error handling over ad-hoc exception flow.
- If you encounter an existing analyzer-workaround pattern in a `catch` block, preserve it unless you are intentionally replacing it with a clearer fix that still satisfies the analyzers.

## Documentation and Suppression

- Preserve existing XML documentation when refactoring.
- Add or update XML documentation on public and internal APIs when the change affects their contract or behaviour.
- Use `[SuppressMessage(...)]` or narrowly scoped `#pragma warning disable` only when the root cause cannot reasonably be fixed.
- Keep suppressions tightly scoped and include a meaningful repository-specific justification.

## Repository-Specific Preferences

- Prefer static utility classes for stateless helpers and dedicated extension classes for extension methods.
- Prefer helper and wrapper patterns already present in the project over introducing a new style for the same problem.
- When choosing between managed .NET APIs and Win32 APIs, follow the existing project approach in that area unless there is a clear reason to diverge.
