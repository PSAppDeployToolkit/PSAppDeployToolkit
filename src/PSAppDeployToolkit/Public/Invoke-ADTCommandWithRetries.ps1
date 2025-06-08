#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTCommandWithRetries
#
#-----------------------------------------------------------------------------

function Invoke-ADTCommandWithRetries
{
    <#
    .SYNOPSIS
        Drop-in replacement for any cmdlet/function where a retry is desirable due to transient issues.

    .DESCRIPTION
        This function invokes the specified cmdlet/function, accepting all of its parameters but retries an operation for the configured value before throwing.

    .PARAMETER Command
        The name of the command to invoke.

    .PARAMETER Retries
        How many retries to perform before throwing.

    .PARAMETER SleepDuration
        How long to sleep between retries.

    .PARAMETER MaximumElapsedTime
        The maximum elapsed time allowed to passed while attempting retries. If the maximum elapsted time has passed and there are still attempts remaining they will be disgarded.

        If this parameter is supplied and the `-Retries` parameter isn't, this command will continue to retry the provided command until the time limit runs out.

    .PARAMETER SleepSeconds
        This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0. Please use `-SleepDuration` instead.

    .PARAMETER Parameters
        A 'ValueFromRemainingArguments' parameter to collect the parameters as would be passed to the provided Command.

        While values can be directly provided to this parameter, it's not designed to be explicitly called.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Object

        Invoke-ADTCommandWithRetries returns the output of the invoked command.

    .EXAMPLE
        Invoke-ADTCommandWithRetries -Command Invoke-WebRequest -Uri https://aka.ms/getwinget -OutFile "$($adtSession.DirSupportFiles)\Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle"

        Downloads the latest WinGet installer to the SupportFiles directory. If the command fails, it will retry 3 times with 5 seconds between each attempt.

    .EXAMPLE
        Invoke-ADTCommandWithRetries Get-FileHash -Path '\\MyShare\MyFile' -MaximumElapsedTime (New-TimeSpan -Seconds 90) -SleepDuration (New-TimeSpan -Seconds 1)

        Gets the hash of a file on an SMB share. If the connection to the SMB share drops, it will retry the command every 2 seconds until it successfully gets the hash or 90 seconds have passed since the initial attempt.

    .EXAMPLE
        Invoke-ADTCommandWithRetries Copy-ADTFile -Path '\\MyShare\MyFile' -Destination 'C:\Windows\Temp' -Retries 5 -MaximumElapsedTime (New-TimeSpan -Minutes 5)

        Copies a file from an SMB share to C:\Windows\Temp. If the connection to the SMB share drops, it will retry the command once every 5 seconds until either 5 attempts have been made or 5 minutes have passed since the initial attempt.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Invoke-ADTCommandWithRetries
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [System.Object]$Command,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$Retries = 3,

        [Parameter(Mandatory = $false)]
        [ValidateScript({
                if ($_ -le [System.TimeSpan]::Zero)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName SleepDuration -ProvidedValue $_ -ExceptionMessage 'The specified TimeSpan must be greater than zero.'))
                }
                return !!$_
            })]
        [System.TimeSpan]$SleepDuration = [System.TimeSpan]::FromSeconds(5),

        [Parameter(Mandatory = $false)]
        [ValidateScript({
                if ($_ -le [System.TimeSpan]::Zero)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName MaximumElapsedTime -ProvidedValue $_ -ExceptionMessage 'The specified TimeSpan must be greater than zero.'))
                }
                return !!$_
            })]
        [System.TimeSpan]$MaximumElapsedTime,

        [Parameter(Mandatory = $false)]
        [System.Obsolete("Please use 'SleepDuration' instead as this will be removed in PSAppDeployToolkit 4.2.0.")]
        [ValidateRange(1, 60)]
        [System.UInt32]$SleepSeconds = 5,

        [Parameter(Mandatory = $false, ValueFromRemainingArguments = $true, DontShow = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Generic.List[System.Object]]$Parameters
    )

    begin
    {
        # Initialize function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        # Log the deprecation of -SleepSeconds to the log.
        if ($PSBoundParameters.ContainsKey('SleepSeconds'))
        {
            Write-ADTLogEntry -Message "The parameter [-SleepSeconds] is obsolete and will be removed in PSAppDeployToolkit 4.2.0. Please use [-SleepDuration] instead." -Severity 2
            if (!$PSBoundParameters.ContainsKey('SleepDuration'))
            {
                $SleepDuration = [System.TimeSpan]::FromSeconds($SleepSeconds)
            }
        }
    }

    process
    {
        try
        {
            try
            {
                # Attempt to get command from our lookup table.
                $commandObj = if ($Command -is [System.Management.Automation.CommandInfo])
                {
                    $Command
                }
                elseif ($Script:CommandTable.ContainsKey($Command))
                {
                    $Script:CommandTable.$Command
                }
                else
                {
                    Get-Command -Name $Command
                }

                # Convert the passed parameters into a dictionary for splatting onto the command.
                $boundParams = Convert-ADTValuesFromRemainingArguments -RemainingArguments $Parameters

                # If the command in question supports supplying an ErrorAction, force it to stop so the logic works.
                if ($commandObj.Parameters.ContainsKey('ErrorAction'))
                {
                    $boundParams.Add('ErrorAction', $ErrorActionPreference)
                }

                # Set up a stopwatch when we're tracking the maximum allowed retry duration.
                $maxElapsedStopwatch = if ($PSBoundParameters.ContainsKey('MaximumElapsedTime'))
                {
                    [System.Diagnostics.Stopwatch]::StartNew()
                }

                # Perform the request, and retry it as per the configured values.
                $i = 0
                while ($true)
                {
                    try
                    {
                        return (& $commandObj @boundParams)
                    }
                    catch
                    {
                        # Break if we've exceeded our bounds.
                        if ($maxElapsedStopwatch)
                        {
                            if (($maxElapsedStopwatch.Elapsed -ge $MaximumElapsedTime) -or ($PSBoundParameters.ContainsKey('Retries') -and ($i -ge $Retries)))
                            {
                                if ($commandObj.Module -eq $MyInvocation.MyCommand.Module.Name)
                                {
                                    $PSCmdlet.ThrowTerminatingError($_)
                                }
                                throw
                            }
                        }
                        elseif ($i -ge $Retries)
                        {
                            if ($commandObj.Module -eq $MyInvocation.MyCommand.Module.Name)
                            {
                                $PSCmdlet.ThrowTerminatingError($_)
                            }
                            throw
                        }
                        Write-ADTLogEntry -Message "The invocation to '$($commandObj.Name)' failed with message: $($_.Exception.Message.TrimEnd('.')). Trying again in $($SleepDuration.TotalSeconds) second$(if (!$SleepDuration.TotalSeconds.Equals(1)) {'s'})." -Severity 2
                        [System.Threading.Thread]::Sleep($SleepDuration)
                    }
                    finally
                    {
                        $i++
                    }
                }
            }
            catch
            {
                # Re-writing the ErrorRecord with Write-Error ensures the correct PositionMessage is used.
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            # Process the caught error, log it and throw depending on the specified ErrorAction.
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
