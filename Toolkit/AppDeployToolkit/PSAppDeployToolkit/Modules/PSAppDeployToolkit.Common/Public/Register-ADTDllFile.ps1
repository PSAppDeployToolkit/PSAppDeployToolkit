function Register-ADTDllFile
{
    <#

    .SYNOPSIS
    Register a DLL file.

    .DESCRIPTION
    Register a DLL file using regsvr32.exe.

    .PARAMETER FilePath
    Path to the DLL file.

    .PARAMETER DLLAction
    Specify whether to register the DLL.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not return objects.

    .EXAMPLE
    # Register DLL file.
    Register-ADTDllFile -FilePath "C:\Test\DcTLSFileToDMSComp.dll"

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
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process {
        Invoke-ADTDllFileAction @PSBoundParameters -DLLAction Register
    }

    end {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
