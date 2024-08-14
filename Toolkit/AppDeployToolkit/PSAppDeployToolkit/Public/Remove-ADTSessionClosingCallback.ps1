#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Remove-ADTSessionClosingCallback
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
		Invoke-ADTSessionCallbackOperation -Type Closing -Action Remove @PSBoundParameters
	}
	catch
	{
		$PSCmdlet.ThrowTerminatingError($_)
	}
}
