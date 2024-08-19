#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Register-ADTDllFile
{
    <#
    .SYNOPSIS
        Register a DLL file.

    .DESCRIPTION
        This function registers a DLL file using regsvr32.exe. It ensures that the specified DLL file exists before attempting to register it. If the file does not exist, it throws an error.

    .PARAMETER FilePath
        Path to the DLL file.

        Mandatory: True

    .INPUTS
        None

        This function does not take any pipeline input.

    .OUTPUTS
        None

        This function does not return objects.

    .EXAMPLE
        # Example 1
        Register-ADTDllFile -FilePath "C:\Test\DcTLSFileToDMSComp.dll"

        Registers the specified DLL file.

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
                return !!$_
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
            Invoke-ADTDllFileAction @PSBoundParameters -DLLAction Register
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
