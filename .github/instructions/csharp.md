---
applyTo: "**/*.cs"
---

# C# Coding Conventions for PSAppDeployToolkit

## Multi-Targeting

Most first-party C# projects target both **.NET Framework 4.7.2** and the current modern .NET Windows target. Check the specific `.csproj` before assuming dual-targeting, because some projects are single-targeted.

- Use polyfills for APIs unavailable on .NET Framework (e.g., `ArgumentNullException.ThrowIfNull` is polyfilled in `PSADT.Interop/Polyfills/`).
- Guard framework-specific code with the same conditional compilation symbols already used in the repo, such as `NETFRAMEWORK` and `!NET6_0_OR_GREATER` where appropriate.
- Test compilation against both targets to avoid runtime failures on either platform.

## Build Enforcement

The repository already enforces several C# standards through `Directory.Build.props` and `.editorconfig`:

- Nullable reference types are enabled globally.
- Warnings are treated as errors.
- XML documentation files are generated.
- .NET analyzers run at `latest-all` with code style enforced in build.
- Banned APIs from `src/BannedSymbols.txt` are enforced via BannedApiAnalyzers.

Prefer guidance that aligns with these enforced settings over personal style preferences.
Do not relax analyzer severity, nullable behaviour, or banned API enforcement as part of a normal feature change.

## PowerShell Interop

C# types that receive values from PowerShell (transformation attributes, validation attributes) must defensively unwrap `PSObject` wrappers before processing:

```csharp
while (inputData is PSObject psObject)
{
    inputData = psObject.BaseObject;
}
```

This pattern is used in transformation and validation attributes (e.g., `DateTimeTransformationAttribute`, `TimeSpanTransformationAttribute`, `ValidateGreaterThanZeroAttribute`) to ensure the underlying .NET value is used rather than the PowerShell wrapper.

## Code Organisation

- **One primary type per file.** File name matches the type name.
- **Follow the namespace style already used in the file or project.** Most current repository files use block-scoped namespaces, so do not convert files to file-scoped namespaces as a style-only change.
- **`sealed`** by default - only omit `sealed` when a type is explicitly designed for inheritance.
- **`internal`** by default - only use `public` on types consumed by PowerShell or external assemblies.
- **Static classes** for stateless utility methods (e.g., `MsiUtilities`, `FontUtilities`, `WindowUtilities`).
- **Extension methods** in dedicated static classes in an `Extensions/` folder.
- **Avoid large style-only refactors.** Keep edits localized to the file or feature being changed.

The solution uses `InternalsVisibleTo` extensively across related assemblies. Prefer preserving existing assembly boundaries and internal visibility over widening APIs to `public` just to satisfy another project or test.

## P/Invoke & Native Interop (CsWin32)

The project makes **heavy use of the CsWin32 source generator** for Win32 API access. Prefer Win32 APIs over managed .NET alternatives where it provides better control or performance (process management, registry, window management, security, services, etc.).

### Adding New Win32 Symbols

Declare needed symbols in the project's `NativeMethods.txt` file (one symbol per line). CsWin32 auto-generates the P/Invoke code at compile time.

```text
// NativeMethods.txt - just add the symbol name
CreateProcess
GetExitCodeProcess
PROCESS_BASIC_INFORMATION
HRESULT_FROM_WIN32
```

`NativeMethods.txt` accepts function names, constants, structs, enums, and interface names.

**Never create ad-hoc `[DllImport]` declarations or local P/Invoke definitions** - always add the symbol to `NativeMethods.txt` and let CsWin32 generate it.

### Wrapper Pattern

Wrap raw CsWin32 calls in a managed `NativeMethods` static class (e.g., `PSADT.Interop.NativeMethods`) with:

- Full XML documentation (`<summary>`, `<remarks>`, `<param>`, `<returns>`, `<exception>`)
- Proper error handling (e.g., `.ThrowOnFailure()` for `HRESULT`/`WIN32_ERROR` returns)
- SafeHandle usage for native handles
- Consistent parameter validation

Generated APIs are accessed via:

- `PInvoke.*` - Win32 functions
- `Windows.Win32.PInvoke.*` - Win32 namespace-qualified access
- `Windows.Wdk.PInvoke.*` - Windows Driver Kit functions

### SafeHandle Patterns

