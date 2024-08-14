function Remove-ADTSessionClosingCallback
{
	[CmdletBinding()]
	param
	(
		[Parameter(Mandatory = $true, ValueFromPipeline = $true)]
		[ValidateNotNullOrEmpty()]
		[System.Management.Automation.CommandInfo]$Callback
	)

	# Grab all pipeline accumulation and remove all applicable callbacks.
	$closingCallbacks = (Get-ADTModuleData).ClosingCallbacks
	$input.Where({$closingCallbacks.Contains($_)}).ForEach({$closingCallbacks.Remove($_)})
}
