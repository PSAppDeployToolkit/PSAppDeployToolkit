#-----------------------------------------------------------------------------
#
# MARK: Register-ADTDll
#
#-----------------------------------------------------------------------------

function Register-ADTDll
{
    <#
    .SYNOPSIS
        Register a DLL file.

    .DESCRIPTION
        This function registers a DLL file using regsvr32.exe. It ensures that the specified DLL file exists before attempting to register it. If the file does not exist, it throws an error.

    .PARAMETER FilePath
        Path to the DLL file.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return objects.

    .EXAMPLE
        Register-ADTDll -FilePath "C:\Test\DcTLSFileToDMSComp.dll"

        Registers the specified DLL file.

    .NOTES
        An active ADT session is NOT required to use this function.

        This function supports the -WhatIf and -Confirm parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Register-ADTDll
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
        [System.String]$FilePath
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        if (!$PSCmdlet.ShouldProcess("DLL [$FilePath]", 'Register'))
        {
            return
        }
        try
        {
            Invoke-ADTRegSvr32 @PSBoundParameters -Action Register
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
