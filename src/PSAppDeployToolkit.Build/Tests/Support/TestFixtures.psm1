<#

.SYNOPSIS
Shared real-fixture toolkit for PSAppDeployToolkit unit and integration tests.

.DESCRIPTION
Provides helper functions that author lightweight, real on-disk fixtures for use by the
Pester test suites. Everything written by these helpers targets a caller-supplied path
(normally $TestDrive) - none of these helpers mutate the host machine (no registry, no
services, no global state, no machine-wide installs).

This file is intentionally a .psm1 (NOT a *.Tests.ps1) and lives under Tests\Support, which
is a sibling of Tests\Unit. The unit-test runner (Invoke-ADTPesterUnitTesting) points
Pester's Run.Path at Tests\Unit only and collects *.Tests.ps1 files, so nothing in this
folder is ever picked up as a test.

PSAppDeployToolkit is licensed under the GNU LGPLv3 License - © 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).

This program is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the
Free Software Foundation, either version 3 of the License, or any later version. This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License
for more details. You should have received a copy of the GNU Lesser General Public License along with this program. If not, see <http://www.gnu.org/licenses/>.

.LINK
https://psappdeploytoolkit.com

#>

#-----------------------------------------------------------------------------
#
# MARK: Module-scope state
#
#-----------------------------------------------------------------------------

# Per-run cache of compiled FakeInstaller.exe paths, keyed by the (lower-cased) output path.
# This prevents recompiling the same executable repeatedly within a single test run.
$Script:FakeInstallerCache = @{}

# C# source for the fake installer console application. Kept deliberately tiny and tolerant of
# argument order. The contract is documented in Get-ADTFakeInstaller and the Support README.
$Script:FakeInstallerSource = @'
using System;
using System.IO;
using System.Threading;

internal static class FakeInstaller
{
    private static int Main(string[] args)
    {
        int exitCode = 0;
        int sleepMs = 0;
        string stdoutText = null;
        string stderrText = null;
        string writeFile = null;
        int failTimes = 0;
        string stateFile = null;

        // Tolerant, order-independent flag parser. Each known flag consumes its value (if any).
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            switch (arg)
            {
                case "--exit-code":
                    if (i + 1 < args.Length) { int.TryParse(args[++i], out exitCode); }
                    break;
                case "--sleep":
                    if (i + 1 < args.Length) { int.TryParse(args[++i], out sleepMs); }
                    break;
                case "--stdout":
                    if (i + 1 < args.Length) { stdoutText = args[++i]; }
                    break;
                case "--stderr":
                    if (i + 1 < args.Length) { stderrText = args[++i]; }
                    break;
                case "--write-file":
                    if (i + 1 < args.Length) { writeFile = args[++i]; }
                    break;
                case "--fail-times":
                    if (i + 1 < args.Length) { int.TryParse(args[++i], out failTimes); }
                    break;
                case "--state-file":
                    if (i + 1 < args.Length) { stateFile = args[++i]; }
                    break;
                default:
                    // Unknown tokens are ignored to keep the parser tolerant.
                    break;
            }
        }

        if (sleepMs > 0)
        {
            Thread.Sleep(sleepMs);
        }

        if (stdoutText != null)
        {
            Console.Out.WriteLine(stdoutText);
        }

        if (stderrText != null)
        {
            Console.Error.WriteLine(stderrText);
        }

        if (writeFile != null)
        {
            File.WriteAllText(writeFile, "FakeInstaller marker");
        }

        // Transient-failure simulation for retry testing. Reads an integer counter from the
        // state file; while the counter is below failTimes it increments+rewrites and exits 1.
        // Once the counter has reached failTimes it stops failing and falls through to exitCode.
        if (failTimes > 0 && stateFile != null)
        {
            int count = 0;
            if (File.Exists(stateFile))
            {
                string raw = File.ReadAllText(stateFile).Trim();
                int.TryParse(raw, out count);
            }

            if (count < failTimes)
            {
                count++;
                File.WriteAllText(stateFile, count.ToString());
                return 1;
            }
        }

        return exitCode;
    }
}
'@

