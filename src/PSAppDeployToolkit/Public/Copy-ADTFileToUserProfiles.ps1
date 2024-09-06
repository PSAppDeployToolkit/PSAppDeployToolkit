#-----------------------------------------------------------------------------
#
# MARK: Copy-FileToUserProfiles
#
#-----------------------------------------------------------------------------

function Copy-ADTFileToUserProfiles
{
    <#
    .SYNOPSIS
        Copy one or more items to each user profile on the system.

    .DESCRIPTION
        The Copy-FileToUserProfiles function copies one or more items to each user profile on the system. It supports various options such as recursion, flattening files, and using Robocopy to overcome the 260 character limit.

    .PARAMETER Path
        The path of the file or folder to copy.

    .PARAMETER Destination
        The path of the destination folder to append to the root of the user profile.

    .PARAMETER Recurse
        Copy files in subdirectories.

    .PARAMETER Flatten
        Flattens the files into the root destination directory.

    .PARAMETER ContinueFileCopyOnError
        Continue copying files if an error is encountered. This will continue the deployment script and will warn about files that failed to be copied. Default is: $false.

    .PARAMETER FileCopyMode
        Select from 'Native' or 'Robocopy'. Default is configured in config.psd1. Note that Robocopy supports * in file names, but not folders, in source paths.

    .PARAMETER ExcludeNTAccount
        Specify NT account names in Domain\Username format to exclude from the list of user profiles.

    .PARAMETER IncludeSystemProfiles
        Include system profiles: SYSTEM, LOCAL SERVICE, NETWORK SERVICE. Default is: $false.

    .PARAMETER IncludeServiceProfiles
        Include service profiles where NTAccount begins with NT SERVICE. Default is: $false.

    .PARAMETER ExcludeDefaultUser
        Exclude the Default User. Default is: $false.

    .INPUTS
        System.String[]

        You can pipe in string values for $Path.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Copy-FileToUserProfiles -Path "$dirSupportFiles\config.txt" -Destination "AppData\Roaming\MyApp"

        Copy a single file to C:\Users\<UserName>\AppData\Roaming\MyApp for each user.

    .EXAMPLE
        Copy-FileToUserProfiles -Path "$dirSupportFiles\config.txt","$dirSupportFiles\config2.txt" -Destination "AppData\Roaming\MyApp"

        Copy two files to C:\Users\<UserName>\AppData\Roaming\MyApp for each user.

    .EXAMPLE
        Copy-FileToUserProfiles -Path "$dirFiles\MyApp" -Destination "AppData\Local" -Recurse

        Copy an entire folder to C:\Users\<UserName>\AppData\Local for each user.

    .EXAMPLE
        Copy-FileToUserProfiles -Path "$dirFiles\.appConfigFolder" -Recurse

        Copy an entire folder to C:\Users\<UserName> for each user.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '', Justification = "This function is appropriately named and we don't need PSScriptAnalyzer telling us otherwise.")]
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true, Position = 1, ValueFromPipeline = $true)]
        [String[]]$Path,

        [Parameter(Mandatory = $false, Position = 2)]
        [String]$Destination,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Recurse,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Flatten,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Native', 'Robocopy')]
        [System.String]$FileCopyMode,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String[]]$ExcludeNTAccount,

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

    dynamicparam
    {
        # Define parameter dictionary for returning at the end.
        $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()

        if ($PSBoundParameters.ContainsKey('FileCopyMode') -and ($PSBoundParameters.FileCopyMode -eq 'Robocopy'))
        {
            # Define the RobocopyParams parameter.
            $paramDictionary.Add('RobocopyParams', [System.Management.Automation.RuntimeDefinedParameter]::new(
                    'RobocopyParams', [System.String], $(
                        [System.Management.Automation.ParameterAttribute]@{ Mandatory = $false; HelpMessage = 'Override the default Robocopy parameters when FileCopyMode = Robocopy. Default is: /NJH /NJS /NS /NC /NP /NDL /FP /IS /IT /IM /XX /MT:4 /R:1 /W:1' }
                        [System.Management.Automation.AllowEmptyStringAttribute]::new()
                    )
                ))

            # Define the RobocopyAdditionalParams parameter.
            $paramDictionary.Add('RobocopyAdditionalParams', [System.Management.Automation.RuntimeDefinedParameter]::new(
                    'RobocopyAdditionalParams', [System.String], $(
                        [System.Management.Automation.ParameterAttribute]@{ Mandatory = $false; HelpMessage = 'Append to the default Robocopy parameters when FileCopyMode = Robocopy.' }
                        [System.Management.Automation.AllowEmptyStringAttribute]::new()
                    )
                ))
        }

        # Return the populated dictionary.
        return $paramDictionary
    }

    begin
    {
        # Initalise function.
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

        # Collector for all provided paths.
        $sourcePaths = [System.Collections.Specialized.StringCollection]::new()
    }

    process
    {
        # Add all source paths to the collection.
        $sourcePaths.AddRange($Path)
    }

    end
    {
        # Copy all paths to the specified destination.
        foreach ($UserProfilePath in (Get-ADTUserProfiles @GetUserProfileSplat).ProfilePath)
        {
            $dest = & $Script:CommandTable.'Join-Path' $UserProfilePath $Destination
            Write-ADTLogEntry -Message "Copying path [$Path] to $($dest):"
            Copy-ADTFile -Path $sourcePaths -Destination $dest @CopyFileSplat
        }

        # Finalise function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
