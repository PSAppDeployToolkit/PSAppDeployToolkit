#-----------------------------------------------------------------------------
#
# MARK: Remove-ADTDesktopShortcut
#
#-----------------------------------------------------------------------------

function Remove-ADTDesktopShortcut
{
    <#
    .SYNOPSIS
        Removes desktop shortcuts from the common desktop folder or logged on user's desktop.

    .DESCRIPTION
        Removes desktop shortcuts from the common desktop folder or logged on user's desktop, either all since the commencement of the session, all shortcuts in general, or based on a custom FilterScript.

    .PARAMETER Scope
        The scope of which to target (common desktop and/or logged on user's desktop).

    .PARAMETER SinceSessionStart
        Removes all shortcuts created after the active deployment session commenced.

    .PARAMETER RemoveAllShortcuts
        Removes all shortcuts in the nominated scope(s).

    .PARAMETER FilterScript
        Removes all shortcuts that match the given FilterScript (filtration on any FileInfo property/method is available).

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function returns no output.

    .EXAMPLE
        Remove-ADTDesktopShortcut -SinceSessionStart

        This example removes all shortcuts created after the active deployment session started from the common desktop folder.

    .NOTES
        An active ADT session is required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Remove-ADTDesktopShortcut
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'SinceSessionStart', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'RemoveAllShortcuts', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false, ParameterSetName = "SinceSessionStart")]
        [Parameter(Mandatory = $false, ParameterSetName = "RemoveAllShortcuts")]
        [Parameter(Mandatory = $false, ParameterSetName = "FilterScript")]
        [ValidateSet('AllUsersDesktop', 'RunAsActiveUser')]
        [System.String[]]$Scope = 'AllUsersDesktop',

        [Parameter(Mandatory = $true, ParameterSetName = "SinceSessionStart")]
        [System.Management.Automation.SwitchParameter]$SinceSessionStart,

        [Parameter(Mandatory = $true, ParameterSetName = "RemoveAllShortcuts")]
        [System.Management.Automation.SwitchParameter]$RemoveAllShortcuts,

        [Parameter(Mandatory = $true, ParameterSetName = "FilterScript")]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ScriptBlock]$FilterScript
    )

    begin
    {
        # Initialise function. We depend on a session being active when we're removing shortcuts since session start.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $adtSession = try
        {
            Get-ADTSession
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }

        # Establish lookup table for the differing scopes.
        $adtEnv = Get-ADTEnvironmentTable
        $scopeTable = @{
            RunAsActiveUser = @{
                Path = if ($adtEnv.RunAsActiveUser)
                {
                    (Get-ADTUserProfiles -SID $adtEnv.RunAsActiveUser.SID -LoadProfilePaths -InformationAction SilentlyContinue).DesktopPath.FullName
                }
            }
            AllUsersDesktop = @{
                Path = $adtEnv.envCommonDesktop
            }
        }
    }

    process
    {
        # Process each scope.
        foreach ($desktop in $Scope)
        {
            # Get all shortcuts and filter the results down appropriately.
            $contents = Get-ChildItem -LiteralPath ($desktopPath = $scopeTable.$desktop.Path) -Filter *.lnk
            $shortcuts = switch ($PSCmdlet.ParameterSetName)
            {
                SinceSessionStart
                {
                    $contents | & {
                        process
                        {
                            if ($_.LastWriteTime -gt $adtSession.CurrentDateTime)
                            {
                                return $_
                            }
                        }
                    }
                    break
                }
                FilterScript
                {
                    $contents | Where-Object -FilterScript $FilterScript
                    break
                }
                RemoveAllShortcuts
                {
                    $contents
                    break
                }
            }

            # Only proceed if we have shortcuts to delete.
            if ($shortcuts)
            {
                # We have shortcuts to delete. Count them up and get started.
                Write-ADTLogEntry -Message "Removing [$(($shortcutsCount = ($shortcuts | Measure-Object).Count))] shortcut$(if ($shortcutsCount -gt 1) { 's' }) from path [$desktopPath]."
                try
                {
                    try
                    {
                        # Track how many failures we have. If all fail, throw entirely.
                        $failures = foreach ($shortcut in $shortcuts)
                        {
                            Write-ADTLogEntry -Message "Removing shortcut [$($shortcut.Name)]."
                            try
                            {
                                try
                                {
                                    $shortcut.Delete()
                                }
                                catch
                                {
                                    Write-Error -ErrorRecord $_
                                }
                            }
                            catch
                            {
                                Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to remove desktop shortcut [$($shortcut.FullName)].";
                                $_
                            }
                        }
                        if (($failures | Measure-Object).Count -eq $shortcutsCount)
                        {
                            $naerParams = @{
                                Exception = [System.AggregateException]::new("Failed to remove all desktop shortcuts from [$desktopPath].", [System.Exception[]]$failures.Exception)
                                Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                                ErrorId = 'ShortcutDeletionFullFailure'
                                TargetObject = $shortcuts
                                RecommendedAction = "Please review the errors, ensure you have sufficient access to the path in question, then try again."
                            }
                            throw (New-ADTErrorRecord @naerParams)
                        }
                    }
                    catch
                    {
                        Write-Error -ErrorRecord $_
                    }
                }
                catch
                {
                    Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
                }
            }
            else
            {
                Write-ADTLogEntry -Message "No shortcuts were found in path [$desktopPath]."
            }
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
