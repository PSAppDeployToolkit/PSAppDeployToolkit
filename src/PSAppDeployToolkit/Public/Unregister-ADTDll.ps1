#-----------------------------------------------------------------------------
#
# MARK: Unregister-ADTDll
#
#-----------------------------------------------------------------------------

function Unregister-ADTDll
{
    <#
    .SYNOPSIS
        Unregister a DLL file.

    .DESCRIPTION
        The `Unregister-ADTDll` function unregisters a DLL file using regsvr32.exe. This function takes the path to the DLL file and attempts to unregister it using the regsvr32.exe utility.

    .PARAMETER FilePath
        Path to the DLL file.

    .PARAMETER PerUser
        Specifies that the DLL should be unregistered for the current user only by calling its DllInstall entry point with the 'user' argument (regsvr32.exe /u /n /i:user). If this function is running under the SYSTEM account, regsvr32.exe is executed in the context of the currently logged on user. Note that the DLL must support per-user registration via a DllInstall export for this to work.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return objects.

    .EXAMPLE
        Unregister-ADTDll -FilePath "C:\Test\DcTLSFileToDMSComp.dll"

        Unregisters the specified DLL file.

    .EXAMPLE
        Unregister-ADTDll -FilePath "C:\Test\DcTLSFileToDMSComp.dll" -PerUser

        Unregisters the specified DLL file for the currently logged on user only.

    .NOTES
        An active ADT session is NOT required to use this function.

        This function supports the `-WhatIf` and `-Confirm` parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Unregister-ADTDll

    .LINK
        https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/main/src/PSAppDeployToolkit/Public/Unregister-ADTDll.ps1
    #>

    [CmdletBinding(SupportsShouldProcess = $true)]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateScript({
                if (!(Test-Path -LiteralPath $_ -PathType Leaf))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName FilePath -ProvidedValue $_ -ExceptionMessage 'The specified file does not exist.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [System.String]$FilePath,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PerUser
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        if (!$PSCmdlet.ShouldProcess("DLL [$FilePath]", 'Unregister'))
        {
            return
        }
        try
        {
            Invoke-ADTRegSvr32 @PSBoundParameters -Action Unregister
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
