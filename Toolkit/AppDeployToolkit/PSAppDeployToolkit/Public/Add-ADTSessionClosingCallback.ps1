#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Add-ADTSessionClosingCallback
{
	[CmdletBinding()]
	param
	(
		[Parameter(Mandatory = $true)]
		[ValidateNotNullOrEmpty()]
		[System.Management.Automation.CommandInfo[]]$Callback
	)

	# Send it off to the backend function.
	Invoke-ADTSessionCallbackOperation -Type Closing -Action Add @PSBoundParameters
}
