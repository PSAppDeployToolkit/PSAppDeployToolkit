#-----------------------------------------------------------------------------
#
# MARK: Copy-ADTFileToUserProfiles
#
#-----------------------------------------------------------------------------

function Copy-ADTFileToUserProfiles
{
    <#
    .SYNOPSIS
        Copy one or more items to each user profile on the system.

    .DESCRIPTION
        The Copy-ADTFileToUserProfiles function copies one or more items to each user profile on the system. It supports various options such as recursion, flattening files, and using Robocopy to overcome the 260 character limit.

    .PARAMETER Path
        The path of the file or folder to copy.

    .PARAMETER Destination
        The path of the destination folder to append to the root of the user profile.

    .PARAMETER BasePath
        The base path to append the destination folder to.

    .PARAMETER Recurse
        Copy files in subdirectories.

    .PARAMETER Flatten
        Flattens the files into the root destination directory.

    .PARAMETER ContinueFileCopyOnError
        Continue copying files if an error is encountered. This will continue the deployment script and will warn about files that failed to be copied.

    .PARAMETER FileCopyMode
        Select from 'Native' or 'Robocopy'. Default is configured in config.psd1. Note that Robocopy supports * in file names, but not folders, in source paths.

    .PARAMETER RobocopyParams
        Override the default Robocopy parameters.

    .PARAMETER RobocopyAdditionalParams
        Append to the default Robocopy parameters.

    .PARAMETER ExcludeNTAccount
        Specify NT account names in Domain\Username format to exclude from the list of user profiles.

    .PARAMETER IncludeSystemProfiles
        Include system profiles: SYSTEM, LOCAL SERVICE, NETWORK SERVICE.

    .PARAMETER IncludeServiceProfiles
        Include service profiles where NTAccount begins with NT SERVICE.

    .PARAMETER ExcludeDefaultUser
        Exclude the Default User.

    .INPUTS
        System.String[]

        You can pipe in string values for $Path.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Copy-ADTFileToUserProfiles -Path "$($adtSession.DirSupportFiles)\config.txt" -Destination "AppData\Roaming\MyApp"

        Copy a single file to C:\Users\<UserName>\AppData\Roaming\MyApp for each user.

    .EXAMPLE
        Copy-ADTFileToUserProfiles -Path "$($adtSession.DirSupportFiles)\config.txt","$($adtSession.DirSupportFiles)\config2.txt" -Destination "AppData\Roaming\MyApp"

        Copy two files to C:\Users\<UserName>\AppData\Roaming\MyApp for each user.

    .EXAMPLE
        Copy-ADTFileToUserProfiles -Path "$($adtSession.DirFiles)\MyDocs" Destination "MyApp" -BasePath "Documents" -Recurse

        Copy an entire folder recursively to a new MyApp folder under each user's Documents folder.

    .EXAMPLE
        Copy-ADTFileToUserProfiles -Path "$($adtSession.DirFiles)\.appConfigFolder" -Recurse

        Copy an entire folder to C:\Users\<UserName> for each user.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Copy-ADTFileToUserProfiles
    #>

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true, Position = 1, ValueFromPipeline = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Path,

        [Parameter(Mandatory = $false, Position = 2)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Destination,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Profile', 'AppData', 'LocalAppData', 'Desktop', 'Documents', 'StartMenu', 'Temp', 'OneDrive', 'OneDriveCommercial')]
        [System.String]$BasePath = 'Profile',

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Recurse,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Flatten,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Native', 'Robocopy')]
        [System.String]$FileCopyMode,

        [Parameter(Mandatory = $false)]
        [System.String]$RobocopyParams,

        [Parameter(Mandatory = $false)]
        [System.String]$RobocopyAdditionalParams,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$ExcludeNTAccount,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SwitchParameter]$IncludeSystemProfiles,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SwitchParameter]$IncludeServiceProfiles,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SwitchParameter]$ExcludeDefaultUser,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SwitchParameter]$ContinueFileCopyOnError
    )

    begin
    {
        # Initalize function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        # Define default params for Copy-ADTFile.
        $CopyFileSplat = @{
            Recurse = $Recurse
            Flatten = $Flatten
            ContinueFileCopyOnError = $ContinueFileCopyOnError
        }
        if ($PSBoundParameters.ContainsKey('FileCopyMode'))
        {
            $CopyFileSplat.FileCopyMode = $PSBoundParameters.FileCopyMode
        }
        if ($PSBoundParameters.ContainsKey('RobocopyParams'))
        {
            $CopyFileSplat.RobocopyParams = $PSBoundParameters.RobocopyParams
        }
        if ($PSBoundParameters.ContainsKey('RobocopyAdditionalParams'))
        {
            $CopyFileSplat.RobocopyAdditionalParams = $PSBoundParameters.RobocopyAdditionalParams
        }
        if ($PSBoundParameters.ContainsKey('ErrorAction'))
        {
            $CopyFileSplat.ErrorAction = $PSBoundParameters.ErrorAction
        }

        # Define default params for Get-ADTUserProfiles.
        $GetUserProfileSplat = @{
            IncludeSystemProfiles = $IncludeSystemProfiles
            IncludeServiceProfiles = $IncludeServiceProfiles
            ExcludeDefaultUser = $ExcludeDefaultUser
        }
        if ($ExcludeNTAccount)
        {
            $GetUserProfileSplat.ExcludeNTAccount = $ExcludeNTAccount
        }
        if ($BasePath -ne 'ProfilePath')
        {
            $GetUserProfileSplat.LoadProfilePaths = $true
        }

        # Collector for all provided paths.
        $sourcePaths = [System.Collections.Generic.List[System.String]]::new()
    }

    process
    {
        # Add all source paths to the collection.
        $sourcePaths.AddRange($Path)
    }

    end
    {
        # Copy all paths to the specified destination.
        foreach ($UserProfile in (Get-ADTUserProfiles @GetUserProfileSplat))
        {
            if ([string]::IsNullOrWhiteSpace($UserProfile."$BasePath`Path"))
            {
                Write-ADTLogEntry -Message "Skipping user profile [$($UserProfile.NTAccount)] as path [$BasePath`Path] is not available."
                continue
            }
            $dest = Join-Path $UserProfile."$BasePath`Path" $Destination
            Write-ADTLogEntry -Message "Copying path [$Path] to $($dest):"
            Copy-ADTFile -Path $sourcePaths -Destination $dest @CopyFileSplat
        }

        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
