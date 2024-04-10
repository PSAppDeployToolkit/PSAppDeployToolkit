Write-host "Testing has started..." -ForegroundColor Cyan
Start-Process -FilePath "C:\Users\WDAGUtilityAccount\Desktop\master\Deploy-Application.exe" -Wait
Write-host "Installation completed" -ForegroundColor DarkGreen
Write-host "you have 60 seconds to verify the installation before it is automatically uninstalled" -ForegroundColor Cyan

$Seconds = 60
$EndTime = [datetime]::UtcNow.AddSeconds($Seconds)

while (($TimeRemaining = ($EndTime - [datetime]::UtcNow)) -gt 0) {
  Write-Progress -Activity 'Waiting for...' -Status testing -SecondsRemaining $TimeRemaining.TotalSeconds
  Start-Sleep 1
}

Start-Process -FilePath "C:\Users\WDAGUtilityAccount\Desktop\master\Deploy-Application.exe" -ArgumentList "Uninstall" -Wait
Write-host "test completed" -ForegroundColor DarkGreen
Write-host "You can close sandbox now!" -ForegroundColor Cyan
Read-Host -Prompt "Press any key to continue..."