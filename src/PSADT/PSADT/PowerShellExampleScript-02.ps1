[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingWriteHost', '', Justification = "This is just a demo script.")]
[CmdletBinding()]
param
()

# Import the PSADT assembly
Add-Type -Path "path\to\PSADT.dll"

# SYSTEM Process Script
function Start-SystemProcess
{
    # Ensure this script is running as SYSTEM
    $currentIdentity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    if ($currentIdentity.User -ne 'S-1-5-18')
    {
        throw "This script must be run as SYSTEM"
    }

    # Configure server options
    $serverOptions = [PSADT.SecureIPC.NamedPipeServerOptions]::new()
    $serverOptions.PipeName = "SystemToUserPipe"
    $serverOptions.MaxServerInstances = 1
    $serverOptions.RestrictToPowerShellOnly = $true
    $serverOptions.AllowedUserSid = $null  # Allow any user to connect

    # Create and start the server
    $server = [PSADT.SecureIPC.NamedPipeServer]::new($serverOptions)
    $server.Start()

    try
    {
        # Start the user process asynchronously
        $userProcessTask = Start-UserProcessAsync

        Write-Host "Waiting for client connection..."
        $server.WaitForConnectionAsync().Wait()
        Write-Host "Client connected"

        $stream = $server.GetStream()
        $writer = [System.IO.StreamWriter]::new($stream)
        $reader = [System.IO.StreamReader]::new($stream)

        # Send commands to the user process
        $commands = @(
            "Get-Process | Select-Object -First 5",
            "[System.Security.Principal.WindowsIdentity]::GetCurrent().Name",
            "Get-ChildItem Env: | Select-Object -First 5",
            "exit"
        )

        foreach ($command in $commands)
        {
            Write-Host "Sending command: $command"
            $writer.WriteLine($command)
            $writer.Flush()

            if ($command -ne "exit")
            {
                $response = $reader.ReadLine()
                Write-Host "Received response: $response"
            }
        }

        # Wait for the user process to complete
        $userProcessTask.Wait()
    }
    finally
    {
        $server.Disconnect()
        $server.Dispose()
    }
}

# Function to start the user process asynchronously
function Start-UserProcessAsync
{
    $userScript = @"
    Add-Type -Path "path\to\PSADT.dll"

    # Connect to the named pipe as a client
    `$pipeClient = [System.IO.Pipes.NamedPipeClientStream]::new(".", "SystemToUserPipe", [System.IO.Pipes.PipeDirection]::InOut, [System.IO.Pipes.PipeOptions]::None, [System.Security.Principal.TokenImpersonationLevel]::Impersonation)
    `$pipeClient.Connect()

    `$reader = [System.IO.StreamReader]::new(`$pipeClient)
    `$writer = [System.IO.StreamWriter]::new(`$pipeClient)

    while (`$true) {
        `$command = `$reader.ReadLine()
        if (`$command -eq "exit") {
            break
        }

        try {
            `$result = Invoke-Expression -Command `$command | Out-String
            `$writer.WriteLine(`$result.Trim())
        }
        catch {
            `$writer.WriteLine("Error: `$_")
        }
        `$writer.Flush()
    }

    `$pipeClient.Close()
"@

    # Create execution options for the user process
    $executionOptions = [PSADT.PowerShellHost.PSExecutionOptions]::new()
    $executionOptions.ScriptText = $userScript
    $executionOptions.ExecutionPolicy = [System.Management.Automation.ExecutionPolicy]::Bypass
    $executionOptions.PowerShellVersion = [PSADT.PowerShellHost.PSEdition]::Default

    # Create an impersonator for a standard user (modify as needed)
    $impersonationOptions = [PSADT.Impersonation.ImpersonateOptions]::new()
    $impersonationOptions.ReduceAdminPrivileges = $true
    $impersonator = [PSADT.Impersonation.Impersonator]::new($impersonationOptions)

    # Use WindowsIdentity.Impersonate to get a token for a standard user
    # You may need to modify this part to get the appropriate user token
    $userToken = [System.Security.Principal.WindowsIdentity]::GetCurrent().Token
    [System.Security.Principal.WindowsIdentity]::RunImpersonated($userToken, {
            $impersonator.ImpersonateNamedPipeClient([Microsoft.Win32.SafeHandles.SafePipeHandle]::new(0, $true))
        })

    # Create an ExecutionContext object
    $execContext = [PSADT.PowerShellHost.ExecutionContext]::new($executionOptions, $impersonator)

    # Start the user process asynchronously
    return [PSADT.PowerShellHost.PSADTShell]::ExecuteAsync($execContext)
}

# Run the SYSTEM process
Start-SystemProcess
