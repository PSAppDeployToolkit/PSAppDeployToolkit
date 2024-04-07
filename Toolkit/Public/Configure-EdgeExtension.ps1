Function Configure-EdgeExtension {
	<#
    .SYNOPSIS
    Configures an extension for Microsoft Edge using the ExtensionSettings policy
    .DESCRIPTION
    This function configures an extension for Microsoft Edge using the ExtensionSettings policy: https://learn.microsoft.com/en-us/deployedge/microsoft-edge-manage-extensions-ref-guide
    This enables Edge Extensions to be installed and managed like applications, enabling extensions to be pushed to specific devices or users alongside existing GPO/Intune extension policies.
    This should not be used in conjunction with Edge Management Service which leverages the same registry key to configure Edge extensions.
    .PARAMETER Add
    Adds an extension configuration
    .PARAMETER Remove
    Removes an extension configuration
    .PARAMETER ExtensionID
    The ID of the extension to install.
    .PARAMETER InstallationMode
    The installation mode of the extension. Allowed values: blocked, allowed, removed, force_installed, normal_installed
    .PARAMETER UpdateUrl
    The update URL of the extension. This is the URL where the extension will check for updates.
    .PARAMETER MinimumVersionRequired
    The minimum version of the extension required for installation.
    .EXAMPLE
    Configure-EdgeExtension -Add -ExtensionID "extensionID" -InstallationMode "force_installed" -UpdateUrl "https://edge.microsoft.com/extensionwebstorebase/v1/crx"
    .EXAMPLE
    Configure-EdgeExtension -Remove -ExtensionID "extensionID"
    .NOTES
    This function is provided as a template to install an extension for Microsoft Edge. This should not be used in conjunction with Edge Management Service which leverages the same registry key to configure Edge extensions.
    #>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory = $true, ParameterSetName = 'Add')]
		[Switch]$Add,

		[Parameter(Mandatory = $true, ParameterSetName = 'Remove')]
		[Switch]$Remove,

		[Parameter(Mandatory = $true, ParameterSetName = 'Add')]
		[Parameter(Mandatory = $true, ParameterSetName = 'Remove')]
		[String]$ExtensionID,

		[Parameter(Mandatory = $true, ParameterSetName = 'Add')]
		[ValidateSet('blocked', 'allowed', 'removed', 'force_installed', 'normal_installed')]
		[String]$InstallationMode,

		[Parameter(Mandatory = $true, ParameterSetName = 'Add')]
		[String]$UpdateUrl,

		[Parameter(Mandatory = $false, ParameterSetName = 'Add')]
		[String]$MinimumVersionRequired
	)
	If ($Add) {
		If ($MinimumVersionRequired) {
			Write-Log -Message "Configuring extension with ID [$extensionID] with mode [Add] using installation mode [$InstallationMode] and update URL [$UpdateUrl] with minimum version required [$MinimumVersionRequired]." -Severity 1
		} Else {
			Write-Log -Message "Configuring extension with ID [$extensionID] with mode [Add] using installation mode [$InstallationMode] and update URL [$UpdateUrl]." -Severity 1
		}
	} Else {
		Write-Log -Message "Configuring extension with ID [$extensionID] with mode [Add]." -Severity 1
	}

	$regKeyEdgeExtensions = 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge'
	# Check if the ExtensionSettings registry key exists if not create it
	If (!(Test-RegistryValue -Key $regKeyEdgeExtensions -Value ExtensionSettings)) {
		Set-RegistryKey -Key $regKeyEdgeExtensions -Name ExtensionSettings -Value '' | Out-Null
	} Else {
		# Get the installed extensions
		$installedExtensions = Get-RegistryKey -Key $regKeyEdgeExtensions -Value ExtensionSettings | ConvertFrom-Json -ErrorAction SilentlyContinue
		Write-Log -Message "Configured extensions: [$($installedExtensions | ConvertTo-Json -Compress -ErrorAction SilentlyContinue)]." -Severity 1
	}

	Try {
		If ($Remove) {
			If ($installedExtensions.$($extensionID)) {
				# If the deploymentmode is Remove, remove the extension from the list
				Write-Log -Message "Removing extension with ID [$extensionID]." -Severity 1
				$installedExtensions.PSObject.Properties.Remove($extensionID)
				$jsonExtensionSettings = $installedExtensions | ConvertTo-Json -Compress
				Set-RegistryKey -Key $regKeyEdgeExtensions -Name 'ExtensionSettings' -Value $jsonExtensionSettings | Out-Null
			} Else {
				# If the extension is not configured
				Write-Log -Message "Extension with ID [$extensionID] is not configured. Removal not required." -Severity 1
			}
		}
		# Configure the extension
		ElseIf ($Add) {
			Write-Log -Message "Configuring extension ID [$extensionID]." -Severity 1
			If (!$installedExtensions) {
				$installedExtensions = @{}
			}
			If ($MinimumVersionRequired) {
				$installedExtensions | Add-Member -Name $($extensionID) -Value $(@{ 'installation_mode' = $InstallationMode; 'update_url' = $UpdateUrl; 'minimum_version_required' = $MinimumVersionRequired }) -MemberType NoteProperty -Force
			} Else {
				$installedExtensions | Add-Member -Name $($extensionID) -Value $(@{ 'installation_mode' = $InstallationMode; 'update_url' = $UpdateUrl }) -MemberType NoteProperty -Force
			}
			$jsonExtensionSettings = $installedExtensions | ConvertTo-Json -Compress
			Set-RegistryKey -Key $regKeyEdgeExtensions -Name 'ExtensionSettings' -Value $jsonExtensionSettings | Out-Null
		}
	} Catch {
		Write-Log -Message "Failed to configure extension with ID $extensionID. `r`n$(Resolve-Error)" -Severity 3
		Exit-Script -ExitCode 60001
	}
}
