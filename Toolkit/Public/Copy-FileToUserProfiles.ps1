function Copy-FileToUserProfiles {
	<#
.SYNOPSIS

Copy one or more items to a each user profile on the system.

.DESCRIPTION

Copy one or more items to a each user profile on the system.

.PARAMETER Path

The path of the file or folder to copy.

.PARAMETER Destination

The path of the destination folder to append to the root of the user profile.

.PARAMETER Recurse

Copy files in subdirectories.

.PARAMETER Flatten

Flattens the files into the root destination directory.

.PARAMETER ContinueOnError

Continue if an error is encountered. This will continue the deployment script, but will not continue copying files if an error is encountered. Default is: $true.

.PARAMETER ContinueFileCopyOnError

Continue copying files if an error is encountered. This will continue the deployment script and will warn about files that failed to be copied. Default is: $false.

.PARAMETER UseRobocopy

Use Robocopy to copy files rather than native PowerShell method. Robocopy overcomes the 260 character limit. Only applies if $Path is specified as a folder. Default is configured in the AppDeployToolkitConfig.xml file: $true

.PARAMETER RobocopyAdditionalParams

Additional parameters to pass to Robocopy. Default is: $null

.INPUTS

You can pipe in string values for $Path.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Copy-FileToUserProfiles -Path "$dirSupportFiles\config.txt" -Destination "AppData\Roaming\MyApp"

Copy a single file to C:\Users\<UserName>\AppData\Roaming\MyApp for each user.

.EXAMPLE

Copy-FileToUserProfiles -Path "$dirSupportFiles\config.txt","$dirSupportFiles\config2.txt" -Destination "AppData\Roaming\MyApp"

Copy two files to C:\Users\<UserName>\AppData\Roaming\MyApp for each user.

.EXAMPLE

Copy-FileToUserProfiles -Path "$dirFiles\MyApp" -Destination "AppData\Local" -Recurse

Copy an entire folder to C:\Users\<UserName>\AppData\Local for each user.

.EXAMPLE

Copy-FileToUserProfiles -Path "$dirFiles\.appConfigFolder" -Recurse

Copy an entire folder to C:\Users\<UserName> for each user.

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory = $true, Position = 1, ValueFromPipeline = $true)]
		[String[]]$Path,
		[Parameter(Mandatory = $false, Position = 2)]
		[String]$Destination,
		[Parameter(Mandatory = $false)]
		[Switch]$Recurse = $false,
		[Parameter(Mandatory = $false)]
		[Switch]$Flatten,
		[Parameter(Mandatory = $false)]
		[ValidateNotNullOrEmpty()]
		[Boolean]$ContinueOnError = $true,
		[Parameter(Mandatory = $false)]
		[ValidateNotNullOrEmpty()]
		[Boolean]$ContinueFileCopyOnError = $false,
		[Parameter(Mandatory = $false)]
		[ValidateNotNullOrEmpty()]
		[Boolean]$UseRobocopy = $configToolkitUseRobocopy,
		[Parameter(Mandatory = $false)]
		[ValidateNotNullOrEmpty()]
		[String]$RobocopyAdditionalParams = $null
	)

	Begin {
		## Get the name of this function and write header
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		[Hashtable]$CopyFileSplat = @{
			Path                    = $Path
			Recurse                 = $Recurse
			Flatten                 = $Flatten
			ContinueOnError         = $ContinueOnError
			ContinueFileCopyOnError = $ContinueFileCopyOnError
			UseRobocopy             = $UseRobocopy
		}
		if ($RobocopyAdditionalParams) {
			$CopyFileSplat.RobocopyAdditionalParams = $RobocopyAdditionalParams
		}

		foreach ($UserProfilePath in (Get-UserProfiles).ProfilePath) {
			$CopyFileSplat.Destination = Join-Path $UserProfilePath $Destination
			Write-Log -Message "Copying path [$Path] to $($CopyFileSplat.Destination):" -Source ${CmdletName}
			Copy-File @CopyFileSplat
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
