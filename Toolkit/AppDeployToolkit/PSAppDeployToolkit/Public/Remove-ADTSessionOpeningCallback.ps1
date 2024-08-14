#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Remove-ADTSessionOpeningCallback
{
	[CmdletBinding()]
	param
	(
		[Parameter(Mandatory = $true)]
		[ValidateNotNullOrEmpty()]
		[System.Management.Automation.CommandInfo[]]$Callback
	)

	# Send it off to the backend function.
	try
	{
		Invoke-ADTSessionCallbackOperation -Type Opening -Action Remove @PSBoundParameters
	}
	catch
	{
		$PSCmdlet.ThrowTerminatingError($_)
	}
}
