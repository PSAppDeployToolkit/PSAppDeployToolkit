# Tutorial: your first dialog in ten minutes

In this tutorial you will import the module, show a message, ask a question, build a small form with two prompts and validation, and read the answers back in your script. At the end you will have a working script you can adapt.

You need Windows 10 or 11 and either Windows PowerShell 5.1 (already on your machine) or PowerShell 7.4 or later. No .NET SDK, no project, no XAML.

## 1. Get the module

If you have a release zip, extract it so that the folder `Fluence.Wpf.PowerShell` sits under one of your module paths, for example:

```text
C:\Users\<you>\Documents\PowerShell\Modules\Fluence.Wpf.PowerShell\
```

If you work from the repository instead, stage the library into the module once:

```powershell
dotnet build Fluence.Wpf/Fluence.Wpf.csproj -c Release
pwsh -NoProfile -File Fluence.Wpf.PowerShell.Module/build/Build-Module.ps1
```

Now open a PowerShell console and import the module. From a module path:

```powershell
Import-Module Fluence.Wpf.PowerShell
```

From the repository:

```powershell
Import-Module .\Fluence.Wpf.PowerShell.Module\src\Fluence.Wpf.PowerShell\Fluence.Wpf.PowerShell.psd1
```

Check that it loaded:

```powershell
Get-Command -Module Fluence.Wpf.PowerShell | Select-Object -ExpandProperty Name
```

You should see sixteen commands, from `Close-FluenceProgress` to `Update-FluenceProgress`.

## 2. Show a message

Type this and press Enter:

```powershell
Show-FluenceMessage -Message 'Hello from PowerShell.' -Icon Success
```

A small window appears in the middle of the screen: a green check mark, your text, and an OK button. It follows your Windows light or dark setting and your accent colour. Click OK.

The console shows `OK`. That is the return value: the name of the button that closed the dialog. Every message dialog returns a string like this, so you can branch on it.

## 3. Ask a question

```powershell
$answer = Show-FluenceMessage -Message 'Continue with the setup?' -Icon Question -Buttons YesNo
"You chose $answer"
```

Click Yes or No and read the line the console prints. Now run it again and close the dialog with the X in the title bar instead. The console prints `You chose No`: dismissing a dialog gives you the safe answer, never an empty one.

## 4. Build a form

A form is a list of prompts. Each prompt has a name (the key you read the value back with), a label, and an input type. Type these three lines:

```powershell
$prompts = @(
    New-FluencePrompt -Name Server -Message 'Server name' -DefaultValue 'localhost' -ValidateNotEmpty
    New-FluencePrompt -Name Port -Message 'Port' -InputType Number -DefaultValue 443
)
```

Then show them:

```powershell
$result = Show-FluenceDialog -Title 'Connection' -Message 'Where should the agent connect?' -Prompts $prompts -Buttons 'Connect', 'Cancel'
```

A dialog opens with a text box and a number box. Clear the server name and click Connect: an error bar appears under the fields saying the server name is required, and the dialog stays open. Type a name and click Connect again.

## 5. Read the result

Print what came back:

```powershell
$result
```

You see one property per prompt (`Server`, `Port`) and one boolean per button (`Connect`, `Cancel`), plus `Cancelled` and `TimedOut`. Use them like this:

```powershell
if ($result.Connect)
{
    "Connecting to $($result.Server):$($result.Port)"
}
else
{
    'Cancelled.'
}
```

Run the dialog once more and press Esc. `Cancelled` is now `$true` and `Connect` is `$false`.

## 6. Put it in a script

Save this as `Connect.ps1` and run it with `pwsh -File .\Connect.ps1` or `powershell.exe -File .\Connect.ps1`:

```powershell
Import-Module Fluence.Wpf.PowerShell

$prompts = @(
    New-FluencePrompt -Name Server -Message 'Server name' -DefaultValue 'localhost' -ValidateNotEmpty
    New-FluencePrompt -Name Port -Message 'Port' -InputType Number -DefaultValue 443
)

$result = Show-FluenceDialog -Title 'Connection' -Message 'Where should the agent connect?' -Prompts $prompts -Buttons 'Connect', 'Cancel'

if (-not $result.Connect)
{
    Show-FluenceMessage -Message 'Nothing was changed.' -Icon Info
    return
}

Show-FluenceMessage -Message "Connecting to $($result.Server):$($result.Port)" -Icon Success
```

You have shown a message, asked a question, validated a form and read the answers. The same three cmdlets, `Show-FluenceMessage`, `New-FluencePrompt` and `Show-FluenceDialog`, cover most scripts.

## Where to go next

- Add a timeout, an image or a corner position to a dialog: [Show dialogs and messages](how-to/dialogs.md).
- Use the other input types (passwords, dates, file pickers, lists): [Build forms with validation](how-to/forms-and-validation.md).
- Show progress while a long task runs: [Show progress during long work](how-to/progress.md).
- Look up every parameter: [Reference](reference/README.md).
