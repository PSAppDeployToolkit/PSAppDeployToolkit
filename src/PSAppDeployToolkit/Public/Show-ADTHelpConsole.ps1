#-----------------------------------------------------------------------------
#
# MARK: Show-ADTHelpConsole
#
#-----------------------------------------------------------------------------

function Show-ADTHelpConsole
{
    <#
    .SYNOPSIS
        Displays a help console for the ADT module.

    .DESCRIPTION
        Displays a help console for the ADT module in a new PowerShell window. The console provides a graphical interface to browse and view detailed help information for all commands exported by the ADT module. The help console includes a list box to select commands and a text box to display the full help content for the selected command.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        Show-ADTHelpConsole

        Opens a new PowerShell window displaying the help console for the ADT module.

    .NOTES
        This function can be called without an active ADT session.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    # Run this via a new PowerShell window so it doesn't stall the main thread.
    & $Script:CommandTable.'Start-Process' -FilePath (& $Script:CommandTable.'Get-ADTPowerShellProcessPath') -NoNewWindow -ArgumentList "-ExecutionPolicy Bypass -NonInteractive -NoProfile -NoLogo -EncodedCommand $(& $Script:CommandTable.'Out-ADTPowerShellEncodedCommand' -Command "$({
        # Ensure job runs in strict mode since its in a new scope.
        $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
        $ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
        $WarningPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
        Set-StrictMode -Version 3

        # Import the module and store its passthru data so we can access it later.
        $module = Import-Module -Name $ModulePath -DisableNameChecking -PassThru

        # Build out the form's listbox.
        $helpListBox = [System.Windows.Forms.ListBox]::new()
        $helpListBox.ClientSize = [System.Drawing.Size]::new(261, 675)
        $helpListBox.Font = [System.Drawing.SystemFonts]::MessageBoxFont
        $helpListBox.Location = [System.Drawing.Point]::new(3,0)
        $helpListBox.add_SelectedIndexChanged({$helpTextBox.Text = [System.String]::Join("`n", ((Get-Help -Name $helpListBox.SelectedItem -Full | Out-String -Stream -Width ([System.Int32]::MaxValue)) -replace '^\s+$').TrimEnd()).Trim()})
        $null = $helpListBox.Items.AddRange(($module.ExportedCommands.Keys | Sort-Object))

        # Build out the form's textbox.
        $helpTextBox = [System.Windows.Forms.RichTextBox]::new()
        $helpTextBox.ClientSize = [System.Drawing.Size]::new(1250, 675)
        $helpTextBox.Font = [System.Drawing.Font]::new('Consolas', 9)
        $helpTextBox.Location = [System.Drawing.Point]::new(271,0)
        $helpTextBox.ReadOnly = $true
        $helpTextBox.WordWrap = $false

        # Build out the form. The suspend/resume is crucial for HiDPI support!
        $helpForm = [System.Windows.Forms.Form]::new()
        $helpForm.SuspendLayout()
        $helpForm.Text = "$($module.Name) Help Console"
        $helpForm.Font = [System.Drawing.SystemFonts]::MessageBoxFont
        $helpForm.AutoScaleDimensions = [System.Drawing.SizeF]::new(7,15)
        $helpForm.AutoScaleMode = [System.Windows.Forms.AutoScaleMode]::Font
        $helpForm.AutoSize = $true
        $helpForm.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::Fixed3D
        $helpForm.MaximizeBox = $false
        $helpForm.Controls.Add($helpListBox)
        $helpForm.Controls.Add($helpTextBox)
        $helpForm.ResumeLayout()

        # Show the form. Using Application.Run automatically manages disposal for us.
        [System.Windows.Forms.Application]::Run($helpForm)
    }.ToString().Replace('$ModulePath', "$($Script:PSScriptRoot)\$($MyInvocation.MyCommand.Module.Name).psd1"))")"
}
