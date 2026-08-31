#-----------------------------------------------------------------------------
#
# MARK: Module Constants
#
#-----------------------------------------------------------------------------

# Anchored on $PSScriptRoot because .NET file APIs resolve a relative path against the process working
# directory, which the Set-Location some test files perform does not move.
New-Variable -Name ModuleRoot -Option Constant -Value ([System.IO.Path]::GetFullPath([System.IO.Path]::Combine($PSScriptRoot, '..', '..', '..', 'PSAppDeployToolkit')))
New-Variable -Name ModuleManifest -Option Constant -Value ([System.IO.Path]::Combine($Script:ModuleRoot, 'PSAppDeployToolkit.psd1'))


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
# MARK: Module Exports
#
#-----------------------------------------------------------------------------

Export-ModuleMember -Function Import-ADTModuleUnderTest, Test-ADTCallerElevated
