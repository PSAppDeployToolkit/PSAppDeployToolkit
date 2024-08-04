function Out-ADTPowerShellEncodedCommand
{
    <#

    .NOTES
    This function can be called without an active ADT session.

    #>

	[CmdletBinding()]
	param
	(
		[Parameter(Mandatory = $true)]
		[ValidateNotNullOrEmpty()]
		[System.String]$Command
	)

	return [System.Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($Command))
}
