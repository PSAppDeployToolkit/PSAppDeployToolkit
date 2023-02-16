Set-Location -Path "$PSScriptRoot"
$Application = (Get-ChildItem -Directory | Select-Object -First 1).Name

if (Get-Content -Path "$Application\Deploy-Application.ps1" | Select-String "winget install")
{
    Write-Host "Requires winget"
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $WebClient = New-Object System.Net.WebClient

    # Winget
    [string]$Temp = [IO.Path]::GetTempPath()
    [string]$WingetUrl = 'https://api.github.com/repos/microsoft/winget-cli/releases/latest'
    [string]$AppInstaller = "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe"
    [string]$AppInstallerUrl = ((Invoke-WebRequest $WingetUrl -UseBasicParsing -Verbose | ConvertFrom-Json).assets |
            Where-Object { $_.name -match "^$AppInstaller.msixbundle$" }
    ).browser_download_url

    [string]$AppInstallerShaUrl = ((Invoke-WebRequest $WingetUrl -UseBasicParsing -Verbose | ConvertFrom-Json).assets | 
            Where-Object { $_.name -match "^$AppInstaller.txt$" }).browser_download_url

    [string]$AppInstallerHash = $WebClient.DownloadString($AppInstallerShaUrl)

    $Msix = @{
        fileName = "$AppInstaller.msixbundle"
        url      = "$AppInstallerUrl"
        hash     = "$AppInstallerHash"
    }

    $vcLibsUwp = @{
        fileName = 'Microsoft.VCLibs.x64.14.00.Desktop.appx'
        url      = 'https://aka.ms/Microsoft.VCLibs.x64.14.00.Desktop.appx'
        hash     = 'A39CEC0E70BE9E3E48801B871C034872F1D7E5E8EEBE986198C019CF2C271040'
    }

    $uiLibsUwp = @{
        fileName = 'Microsoft.UI.Xaml.2.7.zip'
        url      = 'https://www.nuget.org/api/v2/package/Microsoft.UI.Xaml/2.7.0/'
        hash     = "422FD24B231E87A842C4DAEABC6A335112E0D35B86FAC91F5CE7CF327E36A591"
    }

    # Download
    $Content = @($Msix, $vcLibsUwp, $uiLibsUwp)

    foreach ($Resource in $Content)
    {
        $Resource.file = Join-Path -Path $Temp -ChildPath $Resource.fileName
        $Resource.pathInSandbox = Join-Path -Path $Temp -ChildPath $Resource.fileName

        try
        {
            $WebClient.DownloadFile($Resource.url, $Resource.file)
        }
        catch
        {
            #Pass the exception as an inner exception
            throw [System.Net.WebException]::new("Download error $($Resource.url).", $_.Exception)
        }
        if (-not ($Resource.hash -eq $(Get-FileHash $Resource.file).Hash))
        {
            throw [System.Activities.VersionMismatchException]::new('Hash mismatch')
        }
    }

    # Extract zip, workaround until https://github.com/microsoft/winget-cli/issues/1861 is resolved.
    Expand-Archive -Path $uiLibsUwp.file -DestinationPath ($Temp + "\Microsoft.UI.Xaml.2.7") -Verbose -Force
    $uiLibsUwp.pathInSandbox = (Join-Path -Path $Temp -ChildPath \Microsoft.UI.Xaml.2.7\tools\AppX\x64\Release\Microsoft.UI.Xaml.2.7.appx)

    #Install
    Add-AppxPackage -Path "$($Msix.pathInSandbox)" -DependencyPath "$($vcLibsUwp.pathInSandbox)", "$($uiLibsUwp.pathInSandbox)" -Verbose

}

# Begin
Start-Process -FilePath .\$Application\Deploy-Application.exe -WindowStyle Maximized -Wait