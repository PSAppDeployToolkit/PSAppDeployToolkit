# PsadtExec

PsadtExec is a versatile command-line utility for launching and managing processes across different user sessions in Windows environments. It provides a robust solution for scenarios where processes need to be executed in specific user contexts, such as in terminal server or multi-user environments.

## Features

- Launch processes in specific user sessions or all active sessions
- Redirect and capture process output
- Support for PowerShell script execution with configurable execution policies
- Flexible command-line argument parsing
- Comprehensive process lifetime management
- Utilizes Windows API for secure session management and process creation

## Requirements

- .NET Standard 2.0 compatible runtime
- Windows operating system (tested on Windows 10 and Windows Server 2016+)
- Appropriate permissions to interact with user sessions and create processes

## Building the Project

1. Clone this repository
2. Open the solution in Visual Studio 2019 or later
3. Build the solution (Ctrl+Shift+B)

## Usage

```
PsadtExec.exe [options]
```

### Options

- `-f, --FilePath`: Path to the executable or script to run
- `-l, --ArgumentList`: Arguments to pass to the executable or script
- `-d, --WorkingDirectory`: Working directory for the process
- `-h, --HideWindow`: Hide the process window
- `-e, --InheritEnvironmentVariables`: Inherit environment variables from the parent process
- `-w, --Wait`: Wait for the process to exit before continuing
- `-i, --SessionId`: Specific session ID to run the process in
- `-s, --AllActiveUserSessions`: Run the process in all active user sessions
- `-a, --UseLinkedAdminToken`: Use the linked admin token when available
- `-x, --PsExecutionPolicy`: Set PowerShell execution policy (default: RemoteSigned)
- `-p, --BypassPsExecutionPolicy`: Bypass PowerShell execution policy
- `-o, --SuccessExitCodes`: List of exit codes considered successful (default: 0,3010)
- `-c, --ConsoleTimeoutInSeconds`: Timeout for console operations (default: 30 seconds)
- `-v, --Verbose`: Enable verbose output
- `-b, --Debug`: Run in debug mode
- `--RedirectOutput`: Redirect process output (default: true)
- `--MergeStdErrAndStdOut`: Merge stderr into stdout
- `--OutputDirectory`: Directory to save redirected output
- `--TerminateOnTimeout`: Terminate process on timeout (default: true)

### Examples

1. Run notepad in session 1:
   ```
   PsadtExec.exe -f notepad.exe -i 1
   ```

2. Run a PowerShell script in all active sessions:
   ```
   PsadtExec.exe -f powershell.exe -l "-File C:\Scripts\MyScript.ps1" -s
   ```

3. Run a command with arguments and wait for completion:
   ```
   PsadtExec.exe -f cmd.exe -l "/c echo Hello World > C:\output.txt" -w
   ```

## Architecture

The solution is composed of several key components:

1. `PsadtExec.Main.Program.cs`: Main entry point and argument parsing
2. `PsadtExec.Main.CommandLineParser`: Custom command-line argument parser
3. `PsadtExec.Process.ProcessLauncher`: Core logic for launching and monitoring processes
4. `PsadtExec.Process.ProcessExecutionManager.cs`: Manages multiple processes across sessions
5. `PsadtExec.PInvoke.NativeMethods`: Windows API declarations and structures
6. `PsadtExec.WtsSession.SessionUtilis.cs`: Utilities for working with user sessions

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Disclaimer

This tool interacts with Windows sessions and can launch processes with different user privileges. Use with caution and ensure you have the necessary permissions and understanding of the security implications.
