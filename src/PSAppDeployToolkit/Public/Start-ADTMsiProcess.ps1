#-----------------------------------------------------------------------------
#
# MARK: Start-ADTMsiProcess
#
#-----------------------------------------------------------------------------

function Start-ADTMsiProcess
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
        The file path to the MSI/MSP file. If the specified FilePath is just a file name, the function will look within `$adtSession.DirFiles` for the specified file, so long as a session is active.

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

    .PARAMETER RunAsActiveUser
        A RunAsActiveUser object to invoke the process as.

    .PARAMETER UseLinkedAdminToken
        Use a user's linked administrative token while running the process under their context.

    .PARAMETER UseHighestAvailableToken
        Use a user's linked administrative token if it's available while running the process under their context.

    .PARAMETER InheritEnvironmentVariables
        Specifies whether the process running as a user should inherit the SYSTEM account's environment variables.

    .PARAMETER DenyUserTermination
        Specifies that users cannot terminate the process started in their context. The user will still be able to terminate the process if they're an administrator, though.

    .PARAMETER UseUnelevatedToken
        If the current process is elevated, starts the new process unelevated using the user's unelevated linked token.

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
        Start-ADTMsiProcess -Action 'Install' -FilePath 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi'

        Install an MSI.

    .EXAMPLE
        Start-ADTMsiProcess -Action 'Install' -FilePath 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi' -Transforms 'Adobe_FlashPlayer_11.2.202.233_x64_EN_01.mst' -ArgumentList '/QN'

        Install an MSI, applying a transform and overriding the default MSI toolkit parameters.

    .EXAMPLE
        $ExecuteMSIResult = Start-ADTMsiProcess -Action 'Install' -FilePath 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi' -PassThru

        Install an MSI and stores the result of the execution into a variable by using the -PassThru option.

    .EXAMPLE
        $ExecuteMSIResult = Start-ADTMsiProcess -Action 'Install' -FilePath 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi' -AdditionalArgumentList 'ALLUSERS=1', 'SOMEPROPERTY=TRUE' -PassThru

        Install an MSI and stores the result of the execution into a variable by using the -PassThru option, specifically taking advantage of our `-AdditionalArgumentList` array support to avoid escaped quote issues.

    .EXAMPLE
        Start-ADTMsiProcess -Action 'Uninstall' -ProductCode '{26923b43-4d38-484f-9b9e-de460746276c}'

        Uninstall an MSI using a product code.

    .EXAMPLE
        Start-ADTMsiProcess -Action 'Patch' -FilePath 'Adobe_Reader_11.0.3_EN.msp'

        Install an MSP.

    .NOTES
        An active ADT session is NOT required to use this function.

        This function supports the -WhatIf and -Confirm parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Start-ADTMsiProcess
    #>

    [CmdletBinding(SupportsShouldProcess = $true)]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateSet('Install', 'Uninstall', 'Patch', 'Repair', 'ActiveSetup')]
        [System.String]$Action = 'Install',

        [Parameter(Mandatory = $true, ParameterSetName = 'FilePath', ValueFromPipeline = $true, HelpMessage = 'Please supply the path to the MSI/MSP file to process.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'FilePath_NoWait', ValueFromPipeline = $true, HelpMessage = 'Please supply the path to the MSI/MSP file to process.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_FilePath', ValueFromPipeline = $true, HelpMessage = 'Please supply the path to the MSI/MSP file to process.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_FilePath_NoWait', ValueFromPipeline = $true, HelpMessage = 'Please supply the path to the MSI/MSP file to process.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseUnelevatedToken_FilePath', ValueFromPipeline = $true, HelpMessage = 'Please supply the path to the MSI/MSP file to process.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseUnelevatedToken_FilePath_NoWait', ValueFromPipeline = $true, HelpMessage = 'Please supply the path to the MSI/MSP file to process.')]
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
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_ProductCode', ValueFromPipeline = $true, HelpMessage = 'Please supply the Product Code to process.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_ProductCode_NoWait', ValueFromPipeline = $true, HelpMessage = 'Please supply the Product Code to process.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseUnelevatedToken_ProductCode', ValueFromPipeline = $true, HelpMessage = 'Please supply the Product Code to process.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseUnelevatedToken_ProductCode_NoWait', ValueFromPipeline = $true, HelpMessage = 'Please supply the Product Code to process.')]
        [ValidateNotNullOrEmpty()]
        [System.Guid]$ProductCode,

        [Parameter(Mandatory = $true, ParameterSetName = 'InstalledApplication', ValueFromPipeline = $true, HelpMessage = 'Please supply the InstalledApplication object to process.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InstalledApplication_NoWait', ValueFromPipeline = $true, HelpMessage = 'Please supply the InstalledApplication object to process.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_InstalledApplication', ValueFromPipeline = $true, HelpMessage = 'Please supply the InstalledApplication object to process.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_InstalledApplication_NoWait', ValueFromPipeline = $true, HelpMessage = 'Please supply the InstalledApplication object to process.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseUnelevatedToken_InstalledApplication', ValueFromPipeline = $true, HelpMessage = 'Please supply the InstalledApplication object to process.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseUnelevatedToken_InstalledApplication_NoWait', ValueFromPipeline = $true, HelpMessage = 'Please supply the InstalledApplication object to process.')]
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

        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_FilePath')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_FilePath_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_ProductCode')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_ProductCode_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_InstalledApplication')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_InstalledApplication_NoWait')]
        [ValidateNotNullOrEmpty()]
        [PSADT.Foundation.RunAsActiveUser]$RunAsActiveUser,

        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_FilePath')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_FilePath_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_ProductCode')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_ProductCode_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_InstalledApplication')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_InstalledApplication_NoWait')]
        [System.Management.Automation.SwitchParameter]$UseLinkedAdminToken,

        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_FilePath')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_FilePath_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_ProductCode')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_ProductCode_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_InstalledApplication')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_InstalledApplication_NoWait')]
        [System.Management.Automation.SwitchParameter]$UseHighestAvailableToken,

        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_FilePath')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_FilePath_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_ProductCode')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_ProductCode_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_InstalledApplication')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_InstalledApplication_NoWait')]
        [System.Management.Automation.SwitchParameter]$InheritEnvironmentVariables,

        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_FilePath')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_FilePath_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_ProductCode')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_ProductCode_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_InstalledApplication')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_InstalledApplication_NoWait')]
        [System.Management.Automation.SwitchParameter]$DenyUserTermination,

        [Parameter(Mandatory = $true, ParameterSetName = 'UseUnelevatedToken_FilePath')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseUnelevatedToken_FilePath_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseUnelevatedToken_ProductCode')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseUnelevatedToken_ProductCode_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseUnelevatedToken_InstalledApplication')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseUnelevatedToken_InstalledApplication_NoWait')]
        [System.Management.Automation.SwitchParameter]$UseUnelevatedToken,

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
                return $true
            })]
        [System.String]$LogFileName = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Repair', 'Reinstall')]
        [System.String]$RepairMode = 'Reinstall',

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$RepairFromSource,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SkipMSIAlreadyInstalledCheck,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$IncludeUpdatesAndHotfixes,

        [Parameter(Mandatory = $false, ParameterSetName = 'FilePath')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InstalledApplication')]
        [Parameter(Mandatory = $false, ParameterSetName = 'ProductCode')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_FilePath')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_InstalledApplication')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_ProductCode')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseUnelevatedToken_FilePath')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseUnelevatedToken_InstalledApplication')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseUnelevatedToken_ProductCode')]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$SuccessExitCodes,

        [Parameter(Mandatory = $false, ParameterSetName = 'FilePath')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InstalledApplication')]
        [Parameter(Mandatory = $false, ParameterSetName = 'ProductCode')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_FilePath')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_InstalledApplication')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_ProductCode')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseUnelevatedToken_FilePath')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseUnelevatedToken_InstalledApplication')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseUnelevatedToken_ProductCode')]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$RebootExitCodes,

        [Parameter(Mandatory = $false, ParameterSetName = 'FilePath')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InstalledApplication')]
        [Parameter(Mandatory = $false, ParameterSetName = 'ProductCode')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_FilePath')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_InstalledApplication')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_ProductCode')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseUnelevatedToken_FilePath')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseUnelevatedToken_InstalledApplication')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseUnelevatedToken_ProductCode')]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$IgnoreExitCodes,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Diagnostics.ProcessPriorityClass]$PriorityClass,

        [Parameter(Mandatory = $false, ParameterSetName = 'FilePath')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InstalledApplication')]
        [Parameter(Mandatory = $false, ParameterSetName = 'ProductCode')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_FilePath')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_InstalledApplication')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_ProductCode')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseUnelevatedToken_FilePath')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseUnelevatedToken_InstalledApplication')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseUnelevatedToken_ProductCode')]
        [System.Management.Automation.SwitchParameter]$ExitOnProcessFailure,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoDesktopRefresh,

        [Parameter(Mandatory = $true, ParameterSetName = 'FilePath_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InstalledApplication_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'ProductCode_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_FilePath_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_InstalledApplication_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_ProductCode_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseUnelevatedToken_FilePath_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseUnelevatedToken_InstalledApplication_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseUnelevatedToken_ProductCode_NoWait')]
        [System.Management.Automation.SwitchParameter]$NoWait,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    begin
    {
        # The use of a ProductCode with an Install action is not supported.
        if ($ProductCode -and ($Action -eq 'Install'))
        {
            $naerParams = @{
                Exception = [System.InvalidOperationException]::new("The ProductCode parameter can only be used with non-install actions.")
                Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                ErrorId = 'ProductCodeInstallActionNotSupported'
                TargetObject = $PSBoundParameters
                RecommendedAction = "Please review the supplied parameters and try again."
            }
            $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
        }
        $adtSession = Initialize-ADTModuleIfUnitialized -Cmdlet $PSCmdlet; $adtConfig = Get-ADTConfig
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        $ExecuteProcessSplat = $null
        try
        {
            try
            {
                # Determine whether the input is a ProductCode or not.
                Write-ADTLogEntry -Message "Executing MSI action [$Action]..."

                # If the MSI is in the Files directory, set the full path to the MSI.
                $msiProduct = switch ($PSCmdlet.ParameterSetName)
                {
                    { $_.EndsWith('FilePath') }
                    {
                        if (Test-Path -LiteralPath $FilePath -PathType Leaf)
                        {
                            (Get-Item -LiteralPath $FilePath).FullName
                        }
                        elseif ($adtSession -and ![System.String]::IsNullOrWhiteSpace($adtSession.DirFiles) -and (Test-Path -LiteralPath ($dirFilesPath = (Join-Path -Path $adtSession.DirFiles -ChildPath $FilePath).Trim()) -PathType Leaf))
                        {
                            $dirFilesPath
                        }
                        else
                        {
                            $naerParams = @{
                                Exception = [System.IO.FileNotFoundException]::new("Failed to find the file [$FilePath].")
                                Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                                ErrorId = 'FilePathNotFound'
                                TargetObject = $FilePath
                                RecommendedAction = "Please confirm the path of the file and try again."
                            }
                            throw (New-ADTErrorRecord @naerParams)
                        }
                        break
                    }

                    { $_.EndsWith('ProductCode') }
                    {
                        $ProductCode.ToString('B')
                        break
                    }

                    { $_.EndsWith('InstalledApplication') }
                    {
                        $InstalledApplication.ProductCode.ToString('B')
                        break
                    }
                }

                # Fix up any bad file paths.
                if ([System.IO.Path]::GetExtension($msiProduct) -eq '.msi')
                {
                    # Iterate transforms.
                    if ($Transforms)
                    {
                        for ($i = 0; $i -lt $Transforms.Length; $i++)
                        {
                            if (Test-Path -LiteralPath ($fullPath = (Join-Path -Path (Get-Item -LiteralPath $msiProduct).DirectoryName -ChildPath $Transforms[$i].Replace('.\', [System.Management.Automation.Language.NullString]::Value)).Trim()) -PathType Leaf)
                            {
                                $Transforms[$i] = $fullPath
                            }
                        }
                    }

                    # Iterate patches.
                    if ($Patches)
                    {
                        for ($i = 0; $i -lt $Patches.Length; $i++)
                        {
                            if (Test-Path -LiteralPath ($fullPath = (Join-Path -Path (Get-Item -LiteralPath $msiProduct).DirectoryName -ChildPath $Patches[$i].Replace('.\', [System.Management.Automation.Language.NullString]::Value)).Trim()) -PathType Leaf)
                            {
                                $Patches[$i] = $fullPath
                            }
                        }
                    }
                }

                # If the provided MSI was a file path, get the Property table and store it.
                $msiPropertyTable = if ([System.IO.Path]::GetExtension($msiProduct) -eq '.msi')
                {
                    $gmtpParams = @{ Path = $msiProduct; Table = 'Property' }; if ($Transforms) { $gmtpParams.Add('TransformPath', $Transforms) }
                    Get-ADTMsiTableProperty @gmtpParams
                }
                $msiPatchData = if ([System.IO.Path]::GetExtension($msiProduct) -eq '.msp')
                {
                    [PSADT.Utilities.MsiUtilities]::ExtractPatchXmlData($msiProduct).MsiPatch.TargetProduct
                }

                # Get the ProductCode of the MSI.
                [System.Guid[]]$msiProductCode = if ($ProductCode)
                {
                    $ProductCode
                }
                elseif ($InstalledApplication)
                {
                    $InstalledApplication.ProductCode
                }
                elseif ($msiPropertyTable)
                {
                    $msiPropertyTable.ProductCode
                }
                elseif ($msiPatchData)
                {
                    $msiPatchData.TargetProductCode.'#text'
                }

                # Check if the MSI is already installed. If no valid ProductCode to check or SkipMSIAlreadyInstalledCheck supplied, then continue with requested MSI action.
                $msiInstalled = if ($msiProductCode -and !$SkipMSIAlreadyInstalledCheck)
                {
                    if (!$InstalledApplication -and ($installedApps = Get-ADTApplication -ProductCode $msiProductCode -IncludeUpdatesAndHotfixes:$IncludeUpdatesAndHotfixes))
                    {
                        $InstalledApplication = $installedApps
                    }
                    !!$InstalledApplication
                }
                else
                {
                    $Action -ne 'Install'
                }

                # Return early if we're installing an installed product, or anything else for a non-installed product.
                if ($msiInstalled -and ($Action -eq 'Install'))
                {
                    Write-ADTLogEntry -Message "The MSI is already installed on this system. Skipping action [$Action]..."
                    return $(if ($PassThru) { [PSADT.ProcessManagement.ProcessResult]::new(1638) })
                }
                elseif (!$msiInstalled -and ($Action -ne 'Install'))
                {
                    Write-ADTLogEntry -Message "The MSI is not installed on this system. Skipping action [$Action]..."
                    return $(if ($PassThru) { [PSADT.ProcessManagement.ProcessResult]::new(1605) })
                }

                # Set up the log extension to use. The caller may provide it, but its optional.
                $logFileExtension = if ($PSBoundParameters.ContainsKey('LogFileName') -and $LogFileName -match '\.(log|txt|out)')
                {
                    $Matches.0
                }
                else
                {
                    '.log'
                }

                # Set up the log file to use.
                $logFile = if ($PSBoundParameters.ContainsKey('LogFileName'))
                {
                    # Strip any found extension off to make the file name easier to handle.
                    $LogFileName -replace ([System.Text.RegularExpressions.Regex]::Escape($logFileExtension))
                }
                elseif (!$adtSession -or !$adtSession.DisableLogging)
                {
                    if ($InstalledApplication)
                    {
                        if ($msiPatchData)
                        {
                            if ($msiPatchData.ChildNodes.LocalName.Contains('UpdatedVersion'))
                            {
                                (Remove-ADTInvalidFileNameChars -Name ($InstalledApplication.DisplayName + '_' + ($msiPatchData.UpdatedVersion | Select-Object -First 1))) -replace '\s+'
                            }
                            else
                            {
                                (Remove-ADTInvalidFileNameChars -Name $InstalledApplication.DisplayName) -replace '\s+'
                            }
                        }
                        else
                        {
                            if (![System.String]::IsNullOrWhiteSpace($InstalledApplication.DisplayVersion))
                            {
                                (Remove-ADTInvalidFileNameChars -Name ($InstalledApplication.DisplayName + '_' + $InstalledApplication.DisplayVersion)) -replace '\s+'
                            }
                            else
                            {
                                (Remove-ADTInvalidFileNameChars -Name $InstalledApplication.DisplayName) -replace '\s+'
                            }
                        }
                    }
                    elseif ($msiPropertyTable)
                    {
                        if ($msiPropertyTable.ContainsKey('ProductVersion'))
                        {
                            (Remove-ADTInvalidFileNameChars -Name ($msiPropertyTable.ProductName + '_' + $msiPropertyTable.ProductVersion)) -replace '\s+'
                        }
                        else
                        {
                            (Remove-ADTInvalidFileNameChars -Name $msiPropertyTable.ProductName) -replace '\s+'
                        }
                    }
                }

                # Build the log path to use.
                $logPath = if ($logFile)
                {
                    # Don't bother with a directory if the log file is fully qualified.
                    if (![System.IO.Path]::IsPathRooted($logFile))
                    {
                        # A defined MSI log path is considered an override.
                        $logPathProperty = ('LogPath', 'LogPathNoAdminRights')[$PSBoundParameters.ContainsKey('RunAsActiveUser')]
                        if (![System.String]::IsNullOrWhiteSpace($adtConfig.MSI.$logPathProperty))
                        {
                            # Create the Log directory if it doesn't already exist.
                            if (!(Test-Path -LiteralPath $adtConfig.MSI.$logPathProperty -PathType Container))
                            {
                                $null = [System.IO.Directory]::CreateDirectory($adtConfig.MSI.$logPathProperty)
                            }

                            # Build the log file path.
                            (Join-Path -Path $adtConfig.MSI.$logPathProperty -ChildPath $logFile).Trim()
                        }
                        elseif ($adtSession -and !$PSBoundParameters.ContainsKey('RunAsActiveUser'))
                        {
                            # Get the log directory from the session. This will factor in
                            # whether we're compressing logs, or logging to a subfolder.
                            if (!(Test-Path -LiteralPath $adtSession.LogPath -PathType Container))
                            {
                                $null = [System.IO.Directory]::CreateDirectory($adtSession.LogPath)
                            }
                            (Join-Path -Path $adtSession.LogPath -ChildPath $logFile).Trim()
                        }
                        else
                        {
                            # Fall back to the toolkit's LogPath.
                            if (!(Test-Path -LiteralPath $adtConfig.Toolkit.$logPathProperty -PathType Container))
                            {
                                $null = [System.IO.Directory]::CreateDirectory($adtConfig.Toolkit.$logPathProperty)
                            }

                            # Build the log file path.
                            (Join-Path -Path $adtConfig.Toolkit.$logPathProperty -ChildPath $logFile).Trim()
                        }
                    }
                    else
                    {
                        # Log path is already fully qualified.
                        $logFile
                    }
                }

                # Post-process the log path if we have one.
                if ($logPath)
                {
                    # Append the action if the log file doesn't have one.
                    if ($logFile -notmatch "$Action`$")
                    {
                        $logPath += "_$Action"
                    }

                    # Append the username to the log file name if the toolkit is not running as an administrator, since users do not have the rights to modify files in the ProgramData folder that belong to other users.
                    if ($PSBoundParameters.ContainsKey('RunAsActiveUser'))
                    {
                        $logPath += "_$(Remove-ADTInvalidFileNameChars -Name $RunAsActiveUser.UserName)"
                    }
                    elseif ((![PSADT.AccountManagement.AccountUtilities]::CallerIsLocalSystem -and [System.Environment]::UserInteractive) -or !(Test-ADTCallerIsAdmin))
                    {
                        $logPath += "_$(Remove-ADTInvalidFileNameChars -Name ([System.Environment]::UserName))"
                    }
                }

                # Set the installation parameters.
                if ($adtSession -and $adtSession.IsNonInteractive())
                {
                    $msiInstallDefaultParams = $adtConfig.MSI.SilentParams
                    $msiUninstallDefaultParams = $adtConfig.MSI.SilentParams
                }
                else
                {
                    $msiInstallDefaultParams = $adtConfig.MSI.InstallParams
                    $msiUninstallDefaultParams = $adtConfig.MSI.UninstallParams
                }

                # Build the MSI parameters.
                switch ($Action)
                {
                    Install
                    {
                        $option = '/i'
                        $msiDefaultParams = $msiInstallDefaultParams
                        break
                    }
                    Uninstall
                    {
                        $option = '/x'
                        $msiDefaultParams = $msiUninstallDefaultParams
                        break
                    }
                    Patch
                    {
                        $option = '/update'
                        $msiDefaultParams = $msiInstallDefaultParams
                        break
                    }
                    { $_ -eq 'Repair' -and $RepairMode -eq 'Reinstall' }
                    {
                        $option = '/i'
                        $msiDefaultParams = "$msiInstallDefaultParams REINSTALL=ALL REINSTALLMODE=$(if ($RepairFromSource) {'v'})omus"
                        break
                    }
                    { $_ -eq 'Repair' -and $RepairMode -eq 'Repair' }
                    {
                        $option = "/f$(if ($RepairFromSource) {'vomus'})"
                        $msiDefaultParams = $msiInstallDefaultParams
                        break
                    }
                    ActiveSetup
                    {
                        $option = '/fups'
                        $msiDefaultParams = $null
                        break
                    }
                }

                # Set the working directory of the MSI.
                if ($PSCmdlet.ParameterSetName.EndsWith('FilePath') -and !$workingDirectory)
                {
                    $WorkingDirectory = [System.IO.Path]::GetDirectoryName($msiProduct)
                }

                # Start building the MsiExec command line starting with the base action and file.
                $msiArgs = [System.Collections.Generic.List[System.String]]@($option, $msiProduct)

                # Add MST.
                if ($Transforms)
                {
                    $msiArgs.Add("TRANSFORMS=`"$([System.String]::Join(';', $Transforms))`"")
                    $msiArgs.Add("TRANSFORMSSECURE=1")
                }

                # Add MSP.
                if ($Patches)
                {
                    $msiArgs.Add("PATCH=`"$([System.String]::Join(';', $Patches))`"")
                }

                # Replace default parameters if specified.
                if ($ArgumentList)
                {
                    if ($ArgumentList.Length -eq 1)
                    {
                        $msiArgs.AddRange([PSADT.ProcessManagement.CommandLineUtilities]::CommandLineToArgumentList($ArgumentList[0]))
                    }
                    else
                    {
                        $msiArgs.AddRange($ArgumentList)
                    }
                }
                elseif (![System.String]::IsNullOrWhiteSpace($msiDefaultParams))
                {
                    $msiArgs.AddRange([PSADT.ProcessManagement.CommandLineUtilities]::CommandLineToArgumentList($msiDefaultParams))
                }

                # Add reinstallmode and reinstall variable for Patch.
                if ($Action -eq 'Patch')
                {
                    $msiArgs.Add('REINSTALLMODE=ecmus')
                    $msiArgs.Add('REINSTALL=ALL')
                }

                # Append parameters to default parameters if specified.
                if ($AdditionalArgumentList)
                {
                    if ($AdditionalArgumentList.Length -eq 1)
                    {
                        $msiArgs.AddRange([PSADT.ProcessManagement.CommandLineUtilities]::CommandLineToArgumentList($AdditionalArgumentList[0]))
                    }
                    else
                    {
                        $msiArgs.AddRange($AdditionalArgumentList)
                    }
                }

                # Add custom Logging Options if specified, otherwise, add default Logging Options from Config file.
                if ($logPath)
                {
                    if ($LoggingOptions)
                    {
                        $msiArgs.AddRange([PSADT.ProcessManagement.CommandLineUtilities]::CommandLineToArgumentList("$LoggingOptions `"$logPath$logFileExtension`""))
                    }
                    else
                    {
                        $msiArgs.AddRange([PSADT.ProcessManagement.CommandLineUtilities]::CommandLineToArgumentList("$($adtConfig.MSI.LoggingOptions) `"$logPath$logFileExtension`""))
                    }
                }

                # Build the hashtable with the options that will be passed to Start-ADTProcess using splatting.
                $ExecuteProcessSplat = @{
                    FilePath = "$([System.Environment]::SystemDirectory)\msiexec.exe"
                    ArgumentList = $msiArgs
                }
                $PSBoundParameters.GetEnumerator() | & {
                    begin
                    {
                        [System.String[]]$sapParams = $Script:CommandTable.'Start-ADTProcess'.Parameters.Keys
                    }

                    process
                    {
                        if ($sapParams.Contains($_.Key) -and !$ExecuteProcessSplat.ContainsKey($_.Key))
                        {
                            $ExecuteProcessSplat.Add($_.Key, $_.Value)
                        }
                    }
                }
            }
            catch
            {
                $ExecuteProcessSplat = $null
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }

        # If we've got no parameters, we error'd out above.
        if ($null -ne $ExecuteProcessSplat)
        {
            try
            {
                # Commence the MSI operation, then refresh Explorer as Windows does not consistently update environment variables created by MSIs.
                if (!$PSCmdlet.ShouldProcess($(if ($FilePath) { "MSI/MSP [$FilePath]" } else { "MSI ProductCode [$ProductCode]" }), $Action))
                {
                    return
                }
                $result = Start-ADTProcess @ExecuteProcessSplat
                if (!$NoDesktopRefresh)
                {
                    Update-ADTDesktop
                }

                # Return the results if passing through.
                if ($result -and $PassThru)
                {
                    return $result
                }
            }
            catch
            {
                $PSCmdlet.ThrowTerminatingError($_)
            }
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
