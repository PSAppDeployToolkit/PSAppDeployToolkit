#-----------------------------------------------------------------------------
#
# MARK: Get-ADTModuleManifest
#
#-----------------------------------------------------------------------------

function Get-ADTModuleManifest
{
    return ([System.Management.Automation.Language.Parser]::ParseFile("$Script:PSScriptRoot\$($MyInvocation.MyCommand.Module.Name).psd1", [ref]$null, [ref]$null).EndBlock.Statements.PipelineElements.Expression.SafeGetValue())
}
