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
        Unregister a DLL file using regsvr32.exe. This function takes the path to the DLL file and attempts to unregister it using the regsvr32.exe utility.

    .PARAMETER FilePath
        Path to the DLL file.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return objects.

    .EXAMPLE
        Unregister-ADTDll -FilePath "C:\Test\DcTLSFileToDMSComp.dll"

        Unregisters the specified DLL file.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateScript({
                if (![System.IO.File]::Exists($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName FilePath -ProvidedValue $_ -ExceptionMessage 'The specified file does not exist.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [System.String]$FilePath
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
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
