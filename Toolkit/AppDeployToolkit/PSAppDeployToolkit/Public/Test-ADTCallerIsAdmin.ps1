function Test-ADTCallerIsAdmin
{
	<#

	.NOTES
	This function can be called without an active ADT session.

	#>

	return [System.Security.Principal.WindowsPrincipal]::new([System.Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([System.Security.Principal.WindowsBuiltinRole]::Administrator)
}
