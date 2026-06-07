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

> **Note — default `ProductCode` is intentionally not a valid GUID.** The default value is fine
> for property-reading tests (`Get-ADTMsiTableProperty`) but will fail if passed to
> `Start-ADTMsiProcess`, which casts `ProductCode` to `[System.Guid]`. Any test that drives an
> MSI through `Start-ADTMsiProcess` must supply a real GUID via `-ProductCode` and `-UpgradeCode`.

```powershell
$msi = New-ADTTestMsiDatabase -Path "$TestDrive\test.msi" -Properties @{ ProductVersion = '1.2.3' }
$copy = "$TestDrive\test_copy.msi"
Copy-Item -LiteralPath $msi -Destination $copy   # copy-before-read for P/Invoke readers
```

### `New-ADTTestInstallMsi -Path <string> [-ProductName <string>] [-ProductCode <guid>] [-UpgradeCode <guid>] [-ProductVersion <string>]`

Authors a minimal but **genuinely installing** `.msi` at `-Path` via the `WindowsInstaller.Installer`
COM object — **no cabinet required**. Unlike `New-ADTTestMsiDatabase` (read-only property table),
the package produced here installs under msiexec, registers with Windows Installer (so it is
discoverable via `Get-ADTApplication`), and removes every artifact on uninstall. **Integration
tests only** (requires elevation + real machine mutation). Returns a descriptor object.

Install artifacts (all under a dedicated `PSADT.Test` namespace; `<Name>` = `-ProductName`):

| Artifact | Location | Created by | Removed by |
| --- | --- | --- | --- |
| Registry value | `HKLM:\SOFTWARE\PSADT.Test\<Name>\InstallMarker = '1'` (REG_SZ, **native 64-bit hive**) | MSI `Registry` table (component KeyPath) | uninstall removes the whole key |
| Real file | `%ProgramData%\PSADT.Test\<Name>\Installed.ini` | MSI `IniFile` table (`WriteIniValues`) | `RemoveIniValues` on uninstall |
| Folders | `%ProgramData%\PSADT.Test\<Name>` | `CreateFolder` table | `RemoveFile` table on uninstall |
| Product registration | Add/Remove Programs (visible to `Get-ADTApplication`) | `RegisterProduct`/`PublishProduct`/`PublishFeatures` | uninstall |

> **64-bit hive:** the package is authored x64 (`Template = 'x64;1033'`) with a 64-bit component
> (`msidbComponentAttributes64bit`), so on a 64-bit OS the registry marker lands in the **native**
> `HKLM\SOFTWARE\PSADT.Test` hive, not `WOW6432Node`. On a 32-bit OS msiexec installs it natively.

> **No upgrade actions / no `File` table:** the execute sequence deliberately omits
> `FindRelatedProducts`/`RemoveExistingProducts` (no `Upgrade` table) and the package carries no
> `File`/cabinet payload. A `Media` table with a single no-cabinet row is present because
> `RegisterProduct` queries it.

**`SIMULATEFAIL=1` contract.** Passing the public property `SIMULATEFAIL=1` on the msiexec command
line (e.g. via `-AdditionalArgumentList 'SIMULATEFAIL=1'`) trips a `LaunchCondition`
(`SIMULATEFAIL <> "1"`). The install fails **before `InstallInitialize`**, msiexec returns
**1603**, and **no artifacts** are written. Through `Start-ADTMsiProcess` this surfaces as:
- a terminating error (`Execution failed with exit code [1603].`) under the default `-ErrorAction`, or
- a returned `ProcessResult` with `ExitCode = 1603` when `-PassThru -ErrorAction SilentlyContinue` is used.

```powershell
$msi  = New-ADTTestInstallMsi -Path "$env:TEMP\PSADT.Test\app.msi"
$desc = $msi   # descriptor: Path, ProductName, ProductCode, UpgradeCode, RegistryKey,
               #             RegistryValueName, InstallFolder, InstalledFile
Start-ADTMsiProcess -Action Install   -FilePath $desc.Path
Start-ADTMsiProcess -Action Uninstall -FilePath $desc.Path
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
