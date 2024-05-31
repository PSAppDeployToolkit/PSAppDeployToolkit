Function Remove-FileFromUserProfiles {
    <#
.SYNOPSIS

Removes one or more items from each user profile on the system.

.DESCRIPTION

Removes one or more items from each user profile on the system.

.PARAMETER Path

Specifies the path to append to the root of the user profile to be resolved. The value of Path will accept wildcards. Will accept an array of values.

.PARAMETER LiteralPath

Specifies the path to append to the root of the user profile to be resolved. The value of LiteralPath is used exactly as it is typed; no characters are interpreted as wildcards. Will accept an array of values.

.PARAMETER Recurse

Deletes the files in the specified location(s) and in all child items of the location(s).

.PARAMETER ExcludeNTAccount

Specify NT account names in Domain\Username format to exclude from the list of user profiles.

.PARAMETER ExcludeSystemProfiles

Exclude system profiles: SYSTEM, LOCAL SERVICE, NETWORK SERVICE. Default is: $true.

.PARAMETER ExcludeServiceProfiles

Exclude service profiles where NTAccount begins with NT SERVICE. Default is: $true.

.PARAMETER ExcludeDefaultUser

Exclude the Default User. Default is: $false.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Remove-FileFromUserProfiles -Path "AppData\Roaming\MyApp\config.txt"

.EXAMPLE

Remove-FileFromUserProfiles -Path "AppData\Local\MyApp" -Recurse

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, Position = 0, ParameterSetName = 'Path')]
        [ValidateNotNullorEmpty()]
        [String[]]$Path,
        [Parameter(Mandatory = $true, Position = 0, ParameterSetName = 'LiteralPath')]
        [ValidateNotNullorEmpty()]
        [String[]]$LiteralPath,
        [Parameter(Mandatory = $false)]
        [Switch]$Recurse = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String[]]$ExcludeNTAccount,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ExcludeSystemProfiles = $true,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ExcludeServiceProfiles = $true,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Switch]$ExcludeDefaultUser = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        Write-ADTDebugHeader
    }
    Process {
        [Hashtable]$RemoveFileSplat = @{
            Recurse = $Recurse
            ContinueOnError = $ContinueOnError
        }

        [Hashtable]$GetUserProfileSplat = @{
            ExcludeSystemProfiles = $ExcludeSystemProfiles
            ExcludeServiceProfiles = $ExcludeServiceProfiles
            ExcludeDefaultUser = $ExcludeDefaultUser
        }
        if ($ExcludeNTAccount) {
            $GetUserProfileSplat.ExcludeNTAccount = $ExcludeNTAccount
        }

        ForEach ($UserProfilePath in (Get-ADTUserProfiles @GetUserProfileSplat).ProfilePath) {
            If ($PSCmdlet.ParameterSetName -eq 'Path') {
                $RemoveFileSplat.Path = $Path | ForEach-Object { Join-Path $UserProfilePath $_ }
                Write-ADTLogEntry -Message "Removing path [$Path] from $UserProfilePath`:"
            }
            ElseIf ($PSCmdlet.ParameterSetName -eq 'LiteralPath') {
                $RemoveFileSplat.LiteralPath = $LiteralPath | ForEach-Object { Join-Path $UserProfilePath $_ }
                Write-ADTLogEntry -Message "Removing literal path [$LiteralPath] from $UserProfilePath`:"
            }
            Remove-ADTFile @RemoveFileSplat
        }
    }
    End {
        Write-ADTDebugFooter
    }
}
