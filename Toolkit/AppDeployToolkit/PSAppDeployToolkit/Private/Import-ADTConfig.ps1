#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Import-ADTConfig
{
    # Get the current environment and root module.
    $adtEnv = Get-ADTEnvironment

    # Create variables within this scope from the database, it's needed during the config import.
    $adtEnv.GetEnumerator() | . { process { & $Script:CommandTable.'New-Variable' -Name $_.Name -Value $_.Value -Option Constant } }

    # Read config file and cast the version into an object.
    $config = & $Script:CommandTable.'Import-LocalizedData' -BaseDirectory $Script:PSScriptRoot\Config -FileName config.psd1
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
    foreach ($asset in ('Icon', 'Logo', 'Banner'))
    {
        $config.Assets.$asset = (& $Script:CommandTable.'Get-Item' -LiteralPath "$Script:PSScriptRoot\Assets\$($config.Assets.$asset)").FullName
    }

    # Grab the bytes of each image asset, store them into a memory stream, then as an image for the form to use.
    $Script:Dialogs.Classic.Assets.Icon = [System.Drawing.Icon]::new([System.IO.MemoryStream]::new([System.IO.File]::ReadAllBytes($config.Assets.Icon)))
    $Script:Dialogs.Classic.Assets.Logo = [System.Drawing.Image]::FromStream([System.IO.MemoryStream]::new([System.IO.File]::ReadAllBytes($config.Assets.Logo)))
    $Script:Dialogs.Classic.Assets.Banner = [System.Drawing.Image]::FromStream([System.IO.MemoryStream]::new([System.IO.File]::ReadAllBytes($config.Assets.Banner)))
    $Script:Dialogs.Classic.BannerHeight = [System.Math]::Ceiling($Script:Dialogs.Classic.Width * ($Script:Dialogs.Classic.Assets.Banner.Height / $Script:Dialogs.Classic.Assets.Banner.Width))

    # If we're using fluent dialogs but running in the ISE, force it back to classic.
    if (!$Host.Name.Equals('ConsoleHost') -and ($config.UI.DialogStyle -eq 'Fluent'))
    {
        $config.UI.DialogStyle = 'Classic'
    }

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
