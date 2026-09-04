#-----------------------------------------------------------------------------
#
# MARK: Module Constants
#
#-----------------------------------------------------------------------------

# Anchored on $PSScriptRoot because .NET file APIs resolve a relative path against the process working
# directory, which the Set-Location some test files perform does not move.
New-Variable -Name ModuleRoot -Option Constant -Value ([System.IO.Path]::GetFullPath([System.IO.Path]::Combine($PSScriptRoot, '..', '..', '..', 'PSAppDeployToolkit')))
New-Variable -Name ModuleManifest -Option Constant -Value ([System.IO.Path]::Combine($Script:ModuleRoot, 'PSAppDeployToolkit.psd1'))

# The current user's uninstall key, which Get-ADTApplication searches alongside the two machine ones.
# Entries a test writes go here, so that finding and removing an application can be exercised for real
# without elevation and without touching anything outside the user running the tests. Held in both the
# provider and the native form, as reg.exe does not understand the drive-qualified one.
New-Variable -Name ADTTestApplicationRoot -Option Constant -Value 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall'
New-Variable -Name ADTTestApplicationRootNative -Option Constant -Value 'HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall'
New-Variable -Name ADTTestApplicationPrefix -Option Constant -Value 'ADTTestApplication'


#-----------------------------------------------------------------------------
#
# MARK: Import-ADTModuleUnderTest
#
#-----------------------------------------------------------------------------

function Import-ADTModuleUnderTest
{
    <#
    .SYNOPSIS
        Ensures the PSAppDeployToolkit module under test is loaded in the current runspace.

    .DESCRIPTION
        The `Import-ADTModuleUnderTest` function imports the module only when it is not already loaded, because a forced reload costs roughly six seconds and Pester runs every test file in the one runspace.

        Call it from both `BeforeDiscovery` and `BeforeAll` in any file whose discovery enumerates something from the module, as discovery runs before `BeforeAll` does.

    .PARAMETER Force
        Removes and reimports the module even when it is already loaded. Use this only in files that mutate the module's internal `$ADT` state, so the next file starts from a clean module.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        Import-ADTModuleUnderTest

        Imports the module if it is not already loaded, and does nothing otherwise.

    .EXAMPLE
        Import-ADTModuleUnderTest -Force

        Removes any loaded copy of the module and reimports it.
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Force
    )

    # A module loaded from somewhere else entirely is not the one under test, so treat that as a miss.
    $loaded = Get-Module -Name PSAppDeployToolkit
    if (!$Force -and $loaded -and $loaded.ModuleBase.Equals($Script:ModuleRoot, [System.StringComparison]::OrdinalIgnoreCase))
    {
        return
    }

    if ($loaded)
    {
        # A test that mocks Exit-ADTInvocation never reaches the Close-ADTClientServerProcess inside it, so
        # the client is left running and holds a handle on the module's own binaries. Closed here, because a
        # forced reload is where module state is meant to be discarded.
        & $loaded { if ($null -ne $ADT.ClientServerProcess) { Close-ADTClientServerProcess -InformationAction SilentlyContinue } }
        Remove-Module -ModuleInfo $loaded -Force
    }

    # -Global because Import-Module called from inside a module otherwise imports into that module's own
    # session state, leaving the module invisible to the test file that asked for it.
    Import-Module -Name $Script:ModuleManifest -Force -Global
}


#-----------------------------------------------------------------------------
#
# MARK: Test-ADTCallerElevated
#
#-----------------------------------------------------------------------------

function Test-ADTCallerElevated
{
    <#
    .SYNOPSIS
        Tests whether the current session is running elevated.

    .DESCRIPTION
        The `Test-ADTCallerElevated` function reports whether the caller holds the built-in Administrator role, so that tests requiring elevation can be skipped rather than failed on a session that does not.

        Gate with `-Skip:` on the result of this function rather than with `#Requires -RunAsAdministrator`, which fails the container instead of skipping it and therefore fails the build.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Boolean

        Returns $true when the caller is elevated, otherwise $false.

    .EXAMPLE
        Test-ADTCallerElevated

        Returns $true when the current session is elevated.
    #>

    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
    )

    # Deliberately not the module's own Test-ADTCallerIsAdmin. That function is itself under test, and
    # gating tests on the code they cover would let a regression in it silently skip its own coverage.
    $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    try
    {
        return [System.Security.Principal.WindowsPrincipal]::new($identity).IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)
    }
    finally
    {
        $identity.Dispose()
    }
}