#-----------------------------------------------------------------------------
#
# MARK: Get-ADTFakeInstaller
#
#-----------------------------------------------------------------------------

function Get-ADTFakeInstaller
{
    <#

    .SYNOPSIS
    Compiles (on first use) and returns the path to a tiny FakeInstaller.exe console app.

    .DESCRIPTION
    Produces a real, runnable Win32 console executable that simulates an installer process for
    process-execution and retry tests. The executable is compiled once per output path per run
    and cached in module scope; subsequent calls for the same path return the cached path without
    recompiling. No machine mutation occurs - the only file written is the executable itself, at
    the supplied path (default: a file under $env:TEMP).

    The compiled executable understands the following command-line flags (order-independent):

      --exit-code N           Exit with code N (default 0).
      --sleep <ms>            Sleep <ms> milliseconds before exiting.
      --stdout <text>         Write <text> (plus newline) to stdout.
      --stderr <text>         Write <text> (plus newline) to stderr.
      --write-file <path>     Create/overwrite a marker file at <path> (writes ONLY where told).
      --fail-times N          Combined with --state-file: simulate N transient failures. While the
      --state-file <path>     counter in <path> is below N, increment+rewrite it and exit 1; once
                              the counter reaches N, stop failing and exit with --exit-code.

    .PARAMETER OutputPath
    The full path where FakeInstaller.exe should be compiled. Defaults to a unique path under
    $env:TEMP. The parent directory must already exist.

    .EXAMPLE
    $exe = Get-ADTFakeInstaller -OutputPath "$TestDrive\FakeInstaller.exe"
    & $exe --exit-code 3; $LASTEXITCODE  # -> 3

    .OUTPUTS
    System.String. The full path to the compiled FakeInstaller.exe.

    #>

    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$OutputPath = [System.IO.Path]::Combine($env:TEMP, "FakeInstaller_$([System.Guid]::NewGuid().ToString('N')).exe")
    )

    # Normalise the cache key so the same path resolves consistently within a run.
    $cacheKey = $OutputPath.ToLowerInvariant()

    # Cache hit: return the cached path if the executable is still present on disk.
    if ($Script:FakeInstallerCache.ContainsKey($cacheKey) -and (Test-Path -LiteralPath $OutputPath -PathType Leaf))
    {
        return $OutputPath
    }

    # Already exists on disk (e.g. from a prior run sharing a fixed path): cache and return it.
    if (Test-Path -LiteralPath $OutputPath -PathType Leaf)
    {
        $Script:FakeInstallerCache[$cacheKey] = $OutputPath
        return $OutputPath
    }

    # Compile the console application. Add-Type -OutputType ConsoleApplication is only supported
    # on Windows PowerShell (Desktop edition, full .NET Framework CodeDOM); PowerShell Core throws
    # 'assembly types ... not currently supported'. On Core we fall back to a transient dotnet-SDK
    # build, which is a documented build prerequisite for this repository.
    if ($PSEdition -eq 'Desktop')
    {
        Add-Type -TypeDefinition $Script:FakeInstallerSource -OutputType ConsoleApplication -OutputAssembly $OutputPath
    }
    else
    {
        Build-ADTFakeInstallerWithDotNet -OutputPath $OutputPath
    }

    $Script:FakeInstallerCache[$cacheKey] = $OutputPath
    return $OutputPath
}

#-----------------------------------------------------------------------------
#
# MARK: Build-ADTFakeInstallerWithDotNet (private)
#
#-----------------------------------------------------------------------------

