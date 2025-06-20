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

    .PARAMETER AdditionalEnvironmentVariables
        A dictionary of key/value pairs to inject into the generated environment table.

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

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Initialize-ADTModule
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
                if (!(Test-Path -LiteralPath $_ -PathType Container))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName ScriptDirectory -ProvidedValue $_ -ExceptionMessage 'The specified directory does not exist.'))
                }
                return $_
            })]
        [System.String[]]$ScriptDirectory,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.IDictionary]$AdditionalEnvironmentVariables
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
        $null = $PSBoundParameters.Remove('ScriptDirectory')
    }

    process
    {
        try
        {
            try
            {
                # Specify the base directory used when searching for config and string tables.
                $Script:ADT.Directories.Script = if ($null -ne $ScriptDirectory)
                {
                    $ScriptDirectory
                }
                else
                {
                    $Script:ADT.Directories.Defaults.Script
                }

                # Initialize remaining directory paths.
                'Config', 'Strings' | & {
                    process
                    {
                        [System.String[]]$Script:ADT.Directories.$_ = foreach ($directory in $Script:ADT.Directories.Script)
                        {
                            if (Test-Path -LiteralPath (Join-Path -Path $directory -ChildPath "$_\$($_.ToLower()).psd1") -PathType Leaf)
                            {
                                Join-Path -Path $directory -ChildPath $_
                            }
                        }
                        if ($null -eq $Script:ADT.Directories.$_)
                        {
                            [System.String[]]$Script:ADT.Directories.$_ = $Script:ADT.Directories.Defaults.$_
                        }
                    }
                }

                # Invoke all callbacks.
                foreach ($callback in $($Script:ADT.Callbacks.([PSADT.Module.CallbackType]::OnInit)))
                {
                    & $callback
                }

                # Close out and reset any client/server process that exists. This should never occur, though.
                if ($null -ne $Script:ADT.ClientServerProcess)
                {
                    Close-ADTClientServerProcess -InformationAction SilentlyContinue
                }

                # Initialize the module's global state.
                $Script:ADT.Environment = New-ADTEnvironmentTable @PSBoundParameters
                $Script:ADT.Config = Import-ADTConfig -BaseDirectory $Script:ADT.Directories.Config
                $Script:ADT.Language = Get-ADTStringLanguage
                $Script:ADT.Strings = Import-ADTStringTable -BaseDirectory $Script:ADT.Directories.Strings -UICulture $Script:ADT.Language
                $Script:ADT.RestartOnExitCountdown = $null
                $Script:ADT.LastExitCode = 0

                # Calculate how long this process took before finishing.
                $Script:ADT.Durations.ModuleInit = [System.DateTime]::Now - $moduleInitStart
                $Script:ADT.Initialized = $true
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
