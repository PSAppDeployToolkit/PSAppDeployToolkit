#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Remove-ADTFileFromUserProfiles
{
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

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not generate any output.

    .EXAMPLE
    Remove-ADTFileFromUserProfiles -Path "AppData\Roaming\MyApp\config.txt"

    .EXAMPLE
    Remove-ADTFileFromUserProfiles -Path "AppData\Local\MyApp" -Recurse

    .NOTES
    This function can be called without an active ADT session.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, Position = 0, ParameterSetName = 'Path')]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Path,

        [Parameter(Mandatory = $true, Position = 0, ParameterSetName = 'LiteralPath')]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$LiteralPath,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Recurse,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$ExcludeNTAccount,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ExcludeDefaultUser,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$IncludeSystemProfiles,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$IncludeServiceProfiles
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $RemoveFileSplat = @{
            Recurse = $Recurse
        }
        $GetUserProfileSplat = @{
            IncludeSystemProfiles = $IncludeSystemProfiles
            IncludeServiceProfiles = $IncludeServiceProfiles
            ExcludeDefaultUser = $ExcludeDefaultUser
        }
        if ($ExcludeNTAccount) {
            $GetUserProfileSplat.ExcludeNTAccount = $ExcludeNTAccount
        }
    }

    process
    {
        foreach ($UserProfilePath in (Get-ADTUserProfiles @GetUserProfileSplat).ProfilePath)
        {
            if ($PSCmdlet.ParameterSetName -eq 'Path')
            {
                $RemoveFileSplat.Path = $Path.ForEach({[System.IO.Path]::Combine($UserProfilePath, $_)})
                Write-ADTLogEntry -Message "Removing path [$Path] from $UserProfilePath`:"
            }
            elseif ($PSCmdlet.ParameterSetName -eq 'LiteralPath')
            {
                $RemoveFileSplat.LiteralPath = $LiteralPath.ForEach({[System.IO.Path]::Combine($UserProfilePath, $_)})
                Write-ADTLogEntry -Message "Removing literal path [$LiteralPath] from $UserProfilePath`:"
            }
            try
            {
                try
                {
                    Remove-ADTFile @RemoveFileSplat
                }
                catch
                {
                    & $Script:CommandTable.'Write-Error' -ErrorRecord $_
                }
            }
            catch
            {
                Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
            }
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
