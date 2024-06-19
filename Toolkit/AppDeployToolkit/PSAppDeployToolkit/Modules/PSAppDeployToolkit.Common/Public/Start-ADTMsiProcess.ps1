function Start-ADTMsiProcess
{
    <#

    .SYNOPSIS
    Executes msiexec.exe to perform the following actions for MSI & MSP files and MSI product codes: install, uninstall, patch, repair, active setup.

    .DESCRIPTION
    Executes msiexec.exe to perform the following actions for MSI & MSP files and MSI product codes: install, uninstall, patch, repair, active setup.

    If the -Action parameter is set to "Install" and the MSI is already installed, the function will exit.

    Sets default switches to be passed to msiexec based on the preferences in the XML configuration file.

    Automatically generates a log file name and creates a verbose log file for all msiexec operations.

    Expects the MSI or MSP file to be located in the "Files" sub directory of the App Deploy Toolkit. Expects transform files to be in the same directory as the MSI file.

    .PARAMETER Action
    The action to perform. Options: Install, Uninstall, Patch, Repair, ActiveSetup.

    .PARAMETER Path
    The path to the MSI/MSP file or the product code of the installed MSI.

    .PARAMETER Transforms
    The name of the transform file(s) to be applied to the MSI. The transform file is expected to be in the same directory as the MSI file.

    .PARAMETER Patches
    The name of the patch (msp) file(s) to be applied to the MSI for use with the "Install" action. The patch file is expected to be in the same directory as the MSI file.

    .PARAMETER Parameters
    Overrides the default parameters specified in the XML configuration file. Install default is: "REBOOT=ReallySuppress /QB!". Uninstall default is: "REBOOT=ReallySuppress /QN".

    .PARAMETER AddParameters
    Adds to the default parameters specified in the XML configuration file. Install default is: "REBOOT=ReallySuppress /QB!". Uninstall default is: "REBOOT=ReallySuppress /QN".

    .PARAMETER SecureParameters
    Hides all parameters passed to the MSI or MSP file from the toolkit Log file.

    .PARAMETER LoggingOptions
    Overrides the default logging options specified in the XML configuration file. Default options are: "/L*v".

    .PARAMETER LogName
    Overrides the default log file name. The default log file name is generated from the MSI file name. If LogName does not end in .log, it will be automatically appended.

    For uninstallations, by default the product code is resolved to the DisplayName and version of the application.

    .PARAMETER LogName
    Overrides the default log file name. The default log file name is generated from the MSI file name. If LogName does not end in .log, it will be automatically appended.

    For uninstallations, by default the product code is resolved to the DisplayName and version of the application.

    .PARAMETER WorkingDirectory
    Overrides the working directory. The working directory is set to the location of the MSI file.

    .PARAMETER SkipMSIAlreadyInstalledCheck
    Skips the check to determine if the MSI is already installed on the system. Default is: $false.

    .PARAMETER IncludeUpdatesAndHotfixes
    Include matches against updates and hotfixes in results.

    .PARAMETER NoWait
    Immediately continue after executing the process.

    .PARAMETER PassThru
    Returns ExitCode, STDOut, and STDErr output from the process.

    .PARAMETER IgnoreExitCodes
    List the exit codes to ignore or * to ignore all exit codes.

    .PARAMETER PriorityClass
    Specifies priority class for the process. Options: Idle, Normal, High, AboveNormal, BelowNormal, RealTime. Default: Normal

    .PARAMETER ExitOnProcessFailure
    Specifies whether the function should call Close-ADTSession when the process returns an exit code that is considered an error/failure. Default: $true

    .PARAMETER RepairFromSource
    Specifies whether we should repair from source. Also rewrites local cache. Default: $false

    .PARAMETER ContinueOnError
    Continue if an error occurred while trying to start the process. Default: $false.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    PSObject. Returns a PSObject with the results of the installation.
    - ExitCode
    - StdOut
    - StdErr

    .EXAMPLE
    # Install an MSI.
    Start-ADTMsiProcess -Action 'Install' -Path 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi'

    .EXAMPLE
    # Install an MSI, applying a transform and overriding the default MSI toolkit parameters.
    Start-ADTMsiProcess -Action 'Install' -Path 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi' -Transform 'Adobe_FlashPlayer_11.2.202.233_x64_EN_01.mst' -Parameters '/QN'

    .EXAMPLE
    # Install an MSI and stores the result of the execution into a variable by using the -PassThru option.
    [PSObject]$ExecuteMSIResult = Start-ADTMsiProcess -Action 'Install' -Path 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi' -PassThru

    .EXAMPLE
    # Uninstall an MSI using a product code.
    Start-ADTMsiProcess -Action 'Uninstall' -Path '{26923b43-4d38-484f-9b9e-de460746276c}'

    .EXAMPLE
    # Install an MSP.
    Start-ADTMsiProcess -Action 'Patch' -Path 'Adobe_Reader_11.0.3_EN.msp'

    .LINK
    https://psappdeploytoolkit.com

    #>

    param (
        [Parameter(Mandatory = $false)]
        [ValidateSet('Install', 'Uninstall', 'Patch', 'Repair', 'ActiveSetup')]
        [System.String]$Action = 'Install',

        [Parameter(Mandatory = $true, HelpMessage = 'Please enter either the path to the MSI/MSP file or the ProductCode')]
        [ValidateScript({($_ -match (Get-ADTEnvironment).MSIProductCodeRegExPattern) -or (('.msi', '.msp') -contains [System.IO.Path]::GetExtension($_))})]
        [Alias('FilePath')]
        [System.String]$Path,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Transforms,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Alias('Arguments')]
        [System.String]$Parameters,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$AddParameters,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SecureParameters,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Patches,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$LoggingOptions,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$LogName,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$WorkingDirectory,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SkipMSIAlreadyInstalledCheck,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$IncludeUpdatesAndHotfixes,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoWait,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$IgnoreExitCodes,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Idle', 'Normal', 'High', 'AboveNormal', 'BelowNormal', 'RealTime')]
        [System.Diagnostics.ProcessPriorityClass]$PriorityClass = 'Normal',

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoExitOnProcessFailure,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$RepairFromSource
    )

    begin {
        $adtEnv = Get-ADTEnvironment
        $adtConfig = Get-ADTConfig
        $adtSession = Get-ADTSession
        $pathIsProductCode = $Path -match $adtEnv.MSIProductCodeRegExPattern
        Write-ADTDebugHeader
    }

    process {
        # If the path matches a product code.
        if ($pathIsProductCode)
        {
            # Resolve the product code to a publisher, application name, and version.
            Write-ADTLogEntry -Message 'Resolving product code to a publisher, application name, and version.'
            $productCodeNameVersion = Get-ADTInstalledApplication -ProductCode $Path -IncludeUpdatesAndHotfixes:$IncludeUpdatesAndHotfixes | Select-Object -Property Publisher, DisplayName, DisplayVersion -First 1 -ErrorAction Ignore

            # Build the log file name.
            if (!$LogName)
            {
                $LogName = if ($productCodeNameVersion)
                {
                    if ($productCodeNameVersion.Publisher)
                    {
                        (Remove-ADTInvalidFileNameChars -Name ($productCodeNameVersion.Publisher + '_' + $productCodeNameVersion.DisplayName + '_' + $productCodeNameVersion.DisplayVersion)) -replace ' '
                    }
                    else
                    {
                        (Remove-ADTInvalidFileNameChars -Name ($productCodeNameVersion.DisplayName + '_' + $productCodeNameVersion.DisplayVersion)) -replace ' '
                    }
                }
                else {
                    # Out of other options, make the Product Code the name of the log file.
                    $Path
                }
            }
        }
        elseif (!$LogName)
        {
            # Get the log file name without file extension.
            $LogName = ([System.IO.FileInfo]$Path).BaseName
        }
        else
        {
            while ('.log', '.txt' -contains [System.IO.Path]::GetExtension($LogName))
            {
                $LogName = [System.IO.Path]::GetFileNameWithoutExtension($LogName)
            }
        }

        # Build the log file path.
        $logPath = if ($adtConfig.Toolkit.CompressLogs)
        {
            [String]$logPath = Join-Path -Path $adtSession.GetPropertyValue('LogTempFolder') -ChildPath $LogName
        }
        else
        {
            # Create the Log directory if it doesn't already exist.
            if (![System.IO.Directory]::Exists($adtConfig.MSI.LogPath))
            {
                [System.Void][System.IO.Directory]::CreateDirectory($adtConfig.MSI.LogPath)
            }

            # Build the log file path.
            Join-Path -Path $adtConfig.MSI.LogPath -ChildPath $LogName
        }

        # Set the installation parameters.
        if ($adtSession.IsSilent())
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
        switch ($action)
        {
            'Install' {
                $option = '/i'
                $msiLogFile = "$logPath" + '_Install'
                $msiDefaultParams = $msiInstallDefaultParams
            }
            'Uninstall' {
                $option = '/x'
                $msiLogFile = "$logPath" + '_Uninstall'
                $msiDefaultParams = $msiUninstallDefaultParams
            }
            'Patch' {
                $option = '/update'
                $msiLogFile = "$logPath" + '_Patch'
                $msiDefaultParams = $msiInstallDefaultParams
            }
            'Repair' {
                $option = "/f$(if ($RepairFromSource) {'vomus'})"
                $msiLogFile = "$logPath" + '_Repair'
                $msiDefaultParams = $msiInstallDefaultParams
            }
            'ActiveSetup' {
                $option = '/fups'
                $msiLogFile = "$logPath" + '_ActiveSetup'
                $msiDefaultParams = $null
            }
        }

        # Append ".log" to the MSI logfile path and enclose in quotes.
        if ([IO.Path]::GetExtension($msiLogFile) -ne '.log')
        {
            $msiLogFile = "`"$($msiLogFile + '.log')`""
        }

        # If the MSI is in the Files directory, set the full path to the MSI.
        [String]$msiFile = if ([System.IO.File]::Exists(($dirFilesPath = [System.IO.Path]::Combine($adtSession.GetPropertyValue('DirFiles'), $Path))))
        {
            $dirFilesPath
        }
        elseif (Test-Path -LiteralPath $Path)
        {
            (Get-Item -LiteralPath $Path).FullName
        }
        elseif ($pathIsProductCode)
        {
            $Path
        }
        else
        {
            Write-ADTLogEntry -Message "Failed to find MSI file [$Path]." -Severity 3
            throw [System.IO.FileNotFoundException]::new("Failed to find MSI file [$Path].")
        }

        # Set the working directory of the MSI.
        if (!$pathIsProductCode -and !$workingDirectory)
        {
            $WorkingDirectory = [System.IO.Path]::GetDirectoryName($msiFile)
        }

        # Enumerate all transforms specified, qualify the full path if possible and enclose in quotes.
        $mstFile = if ($Transforms)
        {
            # Fix up any bad file paths.
            for ($i = 0; $i -lt $Transforms.Length; $i++)
            {
                if (($FullPath = Join-Path -Path (Split-Path -Path $msiFile -Parent) -ChildPath $Transforms[$i].Replace('.\', '')) -and [System.IO.File]::Exists($FullPath))
                {
                    $Transforms[$i] = $FullPath
                }
            }

            # Echo an msiexec.exe compatible string back out with all transforms.
            "`"$($Transforms -join ';')`""
        }

        # Enumerate all patches specified, qualify the full path if possible and enclose in quotes.
        $mspFile = if ($Patches)
        {
            # Fix up any bad file paths.
            for ($i = 0; $i -lt $patches.Length; $i++)
            {
                if (($FullPath = Join-Path -Path (Split-Path -Path $msiFile -Parent) -ChildPath $patches[$i].Replace('.\', '')) -and [System.IO.File]::Exists($FullPath))
                {
                    $Patches[$i] = $FullPath
                }
            }

            # Echo an msiexec.exe compatible string back out with all patches.
            "`"$($Patches -join ';')`""
        }

        # Get the ProductCode of the MSI.
        $MSIProductCode = If ($pathIsProductCode)
        {
            $Path
        }
        elseif ([System.IO.Path]::GetExtension($msiFile) -eq '.msi')
        {
            try
            {
                [Hashtable]$GetMsiTablePropertySplat = @{ Path = $msiFile; Table = 'Property'; ContinueOnError = $false }
                if ($Transforms) {$GetMsiTablePropertySplat.Add('TransformPath', $transforms)}
                Get-MsiTableProperty @GetMsiTablePropertySplat | Select-Object -ExpandProperty ProductCode
            }
            catch
            {
                Write-ADTLogEntry -Message "Failed to get the ProductCode from the MSI file. Continue with requested action [$Action]..."
            }
        }

        # Start building the MsiExec command line starting with the base action and file.
        $argsMSI = "$option `"$msiFile`""

        # Add MST.
        if ($mstFile)
        {
            $argsMSI = "$argsMSI TRANSFORMS=$mstFile TRANSFORMSSECURE=1"
        }

        # Add MSP.
        if ($mspFile)
        {
            $argsMSI = "$argsMSI PATCH=$mspFile"
        }

        # Replace default parameters if specified.
        $argsMSI = if ($Parameters)
        {
            "$argsMSI $Parameters"
        }
        else
        {
            "$argsMSI $msiDefaultParams"
        }

        # Add reinstallmode and reinstall variable for Patch.
        If ($action -eq 'Patch')
        {
            $argsMSI = "$argsMSI REINSTALLMODE=ecmus REINSTALL=ALL"
        }

        # Append parameters to default parameters if specified.
        if ($AddParameters)
        {
            $argsMSI = "$argsMSI $AddParameters"
        }

        # Add custom Logging Options if specified, otherwise, add default Logging Options from Config file.
        $argsMSI = if ($LoggingOptions)
        {
            "$argsMSI $LoggingOptions $msiLogFile"
        }
        else
        {
            "$argsMSI $($adtConfig.MSI.LoggingOptions) $msiLogFile"
        }

        # Check if the MSI is already installed. If no valid ProductCode to check or SkipMSIAlreadyInstalledCheck supplied, then continue with requested MSI action.
        $IsMsiInstalled = if ($MSIProductCode -and !$SkipMSIAlreadyInstalledCheck)
        {
            !!(Get-ADTInstalledApplication -ProductCode $MSIProductCode -IncludeUpdatesAndHotfixes:$IncludeUpdatesAndHotfixes)
        }
        else
        {
            $Action -ne 'Install'
        }

        # Bypass if we're installing and the MSI is already installed, otherwise proceed.
        $ExecuteResults = if ($IsMsiInstalled -and ($Action -eq 'Install'))
        {
            Write-ADTLogEntry -Message "The MSI is already installed on this system. Skipping action [$Action]..."
            [PSADT.Types.ProcessResult]@{ExitCode = 1638; StdOut = [System.String]::Empty; StdErr = [System.String]::Empty}
        }
        elseif ((!$IsMsiInstalled -and ($Action -eq 'Install')) -or $IsMsiInstalled)
        {
            # Build the hashtable with the options that will be passed to Start-ADTProcess using splatting.
            Write-ADTLogEntry -Message "Executing MSI action [$Action]..."
            $ExecuteProcessSplat = @{
                Path = $adtEnv.exeMsiexec
                Parameters = $argsMSI
                WindowStyle = 'Normal'
                NoExitOnProcessFailure = $NoExitOnProcessFailure
            }
            if ($WorkingDirectory)
            {
                $ExecuteProcessSplat.Add('WorkingDirectory', $WorkingDirectory)
            }
            if ($SecureParameters)
            {
                $ExecuteProcessSplat.Add('SecureParameters', $SecureParameters)
            }
            if ($PassThru)
            {
                $ExecuteProcessSplat.Add('PassThru', $PassThru)
            }
            if ($IgnoreExitCodes)
            {
                $ExecuteProcessSplat.Add('IgnoreExitCodes', $IgnoreExitCodes)
            }
            if ($PriorityClass)
            {
                $ExecuteProcessSplat.Add('PriorityClass', $PriorityClass)
            }
            if ($NoWait)
            {
                $ExecuteProcessSplat.Add('NoWait', $NoWait)
            }

            # Call the Start-ADTProcess function.
            Start-ADTProcess @ExecuteProcessSplat

            # Refresh environment variables for Windows Explorer process as Windows does not consistently update environment variables created by MSIs.
            Update-ADTDesktop
        }
        else
        {
            Write-ADTLogEntry -Message "The MSI is not installed on this system. Skipping action [$Action]..."
        }

        # Return the results if passing through.
        if ($PassThru -and $ExecuteResults)
        {
            $ExecuteResults
        }
    }

    end {
        Write-ADTDebugFooter
    }
}
