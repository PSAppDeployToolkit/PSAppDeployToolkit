---
applyTo: "**/*.ps1,**/*.psm1,**/*.psd1"
---

# PowerShell Coding Conventions for PSAppDeployToolkit

## PowerShell 5.1 Compatibility

All code **must** work on both Windows PowerShell 5.1 and PowerShell 7+.

- **Do NOT use PowerShell 7-only syntax**: no ternary (`? :`), null-coalescing (`??` / `??=`), pipeline chain (`&&` / `||`), or null-conditional (`?.`) operators.
- Use `[System.Management.Automation.Language.NullString]::Value` instead of `$null` when passing null strings to .NET methods that accept `[string]` parameters (e.g., `.Replace('text', [System.Management.Automation.Language.NullString]::Value)`).
- PSScriptAnalyzer enforces `PSUseCompatibleCmdlets` with the `desktop-5.1.14393.206-windows` compatibility profile. Avoid cmdlets or parameters exclusive to PowerShell 7+.

## Repository Defaults

- Root `.editorconfig` settings apply to PowerShell files: UTF-8 with BOM, 4-space indentation, trimmed trailing whitespace, and a final newline.
- Preserve the existing public/private file layout under `src/PSAppDeployToolkit/Public` and `src/PSAppDeployToolkit/Private`.
- Avoid style-only churn. Match the surrounding file unless there is a repository-wide reason to normalize the code.

## Public Contract Stability

Public function contracts are part of the module surface area and should stay backward compatible wherever practical.

- On feature branches from `main`, avoid breaking changes to public parameter names, parameter behaviour, and output types unless there is a strong reason and no lower-risk alternative.
- If a public parameter must be renamed, use aliases where possible to preserve existing callers.
- If a function is substantially redesigned, prefer introducing a new function and converting the old function into a compatibility wrapper that preserves the existing parameters and output expectations.
- If you change a function's behaviour internally, keep the existing output contract unless the change is intentional, necessary, and clearly accounted for.
- On release branches such as `4.1.x`, avoid breaking changes to public function contracts at all costs.

## Function Structure & Lifecycle

Every function follows a strict `begin` / `process` / `end` lifecycle pattern with standardised helper calls.

### File Header

Every `.ps1` file starts with a `# MARK:` header for IDE navigation:

```powershell
#-----------------------------------------------------------------------------
#
# MARK: Verb-ADTNoun
#
#-----------------------------------------------------------------------------
```

### Standard Template

```powershell
function Verb-ADTNoun
{
    <# ... comment-based help ... #>

    [CmdletBinding()]
    param
    (
    )

    begin
    {
        # Initialize function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        try
        {
            try
            {
                # Main logic here.
            }
            catch
            {
                # Re-writing the ErrorRecord with Write-Error ensures the correct PositionMessage is used.
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            # Process the caught error, log it and throw depending on the specified ErrorAction.
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
```

### Lifecycle Helpers

| Helper | Where | Purpose |
| ------- | ----- | ------- |
| `Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState` | `begin` | Initialises logging, archives the caller's `ErrorActionPreference`, and forces `$ErrorActionPreference = 'Stop'` inside the function. |
| `Complete-ADTFunction -Cmdlet $PSCmdlet` | `end` | Writes a debug "Function End" log entry and restores any saved state. |
| `Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "..."` | outer `catch` | Logs the error and either throws or writes a non-terminating error based on the caller's original `ErrorAction`. |

### Continue-on-Error Functions

For functions that should **not** stop on error by default (e.g., cleanup or query operations), pass `-ErrorAction SilentlyContinue` to `Initialize-ADTFunction`:

```powershell
begin
{
    # Make this function continue on error.
    Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
}
```

Examples: `Remove-ADTFile`, `Remove-ADTFolder`, `Remove-ADTRegistryKey`, `Get-ADTRegistryKey`, `ConvertTo-ADTNTAccountOrSID`, `Update-ADTGroupPolicy`.

### Naming Conventions

- **Public functions**: always use the `ADT` noun prefix - `Get-ADTApplication`, `Remove-ADTFile`, `Start-ADTProcess`.
- **Private functions**: use the `Private:` scope prefix in the function name - `function Private:Resolve-ADTFileSystemPath`.
- **One function per file**. File name matches function name.
- **Public commands** should explicitly set `SupportsShouldProcess` to match behaviour. Mutating commands typically use `$true`; read-only or helper commands typically use `$false`.

## Error Handling

### Nested try/catch Pattern

The nested `try`/`catch` is the standard error-handling pattern for the `process` block:

1. **Inner `catch`**: re-writes the `ErrorRecord` via `Write-Error -ErrorRecord $_` so that the `PositionMessage` correctly points to the calling line.
2. **Outer `catch`**: delegates to `Invoke-ADTFunctionErrorHandler` which logs the error and either throws or writes a non-terminating error depending on the caller's `ErrorAction`.

