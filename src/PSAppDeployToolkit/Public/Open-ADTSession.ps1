#-----------------------------------------------------------------------------
#
# MARK: Open-ADTSession
#
#-----------------------------------------------------------------------------

function Open-ADTSession
{
    <#
    .SYNOPSIS
        Opens a new ADT session.

    .DESCRIPTION
        This function initializes and opens a new ADT session with the specified parameters. It handles the setup of the session environment and processes any callbacks defined for the session. If the session fails to open, it handles the error and closes the session if necessary.

    .PARAMETER SessionState
        Caller's SessionState.

    .PARAMETER DeploymentType
        Specifies the type of deployment: Install, Uninstall, or Repair.

    .PARAMETER DeployMode
        Specifies the deployment mode: Interactive, NonInteractive, or Silent.

    .PARAMETER AllowRebootPassThru
        Allows reboot pass-through.

    .PARAMETER TerminalServerMode
        Enables Terminal Server mode.

    .PARAMETER DisableLogging
        Disables logging for the session.

    .PARAMETER AppVendor
        Specifies the application vendor.

    .PARAMETER AppName
        Specifies the application name.

    .PARAMETER AppVersion
        Specifies the application version.

    .PARAMETER AppArch
        Specifies the application architecture.

    .PARAMETER AppLang
        Specifies the application language.

    .PARAMETER AppRevision
        Specifies the application revision.

    .PARAMETER AppSuccessExitCodes
        Specifies the application exit codes.

    .PARAMETER AppRebootExitCodes
        Specifies the application reboot codes.

    .PARAMETER AppScriptVersion
        Specifies the application script version.

    .PARAMETER AppScriptDate
        Specifies the application script date.

    .PARAMETER AppScriptAuthor
        Specifies the application script author.

    .PARAMETER InstallName
        Specifies the install name.

    .PARAMETER InstallTitle
        Specifies the install title.

    .PARAMETER DeployAppScriptFriendlyName
        Specifies the friendly name of the deploy application script.

    .PARAMETER DeployAppScriptVersion
        Specifies the version of the deploy application script.

    .PARAMETER DeployAppScriptDate
        Specifies the date of the deploy application script.

    .PARAMETER DeployAppScriptParameters
        Specifies the parameters for the deploy application script.

    .PARAMETER ScriptDirectory
        Specifies the base path for Files and SupportFiles.

    .PARAMETER DirFiles
        Specifies the override path to Files.

    .PARAMETER DirSupportFiles
        Specifies the override path to SupportFiles.

    .PARAMETER DefaultMsiFile
        Specifies the default MSI file.

    .PARAMETER DefaultMstFile
        Specifies the default MST file.

    .PARAMETER DefaultMspFiles
        Specifies the default MSP files.

    .PARAMETER DisableDefaultMsiProcessList
        Specifies that the zero-config MSI code should not gather process names from the MSI file.

    .PARAMETER LogName
        Specifies an override for the default-generated log file name.

    .PARAMETER SessionClass
        Specifies an override for PSADT.Module.DeploymentSession class. Use this if you're deriving a class inheriting off PSAppDeployToolkit's base.

    .PARAMETER ForceWimDetection
        Specifies that WIM files should be detected and mounted during session initialization, irrespective of whether any App values are provided.

    .PARAMETER ForceMsiDetection
        Specifies that MSI files should be detected and parsed during session initialization, irrespective of whether any App values are provided.

    .PARAMETER PassThru
        Passes the session object through the pipeline.

    .PARAMETER UnboundArguments
        Captures any additional arguments passed to the function.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        ADTSession

        This function returns the session object if -PassThru is specified.

    .EXAMPLE
        Open-ADTSession -SessionState $ExecutionContext.SessionState -DeploymentType "Install" -DeployMode "Interactive"

        Opens a new ADT session with the specified parameters.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Open-ADTSession
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SessionState]$SessionState,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Parameter')]
        [ValidateNotNullOrEmpty()]
        [PSADT.Module.DeploymentType]$DeploymentType,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Parameter')]
        [ValidateNotNullOrEmpty()]
        [PSADT.Module.DeployMode]$DeployMode,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Parameter')]
        [System.Management.Automation.SwitchParameter]$AllowRebootPassThru,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Parameter')]
        [System.Management.Automation.SwitchParameter]$TerminalServerMode,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Parameter')]
        [System.Management.Automation.SwitchParameter]$DisableLogging,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [AllowEmptyString()]
        [System.String]$AppVendor,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [AllowEmptyString()]
        [System.String]$AppName,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [AllowEmptyString()]
        [System.String]$AppVersion,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [AllowEmptyString()]
        [System.String]$AppArch,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [AllowEmptyString()]
        [System.String]$AppLang,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [AllowEmptyString()]
        [System.String]$AppRevision,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [ValidateNotNullOrEmpty()]
        [System.Version]$AppScriptVersion,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [ValidateNotNullOrEmpty()]
        [System.DateTime]$AppScriptDate,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$AppScriptAuthor,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [AllowEmptyString()]
        [System.String]$InstallName,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [AllowEmptyString()]
        [System.String]$InstallTitle,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$DeployAppScriptFriendlyName,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [ValidateNotNullOrEmpty()]
        [System.Version]$DeployAppScriptVersion,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [ValidateNotNullOrEmpty()]
        [System.DateTime]$DeployAppScriptDate,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [AllowEmptyCollection()]
        [System.Collections.Generic.Dictionary[System.String, System.Object]]$DeployAppScriptParameters,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$AppSuccessExitCodes,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$AppRebootExitCodes,

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
        [System.String[]]$ScriptDirectory,

        [Parameter(Mandatory = $false)]
        [ValidateScript({
                if ([System.String]::IsNullOrWhiteSpace($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName DirFiles -ProvidedValue $_ -ExceptionMessage 'The specified input is null or empty.'))
                }
                if (![System.IO.Directory]::Exists($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName DirFiles -ProvidedValue $_ -ExceptionMessage 'The specified directory does not exist.'))
                }
                return $_
            })]
        [System.String]$DirFiles,

        [Parameter(Mandatory = $false)]
        [ValidateScript({
                if ([System.String]::IsNullOrWhiteSpace($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName DirSupportFiles -ProvidedValue $_ -ExceptionMessage 'The specified input is null or empty.'))
                }
                if (![System.IO.Directory]::Exists($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName DirSupportFiles -ProvidedValue $_ -ExceptionMessage 'The specified directory does not exist.'))
                }
                return $_
            })]
        [System.String]$DirSupportFiles,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$DefaultMsiFile,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$DefaultMstFile,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$DefaultMspFiles,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$DisableDefaultMsiProcessList,

        [Parameter(Mandatory = $false)]
        [ValidateScript({
                if ([System.String]::IsNullOrWhiteSpace($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName LogName -ProvidedValue $_ -ExceptionMessage 'The specified input is null or empty.'))
                }
                if ([System.IO.Path]::GetExtension($_) -ne '.log')
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName LogName -ProvidedValue $_ -ExceptionMessage 'The specified name does not have a [.log] extension.'))
                }
                return $_
            })]
        [System.String]$LogName,

        [Parameter(Mandatory = $false, DontShow = $true)]
        [ValidateScript({
                if ($null -eq $_)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName SessionClass -ProvidedValue $_ -ExceptionMessage 'The specified input is null or empty.'))
                }
                if (!$_.BaseType.Equals([PSADT.Module.DeploymentSession]))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName SessionClass -ProvidedValue $_ -ExceptionMessage 'The specified type is not derived from the DeploymentSession base class.'))
                }
                return $_
            })]
        [System.Type]$SessionClass = [PSADT.Module.DeploymentSession],

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ForceWimDetection,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ForceMsiDetection,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru,

        [Parameter(Mandatory = $false, ValueFromRemainingArguments = $true, DontShow = $true)]
        [AllowNull()][AllowEmptyCollection()]
        [System.Collections.Generic.List[System.Object]]$UnboundArguments
    )

    begin
    {
        # Initialize function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $adtSession = $null
        $errRecord = $null

        # Determine whether this session is to be in compatibility mode.
        $compatibilityMode = Test-ADTNonNativeCaller
        $callerInvocation = Get-PSCallStack | Select-Object -Skip 1 | Select-Object -First 1 | & { process { $_.InvocationInfo } }
        $noExitOnClose = $callerInvocation -and !$callerInvocation.MyCommand.CommandType.Equals([System.Management.Automation.CommandTypes]::ExternalScript) -and !([System.Environment]::GetCommandLineArgs() -eq '-NonInteractive')

        # Set up the ScriptDirectory if one wasn't provided.
        if (!$PSBoundParameters.ContainsKey('ScriptDirectory'))
        {
            [System.String[]]$PSBoundParameters.ScriptDirectory = if (![System.String]::IsNullOrWhiteSpace(($scriptRoot = $SessionState.PSVariable.GetValue('PSScriptRoot', $null))))
            {
                if ($compatibilityMode)
                {
                    [System.IO.Directory]::GetParent($scriptRoot).FullName
                }
                else
                {
                    $scriptRoot
                }
            }
            else
            {
                $ExecutionContext.SessionState.Path.CurrentLocation.Path
            }
        }

        # Add any unbound arguments into $PSBoundParameters when using a derived class.
        if ($PSBoundParameters.ContainsKey('UnboundArguments') -and !$SessionClass.Equals([PSADT.Module.DeploymentSession]))
        {
            $null = (Convert-ADTValuesFromRemainingArguments -RemainingArguments $UnboundArguments).GetEnumerator().ForEach({
                    $PSBoundParameters.Add($_.Key, $_.Value)
                })
        }

        # Remove any values from $PSBoundParameters that are null (empty strings, mostly).
        $null = ($PSBoundParameters.GetEnumerator().Where({ [System.String]::IsNullOrWhiteSpace((Out-String -InputObject $_.Value)) })).ForEach({ $PSBoundParameters.Remove($_.Key) })
    }

    process
    {
        # If this function is being called from the console or by AppDeployToolkitMain.ps1, clear all previous sessions and go for full re-initialization.
        if (($callerInvocation -and [System.String]::IsNullOrWhiteSpace($callerInvocation.InvocationName) -and [System.String]::IsNullOrWhiteSpace($callerInvocation.Line)) -or $compatibilityMode)
        {
            $Script:ADT.Sessions.Clear()
            $Script:ADT.Initialized = $false
        }
        $firstSession = !$Script:ADT.Sessions.Count

        # Commence the opening process.
        try
        {
            try
            {
                # Initialize the module before opening the first session.
                if ($firstSession -and !$Script:ADT.Initialized)
                {
                    Initialize-ADTModule -ScriptDirectory $PSBoundParameters.ScriptDirectory
                }

                # Instantiate the new session. The constructor will handle adding the session to the module's list.
                $Script:ADT.Sessions.Add(($adtSession = $SessionClass::new($PSBoundParameters, $noExitOnClose, $(if ($compatibilityMode) { $SessionState }))))

                # Invoke all callbacks.
                foreach ($callback in $(if ($firstSession) { $Script:ADT.Callbacks.Starting }; $Script:ADT.Callbacks.Opening))
                {
                    & $callback
                }

                # Add any unbound arguments into the $adtSession object as PSNoteProperty objects.
                if ($PSBoundParameters.ContainsKey('UnboundArguments') -and $SessionClass.Equals([PSADT.Module.DeploymentSession]))
                {
                    (Convert-ADTValuesFromRemainingArguments -RemainingArguments $UnboundArguments).GetEnumerator() | & {
                        begin
                        {
                            $adtSessionProps = $adtSession.PSObject.Properties
                        }

                        process
                        {
                            $adtSessionProps.Add([System.Management.Automation.PSNoteProperty]::new($_.Key, $_.Value))
                        }
                    }
                }

                # Export the environment table to variables within the caller's scope.
                if ($firstSession)
                {
                    Export-ADTEnvironmentTableToSessionState -SessionState $SessionState
                }

                # Change the install phase since we've finished initialising. This should get overwritten shortly.
                $adtSession.InstallPhase = 'Execution'
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            # Process the caught error, log it and throw depending on the specified ErrorAction.
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord ($errRecord = $_) -LogMessage "Failure occurred while opening new deployment session."
        }
        finally
        {
            # Terminate early if we have an active session that failed to open properly.
            if ($errRecord)
            {
                if (!$adtSession)
                {
                    Exit-ADTInvocation -ExitCode 60008 -NoShellExit:$noExitOnClose
                }
                else
                {
                    Close-ADTSession -ExitCode 60008
                }
            }
        }

        # Return the most recent session if passing through.
        if ($PassThru)
        {
            return $adtSession
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
