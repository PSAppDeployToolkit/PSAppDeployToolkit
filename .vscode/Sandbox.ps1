# Vars
. ".vscode\Global.ps1"

# Copy Resources
Copy-Item -Path ".vscode\$LogonCommand" -Destination "$Win32App\" -Recurse -Force -Verbose -ErrorAction Ignore

# Prepare Sandbox
@"
<Configuration>
<Networking>Enabled</Networking>
<MappedFolders>
    <MappedFolder>
    <HostFolder>$Win32App</HostFolder>
    <SandboxFolder>$WDADesktop</SandboxFolder>
    <ReadOnly>true</ReadOnly>
    </MappedFolder>
</MappedFolders>
<LogonCommand>
    <Command>powershell -executionpolicy unrestricted -command "Start-Process powershell -ArgumentList ""-nologo -file $WDADesktop\$LogonCommand"""</Command>
</LogonCommand>
</Configuration>
"@ | Out-File "$Win32App\$Application.wsb"

# Execute Sandbox
Start-Process explorer -ArgumentList "$Win32App\$Application.wsb" -Verbose -WindowStyle Maximized