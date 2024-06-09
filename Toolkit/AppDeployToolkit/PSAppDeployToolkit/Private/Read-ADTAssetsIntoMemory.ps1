function Read-ADTAssetsIntoMemory
{
    # Get the current config.
    $adtConfig = Get-ADTConfig

    # Grab the bytes of each image asset, store them into a memory stream, then as an image for the form to use.
    $Script:FormData.Assets.Icon = [System.Drawing.Icon]::new([System.IO.MemoryStream]::new([System.IO.File]::ReadAllBytes($adtConfig.Assets.Icon)))
    $Script:FormData.Assets.Logo = [System.Drawing.Image]::FromStream([System.IO.MemoryStream]::new([System.IO.File]::ReadAllBytes($adtConfig.Assets.Logo)))
    $Script:FormData.Assets.Banner = [System.Drawing.Image]::FromStream([System.IO.MemoryStream]::new([System.IO.File]::ReadAllBytes($adtConfig.Assets.Banner)))
    $Script:FormData.BannerHeight = [System.Math]::Ceiling($Script:FormData.Width * ($Script:FormData.Assets.Banner.Height / $Script:FormData.Assets.Banner.Width))
}
