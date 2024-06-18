function Initialize-ADTModule
{
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]$Cmdlet
    )

    # Initialise the module's global state.
    Initialize-ADTEnvironment
    Import-ADTConfig
    Import-ADTLocalizedStrings
    (Get-ADT).LastExitCode = 0

    # Export environment variables to the user's scope.
    [System.Void]$ExecutionContext.InvokeCommand.InvokeScript($Cmdlet.SessionState, {$args[0].GetEnumerator().ForEach({New-Variable -Name $_.Key -Value $_.Value -Option ReadOnly -Force})}.Ast.GetScriptBlock(), (Get-ADT).Environment)
}
