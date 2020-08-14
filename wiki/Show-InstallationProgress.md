# Show-InstallationProgress

## SYNOPSIS

Displays a progress dialog in a separate thread with an updateable custom message.

## SYNTAX

 `Show-InstallationProgress [[-StatusMessage] <String>] [[-WindowLocation] <String>] [[-TopMost] <Boolean>] [<CommonParameters>]`

## DESCRIPTION

Create a WPF window in a separate thread to display a marquee style progress ellipse with a custom message that can be updated.

The status message supports line breaks.

The first time this function is called in a script, it will display a balloon tip notification to indicate that the installation has started (provided balloon tips are enabled in the

configuration).

## PARAMETERS

`-StatusMessage <String>`

The status message to be displayed. The default status message is taken from the XML configuration file.

`-WindowLocation <String>`

The location of the progress window. Default: just below top, centered.

`-TopMost <Boolean>`

Specifies whether the progress window should be topmost. Default: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Show-InstallationProgress`

Uses the default status message from the XML configuration file.

-------------------------- EXAMPLE 2 --------------------------

`PS C:>Show-InstallationProgress -StatusMessage 'Installation in Progress...'`

-------------------------- EXAMPLE 3 --------------------------

`PS C:>Show-InstallationProgress -StatusMessage "Installation in Progress...\`nThe installation may take 20 minutes to complete."`

-------------------------- EXAMPLE 4 --------------------------

`PS C:>Show-InstallationProgress -StatusMessage 'Installation in Progress...' -WindowLocation 'BottomRight' -TopMost `$false``

## REMARKS

To see the examples, type: `Get-Help Show-InstallationProgress -Examples`

For more information, type: `Get-Help Show-InstallationProgress -Detailed`

For technical information, type: `Get-Help Show-InstallationProgress -Full`

For online help, type: `Get-Help Show-InstallationProgress -Online`