### Structured Error Records

Create structured errors using `New-ADTErrorRecord` with splatted parameters:

```powershell
$naerParams = @{
    Exception = [System.IO.FileNotFoundException]::new("File [$FilePath] not found.", $FilePath)
    Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
    ErrorId = 'FilePathNotFound'
    TargetObject = $FilePath
    RecommendedAction = "Please confirm the path of the file and try again."
}
throw (New-ADTErrorRecord @naerParams)
```

### Terminating Errors in `begin` Blocks

Use `$PSCmdlet.ThrowTerminatingError()` for fatal errors in `begin`:

```powershell
$PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
```

### ErrorAction Propagation

When calling `Invoke-ADTFunctionErrorHandler`, propagate the caller's explicit `-ErrorAction` if present:

```powershell
catch
{
    $iafehParams = @{
        Cmdlet = $PSCmdlet
        SessionState = $ExecutionContext.SessionState
        ErrorRecord = $_
        LogMessage = "Failed to perform operation."
    }
    if ($PSBoundParameters.ContainsKey('ErrorAction'))
    {
        $iafehParams.Add('ErrorAction', $PSBoundParameters.ErrorAction)
    }
    Invoke-ADTFunctionErrorHandler @iafehParams
}
```

### ValidateScript Error Records

For `[ValidateScript()]` blocks that enforce domain-specific constraints (beyond null/whitespace - use custom validators for that), use `New-ADTValidateScriptErrorRecord`:

```powershell
[ValidateScript({
    if (!$_.Exists)
    {
        $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName File -ProvidedValue $_ -ExceptionMessage 'The specified file does not exist.'))
    }
    if (!$_.VersionInfo -or (!$_.VersionInfo.FileVersion -and !$_.VersionInfo.ProductVersion))
    {
        $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName File -ProvidedValue $_ -ExceptionMessage 'The specified file does not have any version info.'))
    }
    return !!$_.VersionInfo
})]
[System.IO.FileInfo]$File
```

## Session Management

- Functions that **require** an active ADT session should obtain it in `begin` with a try/catch guard:

```powershell
begin
{
    try
    {
        $adtSession = Get-ADTSession
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
    Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
}
```

- Functions that **optionally** use a session:

```powershell
$adtSession = Initialize-ADTModuleIfUninitialized -Cmdlet $PSCmdlet -PassThruActiveSession
```

- To check if a session is active without throwing: `Test-ADTSessionActive`

## Parameter Validation & Decorators

Design parameters deliberately to match how the command is used in the module and by callers.

- If a command supports multiple invocation shapes, define explicit parameter sets and set `DefaultParameterSetName` when needed.
- If a parameter truly supports property binding or pipeline input, declare `ValueFromPipeline` and `ValueFromPipelineByPropertyName` intentionally rather than by habit.
- If a path parameter supports wildcards, mark it with `[SupportsWildcards()]` and resolve the wildcard behaviour explicitly in the function body.

### Custom PSADT Validators

| Attribute | Use When |
| --------- | -------- |
| `[PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]` | String parameters that must not be null, empty, or whitespace. Also works on string collections. |
| `[PSAppDeployToolkit.Attributes.AllowNullButNotEmptyOrWhiteSpace()]` | Optional string parameters where `$null` is acceptable but empty/whitespace is not. |
| `[PSAppDeployToolkit.Attributes.ValidateGreaterThanZero()]` | Numeric parameters that must be positive. |

### Fully Qualified Type Names

Always use fully qualified .NET type names in param blocks and code - never use PowerShell type accelerators:

```powershell
# Correct
[System.String]$Path
[System.Management.Automation.SwitchParameter]$Recurse
[System.Int32]$Timeout

# Wrong
[string]$Path
[switch]$Recurse
[int]$Timeout
```

### SuppressMessageAttribute for Delegate-Used Parameters

Parameters used inside script blocks, delegates, or `.Where()` / `.ForEach()` calls that PSScriptAnalyzer cannot see must be suppressed:

```powershell
[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'ProductCode', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
```

## Performance Patterns

### Pipeline Filter (`& { process { } }`) over `ForEach-Object`

For pipeline filtering and transformation, use the scriptblock invocation pattern instead of `ForEach-Object` for better performance:

```powershell
# Preferred - faster
$collection | & { process { if ($_.Status -eq 'Running') { return $_ } } }

# Avoid
$collection | ForEach-Object { if ($_.Status -eq 'Running') { $_ } }
```

### .Where() and .ForEach() over Where-Object and ForEach-Object

For collections already in memory, use the `.Where()` and `.ForEach()` intrinsic methods:

