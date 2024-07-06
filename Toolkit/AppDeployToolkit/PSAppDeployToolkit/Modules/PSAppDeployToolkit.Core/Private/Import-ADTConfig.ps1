function Import-ADTConfig
{
    # Get the current environment and root module.
    $adtEnv = Get-ADTEnvironment
    $adtModule = Get-ADTModuleInfo

    # Create variables within this scope from the database, it's needed during the config import.
    $adtEnv.GetEnumerator().ForEach({New-Variable -Name $_.Name -Value $_.Value -Option Constant})

    # Read config file and cast the version into an object.
    $config = Import-LocalizedData -BaseDirectory "$($adtModule.ModuleBase)\Config" -FileName config.psd1
    $config.File.Version = [version]$config.File.Version

    # Confirm the config version meets our minimum requirements.
    if ($config.File.Version -lt $adtModule.Version)
    {
        $naerParams = @{
            Exception = [System.Data.VersionNotFoundException]::new("The configuration file version [$($config.File.Version)] is lower than the supported of [$($adtModule.Version)]. Please upgrade the configuration file.")
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
        foreach ($subsection in $($config[$section].Keys))
        {
            if ($config[$section][$subsection] -is [System.String])
            {
                $config[$section][$subsection] = $ExecutionContext.InvokeCommand.ExpandString($config[$section][$subsection])
            }
        }
    }

    # Expand out asset file paths and test that the files are present.
    foreach ($asset in ('Icon', 'Logo', 'Banner'))
    {
        $config.Assets.$asset = (Get-Item -LiteralPath "$($adtModule.ModuleBase)\Assets\$($config.Assets.$asset)").FullName
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
    $config.Toolkit.TempPath = [System.IO.Path]::Combine($config.Toolkit.TempPath, $adtModule.Name)

    # Finally, store the config globally for usage within module.
    (Get-ADTModuleData).Config = $config
}