function Build-ADTFakeInstallerWithDotNet
{
    <#

    .SYNOPSIS
    PowerShell-Core fallback that compiles FakeInstaller.exe via the .NET SDK.

    .DESCRIPTION
    Builds the fake installer console executable using a transient dotnet-SDK project, then copies
    the produced FakeInstaller.exe to the requested output path. Used only on PowerShell Core,
    where Add-Type -OutputType ConsoleApplication is unsupported. The transient build directory is
    created under $env:TEMP and removed afterwards; nothing is written outside that directory and
    the final output path.

    .PARAMETER OutputPath
    The full path where the compiled FakeInstaller.exe should be placed.

    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$OutputPath
    )

    if (!(Get-Command -Name dotnet -ErrorAction SilentlyContinue))
    {
        throw [System.InvalidOperationException]::new("Get-ADTFakeInstaller requires either Windows PowerShell (Desktop) or the .NET SDK ('dotnet') on PATH to compile the fake installer; neither is available.")
    }

    $buildRoot = [System.IO.Path]::Combine($env:TEMP, "FakeInstallerBuild_$([System.Guid]::NewGuid().ToString('N'))")
    $null = [System.IO.Directory]::CreateDirectory($buildRoot)
    try
    {
        # Author the minimal SDK project and program source. The angle brackets are assembled from
        # char codes to keep the surrounding tooling from misparsing literal XML element syntax.
        $lt = [System.Char]60
        $gt = [System.Char]62
        $csprojLines = @(
            ($lt + 'Project Sdk="Microsoft.NET.Sdk"' + $gt)
            ('  ' + $lt + 'PropertyGroup' + $gt)
            ('    ' + $lt + 'OutputType' + $gt + 'Exe' + $lt + '/OutputType' + $gt)
            ('    ' + $lt + 'TargetFramework' + $gt + 'net8.0' + $lt + '/TargetFramework' + $gt)
            ('    ' + $lt + 'AssemblyName' + $gt + 'FakeInstaller' + $lt + '/AssemblyName' + $gt)
            ('    ' + $lt + 'Nullable' + $gt + 'disable' + $lt + '/Nullable' + $gt)
            ('    ' + $lt + 'ImplicitUsings' + $gt + 'disable' + $lt + '/ImplicitUsings' + $gt)
            ('    ' + $lt + 'EnableDefaultCompileItems' + $gt + 'false' + $lt + '/EnableDefaultCompileItems' + $gt)
            ('  ' + $lt + '/PropertyGroup' + $gt)
            ('  ' + $lt + 'ItemGroup' + $gt)
            ('    ' + $lt + 'Compile Include="Program.cs" /' + $gt)
            ('  ' + $lt + '/ItemGroup' + $gt)
            ($lt + '/Project' + $gt)
        )
        $csprojPath = [System.IO.Path]::Combine($buildRoot, 'FakeInstaller.csproj')
        [System.IO.File]::WriteAllText($csprojPath, ($csprojLines -join "`r`n"))
        [System.IO.File]::WriteAllText([System.IO.Path]::Combine($buildRoot, 'Program.cs'), $Script:FakeInstallerSource)

        # Publish a self-contained, single-file executable so the returned path is one fully
        # runnable exe with no sidecar .dll/.runtimeconfig.json/.deps.json files. A framework-
        # dependent build produces an apphost .exe that fails with 'application to execute does
        # not exist' once separated from its sidecars, so single-file self-contained is required.
        $publishDir = [System.IO.Path]::Combine($buildRoot, 'out')
        $rid = if ([System.Environment]::Is64BitOperatingSystem) { 'win-x64' } else { 'win-x86' }
        $buildOutput = & dotnet publish $csprojPath -c Release -o $publishDir -r $rid --self-contained true -p:PublishSingleFile=true --nologo 2>&1
        $builtExe = [System.IO.Path]::Combine($publishDir, 'FakeInstaller.exe')
        if (!(Test-Path -LiteralPath $builtExe -PathType Leaf))
        {
            throw [System.InvalidOperationException]::new("dotnet publish did not produce FakeInstaller.exe. Output:`r`n$($buildOutput -join "`r`n")")
        }

        $destDir = [System.IO.Path]::GetDirectoryName($OutputPath)
        if (![System.String]::IsNullOrEmpty($destDir) -and !(Test-Path -LiteralPath $destDir))
        {
            $null = [System.IO.Directory]::CreateDirectory($destDir)
        }
        Copy-Item -LiteralPath $builtExe -Destination $OutputPath -Force
    }
    finally
    {
        Remove-Item -LiteralPath $buildRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

#-----------------------------------------------------------------------------
#
# MARK: New-ADTTestMsiDatabase
#
#-----------------------------------------------------------------------------

function New-ADTTestMsiDatabase
{
    <#

    .SYNOPSIS
    Authors a minimal but valid MSI database (Property table + summary information) on disk.

    .DESCRIPTION
    Creates a real .msi file at the supplied path using the WindowsInstaller.Installer COM object.
    A Property table is created and seeded with the supplied properties (plus ProductName and
    ProductCode), and a SummaryInformation stream is written so the file is a structurally valid
    installer database. All COM handles are released and the GC is run before the function returns,
    so the file is left unlocked.

    No machine mutation occurs - the database is only authored, never installed.

    COPY-BEFORE-READ GOTCHA: callers that read the produced file via the P/Invoke reader
    (MsiOpenDatabase, used by Get-ADTMsiTableProperty) must COPY the file to a fresh path first.
    Even after the COM handles are released, the P/Invoke layer can fail to open a file that was
    authored by the COM Installer in the same process. Copying produces an unlocked file that the
    P/Invoke reader opens cleanly.

    .PARAMETER Path
    The full path where the MSI should be written (normally a $TestDrive path). The parent
    directory must already exist.

    .PARAMETER Properties
    Optional hashtable of additional Property-table rows (name -> value) to seed.

    .PARAMETER ProductName
    The ProductName property value. Defaults to 'Fixture App'.

    .PARAMETER ProductCode
    The ProductCode property value (also written as the summary RevisionNumber). Defaults to a
    fixed fixture GUID.

    .EXAMPLE
    $msi = New-ADTTestMsiDatabase -Path "$TestDrive\test.msi" -Properties @{ ProductVersion = '1.2.3' }
    $copy = "$TestDrive\test_copy.msi"; Copy-Item $msi $copy   # copy before a P/Invoke read

    .OUTPUTS
    System.String. The full path to the authored MSI file.

    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Justification = 'This is a test-fixture authoring helper that only writes to a caller-supplied path; ShouldProcess support is inappropriate.')]
    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Path,

        [Parameter(Mandatory = $false)]
        [ValidateNotNull()]
        [System.Collections.Hashtable]$Properties = @{},

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ProductName = 'Fixture App',

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ProductCode = '{FIXTURE0-0000-0000-0000-000000000001}'
    )

    # Local helper: insert one Property row via a parameterised INSERT. Mirrors the pattern in
    # Get-ADTMsiTableProperty.Tests.ps1 / Set-ADTMsiProperty.Tests.ps1 exactly.
    $insertProperty = {
        param($Db, $Installer, [System.String]$Name, [System.String]$Value)
        $sqlInsert = 'INSERT INTO Property (Property, Value) VALUES (?, ?)'
        $iv = $Db.GetType().InvokeMember('OpenView', [System.Reflection.BindingFlags]::InvokeMethod, $null, $Db, @($sqlInsert))
        $ir = $Installer.GetType().InvokeMember('CreateRecord', [System.Reflection.BindingFlags]::InvokeMethod, $null, $Installer, @(2))
        $null = $ir.GetType().InvokeMember('StringData', [System.Reflection.BindingFlags]::SetProperty, $null, $ir, @(1, $Name))
        $null = $ir.GetType().InvokeMember('StringData', [System.Reflection.BindingFlags]::SetProperty, $null, $ir, @(2, $Value))
        $null = $iv.GetType().InvokeMember('Execute', [System.Reflection.BindingFlags]::InvokeMethod, $null, $iv, @($ir))
        $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($ir)
        $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($iv)
    }

    # Build the full property set: explicit ProductName/ProductCode plus any caller-supplied rows.
    # Caller-supplied ProductName/ProductCode (via -Properties) take precedence over the params.
    $allProperties = [ordered]@{
        ProductName = $ProductName
        ProductCode = $ProductCode
    }
    foreach ($key in $Properties.Keys)
    {
        $allProperties[[System.String]$key] = [System.String]$Properties[$key]
    }

    $installer = $null
    $db = $null
    try
    {
        $installer = New-Object -ComObject WindowsInstaller.Installer

        # Mode 3 = msiOpenDatabaseModeCreateDirect (create + transact), matching the existing tests.
        $db = $installer.GetType().InvokeMember('OpenDatabase', [System.Reflection.BindingFlags]::InvokeMethod, $null, $installer, @([System.String]$Path, 3))

        # Write the SummaryInformation stream (required for a valid MSI).
        # COM property indices: 2=Subject, 3=Author, 4=Title, 7=Template, 9=RevisionNumber, 14=PageCount, 15=WordCount.
        $si = $db.GetType().InvokeMember('SummaryInformation', [System.Reflection.BindingFlags]::GetProperty, $null, $db, @(10))
        $null = $si.GetType().InvokeMember('Property', [System.Reflection.BindingFlags]::SetProperty, $null, $si, @(2, $ProductCode))
        $null = $si.GetType().InvokeMember('Property', [System.Reflection.BindingFlags]::SetProperty, $null, $si, @(3, 'PSAppDeployToolkit Test Suite'))
        $null = $si.GetType().InvokeMember('Property', [System.Reflection.BindingFlags]::SetProperty, $null, $si, @(4, $ProductName))
        $null = $si.GetType().InvokeMember('Property', [System.Reflection.BindingFlags]::SetProperty, $null, $si, @(7, ';1033'))
        $null = $si.GetType().InvokeMember('Property', [System.Reflection.BindingFlags]::SetProperty, $null, $si, @(9, $ProductCode))
        $null = $si.GetType().InvokeMember('Property', [System.Reflection.BindingFlags]::SetProperty, $null, $si, @(14, 200))
        $null = $si.GetType().InvokeMember('Property', [System.Reflection.BindingFlags]::SetProperty, $null, $si, @(15, 2))
        $null = $si.GetType().InvokeMember('Persist', [System.Reflection.BindingFlags]::InvokeMethod, $null, $si, @())
        $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($si)
        $si = $null

        # Create the Property table.
        $sqlCreate = 'CREATE TABLE Property (Property CHAR(72) NOT NULL, Value CHAR(0) NOT NULL PRIMARY KEY Property)'
        $cv = $db.GetType().InvokeMember('OpenView', [System.Reflection.BindingFlags]::InvokeMethod, $null, $db, @($sqlCreate))
        $null = $cv.GetType().InvokeMember('Execute', [System.Reflection.BindingFlags]::InvokeMethod, $null, $cv, @([System.Reflection.Missing]::Value))
        $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($cv)
        $cv = $null

        # Seed every property row.
        foreach ($key in $allProperties.Keys)
        {
            & $insertProperty $db $installer $key $allProperties[$key]
        }

        $null = $db.GetType().InvokeMember('Commit', [System.Reflection.BindingFlags]::InvokeMethod, $null, $db, @())
    }
    finally
    {
        # Release every COM handle so the file lock is freed, then force a GC so finalizers run.
        if ($null -ne $db)
        {
            $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($db)
            $db = $null
        }
        if ($null -ne $installer)
        {
            $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($installer)
            $installer = $null
        }
        [System.GC]::Collect()
        [System.GC]::WaitForPendingFinalizers()
    }

    return $Path
}