```powershell
# Preferred - operates on the full collection in memory
$items.Where({ $_.Name -eq 'Target' })
$items.ForEach({ $_.ToString() })

# .Where() also supports mode parameters
$items.Where({ $_ -is [System.Management.Automation.ErrorRecord] }, 'First', 1)
```

### Prefer .NET Static Methods

Use .NET static methods where they are more elegant or provide better performance:

```powershell
# Preferred
[System.IO.Path]::Combine($baseDir, 'Fonts')
[System.String]::Join("', '", $items)
[System.String]::IsNullOrWhiteSpace($value)
[System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::Windows)
[System.Environment]::SystemDirectory

# Instead of
Join-Path -Path $baseDir -ChildPath 'Fonts'
$items -join "', '"
```

## Helper Functions & Utilities

### Logging

```powershell
Write-ADTLogEntry -Message "Installing application..."
Write-ADTLogEntry -Message "A problem occurred." -Severity Warning
Write-ADTLogEntry -Message "Operation failed." -Severity Error
Write-ADTLogEntry -Message "Verbose internal detail." -DebugMessage
```

### Path Resolution

`Resolve-ADTFileSystemPath` resolves files and directories with automatic search across session paths, the current directory, and the system `PATH`:

```powershell
$resolvedPath = Resolve-ADTFileSystemPath -LiteralPath $FilePath -File -DefaultExtension '.exe'
$resolvedDir = Resolve-ADTFileSystemPath -LiteralPath $DirPath -Directory
```

### Bound Parameters & Defaults

`Get-ADTBoundParametersAndDefaultValues` returns a `$PSBoundParameters`-compatible dictionary that includes default values:

```powershell
$params = Get-ADTBoundParametersAndDefaultValues -Invocation $MyInvocation
$params = Get-ADTBoundParametersAndDefaultValues -Invocation $MyInvocation -HelpMessage 'New/Set-ItemProperty parameter'
```

Use this when forwarding parameters to helper functions or internal commands and you need defaulted values to participate in the call.

### Internal Command Table

The module pre-builds a `$Script:CommandTable` dictionary mapping command names to `CommandInfo` objects. Reference internal commands through this table when needed (e.g., callbacks):

```powershell
Add-ADTModuleCallback -Hookpoint OnFinish -Callback $Script:CommandTable.'Close-ADTClientServerProcess'
```

### Output Suppression

Suppress unwanted pipeline output with `$null = ...`:

```powershell
$null = Remove-Item -LiteralPath $path -Force
$null = [PSADT.Utilities.FontUtilities]::AddFont($destPath)
```

Suppress verbose output from internal calls with `-InformationAction SilentlyContinue`:

```powershell
Get-ADTApplication -ProductCode $code -InformationAction SilentlyContinue
```

Use this selectively for internal calls that would otherwise create noisy informational output in higher-level commands.

## Comment-Based Help

Required for all **public** functions. Include all standard sections:

```powershell
<#
.SYNOPSIS
    Brief one-line description.

.DESCRIPTION
    Detailed description of what the function does.

.PARAMETER Name
    Description of the parameter.

.INPUTS
    None

    You cannot pipe objects to this function.

.OUTPUTS
    None

    This function does not return any output.

.EXAMPLE
    Verb-ADTNoun -Name 'Value'

    Description of what this example does.

.NOTES
    An active ADT session is NOT required to use this function.

    Tags: psadt<br />
    Website: https://psappdeploytoolkit.com<br />
    Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
    License: https://opensource.org/license/lgpl-3-0

.LINK
    https://psappdeploytoolkit.com
#>
```

- `.NOTES` must state whether an active ADT session is required.
- Use `[CmdletBinding()]` on all functions.
- Declare `[OutputType()]` when the function produces output. Use multiple `[OutputType()]` attributes when a function can return multiple types.
- Private helper functions do not need full comment-based help unless they are intentionally user-facing or unusually complex, but they should still follow the file header and naming conventions.
- Keep examples realistic and aligned with ADT session variables, common path patterns, and real module usage.

## Code Style

- **Formatting**: Use the repository's existing Allman-style braces and 4-space indentation.
- **Encoding**: All `.ps1`, `.psm1`, `.psd1` files **must** be UTF-8 with BOM.
- **One function per file**: file name matches function name.
- **Splatting**: use hashtable splatting for complex parameter passing (`@params`).
- **ShouldProcess**: for public state-changing operations, add `[CmdletBinding(SupportsShouldProcess = $true)]` and call `$PSCmdlet.ShouldProcess()` around the mutation. Match existing repo patterns for action text and targets.
- **Logging**: public functions are expected to log meaningful state transitions and exceptional conditions with `Write-ADTLogEntry`, while keeping noise low in tight loops.
- **Public function behaviour**: prefer explicit command contracts. If a function returns nothing, keep the pipeline clean. If it returns data, document and type it consistently.
