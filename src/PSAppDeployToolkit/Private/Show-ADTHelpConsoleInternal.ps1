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
        [System.String[]]$ModuleBase
    )

    # Ensure script runs in strict mode since this may be called in a new scope.
    $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
    $ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
    Set-StrictMode -Version 3

    # Import the module and store its passthru data so we can access it later.
    $modules = Import-Module -Name ($ModuleBase | Sort-Object) -PassThru
    $defFont = [System.Drawing.Font]::new('Consolas', 9)

    # Calculate a DPI offset. This is pretty rough but there's no great way to adjust these sizes otherwise.
    $dpiOffset = & {
        begin
        {
            $gfx = [System.Drawing.Graphics]::FromHwnd([System.IntPtr]::Zero)
            $scale = $gfx.DpiX / 96
        }
        process
        {
            if ($scale -gt 1.0)
            {
                return $scale
            }
        }
        end
        {
            $gfx.Dispose()
        }
    }

    # Build out a panel to hold the list box (flattens border)
    $helpListBox = [System.Windows.Forms.ListBox]::new()
    $helpListBox.ClientSize = [System.Drawing.Size]::new(307, 532)
    $helpListBox.Location = [System.Drawing.Point]::new(5, 32)
    $helpListBox.Font = $defFont
    $helpListBox.Anchor = "Top, Left, Bottom"
    $helpListBox.IntegralHeight = $false
    $helpListBox.Sorted = $true
    $helpListBox.TabIndex = 1
    $helpListBox.add_SelectedIndexChanged({ $helpTextBox.Text = [System.String]::Join("`n", ((Get-Help -Name $helpListBox.SelectedItem -Full | Out-String -Stream -Width ([System.Int32]::MaxValue)) -replace '^\s+$').TrimEnd()).Trim().Replace('<br />', $null) })

    # Build out a panel to hold the module combo box (flattens border)
    $helpComboBox = [System.Windows.Forms.ComboBox]::new()
    $helpComboBox.ClientSize = [System.Drawing.Size]::new(307, 25)
    $helpComboBox.Location = [System.Drawing.Point]::new(5, 5)
    $helpComboBox.Font = $defFont
    $helpComboBox.Anchor = "Top, Left, Bottom"
    $helpComboBox.Sorted = $true
    $helpComboBox.TabIndex = 0
    $helpComboBox.Items.AddRange($modules) | Out-Null
    $helpComboBox.add_SelectedIndexChanged({ $helpListBox.Items.Clear(); $helpListBox.Items.AddRange($helpComboBox.SelectedItem.ExportedCommands.Keys) })
    $helpComboBox.SelectedIndex = 0

    # Build out a panel to hold the rich text box (flattens border)
    $helpTextBox = [System.Windows.Forms.RichTextBox]::new()
    $helpTextBox.BorderStyle = [System.Windows.Forms.BorderStyle]::FixedSingle
    $helpTextBox.ClientSize = [System.Drawing.Size]::new(992, 559)
    $helpTextBox.Location = [System.Drawing.Point]::new(321, 5)
    $helpTextBox.Font = [System.Drawing.Font]::new('Consolas', 9)
    $helpTextBox.Anchor = "Top, Left, Right, Bottom"
    $helpTextBox.ReadOnly = $true
    $helpTextBox.WordWrap = $false
    $helpTextBox.TabIndex = 2

    # Build out the form. The suspend/resume is crucial for HiDPI support!
    $helpForm = [System.Windows.Forms.Form]::new()
    $helpForm.SuspendLayout()
    $helpForm.Text = "$($modules[0].Name) Help Console"
    $helpForm.Font = $defFont
    $helpForm.AutoScaleDimensions = [System.Drawing.SizeF]::new(7, 14)
    $helpForm.AutoScaleMode = [System.Windows.Forms.AutoScaleMode]::Font
    $helpForm.ClientSize = [System.Drawing.Size]::new([System.Math]::Round(1322 - $dpiOffset + (1. / 65536.)), [System.Math]::Round(573 - $dpiOffset - (1. / 65536.)))
    $helpForm.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::Sizable
    $helpForm.StartPosition = [System.Windows.Forms.FormStartPosition]::CenterScreen
    $helpForm.Controls.Add($helpComboBox)
    $helpForm.Controls.Add($helpListBox)
    $helpForm.Controls.Add($helpTextBox)
    $helpForm.ResumeLayout()

    # Show the form. Using Application.Run automatically manages disposal for us.
    [System.Windows.Forms.Application]::Run($helpForm)
}
