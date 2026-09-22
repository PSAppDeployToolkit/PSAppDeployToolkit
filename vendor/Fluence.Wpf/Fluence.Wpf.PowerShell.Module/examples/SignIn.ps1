# SignIn.ps1 - Account and password sign-in dialog with input validation.
# Run: pwsh -File SignIn.ps1   OR   powershell.exe -File SignIn.ps1

Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

[object[]]$prompts = @(
    New-FluencePrompt -Name User -Message 'Account' -InputType Text -ValidateNotEmpty
    New-FluencePrompt -Name Pass -Message 'Password' -InputType Password -ValidateNotEmpty
)

[object[]]$buttons = @(
    (New-FluenceButton -Text 'Login' -IsDefault)
    'Cancel'
)

$result = Show-FluenceDialog -Title 'Sign In' -Prompts $prompts -Buttons $buttons

try
{
    if ($result.Login)
    {
        $credential = [System.Management.Automation.PSCredential]::new($result.User, $result.Pass)
        # Pass $credential to the intended authentication API here. Do not print its password.
        Write-Output "Credentials collected for: $($credential.UserName)"
    }
    else
    {
        Write-Output 'Sign-in cancelled.'
    }
}
finally
{
    if ($result.Pass -is [System.Security.SecureString]) { $result.Pass.Dispose() }
}