Use `DangerousAddRef` / `DangerousRelease` in `try`/`finally` when working with SafeHandles in P/Invoke scenarios:

```csharp
bool addRef = false;
try
{
    handle.DangerousAddRef(ref addRef);
    unsafe
    {
        // Use handle.DangerousGetHandle() for the P/Invoke call.
    }
}
finally
{
    if (addRef)
    {
        handle.DangerousRelease();
    }
}
```

When validating SafeHandle parameters, **inline** `ThrowIfNullOrInvalid` or `ThrowIfNullOrClosed` into the subsequent usage - do not use standalone calls that discard the return value.

### `unsafe` Keyword

- Use `unsafe` on method **declarations** only if the method takes unsafe parameters (e.g., pointers).
- For methods that only use unsafe code **internally**, use an `unsafe` **block** inside the method body instead.

### Font Resource Management

Do not pre-validate `RemoveFontResource` input with file existence checks - the Win32 API accepts font file names that are not necessarily full paths.

### CsWin32 Symbol Availability

When the user confirms a symbol is available or generated in their local CsWin32 setup, trust that as the source of truth rather than insisting it is unavailable.

## Performance

### Aggressive Inlining

Apply `[MethodImpl(MethodImplOptions.AggressiveInlining)]` to small, frequently-called methods - factory methods, simple wrappers, single-expression property accessors:

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
internal static SafeFreeBSTRHandle Alloc(string str)
{
    return new(Marshal.StringToBSTR(str), true);
}
```

### Discard Pattern

Use `_ =` to explicitly discard unused return values:

```csharp
_ = NativeMethods.TerminateJobObject(job, exitCode);
_ = NativeMethods.GetExitCodeProcess(hProcess, out uint lpExitCode);
```

### Collection Expressions

Use collection expressions `[]` for empty or inline collections:

```csharp
// Preferred
List<string> items = [];
Dictionary<string, object> values = [];
ReadOnlyCollection<string> names = new([.. source.Select(static s => s.Name)]);

// Avoid
var items = new List<string>();
```

### Target-Typed `new()`

Use target-typed `new()` where the type is clear from context:

```csharp
Dictionary<string, object> values = new();
return new(process, launchInfo, commandLine);
```

### Async / Await

- Always append `.ConfigureAwait(false)` on all `await` calls in library code.
- Use `Task.Run` for CPU-bound work.
- Support `CancellationToken` in long-running operations.

```csharp
await Task.WhenAll(hStdOutTask, hStdErrTask).ConfigureAwait(false);
await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
```

## Null Handling & Validation

### Method Entry Validation

Use the `ThrowIf*` guard APIs at method entry:

```csharp
ArgumentNullException.ThrowIfNull(runAsActiveUser);
ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
ArgumentOutOfRangeException.ThrowIfZero(count);
```

When using `ThrowIfNullOrWhiteSpace`, omit the explicit parameter name when the expression already produces a useful name. Pass `nameof(...)` when validating a derived expression and you need the original parameter name to remain clear.

### Optional Nullable Parameters

For optional parameters that may be null, check `is not null` first, then validate:

```csharp
if (displayVersion is not null)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(displayVersion);
}
```

### Pattern Matching for Null Checks

Prefer pattern matching with `is` to tightly scope the variable to the branch where it is needed:

```csharp
// Preferred - variable scoped to the if block
if (GetWindowText(handle) is string windowTitle)
{
    // use windowTitle
}

