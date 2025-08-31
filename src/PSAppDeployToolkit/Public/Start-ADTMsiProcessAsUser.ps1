#-----------------------------------------------------------------------------
#
# MARK: Start-ADTMsiProcessAsUser
#
#-----------------------------------------------------------------------------

function Start-ADTMsiProcessAsUser
{
    <#
    .SYNOPSIS
        Executes msiexec.exe to perform actions such as install, uninstall, patch, repair, or active setup for MSI and MSP files or MSI product codes.

    .DESCRIPTION
        This function utilizes msiexec.exe to handle various operations on MSI and MSP files, as well as MSI product codes. The operations include installation, uninstallation, patching, repair, and setting up active configurations.

        If the -Action parameter is set to "Install" and the MSI is already installed, the function will terminate without performing any actions.

        The function automatically sets default switches for msiexec based on preferences defined in the config.psd1 file. Additionally, it generates a log file name and creates a verbose log for all msiexec operations, ensuring detailed tracking.

        The MSI or MSP file is expected to reside in the "Files" subdirectory of the App Deploy Toolkit, with transform files expected to be in the same directory as the MSI file.

    .PARAMETER Action
        Specifies the action to be performed. Available options: Install, Uninstall, Patch, Repair, ActiveSetup.

    .PARAMETER FilePath
        The file path to the MSI/MSP file.

    .PARAMETER ProductCode
        The product code of the installed MSI.

    .PARAMETER InstalledApplication
        The InstalledApplication object of the installed MSI.

    .PARAMETER ArgumentList
        Overrides the default parameters specified in the config.psd1 file.

    .PARAMETER AdditionalArgumentList
        Adds additional parameters to the default set specified in the config.psd1 file.

    .PARAMETER SecureArgumentList
        Hides all parameters passed to the MSI or MSP file from the toolkit log file.

    .PARAMETER WorkingDirectory
        Overrides the working directory. The working directory is set to the location of the MSI file.

    .PARAMETER Transforms
        The name(s) of the transform file(s) to be applied to the MSI. The transform files should be in the same directory as the MSI file.

    .PARAMETER Patches
        The name(s) of the patch (MSP) file(s) to be applied to the MSI for the "Install" action. The patch files should be in the same directory as the MSI file.

    .PARAMETER Username
        A username to invoke the process as. Only supported while running as the SYSTEM account.

    .PARAMETER UseLinkedAdminToken
        Use a user's linked administrative token while running the process under their context.

    .PARAMETER UseHighestAvailableToken
        Use a user's linked administrative token if it's available while running the process under their context.

    .PARAMETER InheritEnvironmentVariables
        Specifies whether the process running as a user should inherit the SYSTEM account's environment variables.

    .PARAMETER ExpandEnvironmentVariables
        Specifies whether to expand any Windows/DOS-style environment variables in the specified FilePath/ArgumentList.

    .PARAMETER LoggingOptions
        Overrides the default logging options specified in the config.psd1 file.

    .PARAMETER LogFileName
        Overrides the default log file name. The default log file name is generated from the MSI file name. If LogFileName does not end in .log, it will be automatically appended.

        For uninstallations, by default the product code is resolved to the DisplayName and version of the application.

    .PARAMETER RepairMode
        Specifies the mode of repair. Choosing `Repair` will repair via `msiexec.exe /p` (which can trigger unsupressable reboots). Choosing `Reinstall` will reinstall by adding `REINSTALL=ALL REINSTALLMODE=omus` to the standard InstallParams.

    .PARAMETER RepairFromSource
        Specifies whether we should repair from source. Also rewrites local cache.

    .PARAMETER SkipMSIAlreadyInstalledCheck
        Skips the check to determine if the MSI is already installed on the system.

    .PARAMETER IncludeUpdatesAndHotfixes
        Include matches against updates and hotfixes in results.

    .PARAMETER SuccessExitCodes
        List of exit codes to be considered successful. Defaults to values set during ADTSession initialization, otherwise: 0

    .PARAMETER RebootExitCodes
        List of exit codes to indicate a reboot is required. Defaults to values set during ADTSession initialization, otherwise: 1641, 3010

    .PARAMETER IgnoreExitCodes
        List the exit codes to ignore or * to ignore all exit codes.

    .PARAMETER PriorityClass
        Specifies priority class for the process. Options: Idle, Normal, High, AboveNormal, BelowNormal, RealTime.

    .PARAMETER ExitOnProcessFailure
        Automatically closes the active deployment session via Close-ADTSession in the event the process exits with a non-success or non-ignored exit code.

    .PARAMETER NoDesktopRefresh
        If specifies, doesn't refresh the desktop and environment after successful MSI installation.

    .PARAMETER NoWait
        Immediately continue after executing the process.

    .PARAMETER PassThru
        Returns ExitCode, StdOut, and StdErr output from the process. Note that a failed execution will only return an object if either `-ErrorAction` is set to `SilentlyContinue`/`Ignore`, or if `-IgnoreExitCodes`/`-SuccessExitCodes` are used.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSADT.Types.ProcessResult

        Returns an object with the results of the installation if -PassThru is specified.
        - ExitCode
        - StdOut
        - StdErr

    .EXAMPLE
        Start-ADTMsiProcessAsUser -Action 'Install' -FilePath 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi'

        Install an MSI.

    .EXAMPLE
        Start-ADTMsiProcessAsUser -Action 'Install' -FilePath 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi' -Transforms 'Adobe_FlashPlayer_11.2.202.233_x64_EN_01.mst' -ArgumentList '/QN'

        Install an MSI, applying a transform and overriding the default MSI toolkit parameters.

    .EXAMPLE
        $ExecuteMSIResult = Start-ADTMsiProcessAsUser -Action 'Install' -FilePath 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi' -PassThru

        Install an MSI and stores the result of the execution into a variable by using the -PassThru option.

    .EXAMPLE
        Start-ADTMsiProcessAsUser -Action 'Uninstall' -ProductCode '{26923b43-4d38-484f-9b9e-de460746276c}'

        Uninstall an MSI using a product code.

    .EXAMPLE
        Start-ADTMsiProcessAsUser -Action 'Patch' -FilePath 'Adobe_Reader_11.0.3_EN.msp'

        Install an MSP.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Start-ADTMsiProcessAsUser
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Security.Principal.NTAccount]$Username,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Install', 'Uninstall', 'Patch', 'Repair', 'ActiveSetup')]
        [System.String]$Action = 'Install',

        [Parameter(Mandatory = $true, ParameterSetName = 'FilePath', ValueFromPipeline = $true, HelpMessage = 'Please supply the path to the MSI/MSP file to process.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'FilePath_NoWait', ValueFromPipeline = $true, HelpMessage = 'Please supply the path to the MSI/MSP file to process.')]
        [ValidateScript({
                if ([System.IO.Path]::GetExtension($_) -notmatch '^\.ms[ip]$')
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName FilePath -ProvidedValue $_ -ExceptionMessage 'The specified input has an invalid file extension.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [System.String]$FilePath = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $true, ParameterSetName = 'ProductCode', ValueFromPipeline = $true, HelpMessage = 'Please supply the Product Code to process.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'ProductCode_NoWait', ValueFromPipeline = $true, HelpMessage = 'Please supply the Product Code to process.')]
        [ValidateNotNullOrEmpty()]
        [System.Guid]$ProductCode,

        [Parameter(Mandatory = $true, ParameterSetName = 'InstalledApplication', ValueFromPipeline = $true, HelpMessage = 'Please supply the InstalledApplication object to process.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InstalledApplication_NoWait', ValueFromPipeline = $true, HelpMessage = 'Please supply the InstalledApplication object to process.')]
        [ValidateNotNullOrEmpty()]
        [PSADT.Types.InstalledApplication]$InstalledApplication,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSDefaultValue(Help = 'Install (Normal): (Get-ADTConfig).MSI.InstallParams; Install (Silent): (Get-ADTConfig).MSI.SilentParams; Uninstall (Normal): (Get-ADTConfig).MSI.UninstallParams; Uninstall (Silent): (Get-ADTConfig).MSI.SilentParams')]
        [System.String[]]$ArgumentList,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$AdditionalArgumentList,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SecureArgumentList,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$WorkingDirectory = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Transforms,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Patches,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$UseLinkedAdminToken,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$UseHighestAvailableToken,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$InheritEnvironmentVariables,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ExpandEnvironmentVariables,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$LoggingOptions = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false)]
        [ValidateScript({
                if ([System.String]::IsNullOrWhiteSpace($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName LogFileName -ProvidedValue $_ -ExceptionMessage 'The specified input is null or white space.'))
                }
                if ([System.IO.Path]::GetExtension($_) -match '^\.(log|txt)$')
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName LogFileName -ProvidedValue $_ -ExceptionMessage 'The specified input cannot have an extension.'))
                }
                return $true
            })]
        [System.String]$LogFileName = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Repair', 'Reinstall')]
        [System.String]$RepairMode,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$RepairFromSource,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SkipMSIAlreadyInstalledCheck,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$IncludeUpdatesAndHotfixes,

        [Parameter(Mandatory = $false, ParameterSetName = 'FilePath')]
        [Parameter(Mandatory = $false, ParameterSetName = 'ProductCode')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InstalledApplication')]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$SuccessExitCodes,

        [Parameter(Mandatory = $false, ParameterSetName = 'FilePath')]
        [Parameter(Mandatory = $false, ParameterSetName = 'ProductCode')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InstalledApplication')]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$RebootExitCodes,

        [Parameter(Mandatory = $false, ParameterSetName = 'FilePath')]
        [Parameter(Mandatory = $false, ParameterSetName = 'ProductCode')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InstalledApplication')]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$IgnoreExitCodes,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Diagnostics.ProcessPriorityClass]$PriorityClass,

        [Parameter(Mandatory = $false, ParameterSetName = 'FilePath')]
        [Parameter(Mandatory = $false, ParameterSetName = 'ProductCode')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InstalledApplication')]
        [System.Management.Automation.SwitchParameter]$ExitOnProcessFailure,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoDesktopRefresh,

        [Parameter(Mandatory = $true, ParameterSetName = 'FilePath_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'ProductCode_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InstalledApplication_NoWait')]
        [System.Management.Automation.SwitchParameter]$NoWait,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        # Convert the Username field into a RunAsActiveUser object as required by the subsystem.
        $gacsuParams = if ($PSBoundParameters.ContainsKey('Username')) { @{ Username = $Username } } else { @{} }
        if (!($PSBoundParameters.RunAsActiveUser = Get-ADTClientServerUser @gacsuParams))
        {
            try
            {
                $naerParams = @{
                    Exception = [System.ArgumentNullException]::new("There is no logged on user to run a new process as.", $null)
                    Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                    ErrorId = 'NoActiveUserError'
                    TargetObject = $Username
                    RecommendedAction = "Please re-run this command while a user is logged onto the device and try again."
                }
                Write-Error -ErrorRecord (New-ADTErrorRecord @naerParams)
            }
            catch
            {
                Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
                return
            }
        }
        $null = $PSBoundParameters.Remove('Username')

        # Just farm it out to Start-ADTMsiProcess as it can do it all.
        try
        {
            return Start-ADTMsiProcess @PSBoundParameters
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
