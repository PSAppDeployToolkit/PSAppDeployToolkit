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

    .PARAMETER Path
        The path to the software cache folder.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return objects.

    .EXAMPLE
        Remove-ADTContentFromCache -Path "$envWinDir\Temp\PSAppDeployToolkit"

        Removes the toolkit content from the specified cache folder.

    .NOTES
        An active ADT session is required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Remove-ADTContentFromCache
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Path = "$((Get-ADTConfig).Toolkit.CachePath)\$((Get-ADTSession).InstallName)"
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
        if (![System.IO.Directory]::Exists($Path))
        {
            Write-ADTLogEntry -Message "Cache folder [$Path] does not exist."
            return
        }

        Write-ADTLogEntry -Message "Removing cache folder [$Path]."
        try
        {
            try
            {
                Remove-Item -Path $Path -Recurse -Force
                $adtSession.DirFiles = [System.IO.Path]::Combine($scriptDir, 'Files')
                $adtSession.DirSupportFiles = [System.IO.Path]::Combine($scriptDir, 'SupportFiles')
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to remove cache folder [$Path]."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
