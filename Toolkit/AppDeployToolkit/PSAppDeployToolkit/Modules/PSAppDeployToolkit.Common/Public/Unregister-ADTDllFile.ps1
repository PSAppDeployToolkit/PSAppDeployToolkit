function Unregister-ADTDllFile
{
    <#

    .SYNOPSIS
    Unregister a DLL file.

    .DESCRIPTION
    Unregister a DLL file using regsvr32.exe.

    .PARAMETER FilePath
    Path to the DLL file.

    .PARAMETER DLLAction
    Specify whether to register the DLL.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not return objects.

    .EXAMPLE
    # Unregister DLL file.
    Unregister-ADTDllFile -FilePath "C:\Test\DcTLSFileToDMSComp.dll"

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param (
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

    begin {
        Initialize-ADTFunction -Cmdlet $PSCmdlet
    }

    process {
        Invoke-ADTDllFileAction @PSBoundParameters -DLLAction Unregister
    }

    end {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
