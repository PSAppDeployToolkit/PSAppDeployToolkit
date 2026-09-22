function New-FluenceInputControl
{
    <#
    .SYNOPSIS
        Builds the WPF input control for a single Fluence.Prompt and wires its change capture.
    .DESCRIPTION
        Returns one FrameworkElement (a composite StackPanel for Radio and File/Folder prompts)
        rendered with Fluence controls and the seeded theme. Pre-seeds $State.Result[$Prompt.Name]
        with the prompt's DefaultValue and wires the control's change event to write the live value
        back into $State.Result so an untouched field still returns its default. Every handler uses
        GetNewClosure so it captures the current $State and the control instance.
    .PARAMETER Prompt
        A Fluence.Prompt object (see New-FluencePrompt).
    .PARAMETER State
        The shared @{ Result = @{}; Window = $null } hashtable that accumulates captured values.
    .OUTPUTS
        System.Windows.FrameworkElement
    .NOTES
        Must run on a UI (STA) thread; call it through the window builder. No hard-coded colors:
        Fluence controls resolve their own themed brushes from the seeded slots.
    #>
    [CmdletBinding()]
    [OutputType([System.Windows.FrameworkElement])]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Builds a WPF control tree in memory; changes no external system state.')]
    param
    (
        [Parameter(Mandatory = $true)]
        [object]$Prompt,

        [Parameter(Mandatory = $true)]
        [hashtable]$State
    )

    $name = $Prompt.Name

    # Pre-seed the result with the default so an untouched field still returns its default.
    $State.Result[$name] = $Prompt.DefaultValue

    switch ($Prompt.InputType)
    {
        'Multiline'
        {
            $ctl = [Fluence.Wpf.Controls.TextBox]::new()
            $ctl.AcceptsReturn = $true
            $ctl.TextWrapping = [System.Windows.TextWrapping]::Wrap
            $ctl.MinLines = 3
            if ($null -ne $Prompt.DefaultValue)
            {
                $ctl.Text = [string]$Prompt.DefaultValue
            }
            $ctl.add_TextChanged({
                $State.Result[$name] = $ctl.Text
            }.GetNewClosure())
            return $ctl
        }

        'Password'
        {
            # System.Windows.Controls.PasswordBox is sealed, so the library styles the native type through an
            # implicit style and exposes its extras as PasswordBoxExtensions attached properties.
            $ctl = [System.Windows.Controls.PasswordBox]::new()
            [Fluence.Wpf.Controls.PasswordBoxExtensions]::SetRevealButtonEnabled($ctl, $true)
            if ($null -ne $Prompt.DefaultValue)
            {
                $ctl.Password = [string]$Prompt.DefaultValue
            }
            $asPlainText = [bool]$Prompt.AsPlainText
            if ($asPlainText)
            {
                $State.Result[$name] = $ctl.Password
            }
            else
            {
                $State.Result[$name] = $ctl.SecurePassword
            }
            $ctl.add_PasswordChanged({
                if ($State.Result[$name] -is [System.Security.SecureString])
                {
                    $State.Result[$name].Dispose()
                }
                if ($asPlainText)
                {
                    $State.Result[$name] = $ctl.Password
                }
                else
                {
                    $State.Result[$name] = $ctl.SecurePassword
                }
            }.GetNewClosure())
            return $ctl
        }

        'Number'
        {
            $ctl = [Fluence.Wpf.Controls.NumberBox]::new()
            if ($null -ne $Prompt.DefaultValue)
            {
                $ctl.Value = [double]$Prompt.DefaultValue
            }
            # Seed from the control's live value (the NumberBox Value default is 0, not $null) so an
            # untouched field returns a number rather than $null; ValueChanged does not fire on the
            # initial render.
            $State.Result[$name] = $ctl.Value
            $ctl.add_ValueChanged({
                $State.Result[$name] = $ctl.Value
            }.GetNewClosure())
            return $ctl
        }

        'Checkbox'
        {
            $ctl = [Fluence.Wpf.Controls.CheckBox]::new()
            $ctl.Content = $Prompt.Message
            if ($null -ne $Prompt.DefaultValue)
            {
                $ctl.IsChecked = [bool]$Prompt.DefaultValue
            }
            # Seed from the control's live state so an untouched checkbox returns a [bool] (false when
            # there is no default), not the raw/typeless pre-seed; Click only fires on interaction.
            $State.Result[$name] = $ctl.IsChecked
            $ctl.add_Click({
                $State.Result[$name] = $ctl.IsChecked
            }.GetNewClosure())
            return $ctl
        }

        'Toggle'
        {
            $ctl = [Fluence.Wpf.Controls.ToggleSwitch]::new()
            $ctl.OnContent = 'On'
            $ctl.OffContent = 'Off'
            if ($null -ne $Prompt.DefaultValue)
            {
                $ctl.IsChecked = [bool]$Prompt.DefaultValue
            }
            # Seed from the control's live state so an untouched toggle returns a [bool] (false when
            # there is no default), not the raw/typeless pre-seed; Click only fires on interaction.
            $State.Result[$name] = $ctl.IsChecked
            $ctl.add_Click({
                $State.Result[$name] = $ctl.IsChecked
            }.GetNewClosure())
            return $ctl
        }

        'Choice'
        {
            if ($Prompt.As -eq 'Radio')
            {
                $panel = [System.Windows.Controls.StackPanel]::new()
                $panel.Orientation = [System.Windows.Controls.Orientation]::Vertical
                $State.Result[$name] = $null
                foreach ($option in $Prompt.ValidateSet)
                {
                    $radio = [Fluence.Wpf.Controls.RadioButton]::new()
                    $radio.Content = $option
                    $radio.GroupName = $name
                    $radio.Margin = [System.Windows.Thickness]::new(0, 2, 0, 2)
                    if ($null -ne $Prompt.DefaultValue -and [string]$option -eq [string]$Prompt.DefaultValue)
                    {
                        $radio.IsChecked = $true
                        $State.Result[$name] = $option
                    }
                    $radio.add_Checked({
                        $State.Result[$name] = $radio.Content
                    }.GetNewClosure())
                    $null = $panel.Children.Add($radio)
                }
                return $panel
            }

            $ctl = [Fluence.Wpf.Controls.ComboBox]::new()
            foreach ($option in $Prompt.ValidateSet)
            {
                $null = $ctl.Items.Add($option)
            }
            if ($null -ne $Prompt.DefaultValue)
            {
                $ctl.SelectedItem = $Prompt.DefaultValue
            }
            $State.Result[$name] = $ctl.SelectedItem
            $ctl.add_SelectionChanged({
                $State.Result[$name] = $ctl.SelectedItem
            }.GetNewClosure())
            return $ctl
        }

        'List'
        {
            $ctl = [Fluence.Wpf.Controls.ListView]::new()
            $ctl.MaxHeight = 240
            foreach ($option in $Prompt.ValidateSet)
            {
                $null = $ctl.Items.Add($option)
            }

            if ($Prompt.MultiSelect)
            {
                $ctl.SelectionMode = [System.Windows.Controls.SelectionMode]::Extended
                foreach ($default in @($Prompt.DefaultValue))
                {
                    if ($null -ne $default -and $Prompt.ValidateSet -contains [string]$default)
                    {
                        $null = $ctl.SelectedItems.Add([string]$default)
                    }
                }
                # Always an array, even for zero or one selected item, so callers can index and count.
                $State.Result[$name] = [object[]]@($ctl.SelectedItems)
                $ctl.add_SelectionChanged({
                    $State.Result[$name] = [object[]]@($ctl.SelectedItems)
                }.GetNewClosure())
                return $ctl
            }

            $ctl.SelectionMode = [System.Windows.Controls.SelectionMode]::Single
            if ($null -ne $Prompt.DefaultValue)
            {
                $ctl.SelectedItem = [string]$Prompt.DefaultValue
            }
            $State.Result[$name] = $ctl.SelectedItem
            $ctl.add_SelectionChanged({
                $State.Result[$name] = $ctl.SelectedItem
            }.GetNewClosure())
            return $ctl
        }

        'Date'
        {
            $ctl = [Fluence.Wpf.Controls.DatePicker]::new()
            if ($null -ne $Prompt.DefaultValue)
            {
                $ctl.SelectedDate = [datetime]$Prompt.DefaultValue
                $State.Result[$name] = $ctl.SelectedDate
            }
            $ctl.add_SelectedDateChanged({
                $State.Result[$name] = $ctl.SelectedDate
            }.GetNewClosure())
            return $ctl
        }

        'Time'
        {
            $ctl = [Fluence.Wpf.Controls.TimePicker]::new()
            if ($null -ne $Prompt.DefaultValue)
            {
                $ctl.SelectedTime = [timespan]$Prompt.DefaultValue
                $State.Result[$name] = $ctl.SelectedTime
            }
            $ctl.add_SelectedTimeChanged({
                $State.Result[$name] = $ctl.SelectedTime
            }.GetNewClosure())
            return $ctl
        }

        'Link'
        {
            $ctl = [Fluence.Wpf.Controls.HyperlinkButton]::new()
            $text = $Prompt.Message
            if ([string]::IsNullOrWhiteSpace($text))
            {
                $text = [string]$Prompt.DefaultValue
            }
            $ctl.Content = $text
            if ($null -ne $Prompt.DefaultValue)
            {
                $ctl.NavigateUri = [uri]$Prompt.DefaultValue
            }
            # No change capture; the pre-seeded default value is the result.
            return $ctl
        }

        { $_ -in 'FileOpen', 'FileSave', 'FolderOpen' }
        {
            $panel = [System.Windows.Controls.StackPanel]::new()
            $panel.Orientation = [System.Windows.Controls.Orientation]::Horizontal

            $textBox = [Fluence.Wpf.Controls.TextBox]::new()
            $textBox.MinWidth = 240
            if ($null -ne $Prompt.DefaultValue)
            {
                $textBox.Text = [string]$Prompt.DefaultValue
            }
            $textBox.add_TextChanged({
                $State.Result[$name] = $textBox.Text
            }.GetNewClosure())

            $browse = [Fluence.Wpf.Controls.Button]::new()
            $browse.Content = 'Browse...'
            $browse.Margin = [System.Windows.Thickness]::new(8, 0, 0, 0)

            $inputType = $Prompt.InputType
            $browse.add_Click({
                if ($inputType -eq 'FolderOpen')
                {
                    # FolderBrowserDialog works on both PowerShell editions; the net8-only
                    # Microsoft.Win32.OpenFolderDialog must not be used here.
                    Add-Type -AssemblyName System.Windows.Forms
                    $folderDialog = [System.Windows.Forms.FolderBrowserDialog]::new()
                    if (-not [string]::IsNullOrWhiteSpace($textBox.Text))
                    {
                        $folderDialog.SelectedPath = $textBox.Text
                    }
                    if ($folderDialog.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK)
                    {
                        $textBox.Text = $folderDialog.SelectedPath
                    }
                    $folderDialog.Dispose()
                    return
                }

                if ($inputType -eq 'FileSave')
                {
                    $fileDialog = [Microsoft.Win32.SaveFileDialog]::new()
                }
                else
                {
                    $fileDialog = [Microsoft.Win32.OpenFileDialog]::new()
                }
                if (-not [string]::IsNullOrWhiteSpace($textBox.Text))
                {
                    $fileDialog.FileName = $textBox.Text
                }
                if ($fileDialog.ShowDialog() -eq $true)
                {
                    $textBox.Text = $fileDialog.FileName
                }
            }.GetNewClosure())

            $null = $panel.Children.Add($textBox)
            $null = $panel.Children.Add($browse)
            return $panel
        }

        default
        {
            # Text (and any unmapped type) renders as a single-line TextBox.
            $ctl = [Fluence.Wpf.Controls.TextBox]::new()
            if ($null -ne $Prompt.DefaultValue)
            {
                $ctl.Text = [string]$Prompt.DefaultValue
            }
            $ctl.add_TextChanged({
                $State.Result[$name] = $ctl.Text
            }.GetNewClosure())
            return $ctl
        }
    }
}
