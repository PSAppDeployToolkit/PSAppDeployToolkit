# Get-UserProfiles

## SYNOPSIS

Get the User Profile Path, User Account Sid, and the User Account Name for all users that log onto the machine and also the Default User (which does not log on).

## SYNTAX

 `Get-UserProfiles [[-ExcludeNTAccount] <String>] [[-ExcludeSystemProfiles] <Boolean>] [-ExcludeDefaultUser] [<CommonParameters>]`

## DESCRIPTION

Get the User Profile Path, User Account Sid, and the User Account Name for all users that log onto the machine and also the Default User (which does not log on).

Please note that the NTAccount property may be empty for some user profiles but the SID and ProfilePath properties will always be populated.

## PARAMETERS

`-ExcludeNTAccount <String>`

Specify NT account names in Domain\Username format to exclude from the list of user profiles.

`-ExcludeSystemProfiles <Boolean>`

Exclude system profiles: SYSTEM, LOCAL SERVICE, NETWORK SERVICE. Default is: `$true`.

`-ExcludeDefaultUser [<SwitchParameter>]`

Exclude the Default User. Default is: `$false`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Get-UserProfiles`

Returns the following properties for each user profile on the system: NTAccount, SID, ProfilePath

-------------------------- EXAMPLE 2 --------------------------

`PS C:>Get-UserProfiles -ExcludeNTAccount 'CONTOSO\Robot','CONTOSO\ntadmin'`

-------------------------- EXAMPLE 3 --------------------------

`PS C:>[string]$ProfilePaths = Get-UserProfiles | Select-Object -ExpandProperty 'ProfilePath'`

Returns the user profile path for each user on the system. This information can then be used to make modifications under the user profile on the filesystem.

## REMARKS

To see the examples, type: `Get-Help Get-UserProfiles -Examples`

For more information, type: `Get-Help Get-UserProfiles -Detailed`

For technical information, type: `Get-Help Get-UserProfiles -Full`

For online help, type: `Get-Help Get-UserProfiles -Online`
