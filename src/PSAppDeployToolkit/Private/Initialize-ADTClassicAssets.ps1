#-----------------------------------------------------------------------------
#
# MARK: Initialize-ADTClassicAssets
#
#-----------------------------------------------------------------------------

function Initialize-ADTClassicAssets
{
    [CmdletBinding()]
    param
    (
    )

    # Return early if already initialised.
    if ($Script:Dialogs.Classic.BannerHeight)
    {
        return
    }

    # Process the classic assets by grabbing the bytes of each image asset, storing them into a memory stream, then as an image for WinForms to use.
    try
    {
        $adtConfig = Get-ADTConfig
        $Script:Dialogs.Classic.Assets.Logo = [System.Drawing.Image]::FromStream([System.IO.MemoryStream]::new([System.IO.File]::ReadAllBytes($adtConfig.Assets.Logo)))
        $Script:Dialogs.Classic.Assets.Icon = [PSADT.Shared.Utility]::ConvertImageToIcon($Script:Dialogs.Classic.Assets.Logo)
        $Script:Dialogs.Classic.Assets.Banner = [System.Drawing.Image]::FromStream([System.IO.MemoryStream]::new([System.IO.File]::ReadAllBytes($adtConfig.Assets.Banner)))
        $Script:Dialogs.Classic.BannerHeight = [System.Math]::Ceiling($Script:Dialogs.Classic.Width * ($Script:Dialogs.Classic.Assets.Banner.Height / $Script:Dialogs.Classic.Assets.Banner.Width))
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}
