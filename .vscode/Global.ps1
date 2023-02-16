[string]$Desktop = [Environment]::GetFolderPath('DesktopDirectory')
[string]$WDADesktop = "C:\Users\WDAGUtilityAccount\Desktop"
[string]$Win32App = "$env:ProgramData\win32app"
[string]$Application = "$(& git branch --show-current)"
[string]$Cache = "$env:ProgramData\win32app\$Application"
[string]$LogonCommand = "LogonCommand.ps1"

# Cache resources
Remove-Item -Path "$Win32App" -Recurse -Force -ErrorAction Ignore
Copy-Item -Path "Toolkit" -Destination "$Cache" -Recurse -Force -Verbose -ErrorAction Ignore
explorer "$Cache"