// Avoid - variable leaks to outer scope
var windowTitle = GetWindowText(handle);
if (windowTitle != null) { ... }
```

- Prefer `is not null` over `!= null`.
- Do not disable nullable reference types unless there is a narrowly scoped and well-justified reason.

## Error Handling & Resource Management

### Nested try/finally for Resource Cleanup

Use **nested** `try`/`finally` blocks for COM cleanup and resource management rather than flat structures with nullable checks, even if it results in deeper indentation:

```csharp
try
{
    outputPipeClient = new(PipeDirection.Out, outputPipeHandle);
}
catch (Exception ex)
{
    throw new ClientException("Failed to open pipe.", ClientExitCode.InvalidOutputPipe, ex);
}
try
{
    inputPipeClient = new(PipeDirection.In, inputPipeHandle);
}
catch (Exception ex)
{
    throw new ClientException("Failed to open pipe.", ClientExitCode.InvalidInputPipe, ex);
}
```

### Using Declarations

Prefer `using` declarations for disposable resources:

```csharp
using FreeLibrarySafeHandle hInstance = NativeMethods.LoadLibraryEx("msimsg.dll", flags);
using CancellationTokenRegistration ctr = token.Register(() => Cleanup());
```

### Custom SafeHandle Types

The project uses custom SafeHandle types (`SafeFreeBSTRHandle`, `SafePinnedGCHandle`, `MsiCloseHandleSafeHandle`, etc.) for native resource lifetime management. Prefer these over raw `IntPtr`.

### Catch Block return/throw Pattern

If you encounter an existing analyzer-workaround pattern in a `catch` block, preserve it unless you are intentionally replacing it with a clearer fix that still satisfies the analyzers:

```csharp
catch
{
    return null;
    throw;
}
```

## Expression & Type Patterns

### Ternary Expressions

Favour the **positive/valid** scenario first:

```csharp
// Preferred
size >= HeaderSize ? new Instance(data) : null

// Avoid
size < HeaderSize ? null : new Instance(data)
```

**Exception**: when throwing, check for the **invalid** condition first:

```csharp
res != NTSTATUS.STATUS_SUCCESS
    ? throw new Win32Exception((int)PInvoke.RtlNtStatusToDosError(res))
    : (int)res;
```

### Inline Factory Methods

When a factory method already provides context, inline the expression directly rather than introducing a temporary variable:

```csharp
// Preferred
entries.Add(DelayImportEntry.FromOrdinal((ushort)(thunkData & 0xFFFF)));

// Avoid
ushort ordinal = (ushort)(thunkData & 0xFFFF);
entries.Add(DelayImportEntry.FromOrdinal(ordinal));
```

### Type Conventions

| Pattern | Use For | Example |
| ------- | ------- | ------- |
| `sealed record` | Immutable data types | `InstalledApplication`, `UserProfileInfo`, `SmbiosTablePosition`, `LogEntry` |
| `readonly record struct` | Small value types | `BiosExtendedRomSize`, `SystemEnclosureTypeAndLock` |
| `sealed class` | Most non-data classes | `SafeFreeBSTRHandle`, `DataSerialization` |
| `static class` | Stateless utilities | `MsiUtilities`, `NativeMethods`, `PowerShellUtilities` |
| Enums | Strongly-typed domains | `SHOW_WINDOW_CMD`, `LogSeverity`, `DialogPosition` |

- **Strong typing**: prefer enum-backed properties (e.g., `SHOW_WINDOW_CMD`) over primitive types when the domain has a well-defined type.
- **Static abstract interface members**: use where appropriate (e.g., `ISmbiosStructure` pattern).

## XML Documentation

Required on all `public` and `internal` types and members:

```csharp
/// <summary>
/// Retrieves the full file system path of the executable associated with the specified process.
/// </summary>
/// <remarks>This method attempts to retrieve the file path using the process's main module.
/// If that fails, it falls back to an alternative mechanism.</remarks>
/// <param name="process">The process for which to obtain the executable file path.</param>
/// <returns>A FileInfo containing the full file system path.</returns>
/// <exception cref="Win32Exception">Thrown if the process path cannot be determined.</exception>
```

- **Preserve existing XML documentation** when refactoring. Don't strip comments unnecessarily - it adds noise to diffs and loses valuable documentation.
- Only modify documentation that is directly affected by the structural change.

## Named Pipe Security

Preserve strict named pipe security and reject solutions that weaken ACLs. Always create pipes with **explicit `PipeSecurity`** - never create pipes without access control.

## Code Suppression

- Use `[SuppressMessage("Category", "RuleId", Justification = "...")]` with a meaningful justification for deliberately suppressed warnings.
- Use `#pragma warning disable` with specific warning codes when attribute-level suppression is not possible.
- Prefer fixing the root cause over adding new suppressions. If a suppression is necessary, keep it tightly scoped and explain the repository-specific reason, such as dual-targeting, generated interop parity, or unavoidable framework differences.

```csharp
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1069:Enums values should not be duplicated",
    Justification = "These values are precisely as they're defined in the Win32 API.")]
```
