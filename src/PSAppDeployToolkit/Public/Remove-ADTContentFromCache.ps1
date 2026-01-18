#-----------------------------------------------------------------------------
#
# MARK: Remove-ADTContentFromCache
#
#-----------------------------------------------------------------------------

function Remove-ADTContentFromCache
{
    <#
    .SYNOPSIS
        Removes the toolkit content from the cache folder on the local machine and reverts the $adtSession.DirFiles and $adtSession.SupportFiles directory.

    .DESCRIPTION
        This function removes the toolkit content from the cache folder on the local machine. It also reverts the $adtSession.DirFiles and $adtSession.SupportFiles directory to their original state. If the specified cache folder does not exist, it logs a message and exits.

    .PARAMETER LiteralPath
        The path to the software cache folder.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return objects.

    .EXAMPLE
        Remove-ADTContentFromCache -LiteralPath "$envWinDir\Temp\PSAppDeployToolkit"

        Removes the toolkit content from the specified cache folder.

    .NOTES
        An active ADT session is required to use this function.

        This function supports the -WhatIf and -Confirm parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Remove-ADTContentFromCache
    #>

    [CmdletBinding(SupportsShouldProcess = $true)]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Alias('Path', 'PSPath')]
        [System.String]$LiteralPath = "$((Get-ADTConfig).Toolkit.CachePath)\$((Get-ADTSession).InstallName)"
    )

    begin
    {
        try
        {
            $adtSession = Get-ADTSession
            $scriptDir = Get-ADTSessionCacheScriptDirectory
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        if (!(Test-Path -LiteralPath $LiteralPath -PathType Container))
        {
            Write-ADTLogEntry -Message "Cache folder [$LiteralPath] does not exist."
            return
        }

        Write-ADTLogEntry -Message "Removing cache folder [$LiteralPath]."
        if (!$PSCmdlet.ShouldProcess($LiteralPath, 'Remove cache folder'))
        {
            return
        }
        try
        {
            try
            {
                Remove-Item -LiteralPath $LiteralPath -Recurse -Force
                $adtSession.DirFiles = Join-Path -Path $scriptDir -ChildPath Files
                $adtSession.DirSupportFiles = Join-Path -Path $scriptDir -ChildPath SupportFiles
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to remove cache folder [$LiteralPath]."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
