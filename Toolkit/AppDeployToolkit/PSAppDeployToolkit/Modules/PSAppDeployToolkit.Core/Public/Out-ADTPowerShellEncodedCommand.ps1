function Out-ADTPowerShellEncodedCommand
{
	[CmdletBinding()]
	param
	(
		[Parameter(Mandatory = $true)]
		[ValidateNotNullOrEmpty()]
		[System.String]$Command
	)

	return [System.Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($Command))
}
