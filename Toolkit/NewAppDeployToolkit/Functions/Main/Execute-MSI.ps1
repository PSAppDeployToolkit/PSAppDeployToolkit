#region Function Execute-MSI
Function Execute-MSI {
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
.PARAMETER Transform
	The name of the transform file(s) to be applied to the MSI. The transform file is expected to be in the same directory as the MSI file. Multiple transforms have to be separated by a semi-colon.
.PARAMETER Patch
	The name of the patch (msp) file(s) to be applied to the MSI for use with the "Install" action. The patch file is expected to be in the same directory as the MSI file. Multiple patches have to be separated by a semi-colon.
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
	Specifies whether the function should call Exit-Script when the process returns an exit code that is considered an error/failure. Default: $true
.PARAMETER RepairFromSource
	Specifies whether we should repair from source. Also rewrites local cache. Default: $false
.PARAMETER ContinueOnError
	Continue if an error occured while trying to start the process. Default: $false.
.EXAMPLE
	Execute-MSI -Action 'Install' -Path 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi'
	Installs an MSI
.EXAMPLE
	Execute-MSI -Action 'Install' -Path 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi' -Transform 'Adobe_FlashPlayer_11.2.202.233_x64_EN_01.mst' -Parameters '/QN'
	Installs an MSI, applying a transform and overriding the default MSI toolkit parameters
.EXAMPLE
	[psobject]$ExecuteMSIResult = Execute-MSI -Action 'Install' -Path 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi' -PassThru
	Installs an MSI and stores the result of the execution into a variable by using the -PassThru option
.EXAMPLE
	Execute-MSI -Action 'Uninstall' -Path '{26923b43-4d38-484f-9b9e-de460746276c}'
	Uninstalls an MSI using a product code
.EXAMPLE
	Execute-MSI -Action 'Patch' -Path 'Adobe_Reader_11.0.3_EN.msp'
	Installs an MSP
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[ValidateSet('Install','Uninstall','Patch','Repair','ActiveSetup')]
		[string]$Action = 'Install',
		[Parameter(Mandatory=$true,HelpMessage='Please enter either the path to the MSI/MSP file or the ProductCode')]
		[ValidateScript({($_ -match $MSIProductCodeRegExPattern) -or ('.msi','.msp' -contains [IO.Path]::GetExtension($_))})]
		[Alias('FilePath')]
		[string]$Path,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$Transform,
		[Parameter(Mandatory=$false)]
		[Alias('Arguments')]
		[ValidateNotNullorEmpty()]
		[string]$Parameters,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$AddParameters,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[switch]$SecureParameters = $false,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$Patch,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$LoggingOptions,
		[Parameter(Mandatory=$false)]
		[Alias('LogName')]
		[string]$private:LogName,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$WorkingDirectory,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[switch]$SkipMSIAlreadyInstalledCheck = $false,
		[Parameter(Mandatory=$false)]
		[switch]$IncludeUpdatesAndHotfixes = $false,
		[Parameter(Mandatory=$false)]
		[switch]$NoWait = $false,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[switch]$PassThru = $false,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$IgnoreExitCodes,
		[Parameter(Mandatory=$false)]
		[ValidateSet('Idle', 'Normal', 'High', 'AboveNormal', 'BelowNormal', 'RealTime')]
		[Diagnostics.ProcessPriorityClass]$PriorityClass = 'Normal',
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$ExitOnProcessFailure = $true,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$RepairFromSource = $false,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$ContinueOnError = $false
	)

	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionInfo -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		## Initialize variable indicating whether $Path variable is a Product Code or not
		[boolean]$PathIsProductCode = $false

		## If the path matches a product code
		If ($Path -match $MSIProductCodeRegExPattern) {
			#  Set variable indicating that $Path variable is a Product Code
			[boolean]$PathIsProductCode = $true

			#  Resolve the product code to a publisher, application name, and version
			Write-Log -Message 'Resolving product code to a publisher, application name, and version.' -Source ${CmdletName}

			If ($IncludeUpdatesAndHotfixes) {
				[psobject]$productCodeNameVersion = Get-InstalledApplication -ProductCode $path -IncludeUpdatesAndHotfixes | Select-Object -Property 'Publisher', 'DisplayName', 'DisplayVersion' -First 1 -ErrorAction 'SilentlyContinue'
			}
			Else {
				[psobject]$productCodeNameVersion = Get-InstalledApplication -ProductCode $path | Select-Object -Property 'Publisher', 'DisplayName', 'DisplayVersion' -First 1 -ErrorAction 'SilentlyContinue'
			}

			#  Build the log file name
			If (-not $logName) {
				If ($productCodeNameVersion) {
					If ($productCodeNameVersion.Publisher) {
						$logName = (Remove-InvalidFileNameChars -Name ($productCodeNameVersion.Publisher + '_' + $productCodeNameVersion.DisplayName + '_' + $productCodeNameVersion.DisplayVersion)) -replace ' ',''
					}
					Else {
						$logName = (Remove-InvalidFileNameChars -Name ($productCodeNameVersion.DisplayName + '_' + $productCodeNameVersion.DisplayVersion)) -replace ' ',''
					}
				}
				Else {
					#  Out of other options, make the Product Code the name of the log file
					$logName = $Path
				}
			}
		}
		Else {
			#  Get the log file name without file extension
			If (-not $logName) { $logName = ([IO.FileInfo]$path).BaseName } ElseIf ('.log','.txt' -contains [IO.Path]::GetExtension($logName)) { $logName = [IO.Path]::GetFileNameWithoutExtension($logName) }
		}

		If ($configToolkitCompressLogs) {
			## Build the log file path
			[string]$logPath = Join-Path -Path $logTempFolder -ChildPath $logName
		}
		Else {
			## Create the Log directory if it doesn't already exist
			If (-not (Test-Path -LiteralPath $configMSILogDir -PathType 'Container' -ErrorAction 'SilentlyContinue')) {
				$null = New-Item -Path $configMSILogDir -ItemType 'Directory' -ErrorAction 'SilentlyContinue'
			}
			## Build the log file path
			[string]$logPath = Join-Path -Path $configMSILogDir -ChildPath $logName
		}

		## Set the installation Parameters
		If ($deployModeSilent) {
			$msiInstallDefaultParams = $configMSISilentParams
			$msiUninstallDefaultParams = $configMSISilentParams
		}
		Else {
			$msiInstallDefaultParams = $configMSIInstallParams
			$msiUninstallDefaultParams = $configMSIUninstallParams
		}

		## Build the MSI Parameters
		Switch ($action) {
			'Install' { $option = '/i'; [string]$msiLogFile = "$logPath" + '_Install'; $msiDefaultParams = $msiInstallDefaultParams }
			'Uninstall' { $option = '/x'; [string]$msiLogFile = "$logPath" + '_Uninstall'; $msiDefaultParams = $msiUninstallDefaultParams }
			'Patch' { $option = '/update'; [string]$msiLogFile = "$logPath" + '_Patch'; $msiDefaultParams = $msiInstallDefaultParams }
			'Repair' { $option = '/f'; If ($RepairFromSource) {	$option += "v" } [string]$msiLogFile = "$logPath" + '_Repair'; $msiDefaultParams = $msiInstallDefaultParams }
			'ActiveSetup' { $option = '/fups'; [string]$msiLogFile = "$logPath" + '_ActiveSetup' }
		}

		## Append ".log" to the MSI logfile path and enclose in quotes
		If ([IO.Path]::GetExtension($msiLogFile) -ne '.log') {
			[string]$msiLogFile = $msiLogFile + '.log'
			[string]$msiLogFile = "`"$msiLogFile`""
		}

		## If the MSI is in the Files directory, set the full path to the MSI
		If (Test-Path -LiteralPath (Join-Path -Path $dirFiles -ChildPath $path -ErrorAction 'SilentlyContinue') -PathType 'Leaf' -ErrorAction 'SilentlyContinue') {
			[string]$msiFile = Join-Path -Path $dirFiles -ChildPath $path
		}
		ElseIf (Test-Path -LiteralPath $Path -ErrorAction 'SilentlyContinue') {
			[string]$msiFile = (Get-Item -LiteralPath $Path).FullName
		}
		ElseIf ($PathIsProductCode) {
			[string]$msiFile = $Path
		}
		Else {
			Write-Log -Message "Failed to find MSI file [$path]." -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to find MSI file [$path]."
			}
			Continue
		}

		## Set the working directory of the MSI
		If ((-not $PathIsProductCode) -and (-not $workingDirectory)) { [string]$workingDirectory = Split-Path -Path $msiFile -Parent }

		## Enumerate all transforms specified, qualify the full path if possible and enclose in quotes
		If ($transform) {
			[string[]]$transforms = $transform -replace "`"","" -split ';'
			for ($i = 0; $i -lt $transforms.Length; $i++) {
				[string]$FullPath = $null
				[string]$FullPath = Join-Path -Path (Split-Path -Path $msiFile -Parent) -ChildPath $transforms[$i].Replace('.\','')
				If ($FullPath -and (Test-Path -LiteralPath $FullPath -PathType 'Leaf')) {
					$transforms[$i] = $FullPath
				}
			}
			[string]$mstFile = "`"$($transforms -join ';')`""
		}

		## Enumerate all patches specified, qualify the full path if possible and enclose in quotes
		If ($patch) {
			[string[]]$patches = $patch -replace "`"","" -split ';'
			for ($i = 0; $i -lt $patches.Length; $i++) {
				[string]$FullPath = $null
				[string]$FullPath = Join-Path -Path (Split-Path -Path $msiFile -Parent) -ChildPath $patches[$i].Replace('.\','')
				If ($FullPath -and (Test-Path -LiteralPath $FullPath -PathType 'Leaf')) {
					$patches[$i] = $FullPath
				}
			}
			[string]$mspFile = "`"$($patches -join ';')`""
		}

		## Get the ProductCode of the MSI
		If ($PathIsProductCode) {
			[string]$MSIProductCode = $path
		}
		ElseIf ([IO.Path]::GetExtension($msiFile) -eq '.msi') {
			Try {
				[hashtable]$GetMsiTablePropertySplat = @{ Path = $msiFile; Table = 'Property'; ContinueOnError = $false }
				If ($transforms) { $GetMsiTablePropertySplat.Add( 'TransformPath', $transforms ) }
				[string]$MSIProductCode = Get-MsiTableProperty @GetMsiTablePropertySplat | Select-Object -ExpandProperty 'ProductCode' -ErrorAction 'Stop'
			}
			Catch {
				Write-Log -Message "Failed to get the ProductCode from the MSI file. Continue with requested action [$Action]..." -Source ${CmdletName}
			}
		}

		## Enclose the MSI file in quotes to avoid issues with spaces when running msiexec
		[string]$msiFile = "`"$msiFile`""

		## Start building the MsiExec command line starting with the base action and file
		[string]$argsMSI = "$option $msiFile"
		#  Add MST
		If ($transform) { $argsMSI = "$argsMSI TRANSFORMS=$mstFile TRANSFORMSSECURE=1" }
		#  Add MSP
		If ($patch) { $argsMSI = "$argsMSI PATCH=$mspFile" }
		#  Replace default parameters if specified.
		If ($Parameters) { $argsMSI = "$argsMSI $Parameters" } Else { $argsMSI = "$argsMSI $msiDefaultParams" }
		#  Add reinstallmode and reinstall variable for Patch
		If ($action -eq 'Patch') {$argsMSI += " REINSTALLMODE=ecmus REINSTALL=ALL"}
		#  Append parameters to default parameters if specified.
		If ($AddParameters) { $argsMSI = "$argsMSI $AddParameters" }
		#  Add custom Logging Options if specified, otherwise, add default Logging Options from Config file
		If ($LoggingOptions) { $argsMSI = "$argsMSI $LoggingOptions $msiLogFile" } Else { $argsMSI = "$argsMSI $configMSILoggingOptions $msiLogFile" }

		## Check if the MSI is already installed. If no valid ProductCode to check, then continue with requested MSI action.
		If ($MSIProductCode) {
			If ($SkipMSIAlreadyInstalledCheck) {
				[boolean]$IsMsiInstalled = $false
			}
			Else {
				If ($IncludeUpdatesAndHotfixes) {
					[psobject]$MsiInstalled = Get-InstalledApplication -ProductCode $MSIProductCode -IncludeUpdatesAndHotfixes
				}
				Else {
					[psobject]$MsiInstalled = Get-InstalledApplication -ProductCode $MSIProductCode
				}
				If ($MsiInstalled) { [boolean]$IsMsiInstalled = $true }
			}
		}
		Else {
			If ($Action -eq 'Install') { [boolean]$IsMsiInstalled = $false } Else { [boolean]$IsMsiInstalled = $true }
		}

		If (($IsMsiInstalled) -and ($Action -eq 'Install')) {
			Write-Log -Message "The MSI is already installed on this system. Skipping action [$Action]..." -Source ${CmdletName}
		}
		ElseIf (((-not $IsMsiInstalled) -and ($Action -eq 'Install')) -or ($IsMsiInstalled)) {
			Write-Log -Message "Executing MSI action [$Action]..." -Source ${CmdletName}
			#  Build the hashtable with the options that will be passed to Execute-Process using splatting
			[hashtable]$ExecuteProcessSplat =  @{
				Path = $exeMsiexec
				Parameters = $argsMSI
				WindowStyle = 'Normal'
				ExitOnProcessFailure = $ExitOnProcessFailure
				ContinueOnError = $ContinueOnError
			}
			If ($WorkingDirectory) { $ExecuteProcessSplat.Add( 'WorkingDirectory', $WorkingDirectory) }
			If ($SecureParameters) { $ExecuteProcessSplat.Add( 'SecureParameters', $SecureParameters) }
			If ($PassThru) { $ExecuteProcessSplat.Add( 'PassThru', $PassThru) }
			If ($IgnoreExitCodes) {  $ExecuteProcessSplat.Add( 'IgnoreExitCodes', $IgnoreExitCodes) }
			If ($PriorityClass) {  $ExecuteProcessSplat.Add( 'PriorityClass', $PriorityClass) }
			If ($NoWait) { $ExecuteProcessSplat.Add( 'NoWait', $NoWait) }

			#  Call the Execute-Process function
			If ($PassThru) {
				[psobject]$ExecuteResults = Execute-Process @ExecuteProcessSplat
			}
			Else {
				Execute-Process @ExecuteProcessSplat
			}
			#  Refresh environment variables for Windows Explorer process as Windows does not consistently update environment variables created by MSIs
			Update-Desktop
		}
		Else {
			Write-Log -Message "The MSI is not installed on this system. Skipping action [$Action]..." -Source ${CmdletName}
		}
	}
	End {
		If ($PassThru) { Write-Output -InputObject $ExecuteResults }
		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}
#endregion
