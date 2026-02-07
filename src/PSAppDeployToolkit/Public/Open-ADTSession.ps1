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
        Defaults to $PSCmdlet.SessionState to get the caller's SessionState, so only required if you need to override this.

    .PARAMETER DeploymentType
        Specifies the type of deployment: Install, Uninstall, or Repair.

    .PARAMETER DeployMode
        Specifies the deployment mode: Interactive, NonInteractive, or Silent.

    .PARAMETER SuppressRebootPassThru
        Suppresses reboot pass-through.

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

    .PARAMETER DeployAppScriptParameters
        Specifies the parameters for the deploy application script.

    .PARAMETER AppSuccessExitCodes
        Specifies the application exit codes.

    .PARAMETER AppRebootExitCodes
        Specifies the application reboot codes.

    .PARAMETER AppProcessesToClose
        Specifies one or more processes that require closing to ensure a successful deployment.

    .PARAMETER RequireAdmin
        Specifies that this deployment requires administrative permissions.

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

    .PARAMETER ForceMsiDetection
        Specifies that MSI files should be detected and parsed during session initialization, irrespective of whether any App values are provided.

    .PARAMETER ForceWimDetection
        Specifies that WIM files should be detected and mounted during session initialization, irrespective of whether any App values are provided.

    .PARAMETER NoSessionDetection
        When DeployMode is not specified or is Auto, bypasses DeployMode adjustment when there's no logged on user session available.

    .PARAMETER NoOobeDetection
        When DeployMode is not specified or is Auto, bypasses DeployMode adjustment when the device hasn't completed the OOBE or a user ESP is active.

    .PARAMETER NoProcessDetection
        When DeployMode is not specified or is Auto, bypasses DeployMode adjustment when there's no processes to close in the specified AppProcessesToClose list.

    .PARAMETER AllowWowProcess
        When specified, allows the session to initialize within a Windows on Windows (WOW) process, such as a 32-bit PowerShell instance on a 64-bit operating system.

    .PARAMETER PassThru
        Passes the session object through the pipeline.

    .PARAMETER LogName
        Specifies an override for the default-generated log file name.

    .PARAMETER SessionClass
        Specifies an override for PSAppDeployToolkit.Foundation.DeploymentSession class. Use this if you're deriving a class inheriting off PSAppDeployToolkit's base.

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
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Open-ADTSession
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SessionState]$SessionState,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Parameter')]
        [ValidateNotNullOrEmpty()]
        [PSAppDeployToolkit.Foundation.DeploymentType]$DeploymentType,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Parameter')]
        [ValidateNotNullOrEmpty()]
        [PSAppDeployToolkit.Foundation.DeployMode]$DeployMode,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Parameter')]
        [System.Management.Automation.SwitchParameter]$SuppressRebootPassThru,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Parameter')]
        [System.Management.Automation.SwitchParameter]$TerminalServerMode,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Parameter')]
        [System.Management.Automation.SwitchParameter]$DisableLogging,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$AppVendor = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$AppName = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$AppVersion = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$AppArch = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$AppLang = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$AppRevision = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [ValidateNotNullOrEmpty()]
        [System.Version]$AppScriptVersion,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [ValidateNotNullOrEmpty()]
        [System.DateTime]$AppScriptDate,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$AppScriptAuthor = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$InstallName = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$InstallTitle = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$DeployAppScriptFriendlyName = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [ValidateNotNullOrEmpty()]
        [System.Version]$DeployAppScriptVersion,

        [Parameter(Mandatory = $false, HelpMessage = 'Frontend Variable')]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Generic.IReadOnlyDictionary[System.String, System.Object]]$DeployAppScriptParameters,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$AppSuccessExitCodes,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$AppRebootExitCodes,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSADT.ProcessManagement.ProcessDefinition[]]$AppProcessesToClose,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$RequireAdmin,

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
        [ValidateScript({
                if ([System.String]::IsNullOrWhiteSpace($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName DirFiles -ProvidedValue $_ -ExceptionMessage 'The specified input is null or empty.'))
                }
                if (!(Test-Path -LiteralPath $_ -PathType Container))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName DirFiles -ProvidedValue $_ -ExceptionMessage 'The specified directory does not exist.'))
                }
                return $_
            })]
        [System.String]$DirFiles = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false)]
        [ValidateScript({
                if ([System.String]::IsNullOrWhiteSpace($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName DirSupportFiles -ProvidedValue $_ -ExceptionMessage 'The specified input is null or empty.'))
                }
                if (!(Test-Path -LiteralPath $_ -PathType Container))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName DirSupportFiles -ProvidedValue $_ -ExceptionMessage 'The specified directory does not exist.'))
                }
                return $_
            })]
        [System.String]$DirSupportFiles = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$DefaultMsiFile = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$DefaultMstFile = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$DefaultMspFiles,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$DisableDefaultMsiProcessList,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ForceMsiDetection,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ForceWimDetection,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoSessionDetection,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoOobeDetection,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoProcessDetection,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$AllowWowProcess,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru,

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
        [System.String]$LogName = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false, DontShow = $true)]
        [ValidateScript({
                if ($null -eq $_)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName SessionClass -ProvidedValue $_ -ExceptionMessage 'The specified input is null or empty.'))
                }
                if (!$_.BaseType.Equals([PSAppDeployToolkit.Foundation.DeploymentSession]))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName SessionClass -ProvidedValue $_ -ExceptionMessage 'The specified type is not derived from the DeploymentSession base class.'))
                }
                return $_
            })]
        [System.Type]$SessionClass = [PSAppDeployToolkit.Foundation.DeploymentSession],

        [Parameter(Mandatory = $false, ValueFromRemainingArguments = $true, DontShow = $true)]
        [AllowNull()][AllowEmptyCollection()]
        [System.Collections.Generic.IReadOnlyList[System.Object]]$UnboundArguments
    )

    begin
    {
        # Make this function stop on any error and ensure the caller doesn't override ErrorAction.
        $PSBoundParameters.ErrorAction = [System.Management.Automation.ActionPreference]::Stop
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        # Throw if we have duplicated process objects.
        if ($AppProcessesToClose -and !($AppProcessesToClose.Name | Sort-Object | Get-Unique | Measure-Object).Count.Equals($AppProcessesToClose.Count))
        {
            $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName AppProcessesToClose -ProvidedValue $AppProcessesToClose -ExceptionMessage 'The specified AppProcessesToClose array contains duplicate processes.'))
        }

        # Determine whether this session is to be in compatibility mode.
        $compatibilityMode = Test-ADTNonNativeCaller
        $callerInvocation = Get-PSCallStack | Select-Object -Skip 1 | Select-Object -First 1 | & { process { $_.InvocationInfo } }
        $noExitOnClose = $callerInvocation -and !$callerInvocation.MyCommand.CommandType.Equals([System.Management.Automation.CommandTypes]::ExternalScript) -and !([System.Environment]::GetCommandLineArgs() -eq '-NonInteractive')

        # Set up the SessionState if one wasn't provided.
        if (!$PSBoundParameters.ContainsKey('SessionState'))
        {
            $PSBoundParameters.SessionState = $SessionState = $PSCmdlet.SessionState
        }

        # Set up the ScriptDirectory if one wasn't provided.
        if (!$PSBoundParameters.ContainsKey('ScriptDirectory'))
        {
            [System.String[]]$PSBoundParameters.ScriptDirectory = $ScriptDirectory = if (!$Script:ADT.Initialized -or !$Script:ADT.Directories.Script)
            {
                if (![System.String]::IsNullOrWhiteSpace(($scriptRoot = $SessionState.PSVariable.GetValue('PSScriptRoot', $null))))
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
            else
            {
                $Script:ADT.Directories.Script
            }
        }

        # Add any unbound arguments into $PSBoundParameters when using a derived class.
        if ($PSBoundParameters.ContainsKey('UnboundArguments') -and !$SessionClass.Equals([PSAppDeployToolkit.Foundation.DeploymentSession]))
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

        # Perform pre-opening tasks.
        $initialized = $false
        $errRecord = $null
        try
        {
            # Initialize the module before opening the first session.
            if ($firstSession)
            {
                if (($initialized = !$Script:ADT.Initialized))
                {
                    Initialize-ADTModule -ScriptDirectory $PSBoundParameters.ScriptDirectory
                }
                foreach ($callback in $($Script:ADT.Callbacks.([PSAppDeployToolkit.Foundation.CallbackType]::OnStart)))
                {
                    & $callback
                }
            }

            # Invoke pre-open callbacks.
            foreach ($callback in $($Script:ADT.Callbacks.([PSAppDeployToolkit.Foundation.CallbackType]::PreOpen)))
            {
                & $callback
            }
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError(($errRecord = $_))
        }
        finally
        {
            # If we failed here, de-init the module so we can start fresh again next time.
            if ($errRecord -and $initialized)
            {
                $Script:ADT.Initialized = $false
            }
        }

        # Instantiate the new session.
        try
        {
            try
            {
                $adtSession = $SessionClass::new($PSBoundParameters, $noExitOnClose, $compatibilityMode)
                $Script:ADT.Sessions.Add($adtSession)
            }
            catch
            {
                Write-Error -Exception $_.Exception.InnerException -Category OpenError -CategoryTargetName $Script:ADT.LastExitCode -CategoryTargetType $Script:ADT.LastExitCode.GetType().Name
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord ($errRecord = $_) -LogMessage "Failure occurred while instantiating a new deployment session."
        }
        finally
        {
            # If we failed here, exit out with the DeploymentSession's set exit code as we can't continue.
            if ($errRecord)
            {
                if ($initialized)
                {
                    $Script:ADT.Initialized = $false
                }
                Exit-ADTInvocation -ExitCode $Script:ADT.LastExitCode -NoShellExit:$noExitOnClose
            }
        }

        # Perform post-opening tasks.
        try
        {
            try
            {
                # Add any unbound arguments into the $adtSession object as PSNoteProperty objects.
                if ($PSBoundParameters.ContainsKey('UnboundArguments') -and $SessionClass.Equals([PSAppDeployToolkit.Foundation.DeploymentSession]))
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

                # Invoke post-open callbacks.
                foreach ($callback in $($Script:ADT.Callbacks.([PSAppDeployToolkit.Foundation.CallbackType]::PostOpen)))
                {
                    & $callback
                }

                # Export the environment table to variables within the caller's scope.
                if ($firstSession)
                {
                    Export-ADTEnvironmentTableToSessionState -SessionState $SessionState
                }

                # Change the install phase and return the most recent session if passing through.
                $adtSession.InstallPhase = 'Execution'
                if ($PassThru)
                {
                    return $adtSession
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_ -CategoryTargetName 60008 -CategoryTargetType ([System.Int32].Name)
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord ($errRecord = $_) -LogMessage "Failure occurred following new deployment session instantiation."
        }
        finally
        {
            # If we failed here, ensure we close out the instantiated DeploymentSession object.
            if ($errRecord)
            {
                if ($initialized)
                {
                    $Script:ADT.Initialized = $false
                }
                Close-ADTSession -ExitCode $Script:ADT.LastExitCode
            }
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