#-----------------------------------------------------------------------------
#
# MARK: Resolve-ADTParameterName
#
#-----------------------------------------------------------------------------

function Resolve-ADTParameterName
{
    <#
    .SYNOPSIS
        Resolves a parameter name or alias to the name the command declares it under.

    .DESCRIPTION
        The `Resolve-ADTParameterName` function maps whichever spelling a caller would use at the command line onto the declared name, so that the rest of these helpers can match on one form. Names, aliases and the unambiguous abbreviations of either are all accepted, as they all are on the command line. Anything else throws, so that a parameter named wrongly in a test fails as the mistake it is rather than quietly changing what the test asks.
    #>

    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.Management.Automation.CommandInfo]$Command,

        [Parameter(Mandatory = $true)]
        [System.String]$Name
    )

    # An exact name or alias first, since either beats an abbreviation that could reach further.
    if ($Command.Parameters.ContainsKey($Name))
    {
        return $Command.Parameters.$Name.Name
    }
    foreach ($parameter in $Command.Parameters.Values)
    {
        if ($parameter.Aliases -contains $Name)
        {
            return $parameter.Name
        }
    }

    # Then an abbreviation, which the binder takes as long as only one parameter answers to it.
    $abbreviated = [System.Collections.Generic.List[System.String]]::new()
    foreach ($parameter in $Command.Parameters.Values)
    {
        foreach ($spelling in @($parameter.Name) + @($parameter.Aliases))
        {
            if ($spelling.StartsWith($Name, [System.StringComparison]::OrdinalIgnoreCase))
            {
                $abbreviated.Add($parameter.Name)
                break
            }
        }
    }
    if ($abbreviated.Count -eq 1)
    {
        return $abbreviated[0]
    }
    throw "The command [$($Command.Name)] has no parameter answering to [$Name]$(if ($abbreviated.Count) { ", which is ambiguous between ['$([System.String]::Join("', '", $abbreviated))']" })."
}


#-----------------------------------------------------------------------------
#
# MARK: Test-ADTMandatoryParameter
#
#-----------------------------------------------------------------------------

function Test-ADTMandatoryParameter
{
    <#
    .SYNOPSIS
        Tests whether a command declares a parameter as mandatory.

    .DESCRIPTION
        The `Test-ADTMandatoryParameter` function reports whether every parameter set that declares the named parameter declares it as mandatory.

        Ask this rather than calling the command and leaving the parameter out. A host able to prompt does exactly that when a mandatory parameter is missing, so a test written the other way round hangs the run waiting on input instead of failing, and only passes at all in a host that cannot prompt. `build.ps1` runs in a console that can.

    .PARAMETER Command
        The command to examine. Pass `Get-Command` output, obtaining it from inside `InModuleScope` for a command the module does not export.

    .PARAMETER Parameter
        The name of the parameter to examine.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Boolean

        Returns $true when every parameter set declaring the parameter declares it as mandatory, otherwise $false.

    .EXAMPLE
        Test-ADTMandatoryParameter -Command (Get-Command Register-ADTDll) -Parameter FilePath

        Returns $true, since a library to register has to be named.
    #>

    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.CommandInfo]$Command,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Parameter
    )

    # One ParameterAttribute per parameter set the parameter appears in, so all of them have to agree.
    $declarations = $Command.Parameters.(Resolve-ADTParameterName -Command $Command -Name $Parameter).Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] })
    return !!$declarations -and !$declarations.Where({ !$_.Mandatory })
}


#-----------------------------------------------------------------------------
#
# MARK: Test-ADTParameterSetSatisfied
#
#-----------------------------------------------------------------------------

