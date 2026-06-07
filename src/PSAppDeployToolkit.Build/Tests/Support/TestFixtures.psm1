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
# MARK: New-ADTTestInstallMsi
#
#-----------------------------------------------------------------------------

function New-ADTTestInstallMsi
{
    <#

    .SYNOPSIS
    Authors a minimal but genuinely INSTALLING .msi (no cabinet required) for integration tests.

    .DESCRIPTION
    Creates a real, installable Windows Installer database at the supplied path using the
    WindowsInstaller.Installer COM object. Unlike New-ADTTestMsiDatabase (which only authors a
    property table for read-only tests), the package produced here installs successfully under
    msiexec and leaves real, verifiable artifacts on the machine, then removes every one of them
    on uninstall.

    INSTALL ARTIFACTS (all under a dedicated 'PSADT.Test' namespace):
      - A registry value: HKLM\SOFTWARE\PSADT.Test\<ProductName>\InstallMarker = '1' (REG_SZ).
        Authored via the MSI Registry table; this is the component KeyPath. msiexec removes the
        whole HKLM\SOFTWARE\PSADT.Test\<ProductName> key on uninstall.
      - A real file on disk: %ProgramData%\PSADT.Test\<ProductName>\Installed.ini, written via the
        MSI IniFile table (WriteIniValues on install, RemoveIniValues on uninstall). No cabinet or
        File-table payload is needed because an .ini file is materialised directly by the installer.
      - The namespaced folders (CommonAppDataFolder\PSADT.Test\<ProductName>) are created via the
        CreateFolder table and removed on uninstall via the RemoveFile table.

    The package also REGISTERS with Windows Installer (RegisterProduct / PublishProduct /
    PublishFeatures are sequenced), so the installed product is discoverable via Get-ADTApplication
    and msiexec /x by ProductCode.

    SIMULATEFAIL CONTRACT:
      Passing SIMULATEFAIL=1 on the msiexec command line trips a LaunchCondition that fails the
      install before InstallInitialize, so msiexec returns a non-zero exit code (1603) and NO
      artifacts are written. With SIMULATEFAIL unset (or 0) the install proceeds normally.

    A VALID GUID ProductCode and UpgradeCode are required for a real install; sensible fixture
    GUIDs are used by default. All COM handles are released and the GC is run before returning, so
    the file is left unlocked.

    .PARAMETER Path
    The full path where the MSI should be written (normally a temp path). The parent directory must
    already exist.

    .PARAMETER ProductName
    The ProductName property value, also used as the namespaced sub-key/sub-folder name. Defaults
    to 'PSADTTestApp'.

    .PARAMETER ProductCode
    A VALID GUID ProductCode. Defaults to a fixed fixture GUID.

    .PARAMETER UpgradeCode
    A VALID GUID UpgradeCode. Defaults to a fixed fixture GUID.

    .PARAMETER ProductVersion
    The ProductVersion. Defaults to '1.0.0'.

    .EXAMPLE
    $msi = New-ADTTestInstallMsi -Path "$env:TEMP\PSADT.Test\app.msi"
    Start-ADTMsiProcess -Action Install -FilePath $msi

    .OUTPUTS
    System.Management.Automation.PSCustomObject. An object describing the authored package and the
    exact artifacts it creates/removes:
      - Path             : the MSI path
      - ProductName      : the product name
      - ProductCode      : the ProductCode GUID (braced)
      - UpgradeCode      : the UpgradeCode GUID (braced)
      - RegistryKey      : the full HKLM key path created on install / removed on uninstall
      - RegistryValueName: the value name written under RegistryKey
      - InstallFolder    : the on-disk folder created on install / removed on uninstall
      - InstalledFile    : the full path of the .ini file created on install / removed on uninstall

    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Justification = 'This is a test-fixture authoring helper that only writes the MSI to a caller-supplied path; it does not itself install anything. ShouldProcess support is inappropriate.')]
    [CmdletBinding()]
    [OutputType([System.Management.Automation.PSCustomObject])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Path,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ProductName = 'PSADTTestApp',

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ProductCode = '{B2C3D4E5-6F70-4811-9233-445566778899}',

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$UpgradeCode = '{C3D4E5F6-7081-4922-A344-5566778899AA}',

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ProductVersion = '1.0.0'
    )

    # The namespaced artifact names. These mirror exactly what the authored tables below produce so
    # callers (and the returned descriptor) can verify/clean up unambiguously.
    $registryKey = "HKLM:\SOFTWARE\PSADT.Test\$ProductName"
    $registryValueName = 'InstallMarker'
    $installFolder = [System.IO.Path]::Combine($env:ProgramData, 'PSADT.Test', $ProductName)
    $installedFile = [System.IO.Path]::Combine($installFolder, 'Installed.ini')

    # Local helper: execute an arbitrary SQL statement against the database (no parameters).
    $execSql = {
        param($Db, [System.String]$Sql)
        $view = $Db.GetType().InvokeMember('OpenView', [System.Reflection.BindingFlags]::InvokeMethod, $null, $Db, @($Sql))
        $null = $view.GetType().InvokeMember('Execute', [System.Reflection.BindingFlags]::InvokeMethod, $null, $view, @([System.Reflection.Missing]::Value))
        $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($view)
    }

    # Local helper: INSERT a row whose values are all supplied as an ordered string/int array. The
    # caller passes the full INSERT statement and a matching array of field values; CreateRecord is
    # sized to the array and each field is set via StringData (strings) or IntegerData (integers).
    $insertRow = {
        param($Db, $Installer, [System.String]$Sql, [System.Object[]]$Values)
        $view = $Db.GetType().InvokeMember('OpenView', [System.Reflection.BindingFlags]::InvokeMethod, $null, $Db, @($Sql))
        $rec = $Installer.GetType().InvokeMember('CreateRecord', [System.Reflection.BindingFlags]::InvokeMethod, $null, $Installer, @($Values.Count))
        for ($i = 0; $i -lt $Values.Count; $i++)
        {
            $field = $i + 1
            $val = $Values[$i]
            if ($null -eq $val)
            {
                continue
            }
            if ($val -is [System.Int32])
            {
                $null = $rec.GetType().InvokeMember('IntegerData', [System.Reflection.BindingFlags]::SetProperty, $null, $rec, @($field, [System.Int32]$val))
            }
            else
            {
                $null = $rec.GetType().InvokeMember('StringData', [System.Reflection.BindingFlags]::SetProperty, $null, $rec, @($field, [System.String]$val))
            }
        }
        $null = $view.GetType().InvokeMember('Execute', [System.Reflection.BindingFlags]::InvokeMethod, $null, $view, @($rec))
        $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($rec)
        $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($view)
    }

    $installer = $null
    $db = $null
    try
    {
        $installer = New-Object -ComObject WindowsInstaller.Installer

        # Mode 3 = msiOpenDatabaseModeCreateDirect (create + transact).
        $db = $installer.GetType().InvokeMember('OpenDatabase', [System.Reflection.BindingFlags]::InvokeMethod, $null, $installer, @([System.String]$Path, 3))

        # --- SummaryInformation stream -----------------------------------------------------------
        # Template 'x64;1033' marks an x64, en-US package so the HKLM registry marker is written to
        # the native 64-bit hive (HKLM\SOFTWARE\PSADT.Test) rather than WOW6432Node on a 64-bit OS.
        # On a 32-bit OS, msiexec ignores the x64 platform tag and installs natively. Word Count = 2
        # sets the 'source files compressed' hint (irrelevant for a no-payload package but harmless).
        # PageCount 200 is the minimum installer version (Windows Installer 2.0).
        $si = $db.GetType().InvokeMember('SummaryInformation', [System.Reflection.BindingFlags]::GetProperty, $null, $db, @(20))
        $null = $si.GetType().InvokeMember('Property', [System.Reflection.BindingFlags]::SetProperty, $null, $si, @(2, "$ProductName Installer"))
        $null = $si.GetType().InvokeMember('Property', [System.Reflection.BindingFlags]::SetProperty, $null, $si, @(3, 'PSAppDeployToolkit Test Suite'))
        $null = $si.GetType().InvokeMember('Property', [System.Reflection.BindingFlags]::SetProperty, $null, $si, @(4, 'PSAppDeployToolkit Test Suite'))
        $null = $si.GetType().InvokeMember('Property', [System.Reflection.BindingFlags]::SetProperty, $null, $si, @(7, 'x64;1033'))
        $null = $si.GetType().InvokeMember('Property', [System.Reflection.BindingFlags]::SetProperty, $null, $si, @(9, [System.Guid]::NewGuid().ToString('B').ToUpperInvariant()))
        $null = $si.GetType().InvokeMember('Property', [System.Reflection.BindingFlags]::SetProperty, $null, $si, @(14, 200))
        $null = $si.GetType().InvokeMember('Property', [System.Reflection.BindingFlags]::SetProperty, $null, $si, @(15, 2))
        $null = $si.GetType().InvokeMember('Persist', [System.Reflection.BindingFlags]::InvokeMethod, $null, $si, @())
        $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($si)
        $si = $null

        # --- Table creation (DDL) ----------------------------------------------------------------
        & $execSql $db 'CREATE TABLE `Property` (`Property` CHAR(72) NOT NULL, `Value` CHAR(0) NOT NULL PRIMARY KEY `Property`)'
        & $execSql $db 'CREATE TABLE `Directory` (`Directory` CHAR(72) NOT NULL, `Directory_Parent` CHAR(72), `DefaultDir` CHAR(255) NOT NULL PRIMARY KEY `Directory`)'
        & $execSql $db 'CREATE TABLE `Feature` (`Feature` CHAR(38) NOT NULL, `Feature_Parent` CHAR(38), `Title` CHAR(64), `Description` CHAR(255), `Display` INT, `Level` INT NOT NULL, `Directory_` CHAR(72), `Attributes` INT NOT NULL PRIMARY KEY `Feature`)'
        & $execSql $db 'CREATE TABLE `Component` (`Component` CHAR(72) NOT NULL, `ComponentId` CHAR(38), `Directory_` CHAR(72) NOT NULL, `Attributes` INT NOT NULL, `Condition` CHAR(255), `KeyPath` CHAR(72) PRIMARY KEY `Component`)'
        & $execSql $db 'CREATE TABLE `FeatureComponents` (`Feature_` CHAR(38) NOT NULL, `Component_` CHAR(72) NOT NULL PRIMARY KEY `Feature_`, `Component_`)'
        & $execSql $db 'CREATE TABLE `Registry` (`Registry` CHAR(72) NOT NULL, `Root` INT NOT NULL, `Key` CHAR(255) NOT NULL, `Name` CHAR(255), `Value` CHAR(0), `Component_` CHAR(72) NOT NULL PRIMARY KEY `Registry`)'
        & $execSql $db 'CREATE TABLE `IniFile` (`IniFile` CHAR(72) NOT NULL, `FileName` CHAR(255) NOT NULL, `DirProperty` CHAR(72), `Section` CHAR(96) NOT NULL, `Key` CHAR(128) NOT NULL, `Value` CHAR(255) NOT NULL, `Action` INT NOT NULL, `Component_` CHAR(72) NOT NULL PRIMARY KEY `IniFile`)'
        & $execSql $db 'CREATE TABLE `CreateFolder` (`Directory_` CHAR(72) NOT NULL, `Component_` CHAR(72) NOT NULL PRIMARY KEY `Directory_`, `Component_`)'
        & $execSql $db 'CREATE TABLE `RemoveFile` (`FileKey` CHAR(72) NOT NULL, `Component_` CHAR(72) NOT NULL, `FileName` CHAR(255), `DirProperty` CHAR(72) NOT NULL, `InstallMode` INT NOT NULL PRIMARY KEY `FileKey`)'
        & $execSql $db 'CREATE TABLE `Media` (`DiskId` INT NOT NULL, `LastSequence` INT NOT NULL, `DiskPrompt` CHAR(64), `Cabinet` CHAR(255), `VolumeLabel` CHAR(32), `Source` CHAR(72) PRIMARY KEY `DiskId`)'
        & $execSql $db 'CREATE TABLE `LaunchCondition` (`Condition` CHAR(255) NOT NULL, `Description` CHAR(255) NOT NULL PRIMARY KEY `Condition`)'
        & $execSql $db 'CREATE TABLE `InstallExecuteSequence` (`Action` CHAR(72) NOT NULL, `Condition` CHAR(255), `Sequence` INT PRIMARY KEY `Action`)'
        & $execSql $db 'CREATE TABLE `AdminExecuteSequence` (`Action` CHAR(72) NOT NULL, `Condition` CHAR(255), `Sequence` INT PRIMARY KEY `Action`)'
        & $execSql $db 'CREATE TABLE `AdvtExecuteSequence` (`Action` CHAR(72) NOT NULL, `Condition` CHAR(255), `Sequence` INT PRIMARY KEY `Action`)'

        # --- Property table ----------------------------------------------------------------------
        # ALLUSERS=1 forces a per-machine install (writes to HKLM, registers machine-wide).
        # A stable component GUID is required so the same package always owns the same artifacts.
        $componentGuid = '{D4E5F607-8192-4A33-B455-66778899AABB}'
        $properties = [ordered]@{
            ProductName = $ProductName
            ProductCode = $ProductCode
            ProductVersion = $ProductVersion
            UpgradeCode = $UpgradeCode
            Manufacturer = 'PSAppDeployToolkit Test Suite'
            ProductLanguage = '1033'
            ALLUSERS = '1'
        }
        foreach ($key in $properties.Keys)
        {
            & $insertRow $db $installer 'INSERT INTO `Property` (`Property`, `Value`) VALUES (?, ?)' @([System.String]$key, [System.String]$properties[$key])
        }

        # --- Directory table ---------------------------------------------------------------------
        # TARGETDIR (root) -> CommonAppDataFolder (%ProgramData%) -> PSADT.Test -> <ProductName>.
        & $insertRow $db $installer 'INSERT INTO `Directory` (`Directory`, `Directory_Parent`, `DefaultDir`) VALUES (?, ?, ?)' @('TARGETDIR', $null, 'SourceDir')
        & $insertRow $db $installer 'INSERT INTO `Directory` (`Directory`, `Directory_Parent`, `DefaultDir`) VALUES (?, ?, ?)' @('CommonAppDataFolder', 'TARGETDIR', '.')
        & $insertRow $db $installer 'INSERT INTO `Directory` (`Directory`, `Directory_Parent`, `DefaultDir`) VALUES (?, ?, ?)' @('NSDIR', 'CommonAppDataFolder', 'PSADT.Test')
        & $insertRow $db $installer 'INSERT INTO `Directory` (`Directory`, `Directory_Parent`, `DefaultDir`) VALUES (?, ?, ?)' @('INSTALLDIR', 'NSDIR', $ProductName)

        # --- Media (no cabinet; required by RegisterProduct's Media query) -----------------------
        & $insertRow $db $installer 'INSERT INTO `Media` (`DiskId`, `LastSequence`, `DiskPrompt`, `Cabinet`, `VolumeLabel`, `Source`) VALUES (?, ?, ?, ?, ?, ?)' @([System.Int32]1, [System.Int32]0, $null, $null, $null, $null)

        # --- Feature / Component / mapping -------------------------------------------------------
        # Feature Level 1 = installed by default. Component Attributes:
        #   4   = msidbComponentAttributesRegistryKeyPath (KeyPath is a registry value, so no
        #         File/cabinet is required for a valid component), plus
        #   256 = msidbComponentAttributes64bit (write the registry marker to the NATIVE 64-bit hive
        #         HKLM\SOFTWARE\PSADT.Test rather than WOW6432Node on a 64-bit OS). This pairs with
        #         the x64 package template. On a 32-bit OS msiexec treats the component as native.
        & $insertRow $db $installer 'INSERT INTO `Feature` (`Feature`, `Feature_Parent`, `Title`, `Description`, `Display`, `Level`, `Directory_`, `Attributes`) VALUES (?, ?, ?, ?, ?, ?, ?, ?)' @('MainFeature', $null, 'Main', 'Main feature', [System.Int32]1, [System.Int32]1, 'INSTALLDIR', [System.Int32]0)
        & $insertRow $db $installer 'INSERT INTO `Component` (`Component`, `ComponentId`, `Directory_`, `Attributes`, `Condition`, `KeyPath`) VALUES (?, ?, ?, ?, ?, ?)' @('MainComponent', $componentGuid, 'INSTALLDIR', [System.Int32]260, $null, 'RegMarker')
        & $insertRow $db $installer 'INSERT INTO `FeatureComponents` (`Feature_`, `Component_`) VALUES (?, ?)' @('MainFeature', 'MainComponent')

        # --- Registry (the HKLM marker; Root 2 = HKEY_LOCAL_MACHINE) ------------------------------
        # This is the component KeyPath ('RegMarker'). msiexec removes the key on uninstall.
        & $insertRow $db $installer 'INSERT INTO `Registry` (`Registry`, `Root`, `Key`, `Name`, `Value`, `Component_`) VALUES (?, ?, ?, ?, ?, ?)' @('RegMarker', [System.Int32]2, "SOFTWARE\PSADT.Test\$ProductName", $registryValueName, '1', 'MainComponent')

        # --- IniFile (the real on-disk file, written into INSTALLDIR; Action 0 = AddLine) ---------
        & $insertRow $db $installer 'INSERT INTO `IniFile` (`IniFile`, `FileName`, `DirProperty`, `Section`, `Key`, `Value`, `Action`, `Component_`) VALUES (?, ?, ?, ?, ?, ?, ?, ?)' @('IniMarker', 'Installed.ini', 'INSTALLDIR', 'PSADT', 'Installed', '1', [System.Int32]0, 'MainComponent')

        # --- CreateFolder + RemoveFile (own + clean up the namespaced folder) ---------------------
        & $insertRow $db $installer 'INSERT INTO `CreateFolder` (`Directory_`, `Component_`) VALUES (?, ?)' @('INSTALLDIR', 'MainComponent')
        # InstallMode 2 = msidbRemoveFileInstallModeOnRemove (remove on uninstall). FileName null = the folder itself.
        & $insertRow $db $installer 'INSERT INTO `RemoveFile` (`FileKey`, `Component_`, `FileName`, `DirProperty`, `InstallMode`) VALUES (?, ?, ?, ?, ?)' @('RemoveInstallDir', 'MainComponent', $null, 'INSTALLDIR', [System.Int32]2)
        & $insertRow $db $installer 'INSERT INTO `RemoveFile` (`FileKey`, `Component_`, `FileName`, `DirProperty`, `InstallMode`) VALUES (?, ?, ?, ?, ?)' @('RemoveNsDir', 'MainComponent', $null, 'NSDIR', [System.Int32]2)

        # --- LaunchCondition (SIMULATEFAIL contract) ---------------------------------------------
        # The install proceeds only when SIMULATEFAIL is not '1'. When SIMULATEFAIL=1 this condition
        # is false, LaunchConditions fails, and msiexec returns non-zero (1603) before any artifact
        # is written.
        & $insertRow $db $installer 'INSERT INTO `LaunchCondition` (`Condition`, `Description`) VALUES (?, ?)' @('SIMULATEFAIL <> "1"', 'SIMULATEFAIL=1 was specified, failing the installation on purpose for testing.')

        # --- InstallExecuteSequence --------------------------------------------------------------
        # A complete, valid execute sequence (no upgrade actions, so no Upgrade table is needed).
        # RegisterProduct/PublishFeatures/PublishProduct make
        # the product discoverable (Add/Remove Programs, Get-ADTApplication). WriteIniValues and
        # RemoveIniValues materialise/remove the .ini file. RemoveRegistryValues/WriteRegistryValues
        # handle the registry marker. RemoveFiles handles the folder cleanup on uninstall.
        # An [ordered] map of action -> sequence number keeps each entry flat (no nested-array
        # flattening pitfalls). All conditions are null here.
        $seq = [ordered]@{
            LaunchConditions = 100
            CostInitialize = 800
            FileCost = 900
            CostFinalize = 1000
            InstallValidate = 1400
            InstallInitialize = 1500
            ProcessComponents = 1600
            RemoveRegistryValues = 2600
            RemoveIniValues = 3100
            RemoveFiles = 3500
            CreateFolders = 3700
            WriteRegistryValues = 5000
            WriteIniValues = 5100
            RegisterUser = 6000
            RegisterProduct = 6100
            PublishFeatures = 6300
            PublishProduct = 6400
            InstallFinalize = 6600
        }
        foreach ($action in $seq.Keys)
        {
            & $insertRow $db $installer 'INSERT INTO `InstallExecuteSequence` (`Action`, `Condition`, `Sequence`) VALUES (?, ?, ?)' @([System.String]$action, $null, [System.Int32]$seq[$action])
        }

        $null = $db.GetType().InvokeMember('Commit', [System.Reflection.BindingFlags]::InvokeMethod, $null, $db, @())
    }
    finally
    {
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

    return [PSCustomObject]@{
        Path = $Path
        ProductName = $ProductName
        ProductCode = $ProductCode
        UpgradeCode = $UpgradeCode
        RegistryKey = $registryKey
        RegistryValueName = $registryValueName
        InstallFolder = $installFolder
        InstalledFile = $installedFile
    }
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

Export-ModuleMember -Function Get-ADTFakeInstaller, New-ADTTestMsiDatabase, New-ADTTestInstallMsi, New-ADTTestRegFile, New-ADTTestWim
