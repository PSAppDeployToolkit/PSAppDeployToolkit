#-----------------------------------------------------------------------------
#
# MARK: Install-ADTMSUpdates
#
#-----------------------------------------------------------------------------

function Install-ADTMSUpdates
{
    <#
    .SYNOPSIS
        Install all Microsoft Updates in a given directory.

    .DESCRIPTION
        Install all Microsoft Updates of type ".msu" in a given directory (recursively searches directory).

    .PARAMETER LiteralPath
        Directory containing the updates.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any objects.

    .EXAMPLE
        Install-ADTMSUpdates -LiteralPath "$($adtSession.DirFiles)\MSUpdates"

        Installs all Microsoft Updates found in the specified directory.

    .NOTES
        An active ADT session is NOT required to use this function.

        This function supports the -WhatIf and -Confirm parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Install-ADTMSUpdates
    #>

    [CmdletBinding(SupportsShouldProcess = $true)]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateScript({
                if (!(Test-Path -LiteralPath $_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName LiteralPath -ProvidedValue $_ -ExceptionMessage 'The specified directory does not exist.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [Alias('Directory')]
        [System.String]$LiteralPath
    )

    begin
    {
        # Initialize function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        if (!($updates = if ([System.IO.Directory]::Exists($LiteralPath)) { Get-ChildItem @PSBoundParameters -Filter *.msu -Recurse -ErrorAction Ignore } else { $LiteralPath }))
        {
            $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName LiteralPath -ProvidedValue $_ -ExceptionMessage 'The specified path contains no updates.'))
        }
    }

    process
    {
        # Get all hotfixes and install if required.
        if ($updates -isnot [System.String])
        {
            Write-ADTLogEntry -Message "Recursively installing all Microsoft Updates in directory [$LiteralPath]."
        }
        else
        {
            Write-ADTLogEntry -Message "Installing Microsoft Update [$updates]."
        }
        foreach ($update in $updates)
        {
            if ($PSCmdlet.ShouldProcess("Microsoft Update [$($update.Name)]", 'Install'))
            {
                Start-ADTProcess -FilePath $update.FullName -ArgumentList '/quiet /norestart' -WindowStyle 'Hidden'
            }
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
