function Get-ADT
{
	& (Get-Module -Name $MyInvocation.MyCommand.Module.Name) {$ADT}
}
