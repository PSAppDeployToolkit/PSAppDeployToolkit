Function Update-SessionEnvironmentVariables {
<#
.SYNOPSIS
	Updates the environment variables for the current PowerShell session with any environment variable changes that may have occurred during script execution.
.DESCRIPTION
	Environment variable changes that take place during script execution are not visible to the current PowerShell session.
	Use this function to refresh the current PowerShell session with all environment variable settings.
.PARAMETER LoadLoggedOnUserEnvironmentVariables
	If script is running in SYSTEM context, this option allows loading environment variables from the active console user. If no console user exists but users are logged in, such as on terminal servers, then the first logged-in non-console user.
.PARAMETER ContinueOnError
	Continue if an error is encountered. Default is: $true.
.EXAMPLE
	Update-SessionEnvironmentVariables
.NOTES
	This function has an alias: Update-SessionEnvironmentVariables
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[ValidateNotNullOrEmpty()]
		[Switch]$LoadLoggedOnUserEnvironmentVariables,

		[ValidateNotNullOrEmpty()]
		[Switch]$ContinueOnError = $True
	)

	begin {
		## Get the name of this function and write header
		$CmdletName = ($PSCmdlet.MyInvocation.MyCommand.Name)

		Write-FunctionInfo -CmdletName $CmdletName -CmdletBoundParameters $PSBoundParameters -Header

		$GetEnvironmentVar = {
			Param (
				$Key,
				$Scope
			)
			[Environment]::GetEnvironmentVariable($Key, $Scope)
		}
	}

	process {
		Try {

			Write-Log -Message 'Refreshing the environment variables for this PowerShell session.' -Source ${CmdletName}

			If ($LoadLoggedOnUserEnvironmentVariables -and $RunAsActiveUser) {
				$CurrentUserEnvironmentSID = $RunAsActiveUser.SID
			} Else {
				$CurrentUserEnvironmentSID = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
			}

			$MachineEnvironmentVars = 'Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment'
			$UserEnvironmentVars = "Registry::HKEY_USERS\$CurrentUserEnvironmentSID\Environment"

			# Update all session environment variables. 
			# Ordering is important here: $UserEnvironmentVars comes second so that we can override $MachineEnvironmentVars.
			$MachineEnvironmentVars, $UserEnvironmentVars | Get-Item | ForEach-Object {
				if($_) {
					$envRegPath = $_.PSPath; $_.Property | ForEach-Object {
						Set-Item -LiteralPath "env:$($_)" -Value (Get-ItemProperty -LiteralPath $envRegPath -Name $_).$_
					}
				}
			}

			# Set PATH environment variable separately because it is a combination of the user and machine environment variables
			$PathFolders = 'Machine', 'User' | ForEach-Object {
				$EachPathFolder = (& $GetEnvironmentVar -Key 'PATH' -Scope $_)
				
				if($EachPathFolder){
					$EachPathFolder.Trim(';').Split(';').Trim().Trim('"')
				}
			} | Select-Object -Unique

			$env:PATH = $PathFolders -join ';'
		} Catch {

			Write-Log -Message "Failed to refresh the environment variables for this PowerShell session. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			
			If (-not $ContinueOnError) {
				Throw "Failed to refresh the environment variables for this PowerShell session: $($_.Exception.Message)"
			}
		}
	}

	end {
		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}