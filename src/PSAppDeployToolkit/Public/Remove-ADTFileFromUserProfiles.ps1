#-----------------------------------------------------------------------------
#
# MARK: Remove-ADTFileFromUserProfiles
#
#-----------------------------------------------------------------------------

function Remove-ADTFileFromUserProfiles
{
    <#
    .SYNOPSIS
        Removes one or more items from each user profile on the system.

    .DESCRIPTION
        This function removes one or more items from each user profile on the system. It can handle both wildcard paths and literal paths. If the specified path does not exist, it logs a warning instead of throwing an error. The function can also delete items recursively if the Recurse parameter is specified. Additionally, it allows excluding specific NT accounts, system profiles, service profiles, and the default user profile.

    .PARAMETER Path
        Specifies the path to append to the root of the user profile to be resolved. The value of Path will accept wildcards. Will accept an array of values.

    .PARAMETER LiteralPath
        Specifies the path to append to the root of the user profile to be resolved. The value of LiteralPath is used exactly as it is typed; no characters are interpreted as wildcards. Will accept an array of values.

    .PARAMETER Recurse
        Deletes the files in the specified location(s) and in all child items of the location(s).

    .PARAMETER ExcludeNTAccount
        Specify NT account names in Domain\Username format to exclude from the list of user profiles.

    .PARAMETER ExcludeDefaultUser
        Exclude the Default User.

    .PARAMETER IncludeSystemProfiles
        Include system profiles: SYSTEM, LOCAL SERVICE, NETWORK SERVICE.

    .PARAMETER IncludeServiceProfiles
        Include service profiles where NTAccount begins with NT SERVICE.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Remove-ADTFileFromUserProfiles -Path "AppData\Roaming\MyApp\config.txt"

        Removes the specified file from each user profile on the system.

    .EXAMPLE
        Remove-ADTFileFromUserProfiles -Path "AppData\Local\MyApp" -Recurse

        Removes the specified folder and all its contents recursively from each user profile on the system.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Remove-ADTFileFromUserProfiles
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'LiteralPath', Justification = "This parameter is accessed programmatically via the ParameterSet it's within, which PSScriptAnalyzer doesn't understand.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Path', Justification = "This parameter is accessed programmatically via the ParameterSet it's within, which PSScriptAnalyzer doesn't understand.")]
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
        if ($ExcludeNTAccount)
        {
            $GetUserProfileSplat.ExcludeNTAccount = $ExcludeNTAccount
        }

        # Store variable based on ParameterSetName.
        $pathVar = Get-Variable -Name $PSCmdlet.ParameterSetName
    }

    process
    {
        foreach ($UserProfilePath in (Get-ADTUserProfiles @GetUserProfileSplat).ProfilePath)
        {
            $RemoveFileSplat.Path = $pathVar.Value | & { process { [System.IO.Path]::Combine($UserProfilePath, $_) } }
            Write-ADTLogEntry -Message "Removing $($pathVar.Name) [$($pathVar.Value)] from $UserProfilePath`:"
            try
            {
                try
                {
                    Remove-ADTFile @RemoveFileSplat
                }
                catch
                {
                    Write-Error -ErrorRecord $_
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