#-----------------------------------------------------------------------------
#
# MARK: New-ADTTestRegFile
#
#-----------------------------------------------------------------------------

function New-ADTTestRegFile
{
    <#

    .SYNOPSIS
    Writes a valid Windows .reg file to the supplied path.

    .DESCRIPTION
    Produces a Windows Registry Editor v5.00 .reg file at the supplied path (normally a $TestDrive
    path). No machine mutation occurs - the file is authored on disk only and is never imported.

    The -Content parameter accepts either:
      - A [string], which is written verbatim as the body beneath the header, or
      - A [hashtable] keyed by full registry key path; each value is itself a hashtable of
        value-name -> value-data pairs (string values are emitted as REG_SZ).

    .PARAMETER Path
    The full path where the .reg file should be written. The parent directory must already exist.

    .PARAMETER Content
    Either a raw string body or a hashtable describing keys and their string values (see above).

    .EXAMPLE
    New-ADTTestRegFile -Path "$TestDrive\test.reg" -Content @{
        'HKEY_LOCAL_MACHINE\SOFTWARE\Fixture' = @{ Name = 'Value'; Version = '1.0' }
    }

    .OUTPUTS
    System.String. The full path to the authored .reg file.

    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Justification = 'This is a test-fixture authoring helper that only writes to a caller-supplied path; ShouldProcess support is inappropriate.')]
    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Path,

        [Parameter(Mandatory = $true)]
        [ValidateNotNull()]
        [System.Object]$Content
    )

    $header = 'Windows Registry Editor Version 5.00'
    $sb = [System.Text.StringBuilder]::new()
    $null = $sb.AppendLine($header)
    $null = $sb.AppendLine()

    if ($Content -is [System.Collections.Hashtable])
    {
        foreach ($keyPath in $Content.Keys)
        {
            $null = $sb.AppendLine("[$keyPath]")
            $values = $Content[$keyPath]
            if ($values -is [System.Collections.Hashtable])
            {
                foreach ($valueName in $values.Keys)
                {
                    # Emit a REG_SZ entry. Backslashes and quotes are escaped per .reg syntax.
                    $escaped = ([System.String]$values[$valueName]).Replace('\', '\\').Replace('"', '\"')
                    $null = $sb.AppendLine("`"$valueName`"=`"$escaped`"")
                }
            }
            $null = $sb.AppendLine()
        }
    }
    else
    {
        # Treat as a verbatim string body.
        $null = $sb.AppendLine([System.String]$Content)
    }

    # .reg files are conventionally UTF-16 LE; write with a BOM so REGEDIT parses them correctly.
    [System.IO.File]::WriteAllText($Path, $sb.ToString(), [System.Text.Encoding]::Unicode)

    return $Path
}

#-----------------------------------------------------------------------------
#
# MARK: New-ADTTestWim
#
#-----------------------------------------------------------------------------

function New-ADTTestWim
{
    <#

    .SYNOPSIS
    Captures a small source folder into a .wim image (integration tests only).

    .DESCRIPTION
    Captures the contents of -SourceFolder into a Windows image (.wim) at -Path using
    New-WindowsImage from the Dism module. This typically requires an elevated session; if the
    capture cannot be performed (Dism unavailable or insufficient privileges) the function throws
    a clear, actionable error rather than silently producing nothing.

    This helper is intended for INTEGRATION tests only and is never invoked at module load. No
    machine mutation occurs beyond writing the .wim at the supplied path.

    .PARAMETER SourceFolder
    The folder whose contents will be captured into the image.

    .PARAMETER Path
    The full path where the .wim file should be written. The parent directory must already exist.

    .EXAMPLE
    New-ADTTestWim -SourceFolder "$TestDrive\payload" -Path "$TestDrive\image.wim"

    .OUTPUTS
    System.String. The full path to the captured .wim file.

    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Justification = 'This is a test-fixture authoring helper that only writes to a caller-supplied path; ShouldProcess support is inappropriate.')]
    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$SourceFolder,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Path
    )

    if (!(Get-Command -Name New-WindowsImage -ErrorAction SilentlyContinue))
    {
        throw [System.InvalidOperationException]::new("New-ADTTestWim requires the Dism module's New-WindowsImage cmdlet, which is not available in this session.")
    }

    if (!(Test-Path -LiteralPath $SourceFolder -PathType Container))
    {
        throw [System.IO.DirectoryNotFoundException]::new("The source folder '$SourceFolder' does not exist.")
    }

    try
    {
        $null = New-WindowsImage -CapturePath $SourceFolder -ImagePath $Path -Name 'PSADT Test Image' -CompressionType None -ErrorAction Stop
    }
    catch
    {
        throw [System.InvalidOperationException]::new("Failed to capture '$SourceFolder' into a .wim. Capturing a Windows image typically requires an elevated session. Inner error: $($_.Exception.Message)", $_.Exception)
    }

    return $Path
}

#-----------------------------------------------------------------------------
#
# MARK: Exports
#
#-----------------------------------------------------------------------------

Export-ModuleMember -Function Get-ADTFakeInstaller, New-ADTTestMsiDatabase, New-ADTTestRegFile, New-ADTTestWim
