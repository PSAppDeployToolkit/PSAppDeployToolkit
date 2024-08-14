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
	Invoke-ADTSessionCallbackOperation -Type Opening -Action Remove @PSBoundParameters
}
