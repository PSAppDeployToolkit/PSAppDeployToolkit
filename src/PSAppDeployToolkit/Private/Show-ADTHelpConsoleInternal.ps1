#-----------------------------------------------------------------------------
#
# MARK: Show-ADTHelpConsoleInternal
#
#-----------------------------------------------------------------------------

function Show-ADTHelpConsoleInternal
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ModuleName,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Guid]$Guid,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Version]$ModuleVersion
    )

    # Ensure script runs in strict mode since this may be called in a new scope.
    $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
    $ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
    Set-StrictMode -Version 3

    # Import the module and store its passthru data so we can access it later.
    $module = Import-Module -FullyQualifiedName ([Microsoft.PowerShell.Commands.ModuleSpecification]::new($PSBoundParameters)) -PassThru

    # Build out a panel to hold the list box (flattens border)
    $helpListPanel = [System.Windows.Forms.Panel]::new()
    $helpListPanel.ClientSize = [System.Drawing.Size]::new(281, 559)
    $helpListPanel.Location = [System.Drawing.Point]::new(3, 0)
    $helpListPanel.BorderStyle = [System.Windows.Forms.BorderStyle]::FixedSingle
    $helpListPanel.Anchor = "Top, Left, Bottom"
    $helpListPanel.Controls.Add(($helpListBox = [System.Windows.Forms.ListBox]::new()))
    $helpListBox.BorderStyle = [System.Windows.Forms.BorderStyle]::None
    $helpListBox.Font = [System.Drawing.SystemFonts]::MessageBoxFont
    $helpListBox.Dock = [System.Windows.Forms.DockStyle]::Fill
    $helpListBox.IntegralHeight = $false
    $helpListBox.add_SelectedIndexChanged({ $helpTextBox.Text = [System.String]::Join("`n", ((Get-Help -Name $helpListBox.SelectedItem -Full | Out-String -Stream -Width ([System.Int32]::MaxValue)) -replace '^\s+$').TrimEnd()).Trim().Replace('<br />', $null) })
    $null = $helpListBox.Items.AddRange(($module.ExportedCommands.Keys | Sort-Object))

    # Build out a panel to hold the rich text box (flattens border)
    $helpTextPanel = [System.Windows.Forms.Panel]::new()
    $helpTextPanel.ClientSize = [System.Drawing.Size]::new(1034, 559)
    $helpTextPanel.Location = [System.Drawing.Point]::new(287, 0)
    $helpTextPanel.BorderStyle = [System.Windows.Forms.BorderStyle]::FixedSingle
    $helpTextPanel.Anchor = "Top, Left, Right, Bottom"
    $helpTextPanel.Controls.Add(($helpTextBox = [System.Windows.Forms.RichTextBox]::new()))
    $helpTextBox.BorderStyle = [System.Windows.Forms.BorderStyle]::None
    $helpTextBox.Font = [System.Drawing.Font]::new('Consolas', 9)
    $helpTextBox.Dock = [System.Windows.Forms.DockStyle]::Fill
    $helpTextBox.ReadOnly = $true
    $helpTextBox.WordWrap = $false

    # Build out the form. The suspend/resume is crucial for HiDPI support!
    $helpForm = [System.Windows.Forms.Form]::new()
    $helpForm.SuspendLayout()
    $helpForm.Text = "$($module.Name) Help Console"
    $helpForm.Font = [System.Drawing.SystemFonts]::MessageBoxFont
    $helpForm.AutoScaleDimensions = [System.Drawing.SizeF]::new(7, 15)
    $helpForm.AutoScaleMode = [System.Windows.Forms.AutoScaleMode]::Font
    $helpForm.ClientSize = [System.Drawing.Size]::new(1324, 562)
    $helpForm.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::Sizable
    $helpForm.StartPosition = [System.Windows.Forms.FormStartPosition]::CenterScreen
    $helpForm.Controls.Add($helpListPanel)
    $helpForm.Controls.Add($helpTextPanel)
    $helpForm.ResumeLayout()

    # Show the form. Using Application.Run automatically manages disposal for us.
    [System.Windows.Forms.Application]::Run($helpForm)
}
