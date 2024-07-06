function Get-ADT
{
	& (Get-Module -Name $Script:MyInvocation.MyCommand.ScriptBlock.Module.Name) {$ADT}
}