function Test-ADTParameterSetSatisfied
{
    <#
    .SYNOPSIS
        Tests whether naming a given set of parameters is enough to resolve a call to one of a command's parameter sets.

    .DESCRIPTION
        The `Test-ADTParameterSetSatisfied` function reports whether any one of a command's parameter sets both accepts every parameter named and has every one of its own mandatory parameters among them. That is what PowerShell asks when it resolves a call, so a $false answer means the call cannot proceed as written.

        Ask this rather than making the call and seeing it refused. A host able to prompt asks for whatever mandatory parameter is still missing instead of failing, so a test written the other way round hangs the run waiting on input, and only passes at all in a host that cannot prompt. `build.ps1` runs in a console that can.

        Use this where the claim is about the shape of a call - that nothing satisfies it, or that two parameters do not go together. Use `Test-ADTMandatoryParameter` where the claim is about one parameter being required.

    .PARAMETER Command
        The command to examine. Pass `Get-Command` output, obtaining it from inside `InModuleScope` for a command the module does not export.

    .PARAMETER Parameter
        The names of the parameters the call names. Omit for a call that names none.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Boolean

        Returns $true when some parameter set is satisfied by exactly these parameters, otherwise $false.

    .EXAMPLE
        Test-ADTParameterSetSatisfied -Command (Get-Command Dismount-ADTWimFile)

        Returns $false, since either the mount path or the image path has to be named.

    .EXAMPLE
        Test-ADTParameterSetSatisfied -Command (Get-Command Send-ADTKeys) -Parameter WindowTitle

        Returns $false, since naming a window still leaves the keys to send unnamed.
    #>

    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.CommandInfo]$Command,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Parameter = @()
    )

    # Resolved up front so that the matching below compares one spelling against another, and so that a
    # parameter named wrongly in a test fails as the mistake it is rather than disqualifying every set and
    # reading as a call that cannot resolve.
    $named = $Parameter | & { process { Resolve-ADTParameterName -Command $Command -Name $_ } }

    foreach ($parameterSet in $Command.ParameterSets)
    {
        # Both halves of what the binder asks: the set has to accept everything named, and everything it
        # insists on has to be named.
        $accepted = !$named.Where({ $parameterSet.Parameters.Name -notcontains $_ })
        $supplied = !$parameterSet.Parameters.Where({ $_.IsMandatory }).Where({ $named -notcontains $_.Name })
        if ($accepted -and $supplied)
        {
            return $true
        }
    }
    return $false
}


#-----------------------------------------------------------------------------
#
# MARK: Initialize-ADTTestModule
#
#-----------------------------------------------------------------------------

function Initialize-ADTTestModule
{
    <#
    .SYNOPSIS
        Initialises the module under test with its log output redirected.

    .DESCRIPTION
        The `Initialize-ADTTestModule` function initialises the module and repoints its log, temp and cache directories at a location of the caller's choosing, so that opening a session writes under `TestDrive` rather than anywhere on the machine.

        Each path is set in both its ordinary and its no-admin-rights form, because the module picks between them on whether the caller is elevated, and a test should not write somewhere different depending on how it was run.

        Host logging is turned off as well, so a session's banner does not interleave itself through the test output. A test that needs to see that output can turn it back on for itself.

        Call `Import-ADTModuleUnderTest -Force` afterwards to discard the state this leaves behind.

    .PARAMETER Path
        The directory to write logs, temporary files and cached content into, normally `TestDrive`.

    .PARAMETER RegistryPath
        The registry key to write under, normally something below `TestRegistry`. Supply this in any test that reaches something persisting to the registry, such as deferral history, which otherwise writes to the machine's real toolkit key.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        Initialize-ADTTestModule -Path $TestDrive

        Initialises the module and keeps everything a session writes on the test's own drive.
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]$Path,

        [Parameter(Mandatory = $false)]
        [System.String]$RegistryPath
    )

    Initialize-ADTModule -InformationAction SilentlyContinue

    & (Get-Module -Name PSAppDeployToolkit) {
        foreach ($setting in 'LogPath', 'LogPathNoAdminRights', 'TempPath', 'TempPathNoAdminRights', 'CachePath', 'CachePathNoAdminRights')
        {
            $ADT.Config.Toolkit.$setting = [System.IO.Path]::Combine($args[0], $setting.Replace('NoAdminRights', [System.String]::Empty))
        }
        $ADT.Config.Toolkit.LogWriteToHost = $false

        if (![System.String]::IsNullOrWhiteSpace($args[1]))
        {
            $ADT.Config.Toolkit.RegPath = $ADT.Config.Toolkit.RegPathNoAdminRights = $args[1]
        }
    } $Path $RegistryPath
}


