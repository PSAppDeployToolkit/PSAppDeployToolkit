#-----------------------------------------------------------------------------
#
# MARK: Import-ADTConfig
#
#-----------------------------------------------------------------------------

function Import-ADTConfig
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$BaseDirectory
    )

    # Internal filter to process asset file paths.
    filter Update-ADTAssetFilePath
    {
        # Go recursive if we've received a hashtable, otherwise just update the values.
        foreach ($asset in $($_.GetEnumerator()))
        {
            # Re-process if this is a hashtable.
            if ($asset.Value -is [System.Collections.Hashtable])
            {
                $asset.Value | & $MyInvocation.MyCommand; continue
            }

            # Skip if the path is fully qualified.
            if ([System.IO.Path]::IsPathRooted($asset.Value))
            {
                continue
            }

            # Get the asset's full path based on the supplied BaseDirectory.
            # Fall back to the module's path if the asset is unable to be found.
            if ([System.IO.File]::Exists("$BaseDirectory\$($_.($asset.Key))"))
            {
                $_.($asset.Key) = (Get-Item -LiteralPath "$BaseDirectory\$($_.($asset.Key))").FullName
            }
            else
            {
                $_.($asset.Key) = (Get-Item -LiteralPath "$($BaseDirectory -replace '^.+\\', "$Script:PSScriptRoot\")\$($_.($asset.Key))").FullName
            }
        }
    }

    # Get the current environment and create variables within this scope from the database, it's needed during the config import.
    ($adtEnv = Get-ADTEnvironment).GetEnumerator() | . { process { New-Variable -Name $_.Name -Value $_.Value -Option Constant } }
    $config = Import-ADTModuleDataFile @PSBoundParameters -FileName config.psd1

    # Confirm the specified dialog type is valid.
    if (($config.UI.DialogStyle -ne 'Classic') -and (Test-ADTNonNativeCaller))
    {
        $config.UI.DialogStyle = if ($config.UI.ContainsKey('DialogStyleCompatMode'))
        {
            $config.UI.DialogStyleCompatMode
        }
        else
        {
            'Classic'
        }
    }
    if (!$Script:Dialogs.Contains($config.UI.DialogStyle))
    {
        $naerParams = @{
            Exception = [System.NotSupportedException]::new("The specified dialog style [$($config.UI.DialogStyle)] is not supported. Valid styles are ['$($Script:Dialogs.Keys -join "', '")'].")
            Category = [System.Management.Automation.ErrorCategory]::InvalidData
            ErrorId = 'DialogStyleNotSupported'
            TargetObject = $config
            RecommendedAction = "Please review the supplied configuration file and try again."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }

    # Process the config and expand out variables.
    foreach ($section in $($config.Keys))
    {
        foreach ($subsection in $($config.$section.Keys))
        {
            if ($config.$section.$subsection -is [System.String])
            {
                $config.$section.$subsection = $ExecutionContext.InvokeCommand.ExpandString($config.$section.$subsection)
            }
        }
    }

    # Expand out asset file paths and test that the files are present.
    $config.Assets | Update-ADTAssetFilePath

    # Process the classic assets by grabbing the bytes of each image asset, storing them into a memory stream, then as an image for WinForms to use.
    $Script:Dialogs.Classic.Assets.Icon = [System.Drawing.Icon]::new([System.IO.MemoryStream]::new([System.IO.File]::ReadAllBytes($config.Assets.Classic.Icon)))
    $Script:Dialogs.Classic.Assets.Logo = [System.Drawing.Image]::FromStream([System.IO.MemoryStream]::new([System.IO.File]::ReadAllBytes($config.Assets.Classic.Logo)))
    $Script:Dialogs.Classic.Assets.Banner = [System.Drawing.Image]::FromStream([System.IO.MemoryStream]::new([System.IO.File]::ReadAllBytes($config.Assets.Classic.Banner)))
    $Script:Dialogs.Classic.BannerHeight = [System.Math]::Ceiling($Script:Dialogs.Classic.Width * ($Script:Dialogs.Classic.Assets.Banner.Height / $Script:Dialogs.Classic.Assets.Banner.Width))

    # Change paths to user accessible ones if user isn't an admin.
    if (!$adtEnv.IsAdmin)
    {
        if ($config.Toolkit.TempPathNoAdminRights)
        {
            $config.Toolkit.TempPath = $config.Toolkit.TempPathNoAdminRights
        }
        if ($config.Toolkit.RegPathNoAdminRights)
        {
            $config.Toolkit.RegPath = $config.Toolkit.RegPathNoAdminRights
        }
        if ($config.Toolkit.LogPathNoAdminRights)
        {
            $config.Toolkit.LogPath = $config.Toolkit.LogPathNoAdminRights
        }
        if ($config.MSI.LogPathNoAdminRights)
        {
            $config.MSI.LogPath = $config.MSI.LogPathNoAdminRights
        }
    }

    # Append the toolkit's name onto the temporary path.
    $config.Toolkit.TempPath = [System.IO.Path]::Combine($config.Toolkit.TempPath, $adtEnv.appDeployToolkitName)

    # Set up the user temp path. This needs to be performed here as we need the config available, but the config depends on the environment being up first.
    $adtEnv.loggedOnUserTempPath = if (($null -eq $adtEnv.RunAsActiveUser.NTAccount) -or ![System.IO.Directory]::Exists($adtEnv.runasUserProfile))
    {
        $config.Toolkit.TempPath
    }
    else
    {
        [System.IO.Path]::Combine($adtEnv.runasUserProfile, $adtEnv.appDeployToolkitName)
    }

    # Finally, return the config for usage within module.
    return $config
}
