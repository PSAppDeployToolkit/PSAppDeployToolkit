#-----------------------------------------------------------------------------
#
# MARK: Initialize-ADTModule
#
#-----------------------------------------------------------------------------

function Initialize-ADTModule
{
    <#
    .SYNOPSIS
        Initializes the ADT module by setting up necessary configurations and environment.

    .DESCRIPTION
        The Initialize-ADTModule function sets up the environment for the ADT module by initializing necessary variables, configurations, and string tables. It ensures that the module is not initialized while there is an active ADT session in progress. This function prepares the module for use by clearing callbacks, sessions, and setting up the environment table.

    .PARAMETER ScriptDirectory
        An override directory to use for config and string loading.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        Initialize-ADTModule

        Initializes the ADT module with the default settings and configurations.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateScript({
                if ([System.String]::IsNullOrWhiteSpace($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName ScriptDirectory -ProvidedValue $_ -ExceptionMessage 'The specified input is null or empty.'))
                }
                if (![System.IO.Directory]::Exists($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName ScriptDirectory -ProvidedValue $_ -ExceptionMessage 'The specified directory does not exist.'))
                }
                return $_
            })]
        [System.String]$ScriptDirectory
    )

    begin
    {
        # Log our start time to clock the module init duration.
        $moduleInitStart = [System.DateTime]::Now

        # Ensure this function isn't being called mid-flight.
        if (Test-ADTSessionActive)
        {
            $naerParams = @{
                Exception = [System.InvalidOperationException]::new("This function cannot be called while there is an active ADTSession in progress.")
                Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                ErrorId = 'InitWithActiveSessionError'
                TargetObject = Get-ADTSession
                RecommendedAction = "Please attempt module re-initialization once the active ADTSession(s) have been closed."
            }
            $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
        }
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $adtData = Get-ADTModuleData
    }

    process
    {
        try
        {
            try
            {
                # Specify the base directory used when searching for config and string tables.
                $adtData.Directories.Script = if ($PSBoundParameters.ContainsKey('ScriptDirectory'))
                {
                    $ScriptDirectory
                }
                else
                {
                    $adtData.Directories.Defaults.Script
                }

                # Initialize remaining directory paths.
                'Config', 'Strings' | & {
                    process
                    {
                        $adtData.Directories.$_ = if ([System.IO.File]::Exists([System.IO.Path]::Combine($adtData.Directories.Script, $_, "$($_.ToLower()).psd1")))
                        {
                            [System.IO.Path]::Combine($adtData.Directories.Script, $_)
                        }
                        else
                        {
                            $adtData.Directories.Defaults.$_
                        }
                    }
                }

                # Initialize the module's global state.
                $adtData.Callbacks.Starting.Clear()
                $adtData.Callbacks.Opening.Clear()
                $adtData.Callbacks.Closing.Clear()
                $adtData.Callbacks.Finishing.Clear()
                $adtData.Sessions.Clear()
                $adtData.Environment = New-ADTEnvironmentTable
                $adtData.Config = Import-ADTConfig -BaseDirectory $adtData.Directories.Config
                $adtData.Language = Get-ADTStringLanguage
                $adtData.Strings = Import-ADTModuleDataFile -BaseDirectory $adtData.Directories.Strings -FileName strings.psd1 -UICulture $adtData.Language
                $adtData.LastExitCode = 0
                $adtData.TerminalServerMode = $false

                # Mark the environment table as read-only before finishing.
                $adtData.Environment = $adtData.Environment.AsReadOnly()
                $adtData.Durations.ModuleInit = [System.DateTime]::Now - $moduleInitStart
                $adtData.Initialized = $true
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

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