#-----------------------------------------------------------------------------
#
# MARK: New-ADTTestApplicationName
#
#-----------------------------------------------------------------------------

function New-ADTTestApplicationName
{
    <#
    .SYNOPSIS
        Invents a name for an application entry no machine could already carry.

    .DESCRIPTION
        The `New-ADTTestApplicationName` function returns a display name suffixed with a GUID, so that an entry written under it can never collide with, or be mistaken for, software installed on the machine, and a search for it can only find what the test itself created.

        Pass the result to `New-ADTTestApplicationEntry` to write the entry, and to `Get-ADTTestUninstallCommand` to build an uninstall program that removes it.

    .PARAMETER Suffix
        Text to append after the generated name, for a test needing the name to match something. Supply `Security Update` to have the entry read as a Microsoft update, for instance.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.String

        Returns the invented display name.

    .EXAMPLE
        New-ADTTestApplicationName

        Returns a name such as `ADTTestApplication5f2b1c9e4a7d4f0e8b3c6a1d2e5f8091`.
    #>

    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $false)]
        [System.String]$Suffix
    )

    # The prefix is what the cleanup matches on, so it has to be the leading text and never vary. A GUID in
    # 'N' form is hex, so appending one cannot introduce anything the update and hotfix filter looks for.
    return "$script:ADTTestApplicationPrefix$([System.Guid]::NewGuid().ToString('N'))$(if ($PSBoundParameters.ContainsKey('Suffix')) { " $Suffix" })"
}


#-----------------------------------------------------------------------------
#
# MARK: New-ADTTestApplicationEntry
#
#-----------------------------------------------------------------------------

function New-ADTTestApplicationEntry
{
    <#
    .SYNOPSIS
        Writes an installed application entry for a test to find.

    .DESCRIPTION
        The `New-ADTTestApplicationEntry` function writes an uninstall entry under the current user's own hive, which `Get-ADTApplication` searches alongside the two machine ones. Writing there is what lets a test exercise finding and removing an application without elevation and without touching anything outside the user running it.

        `DisplayName` is always written, as it is what the search keys off. Everything else is up to the caller, with integers written as DWORDs to match the real flags.

        Clean up with `Remove-ADTTestApplicationEntries`, which every test file writing entries should call from an `AfterEach`.

    .PARAMETER Name
        The entry's name, from `New-ADTTestApplicationName`. Used as both the key name and the display name.

    .PARAMETER Values
        The registry values to write alongside the display name, such as `QuietUninstallString` or `SystemComponent`.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        New-ADTTestApplicationEntry -Name $name -Values @{ QuietUninstallString = Get-ADTTestUninstallCommand -Name $name }

        Writes an entry whose uninstall program removes the entry itself.
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]$Name,

        [Parameter(Mandatory = $false)]
        [System.Collections.Hashtable]$Values = @{}
    )

    $null = New-Item -Path "$script:ADTTestApplicationRoot\$Name" -Force
    $null = New-ItemProperty -LiteralPath "$script:ADTTestApplicationRoot\$Name" -Name DisplayName -Value $Name -PropertyType String -Force
    foreach ($value in $Values.GetEnumerator())
    {
        $null = New-ItemProperty -LiteralPath "$script:ADTTestApplicationRoot\$Name" -Name $value.Key -Value $value.Value -PropertyType $(if ($value.Value -is [System.Int32]) { 'DWord' } else { 'String' }) -Force
    }
}


#-----------------------------------------------------------------------------
#
# MARK: Get-ADTTestUninstallCommand
#
#-----------------------------------------------------------------------------

