# Show-BalloonTip

## SYNOPSIS

Displays a balloon tip notification in the system tray.

## SYNTAX

 `Show-BalloonTip [-BalloonTipText] <String> [[-BalloonTipTitle] <String>] [[-BalloonTipIcon] {None | Info | Warning | Error}] [[-BalloonTipTime] <Int32>] [<CommonParameters>]`

## DESCRIPTION

Displays a balloon tip notification in the system tray.

## PARAMETERS

`-BalloonTipText <String>`

Text of the balloon tip.

`-BalloonTipTitle <String>`

Title of the balloon tip.

`-BalloonTipIcon`

Icon to be used. Options: 'Error', 'Info', 'None', 'Warning'. Default is: Info.

`-BalloonTipTime <Int32>`

Time in milliseconds to display the balloon tip. Default: 500.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Show-BalloonTip -BalloonTipText 'Installation Started' -BalloonTipTitle 'Application Name'`

-------------------------- EXAMPLE 2 --------------------------

`PS C:>Show-BalloonTip -BalloonTipIcon 'Info' -BalloonTipText 'Installation Started' -BalloonTipTitle 'Application Name' -BalloonTipTime 1000`

## REMARKS

To see the examples, type: `Get-Help Show-BalloonTip -Examples`

For more information, type: `Get-Help Show-BalloonTip -Detailed`

For technical information, type: `Get-Help Show-BalloonTip -Full`

For online help, type: `Get-Help Show-BalloonTip -Online`
