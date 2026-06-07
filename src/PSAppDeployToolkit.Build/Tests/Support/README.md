# Tests\Support

Shared real-fixture toolkit for the PSAppDeployToolkit Pester suites. Everything here authors
**real** on-disk fixtures (a runnable EXE, a valid MSI, a `.reg` file, a `.wim`) so tests can
exercise the production code against genuine artifacts instead of brittle mocks.

## Golden rules

- **No machine mutation.** Every helper writes only to a caller-supplied path (normally
  `$TestDrive`). Nothing here touches the registry, services, machine-wide installs, or any
  global state. The MSI is *authored*, never installed; the `.reg` is *written*, never imported.
- **Copy before read (MSI).** A file authored by the WindowsInstaller COM object in the current
  process can fail to open via the P/Invoke reader (`MsiOpenDatabase`, used by
  `Get-ADTMsiTableProperty`) even after the COM handles are released. **Copy the MSI to a fresh
  path and read the copy.**
- **Not collected as tests.** This folder is `Tests\Support`, a sibling of `Tests\Unit`. The unit
  runner (`Invoke-ADTPesterUnitTesting`) points Pester's `Run.Path` at `Tests\Unit` and collects
  only `*.Tests.ps1` files, so nothing here is ever run as a test. Do **not** give any file in
  this folder a `.Tests.ps1` suffix.

## Usage

```powershell
Import-Module "$PSScriptRoot\..\Support\TestFixtures.psm1" -Force
```

## Helpers

### `Get-ADTFakeInstaller [-OutputPath <string>]`

Compiles a tiny C# console application (`FakeInstaller.exe`) on first use and caches it per
output path per run (recompiles are skipped while the EXE exists on disk). Returns the path to
the compiled executable. The only file written is the EXE itself, at `-OutputPath` (default: a
unique file under `$env:TEMP`).

CLI contract (flags are order-independent; unknown tokens are ignored):

| Flag | Effect |
| --- | --- |
| `--exit-code N` | Exit with code `N` (default `0`). |
| `--sleep <ms>` | Sleep `<ms>` milliseconds before exiting. |
| `--stdout <text>` | Write `<text>` (plus newline) to stdout. |
| `--stderr <text>` | Write `<text>` (plus newline) to stderr. |
| `--write-file <path>` | Create/overwrite a marker file at `<path>` (writes **only** where told). |
| `--fail-times N` + `--state-file <path>` | Simulate transient failures. While the integer counter in `<path>` is below `N`, increment+rewrite it and exit `1`; once the counter reaches `N`, stop failing and exit with `--exit-code`. Enables retry testing. |

```powershell
$exe = Get-ADTFakeInstaller -OutputPath "$TestDrive\FakeInstaller.exe"
& $exe --exit-code 3            # $LASTEXITCODE -> 3
& $exe --stdout hello           # prints: hello
& $exe --fail-times 2 --state-file "$TestDrive\state.txt"  # fails twice, then succeeds
```

### `New-ADTTestMsiDatabase -Path <string> [-Properties <hashtable>] [-ProductName <string>] [-ProductCode <string>]`

Authors a minimal but valid MSI (Property table + SummaryInformation stream) at `-Path` via the
`WindowsInstaller.Installer` COM object. Seeds `ProductName` and `ProductCode` plus any rows
supplied via `-Properties`. All COM handles are released and the GC is run before returning, so
the file is unlocked. Returns the MSI path.

```powershell
$msi = New-ADTTestMsiDatabase -Path "$TestDrive\test.msi" -Properties @{ ProductVersion = '1.2.3' }
$copy = "$TestDrive\test_copy.msi"
Copy-Item -LiteralPath $msi -Destination $copy   # copy-before-read for P/Invoke readers
```

### `New-ADTTestRegFile -Path <string> -Content <hashtable|string>`

Writes a valid `Windows Registry Editor Version 5.00` `.reg` file at `-Path` (UTF-16 LE with BOM).
`-Content` is either a verbatim string body or a hashtable keyed by full registry key path, whose
values are hashtables of value-name -> string-value (emitted as `REG_SZ`). Returns the file path.

```powershell
New-ADTTestRegFile -Path "$TestDrive\test.reg" -Content @{
    'HKEY_LOCAL_MACHINE\SOFTWARE\Fixture' = @{ Name = 'Value'; Version = '1.0' }
}
```

### `New-ADTTestWim -SourceFolder <string> -Path <string>`

Captures `-SourceFolder` into a `.wim` at `-Path` via `New-WindowsImage` (Dism). This typically
requires an **elevated** session; if Dism is unavailable or the capture fails the function throws
a clear, actionable error. Intended for **integration tests only** and never invoked at module
load. Returns the `.wim` path.

```powershell
New-ADTTestWim -SourceFolder "$TestDrive\payload" -Path "$TestDrive\image.wim"
```
