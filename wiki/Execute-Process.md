# Execute-Process

## SYNOPSIS

Execute a process with optional arguments, working directory, window style.

## SYNTAX

 `Execute-Process [-Path] <String> [[-Parameters] <String>] [-SecureParameters] [[-WindowStyle] {Normal | Hidden | Minimized | Maximized}] [-CreateNoWindow] [[-WorkingDirectory] <String>]`

[-NoWait] [-PassThru] [-WaitForMsiExec] [[-MsiExecWaitTime] <TimeSpan>] [[-IgnoreExitCodes] <String>] [[-ContinueOnError] <Boolean>] [<CommonParameters>]

## DESCRIPTION

Executes a process, e.g. a file included in the Files directory of the App Deploy Toolkit, or a file on the local machine.

Provides various options for handling the return codes (see Parameters).

## PARAMETERS

`-Path <String>`

Path to the file to be executed. If the file is located directly in the "Files" directory of the App Deploy Toolkit, only the file name needs to be specified.

Otherwise, the full path of the file must be specified. If the files is in a subdirectory of "Files", use the "$dirFiles" variable as shown in the example.

`-Parameters <String>`

Arguments to be passed to the executable

`-SecureParameters [<SwitchParameter>]`

Hides all parameters passed to the executable from the Toolkit log file

`-WindowStyle

Style of the window of the process executed. Options: Normal, Hidden, Maximized, Minimized. Default: Normal.

Note: Not all processes honor the "Hidden" flag. If it it not working, then check the command line options for the process being executed to see it has a silent option.

`-CreateNoWindow [<SwitchParameter>]`

Specifies whether the process should be started with a new window to contain it. Default is false.

`-WorkingDirectory <String>`

The working directory used for executing the process. Defaults to the directory of the file being executed.

`-NoWait [<SwitchParameter>]`

Immediately continue after executing the process.

`-PassThru[<SwitchParameter>]`

Returns ExitCode, STDOut, and STDErr output from the process.

`-WaitForMsiExec [<SwitchParameter>]`

Sometimes an EXE bootstrapper will launch an MSI install. In such cases, this variable will ensure that

this function waits for the msiexec engine to become available before starting the install.

`-MsiExecWaitTime <TimeSpan>`

Specify the length of time in seconds to wait for the msiexec engine to become available. Default: 600 seconds (10 minutes).

`-IgnoreExitCodes <String>`

List the exit codes to ignore.

`-ContinueOnError <Boolean>`

Continue if an exit code is returned by the process that is not recognized by the App Deploy Toolkit. Default: `$false`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Execute-Process -Path 'uninstall_flash_player_64bit.exe' -Parameters '/uninstall' -WindowStyle 'Hidden'`

If the file is in the "Files" directory of the App Deploy Toolkit, only the file name needs to be specified.

-------------------------- EXAMPLE 2 --------------------------

`PS C:>Execute-Process -Path "$dirFiles\Bin\setup.exe" -Parameters '/S' -WindowStyle 'Hidden'`

-------------------------- EXAMPLE 3 --------------------------

`PS C:>Execute-Process -Path 'setup.exe' -Parameters '/S' -IgnoreExitCodes '1,2'`

-------------------------- EXAMPLE 4 --------------------------

`PS C:>Execute-Process -Path 'setup.exe' -Parameters "-s -f2\`"$configToolkitLogDir\$installName.log\`""`

Launch InstallShield "setup.exe" from the ".\Files" sub-directory and force log files to the logging folder.

-------------------------- EXAMPLE 5 --------------------------

`PS C:>Execute-Process -Path 'setup.exe' -Parameters "/s /v\`"ALLUSERS=1 /qn /L\* \\`"$configToolkitLogDir\$installName.log\`"\`""`

Launch InstallShield "setup.exe" with embedded MSI and force log files to the logging folder.

## REMARKS

To see the examples, type: `Get-Help Execute-Process -Examples`

For more information, type: `Get-Help Execute-Process -Detailed`

For technical information, type: `Get-Help Execute-Process -Full`

For online help, type: `Get-Help Execute-Process -Online`
