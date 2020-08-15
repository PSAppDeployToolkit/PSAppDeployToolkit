# Send-Keys

## SYNOPSIS

Send a sequence of keys to one or more application windows.

## SYNTAX

 `Send-Keys [[-WindowTitle] <String>] [[-GetAllWindowTitles]] [[-WindowHandle] <IntPtr>] [[-Keys] <String>] [[-WaitSeconds] <Int32>] [<CommonParameters>]`

## DESCRIPTION

Send a sequence of keys to one or more application window. If window title searched for returns more than one window, then all of them will receive the sent keys.

Function does not work in SYSTEM context unless launched with "psexec.exe -s -i" to run it as an interactive process under the SYSTEM account.

## PARAMETERS

`-WindowTitle <String>`

The title of the application window to search for using regex matching.

`-GetAllWindowTitles [<SwitchParameter>]`

Get titles for all open windows on the system.

`-WindowHandle <IntPtr>`

Send keys to a specific window where the Window Handle is already known.

`-Keys <String>`

The sequence of keys to send. Info on Key input at: http://msdn.microsoft.com/en-us/library/System.Windows.Forms.SendKeys(v=vs.100).aspx

`-WaitSeconds <Int32>`

An optional number of seconds to wait after the sending of the keys.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Send-Keys -WindowTitle 'foobar - Notepad' -Key 'Hello world'`

Send the sequence of keys "Hello world" to the application titled "foobar - Notepad".

-------------------------- EXAMPLE 2 --------------------------

`PS C:>Send-Keys -WindowTitle 'foobar - Notepad' -Key 'Hello world' -WaitSeconds 5`

Send the sequence of keys "Hello world" to the application titled "foobar - Notepad" and wait 5 seconds.

-------------------------- EXAMPLE 3 --------------------------

`PS C:>Send-Keys -WindowHandle ([IntPtr]17368294) -Key 'Hello world'`

Send the sequence of keys "Hello world" to the application with a Window Handle of '17368294'.

## REMARKS

To see the examples, type: `Get-Help Send-Keys -Examples`

For more information, type: `Get-Help Send-Keys -Detailed`

For technical information, type: `Get-Help Send-Keys -Full`

For online help, type: `Get-Help Send-Keys -Online`
