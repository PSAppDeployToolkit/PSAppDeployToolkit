function Remove-ADTSessionOpeningCallback
{
	[CmdletBinding()]
	param
	(
		[Parameter(Mandatory = $true, ValueFromPipeline = $true)]
		[ValidateNotNullOrEmpty()]
		[System.Management.Automation.CommandInfo]$Callback
	)

	# Grab all pipeline accumulation and remove all applicable callbacks.
	$openingCallbacks = (Get-ADTModuleData).OpeningCallbacks
	$input.Where({$openingCallbacks.Contains($_)}).ForEach({$openingCallbacks.Remove($_)})
}
