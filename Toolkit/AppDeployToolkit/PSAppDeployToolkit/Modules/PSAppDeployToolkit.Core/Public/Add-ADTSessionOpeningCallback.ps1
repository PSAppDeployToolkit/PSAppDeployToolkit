function Add-ADTSessionOpeningCallback
{
	[CmdletBinding()]
	param
	(
		[Parameter(Mandatory = $true, ValueFromPipeline = $true)]
		[ValidateNotNullOrEmpty()]
		[System.Management.Automation.CommandInfo]$Callback
	)

	# Grab all pipeline accumulation and add all valid callbacks.
	$openingCallbacks = (Get-ADTModuleData).OpeningCallbacks
	$input.Where({!$openingCallbacks.Contains($_)}).ForEach({$openingCallbacks.Add($_)})
}
