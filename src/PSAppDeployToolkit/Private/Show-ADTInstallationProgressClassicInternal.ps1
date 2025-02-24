#-----------------------------------------------------------------------------
#
# MARK: Show-ADTInstallationProgressClassicInternal
#
#-----------------------------------------------------------------------------

function Show-ADTInstallationProgressClassicInternal
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'DisableWindowCloseButton', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'UpdateWindowLocation', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'WindowLocation', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Xml.XmlDocument]$Xaml,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.IO.FileInfo]$Icon,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.IO.FileInfo]$Banner,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Default', 'TopLeft', 'Top', 'TopRight', 'TopCenter', 'BottomLeft', 'Bottom', 'BottomRight')]
        [System.String]$WindowLocation,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ScriptBlock]$UpdateWindowLocation,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ScriptBlock]$DisableWindowCloseButton
    )

    # Set required variables to ensure script functionality.
    $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
    $ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
    Set-StrictMode -Version 3

    # Create XAML window and bring it up.
    try
    {
        $SyncHash.Add('Window', [System.Windows.Markup.XamlReader]::Load([System.Xml.XmlNodeReader]::new($Xaml)))
        $SyncHash.Add('Message', $SyncHash.Window.FindName('ProgressText'))
        $SyncHash.Window.Icon = [System.Windows.Media.Imaging.BitmapFrame]::Create([System.IO.MemoryStream]::new([System.IO.File]::ReadAllBytes($Icon)), [System.Windows.Media.Imaging.BitmapCreateOptions]::IgnoreImageCache, [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
        $SyncHash.Window.FindName('ProgressBanner').Source = [System.Windows.Media.Imaging.BitmapFrame]::Create([System.IO.MemoryStream]::new([System.IO.File]::ReadAllBytes($Banner)), [System.Windows.Media.Imaging.BitmapCreateOptions]::IgnoreImageCache, [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
        $SyncHash.Window.add_MouseLeftButtonDown({ $this.DragMove() })
        $SyncHash.Window.add_Loaded({
                # Relocate the window and disable the X button.
                & $UpdateWindowLocation -Window $this -Location $WindowLocation
                & $DisableWindowCloseButton -WindowHandle ([System.Windows.Interop.WindowInteropHelper]::new($this).Handle)
            })
        $null = $SyncHash.Window.ShowDialog()
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}
