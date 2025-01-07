[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingWriteHost', '', Justification = "This is just a demo script.")]
[CmdletBinding()]
param
()

# Import the SecureIPC and PSHost assemblies
Add-Type -Path "path\to\PSADT.dll"

# Configure server options
$serverOptions = [PSADT.SecureIPC.NamedPipeServerOptions]::new()
$serverOptions.PipeName = "MySecurePipe"
$serverOptions.MaxServerInstances = 1
$serverOptions.RestrictToPowerShellOnly = $true
$serverOptions.AllowedUserSid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User

# Configure impersonation options
$impersonationOptions = [PSADT.Impersonation.ImpersonateOptions]::new()
$impersonationOptions.ReduceAdminPrivileges = $true
$impersonationOptions.AllowSystemImpersonation = $false
$impersonationOptions.AllowNonAdminToAdminImpersonation = $false
$impersonationOptions.DoNotCheckAppLockerRulesOrApplySRP = $false
$serverOptions.ImpersonationOptions = $impersonationOptions

# Create and start the server
$server = [PSADT.SecureIPC.NamedPipeServer]::new($serverOptions)
$server.Start()

try
{
    Write-Host "Waiting for client connection..."
    $server.WaitForConnectionAsync().Wait()
    Write-Host "Client connected"

    # Create an Impersonator object
    $impersonator = $server.ImpersonateClient()

    # Define a PowerShell script to execute
    $scriptText = @"
    `$identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    Write-Output "Current user: `$(`$identity.Name)"
    Get-Process | Select-Object -First 5
    Start-Sleep -Seconds 2  # Simulate some work
    Get-Date
"@

    # Create ExecutionOptions
    $executionOptions = [PSADT.PowerShellHost.PSExecutionOptions]::new()
    $executionOptions.ScriptText = $scriptText
    $executionOptions.ExecutionPolicy = [System.Management.Automation.ExecutionPolicy]::Bypass
    $executionOptions.ExecutionPolicyScope = [System.Management.Automation.ExecutionPolicyScope]::Process
    $executionOptions.ForceExecutionPolicy = $true
    $executionOptions.ExecutionPolicyErrorAction = [System.Management.Automation.ActionPreference]::SilentlyContinue
    $executionOptions.PowerShellVersion = [PSADT.PowerShellHost.PSEdition]::Default

    # Create an ExecutionContext object
    $execContext = [PSADT.PowerShellHost.ExecutionContext]::new($executionOptions, $impersonator)

    # Execute the script synchronously
    Write-Host "Executing script synchronously:"
    $result = [PSADT.PowerShellHost.PSADTShell]::Execute($execContext)
    $result | ForEach-Object { $_.BaseObject | Format-Table -AutoSize | Out-String -Width 4096 }

    # Execute the script asynchronously
    Write-Host "Executing script asynchronously:"
    $task = [PSADT.PowerShellHost.PSADTShell]::ExecuteAsync($execContext)

    # Do some other work while the script is running
    Write-Host "Doing some other work while the script is running..."
    1..5 | ForEach-Object {
        Write-Host "Working... $($_)"
        Start-Sleep -Seconds 1
    }

    # Wait for the async task to complete and get the result
    $result = $task.GetAwaiter().GetResult()
    $result | ForEach-Object { $_.BaseObject | Format-Table -AutoSize | Out-String -Width 4096 }

    # Demonstrate PowerShell's native asynchronous capabilities
    Write-Host "Demonstrating PowerShell's native asynchronous capabilities:"
    $asyncResult = $null
    $task = [PSADT.PowerShellHost.PSADTShell]::ExecuteAsync($execContext)

    # Register a continuation action
    $task.ContinueWith({
            param($task)
            $script:asyncResult = $task.Result
        }, [System.Threading.Tasks.TaskContinuationOptions]::OnlyOnRanToCompletion)

    # Do other work while waiting for the result
    Write-Host "Doing other work while waiting for the async result..."
    1..10 | ForEach-Object {
        Write-Host "Still working... $($_)"
        Start-Sleep -Milliseconds 500
    }

    # Wait for and process the async result
    while ($null -eq $asyncResult)
    {
        Start-Sleep -Milliseconds 100
    }

    Write-Host "Async execution completed. Results:"
    $asyncResult | ForEach-Object { $_.BaseObject | Format-Table -AutoSize | Out-String -Width 4096 }

    # Perform pipe operations
    $stream = $server.GetStream()
    $writer = [System.IO.StreamWriter]::new($stream)
    $writer.WriteLine("Hello from the server!")
    $writer.Flush()

    $reader = [System.IO.StreamReader]::new($stream)
    $message = $reader.ReadLine()
    Write-Host "Received message: $message"
}
finally
{
    # Disconnect and dispose of the server
    $server.Disconnect()
    $server.Dispose()
}

<#
Explanation of the asynchronous execution:

1. We use [PSADT.PowerShellHost.PSADTShell]::ExecuteAsync to start the asynchronous execution.
   This method returns a Task<PSDataCollection<PSObject>>.

2. We use the ContinueWith method to register a continuation action. This action will be executed
   when the task completes successfully. It stores the result in a script-scoped variable ($script:asyncResult)
   so we can access it later.

3. While the async task is running, we perform other work. This demonstrates how asynchronous
   execution allows us to utilize our time effectively instead of blocking and waiting.

4. After our "other work" is done, we wait for the async result using a simple polling loop.
   In a real-world scenario, you might want to use a more sophisticated waiting mechanism or
   continue with other tasks.

5. Once the result is available, we process and display it.

This approach allows for truly asynchronous operation in PowerShell, letting you continue executing
other code while waiting for the ExecuteAsync task to complete. It's particularly useful for long-running
operations or when you need to maintain responsiveness in your script.
#>