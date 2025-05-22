#-----------------------------------------------------------------------------
#
# MARK: Send-ADTKeys
#
#-----------------------------------------------------------------------------

function Send-ADTKeys
{
    <#
    .SYNOPSIS
        Send a sequence of keys to one or more application windows.

    .DESCRIPTION
        Send a sequence of keys to one or more application windows. If the window title searched for returns more than one window, then all of them will receive the sent keys.

        Function does not work in SYSTEM context unless launched with "psexec.exe -s -i" to run it as an interactive process under the SYSTEM account.

    .PARAMETER WindowTitle
        The title of the application window to search for using regex matching.

    .PARAMETER GetAllWindowTitles
        Get titles for all open windows on the system.

    .PARAMETER WindowHandle
        Send keys to a specific window where the Window Handle is already known.

    .PARAMETER Keys
        The sequence of keys to send. Info on Key input at: http://msdn.microsoft.com/en-us/library/System.Windows.Forms.SendKeys(v=vs.100).aspx

    .PARAMETER WaitSeconds
        This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0. Please use `-WaitDuration` instead.

    .PARAMETER WaitDuration
        An optional amount of time to wait after the sending of the keys.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any objects.

    .EXAMPLE
        Send-ADTKeys -WindowTitle 'foobar - Notepad' -Keys 'Hello world'

        Send the sequence of keys "Hello world" to the application titled "foobar - Notepad".

    .EXAMPLE
        Send-ADTKeys -WindowTitle 'foobar - Notepad' -Keys 'Hello world' WaitDuration (New-TimeSpan -Seconds 5)

        Send the sequence of keys "Hello world" to the application titled "foobar - Notepad" and wait 5 seconds.

    .EXAMPLE
        Send-ADTKeys -WindowHandle ([IntPtr]17368294) -Keys 'Hello World'

        Send the sequence of keys "Hello World" to the application with a Window Handle of '17368294'.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        http://msdn.microsoft.com/en-us/library/System.Windows.Forms.SendKeys(v=vs.100).aspx

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Send-ADTKeys
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '', Justification = "This function is appropriately named and we don't need PSScriptAnalyzer telling us otherwise.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, Position = 0, ParameterSetName = 'WindowTitle')]
        [AllowEmptyString()]
        [ValidateNotNull()]
        [System.String]$WindowTitle,

        [Parameter(Mandatory = $true, Position = 1, ParameterSetName = 'GetAllWindowTitles')]
        [System.Management.Automation.SwitchParameter]$GetAllWindowTitles,

        [Parameter(Mandatory = $true, Position = 2, ParameterSetName = 'WindowHandle')]
        [ValidateNotNullOrEmpty()]
        [System.IntPtr]$WindowHandle,

        [Parameter(Mandatory = $true, Position = 3, ParameterSetName = 'WindowTitle')]
        [Parameter(Mandatory = $true, Position = 3, ParameterSetName = 'GetAllWindowTitles')]
        [Parameter(Mandatory = $true, Position = 3, ParameterSetName = 'WindowHandle')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Keys,

        [Parameter(Mandatory = $false, Position = 4, ParameterSetName = 'WindowTitle')]
        [Parameter(Mandatory = $false, Position = 4, ParameterSetName = 'GetAllWindowTitles')]
        [Parameter(Mandatory = $false, Position = 4, ParameterSetName = 'WindowHandle')]
        [System.Obsolete("Please use 'WaitDuration' instead as this will be removed in PSAppDeployToolkit 4.2.0.")]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$WaitSeconds,

        [Parameter(Mandatory = $false, Position = 4, ParameterSetName = 'WindowTitle')]
        [Parameter(Mandatory = $false, Position = 4, ParameterSetName = 'GetAllWindowTitles')]
        [Parameter(Mandatory = $false, Position = 4, ParameterSetName = 'WindowHandle')]
        [ValidateNotNullOrEmpty()]
        [System.TimeSpan]$WaitDuration
    )

    begin
    {
        # Announce deprecation to callers.
        Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated and will be removed in PSAppDeployToolkit 4.2.0. Please raise a case at [https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/issues] if you require this function." -Severity 2

        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
        $gawtParams = @{ $PSCmdlet.ParameterSetName = Get-Variable -Name $PSCmdlet.ParameterSetName -ValueOnly }

        # Log the deprecation of -WaitSeconds to the log.
        if ($PSBoundParameters.ContainsKey('WaitSeconds'))
        {
            Write-ADTLogEntry -Message "The parameter [-WaitSeconds] is obsolete and will be removed in PSAppDeployToolkit 4.2.0. Please use [-WaitDuration] instead." -Severity 2
            if (!$PSBoundParameters.ContainsKey('WaitDuration'))
            {
                $WaitDuration = [System.TimeSpan]::FromSeconds($WaitSeconds)
            }
        }
    }

    process
    {
        # Get the specified windows.
        try
        {
            if (!($Windows = Get-ADTWindowTitle @gawtParams))
            {
                Write-ADTLogEntry -Message "No windows matching the specified input were discovered." -Severity 2
                return
            }
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }

        # Process each found window.
        foreach ($window in $Windows)
        {
            try
            {
                try
                {
                    # Bring the window to the foreground and make sure it's enabled.
                    if (![PSADT.Utilities.WindowUtilities]::BringWindowToFront($window.WindowHandle))
                    {
                        $naerParams = @{
                            Exception = [System.ApplicationException]::new('Failed to bring window to foreground.')
                            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                            ErrorId = 'WindowHandleForegroundError'
                            TargetObject = $window
                            RecommendedAction = "Please check the status of this window and try again."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }
                    if (![PSADT.LibraryInterfaces.User32]::IsWindowEnabled($window.WindowHandle))
                    {
                        $naerParams = @{
                            Exception = [System.ApplicationException]::new('Unable to send keys to window because it may be disabled due to a modal dialog being shown.')
                            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                            ErrorId = 'WindowHandleDisabledError'
                            TargetObject = $window
                            RecommendedAction = "Please check the status of this window and try again."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }

                    # Send the Key sequence.
                    Write-ADTLogEntry -Message "Sending key(s) [$Keys] to window title [$($window.WindowTitle)] with window handle [$($window.WindowHandle)]."
                    [System.Windows.Forms.SendKeys]::SendWait($Keys)
                    if ($WaitDuration)
                    {
                        Write-ADTLogEntry -Message "Sleeping for [$($WaitDuration.TotalSeconds)] seconds."
                        [System.Threading.Thread]::Sleep($WaitDuration)
                    }
                }
                catch
                {
                    Write-Error -ErrorRecord $_
                }
            }
            catch
            {
                Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to send keys to window title [$($window.WindowTitle)] with window handle [$($window.WindowHandle)]." -ErrorAction SilentlyContinue
            }
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
