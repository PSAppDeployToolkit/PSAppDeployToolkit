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
        Send-ADTKeys -WindowTitle 'foobar - Notepad' -Keys 'Hello world' WaitDuration 00:00:05

        Send the sequence of keys "Hello world" to the application titled "foobar - Notepad" and wait 5 seconds.

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
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        http://msdn.microsoft.com/en-us/library/System.Windows.Forms.SendKeys(v=vs.100).aspx

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Send-ADTKeys
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'WindowTitle', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'WindowHandle', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'WindowTitle')]
        [ValidateNotNullOrEmpty()]
        [System.String]$WindowTitle,

        [Parameter(Mandatory = $true, ParameterSetName = 'WindowHandle')]
        [ValidateNotNullOrEmpty()]
        [System.IntPtr]$WindowHandle,

        [Parameter(Mandatory = $true, ParameterSetName = 'WindowTitle')]
        [Parameter(Mandatory = $true, ParameterSetName = 'WindowHandle')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Keys,

        [Parameter(Mandatory = $false, ParameterSetName = 'WindowTitle')]
        [Parameter(Mandatory = $false, ParameterSetName = 'WindowHandle')]
        [ValidateNotNullOrEmpty()]
        [System.TimeSpan]$WaitDuration
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $gawtParams = @{ $PSCmdlet.ParameterSetName = $PSBoundParameters.($PSCmdlet.ParameterSetName) }
    }

    process
    {
        # Bypass if no one's logged onto the device.
        if (!($runAsActiveUser = Get-ADTClientServerUser -AllowSystemFallback))
        {
            Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) as there is no active user logged onto the system."
            return
        }

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
                    # Send the Key sequence.
                    Write-ADTLogEntry -Message "Sending key(s) [$Keys] to window title [$($window.WindowTitle)] with window handle [$($window.WindowHandle)]."
                    Invoke-ADTClientServerOperation -SendKeys -User $runAsActiveUser -Options ([PSADT.Types.SendKeysOptions]::new($window.WindowHandle, $Keys))
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