function Get-ADTTestUninstallCommand
{
    <#
    .SYNOPSIS
        Builds an uninstall program that removes the entry naming it.

    .DESCRIPTION
        The `Get-ADTTestUninstallCommand` function returns a command line suitable for an entry's `UninstallString` or `QuietUninstallString`, which removes the entry it was written to. That is what an uninstaller does, and it is what lets a test assert a real removal rather than a mocked one while leaving nothing behind.

        `reg.exe` is asked to do the removal as it is on every machine and needs nothing set up.

    .PARAMETER Name
        The entry to remove, from `New-ADTTestApplicationName`.

    .PARAMETER Unqualified
        Name the program without saying where it lives, for a test covering resolution against the system directory. A command line written to the registry is not obliged to spell out the path, and most do not.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.String

        Returns the command line.

    .EXAMPLE
        Get-ADTTestUninstallCommand -Name $name

        Returns a command line that removes the entry called $name.
    #>

    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]$Name,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Unqualified
    )

    return "$(if ($Unqualified) { 'cmd.exe' } else { [System.IO.Path]::Combine([System.Environment]::SystemDirectory, 'cmd.exe') }) /c reg.exe delete `"$script:ADTTestApplicationRootNative\$Name`" /f"
}


#-----------------------------------------------------------------------------
#
# MARK: Get-ADTTestApplicationKeyPath
#
#-----------------------------------------------------------------------------

function Get-ADTTestApplicationKeyPath
{
    <#
    .SYNOPSIS
        Returns the registry path of a test's application entry.

    .DESCRIPTION
        The `Get-ADTTestApplicationKeyPath` function gives back the path the entry is written to, so that a test needing to name that key, such as one supplying its own arguments to an uninstall program, does not restate the uninstall key itself.

    .PARAMETER Name
        The entry to name, from `New-ADTTestApplicationName`.

    .PARAMETER Native
        Return the path in the form `reg.exe` understands, rather than the drive-qualified form the registry provider takes.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.String

        Returns the entry's registry path.

    .EXAMPLE
        Get-ADTTestApplicationKeyPath -Name $name -Native

        Returns a path such as `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ADTTestApplication...`.
    #>

    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]$Name,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Native
    )

    return "$(if ($Native) { $script:ADTTestApplicationRootNative } else { $script:ADTTestApplicationRoot })\$Name"
}


#-----------------------------------------------------------------------------
#
# MARK: Test-ADTTestApplicationEntry
#
#-----------------------------------------------------------------------------

function Test-ADTTestApplicationEntry
{
    <#
    .SYNOPSIS
        Tests whether a test's application entry is still there.

    .DESCRIPTION
        The `Test-ADTTestApplicationEntry` function reports whether the entry is present, which is how a test asserts that a removal happened, or that something declined to remove it.

    .PARAMETER Name
        The entry to look for, from `New-ADTTestApplicationName`.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Boolean

        Returns $true while the entry is present, otherwise $false.

    .EXAMPLE
        Test-ADTTestApplicationEntry -Name $name

        Returns $false once the entry has been removed.
    #>

    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]$Name
    )

    return Test-Path -LiteralPath "$script:ADTTestApplicationRoot\$Name"
}


#-----------------------------------------------------------------------------
#
# MARK: Remove-ADTTestApplicationEntries
#
#-----------------------------------------------------------------------------

function Remove-ADTTestApplicationEntries
{
    <#
    .SYNOPSIS
        Removes every application entry the tests have written.

    .DESCRIPTION
        The `Remove-ADTTestApplicationEntries` function removes the entries by the prefix they all carry, rather than by names a test recorded, so that a test which fell over before it could clean up still leaves nothing on the machine.

        Call it from an `AfterEach` in every test file writing entries, and from `AfterAll` as well.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        Remove-ADTTestApplicationEntries

        Removes every entry written by any test in the run.
    #>

    [CmdletBinding()]
    param
    (
    )

    Get-ChildItem -LiteralPath $script:ADTTestApplicationRoot -ErrorAction Ignore | Where-Object { $_.PSChildName.StartsWith($script:ADTTestApplicationPrefix) } | Remove-Item -Recurse -Force
}


#-----------------------------------------------------------------------------
#
# MARK: Module Exports
#
#-----------------------------------------------------------------------------

Export-ModuleMember -Function Import-ADTModuleUnderTest, Test-ADTCallerElevated, Test-ADTMandatoryParameter, Test-ADTParameterSetSatisfied, Initialize-ADTTestModule, Get-ADTTestApplicationKeyPath, New-ADTTestApplicationName, New-ADTTestApplicationEntry, Get-ADTTestUninstallCommand, Test-ADTTestApplicationEntry, Remove-ADTTestApplicationEntries
