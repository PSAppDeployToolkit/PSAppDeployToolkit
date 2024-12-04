# Load the C# DLL
Add-Type -Path "C:\path\to\your\CSharpLibrary.dll"

# Define the log options
$logOptions = [PSADT.Logging.LogOptions]::CreateBuilder()
$logOptions = $logOptions.SetLogDirectory("C:\logs")
                         .SetLogFileNamePrefix("PowerShellLog_")
                         .SetLogFileNameTimestamp([DateTime]::Now)
                         .SetLogFileNameTimestampFormat("yyyy-MM-dd-HH-mm")
                         .SetLogFileExtension("log")
                         .SetLogFormat([PSADT.Logging.TextLogFormat]::Default)
                         .Build()

# Create a FileLogDestination
$fileLogDestination = New-Object PSADT.Logging.FileLogDestination($logOptions)

# Add the FileLogDestination to the EnhancedLog
[PSADT.Logging.EnhancedLog]::AddLogDestination($fileLogDestination)

# Log a message from PowerShell
[PSADT.Logging.EnhancedLog]::LogInformation("This is a log message from PowerShell.")

# Call a C# method that may log errors or throw exceptions
try {
    [YourNamespace.YourClass]::SomeMethod()
}
catch {
    Write-Error "Caught an exception from the C# method: $_"
}
