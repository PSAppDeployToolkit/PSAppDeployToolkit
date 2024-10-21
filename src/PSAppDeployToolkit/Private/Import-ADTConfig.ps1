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
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Alias('ScriptDirectory')]
        [System.String]$BaseDirectory
    )

    # Internal filter to process asset filepaths.
    filter Update-ADTAssetFilePath
    {
        # Go recursive if we've received a hashtable, otherwise just update the values.
        foreach ($asset in $($_.GetEnumerator()))
        {
            if ($asset.Value -isnot [System.Collections.Hashtable])
            {
                $_.($asset.Key) = (Get-Item -LiteralPath "$Script:PSScriptRoot\Assets\$($_.($asset.Key))").FullName
            }
            else
            {
                $asset.Value | & $MyInvocation.MyCommand
            }
        }
    }

    # Process the incoming $BaseDirectory value.
    $PSBoundParameters.BaseDirectory = if (![System.IO.File]::Exists([System.IO.Path]::Combine(($dataDir = [System.IO.Path]::Combine($BaseDirectory, 'Config')), 'config.psd1')))
    {
        [System.IO.Path]::Combine($Script:PSScriptRoot, 'Config')
    }
    else
    {
        $dataDir
    }

    # Get the current environment and create variables within this scope from the database, it's needed during the config import.
    $adtEnv = Get-ADTEnvironment
    $adtEnv.GetEnumerator() | . { process { New-Variable -Name $_.Name -Value $_.Value -Option Constant } }

    # Read config file and cast the version into an object.
    $config = Import-LocalizedData -FileName config.psd1 @PSBoundParameters
    $config.File.Version = [version]$config.File.Version

    # Confirm the config version meets our minimum requirements.
    if ($config.File.Version -lt $MyInvocation.MyCommand.Module.Version)
    {
        $naerParams = @{
            Exception = [System.Data.VersionNotFoundException]::new("The configuration file version [$($config.File.Version)] is lower than the supported of [$($MyInvocation.MyCommand.Module.Version)]. Please upgrade the configuration file.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidData
            ErrorId = 'ConfigFileVersionMismatch'
            TargetObject = $config
            RecommendedAction = "Please review the supplied configuration file and try again."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }

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
    if (!$Script:DialogDispatcher.Contains($config.UI.DialogStyle))
    {
        $naerParams = @{
            Exception = [System.NotSupportedException]::new("The specified dialog style [$($config.UI.DialogStyle)] is not supported. Valid styles are ['$($Script:DialogDispatcher.Keys -join "', '")'].")
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
