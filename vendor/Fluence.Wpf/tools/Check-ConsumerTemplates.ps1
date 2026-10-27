param(
    [ValidateSet('fluence-window', 'fluence-nav-mvvm', 'fluence-settings')]
    [string[]]$Template = @('fluence-window', 'fluence-nav-mvvm', 'fluence-settings'),
    [switch]$KeepArtifacts,
    [string]$ReportPath,
    [string]$InspectGeneratedPath
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$version = '0.9.4-starters-dev.gb3879f16.r8'
$selectedTemplates = @($Template)
$names = @{
    'fluence-window' = 'window'
    'fluence-nav-mvvm' = 'navigation-mvvm'
    'fluence-settings' = 'settings-shell'
}
$identities = @{
    'fluence-window' = 'Fluence.Wpf.Templates.Window.CSharp'
    'fluence-nav-mvvm' = 'Fluence.Wpf.Templates.NavigationMvvm.CSharp'
    'fluence-settings' = 'Fluence.Wpf.Templates.SettingsShell.CSharp'
}
$tempRoot = (Resolve-Path $env:TEMP).Path
$run = Join-Path $tempRoot ('fluence-consumer-template-check-' + [guid]::NewGuid().ToString('N'))
$feed = Join-Path $run 'feed'
$packages = Join-Path $run 'packages'
$generated = Join-Path $run 'generated'
$hive = Join-Path $run 'hive'
$logs = Join-Path $run 'logs'
$nugetConfig = Join-Path $run 'NuGet.Config'
$result = [ordered]@{
    status = 'FAIL'
    exitCode = 1
    version = $version
    templates = @($selectedTemplates)
    run = $run
    feed = $feed
    packages = $packages
    generated = $generated
    hive = $hive
    logs = $logs
    nugetConfig = $nugetConfig
    packageFiles = @()
    checkedTemplates = @()
    commandResults = @()
    error = $null
    retained = $true
}
$installed = $false
$success = $false

function Invoke-Dotnet {
    param([string]$Name, [string[]]$Arguments)

    $stdout = Join-Path $logs ($Name + '.stdout.log')
    $stderr = Join-Path $logs ($Name + '.stderr.log')
    $quoted = foreach ($argument in $Arguments) {
        '"' + $argument.Replace('"', '\"') + '"'
    }
    $process = Start-Process -FilePath 'dotnet' -ArgumentList ($quoted -join ' ') -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    $process.WaitForExit()
    $result.commandResults += [ordered]@{
        name = $Name
        exitCode = $process.ExitCode
        stdout = $stdout
        stderr = $stderr
    }
    if ($process.ExitCode -ne 0) {
        throw "$Name failed with exit code $($process.ExitCode). See $stdout and $stderr"
    }
    return (Get-Content -Raw $stdout)
}

function Assert-TextContains {
    param([string]$Text, [string]$Pattern, [string]$Description)

    if ($Text -notmatch $Pattern) { throw "Structural check failed: $Description" }
}

function Assert-GeneratedIgnore {
    param([string]$Output)

    $ignorePath = Join-Path $Output '.gitignore'
    if (-not (Test-Path -LiteralPath $ignorePath -PathType Leaf)) {
        throw "Generated .gitignore is missing: $Output"
    }
    $lines = @(Get-Content -LiteralPath $ignorePath | ForEach-Object { $_.Trim() })
    foreach ($entry in @('/.nuget-packages/', '/bin/', '/obj/', '/NuGet.Config')) {
        if ($lines -cnotcontains $entry) { throw "Generated .gitignore is missing $entry in $Output" }
    }
}

function Assert-NoStarterBrand {
    param([string]$Output)

    $files = @(Get-ChildItem -LiteralPath $Output -Recurse -File)
    $icons = @($files | Where-Object { $_.Extension -ieq '.ico' })
    if ($icons.Count -gt 0) { throw "Bundled .ico is not allowed in a starter: $($icons[0].FullName)" }
    foreach ($file in $files) {
        if ($file.Extension -ieq '.csproj') {
            [xml]$project = Get-Content -Raw -LiteralPath $file.FullName
            if (@($project.SelectNodes('//*[local-name()="ApplicationIcon"]')).Count -gt 0) {
                throw "ApplicationIcon is not allowed in a starter: $($file.FullName)"
            }
        } elseif ($file.Extension -ieq '.xaml') {
            [xml]$xaml = Get-Content -Raw -LiteralPath $file.FullName
            $windows = @($xaml.SelectNodes('//*[local-name()="Window" or local-name()="FluenceWindow"]'))
            foreach ($window in $windows) {
                if (@($window.Attributes | Where-Object { $_.LocalName -eq 'Icon' }).Count -gt 0) {
                    throw "Window Icon attribute is not allowed in a starter: $($file.FullName)"
                }
            }
            if (@($xaml.SelectNodes('//*[local-name()="Window.Icon" or local-name()="FluenceWindow.Icon"]')).Count -gt 0) {
                throw "Window Icon property is not allowed in a starter: $($file.FullName)"
            }
        } elseif ($file.Extension -ieq '.cs') {
            if ((Get-Content -Raw -LiteralPath $file.FullName) -match 'FluenceWindow\.DefaultIcon') {
                throw "Fluence default icon assignment is not allowed in a starter: $($file.FullName)"
            }
        }
    }
}

function Assert-NoAppSdkComponents {
    param([string]$Path)

    $files = @(Get-ChildItem -LiteralPath $Path -Recurse -File)
    $payload = @($files | Where-Object { $_.Name -match '(?i)\.pri$|^Microsoft\.WindowsAppRuntime.*\.dll$|^Microsoft\.WindowsAppSDK.*\.dll$' })
    if ($payload.Count -gt 0) { throw "Windows App SDK payload is forbidden: $($payload[0].FullName)" }
    foreach ($file in $files | Where-Object { $_.Extension -in @('.cs', '.csproj', '.xaml', '.props', '.targets') }) {
        $source = Get-Content -Raw -LiteralPath $file.FullName
        if ($source -match '(?i)Microsoft\.WindowsAppSDK|WindowsAppRuntime|MddBootstrap|Bootstrap\.Initialize|Microsoft\.UI\.Xaml') {
            throw "Windows App SDK source component is forbidden: $($file.FullName)"
        }
        if ($file.Extension -eq '.csproj') {
            [xml]$project = $source
            $references = @($project.SelectNodes('//*[local-name()="PackageReference"]'))
            if (@($references | Where-Object { $_.GetAttribute('Include') -match '(?i)WindowsAppSDK|WindowsAppRuntime' }).Count -gt 0) {
                throw "Windows App SDK package reference is forbidden: $($file.FullName)"
            }
        }
    }
}

function Assert-PngSize {
    param([string]$Path, [int]$Expected)

    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -lt 24 -or [BitConverter]::ToString($bytes, 0, 8).Replace('-', '') -ne '89504E470D0A1A0A' -or
        [System.Text.Encoding]::ASCII.GetString($bytes, 12, 4) -ne 'IHDR') {
        throw "Invalid PNG header: $Path"
    }
    $width = $bytes[16] * 16777216 + $bytes[17] * 65536 + $bytes[18] * 256 + $bytes[19]
    $height = $bytes[20] * 16777216 + $bytes[21] * 65536 + $bytes[22] * 256 + $bytes[23]
    if ($width -ne $Expected -or $height -ne $Expected) {
        throw "PNG dimensions must be $Expected x ${Expected}: $Path ($width x $height)"
    }
}

function Assert-SettingsAssets {
    param([string]$Output, [switch]$CheckBuildOutput)

    $iconDir = Join-Path $Output 'Assets/SettingsIcons'
    $sizes = @{ 100 = 20; 125 = 25; 150 = 30; 200 = 40; 400 = 80 }
    $expected = @('ARTWORK-NOTICE.md', 'MICROSOFT-FLUENT-LICENSE.txt')
    foreach ($category in @('General', 'Appearance', 'Notifications', 'About')) {
        foreach ($scale in @(100, 125, 150, 200, 400)) {
            $name = "$category-scale-$scale.png"
            $expected += $name
            Assert-PngSize (Join-Path $iconDir $name) $sizes[$scale]
        }
    }
    $actual = @(Get-ChildItem -LiteralPath $iconDir -File | ForEach-Object Name | Sort-Object)
    if (($actual -join '|') -cne (($expected | Sort-Object) -join '|')) { throw "Settings image/notice inventory mismatch: $iconDir" }
    $notice = Get-Content -Raw -LiteralPath (Join-Path $iconDir 'ARTWORK-NOTICE.md')
    if ($notice -notmatch 'No explicit redistribution grant' -or $notice -notmatch 'MIT notice' -or
        $notice -notmatch 'General' -or $notice -notmatch 'Appearance' -or
        $notice -notmatch 'Notifications' -or $notice -notmatch 'About') {
        throw 'Settings artwork provenance must distinguish Windows artwork from MIT Fluent artwork.'
    }
    if ((Get-Content -Raw -LiteralPath (Join-Path $iconDir 'MICROSOFT-FLUENT-LICENSE.txt')) -notmatch 'Copyright \(c\) Microsoft Corporation') {
        throw 'Microsoft Fluent artwork MIT notice is missing.'
    }
    if ($CheckBuildOutput) {
        $built = Join-Path $Output 'bin/Release/net10.0-windows10.0.26100.0/Assets/SettingsIcons'
        foreach ($name in @('ARTWORK-NOTICE.md', 'MICROSOFT-FLUENT-LICENSE.txt')) {
            $sourceHash = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $iconDir $name)).Hash
            $builtHash = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $built $name)).Hash
            if ($sourceHash -cne $builtHash) { throw "Settings output attribution differs from source: $name" }
        }
    }
}

function Assert-GeneratedBytes {
    param([string]$Output, [string]$ShortName)

    $sourceRoot = Join-Path $repo "templates/Fluence.Wpf.Templates/content/$($names[$ShortName])"
    $expectedNames = @()
    foreach ($sourceFile in Get-ChildItem -LiteralPath $sourceRoot -Recurse -File) {
        $relative = [System.IO.Path]::GetRelativePath($sourceRoot, $sourceFile.FullName)
        if ($relative -match '(^|[\\/])\.template\.config([\\/]|$)') { continue }
        $generatedName = $relative.Replace('FluenceStarterApp', 'Contoso.Sample')
        $expectedNames += $generatedName.Replace('\', '/')
        $generatedPath = Join-Path $Output $generatedName
        if (-not (Test-Path -LiteralPath $generatedPath -PathType Leaf)) { throw "Generated file missing: $generatedPath" }
        $sourceBytes = [System.IO.File]::ReadAllBytes($sourceFile.FullName)
        $actualBytes = [System.IO.File]::ReadAllBytes($generatedPath)
        if ($sourceFile.Extension -eq '.png') {
            $expectedBytes = $sourceBytes
        } else {
            $text = [System.Text.Encoding]::UTF8.GetString($sourceBytes).Replace('FluenceStarterApp', 'Contoso.Sample')
            $expectedBytes = [System.Text.Encoding]::UTF8.GetBytes($text)
        }
        $expectedHash = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($expectedBytes))
        $actualHash = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($actualBytes))
        if ($expectedHash -cne $actualHash) { throw "Generated file bytes differ from template substitution: $generatedPath" }
    }
    $actualNames = @(Get-ChildItem -LiteralPath $Output -Recurse -File |
        ForEach-Object { [System.IO.Path]::GetRelativePath($Output, $_.FullName).Replace('\', '/') } | Sort-Object)
    if (($actualNames -join '|') -cne (($expectedNames | Sort-Object) -join '|')) {
        throw "Generated file inventory differs from source template: $Output"
    }
}

function Assert-WorkflowContract {
    $agentGuide = Get-Content -Raw -LiteralPath (Join-Path $repo 'AGENTS.md')
    $skill = Get-Content -Raw -LiteralPath (Join-Path $repo 'skills/fluence-consumer-starter/SKILL.md')
    $readme = Get-Content -Raw -LiteralPath (Join-Path $repo 'templates/README.md')
    Assert-TextContains $agentGuide 'skills/fluence-consumer-starter/SKILL\.md' 'root AGENTS.md skill route'
    Assert-TextContains $skill 'dotnet new install' 'skill installation guidance'
    Assert-TextContains $skill 'Restore and build' 'skill restore/build guidance'
    Assert-TextContains $readme 'Check-ConsumerTemplates\.ps1' 'templates README checker'
    Assert-TextContains $readme '--debug:custom-hive' 'templates README isolated hive'
    Assert-TextContains $readme 'dotnet new install' 'templates README installation'
    Assert-TextContains $readme 'dotnet new list fluence' 'templates README discovery'
    Assert-TextContains $readme 'dotnet restore' 'templates README restore'
    Assert-TextContains $readme 'dotnet build' 'templates README build'
    Assert-TextContains $readme 'dotnet run' 'templates README run'
    Assert-TextContains $readme '\.gitignore' 'templates README cache ignore guidance'
    Assert-TextContains $skill '\.nuget-packages/' 'skill cache ignore guidance'
    foreach ($shortName in $names.Keys) {
        Assert-TextContains $skill ([regex]::Escape($shortName)) "skill route $shortName"
        Assert-TextContains $readme ([regex]::Escape($shortName)) "templates README route $shortName"
    }
}

function Assert-GeneratedStructure {
    param([string]$Output, [string]$ShortName)

    $projectPath = Join-Path $Output 'Contoso.Sample.csproj'
    [xml]$project = Get-Content -Raw -LiteralPath $projectPath
    $properties = $project.Project.PropertyGroup
    if ($properties.TargetFramework -ne 'net10.0-windows10.0.26100.0' -or $properties.UseWPF -ne 'true' -or $properties.OutputType -ne 'WinExe') {
        throw "Generated project framework/WPF/output contract failed: $projectPath"
    }
    $references = @($project.SelectNodes('//*[local-name()="PackageReference"]'))
    $expectedCount = if ($ShortName -eq 'fluence-nav-mvvm') { 2 } else { 1 }
    if ($references.Count -ne $expectedCount) { throw "Unexpected generated PackageReference set: $projectPath" }
    Assert-NoAppSdkComponents $Output
    if ($ShortName -eq 'fluence-nav-mvvm') {
        $toolkit = @($references | Where-Object { $_.Include -eq 'CommunityToolkit.Mvvm' -and $_.Version -eq '8.4.2' })
        if ($toolkit.Count -ne 1) { throw "CommunityToolkit.Mvvm 8.4.2 reference missing: $projectPath" }
    }
    $appXaml = Get-Content -Raw -LiteralPath (Join-Path $Output 'App.xaml')
    $startup = Get-Content -Raw -LiteralPath (Join-Path $Output 'App.xaml.cs')
    Assert-TextContains $startup 'ApplicationThemeManager\.Apply\(ApplicationTheme\.Auto\)' "theme startup for $ShortName"
    Assert-TextContains $startup 'ApplicationAccentColorManager\.ApplySystemAccent\(\)' "accent startup for $ShortName"
    if ($appXaml -match 'Generic\.xaml') { throw "App.xaml duplicates Fluence Generic.xaml: $Output" }
    Assert-GeneratedIgnore $Output
    $generatedReadme = Get-Content -Raw -LiteralPath (Join-Path $Output 'README.md')
    Assert-TextContains $generatedReadme '\.gitignore' "generated cache ignore guidance for $ShortName"
    Assert-TextContains $generatedReadme '\.nuget-packages/' "generated package cache guidance for $ShortName"

    if ($ShortName -eq 'fluence-window') {
        [xml]$window = Get-Content -Raw -LiteralPath (Join-Path $Output 'MainWindow.xaml')
        $code = Get-Content -Raw -LiteralPath (Join-Path $Output 'MainWindow.xaml.cs')
        $titleHost = @($window.SelectNodes('//*[local-name()="FluenceWindow.TitleBar"]/*[local-name()="Grid"]'))
        $titleBar = @($window.SelectNodes('//*[local-name()="FluenceWindow.TitleBar"]/*[local-name()="Grid"]/*[local-name()="TitleBar"]'))
        $icon = @($window.SelectNodes('//*[local-name()="FluenceWindow.TitleBar"]/*[local-name()="Grid"]/*[local-name()="Grid" and @Width="20" and @Height="20" and @Margin="12,0,8,0"]'))
        if ($titleHost.Count -ne 1 -or $titleBar.Count -ne 1 -or $icon.Count -ne 1 -or
            @($titleHost[0].SelectNodes('./*[local-name()="Grid.ColumnDefinitions"]/*[local-name()="ColumnDefinition" and @Width="40"]')).Count -ne 1 -or
            @($icon[0].SelectNodes('./*[local-name()="Ellipse" and @Stroke="{DynamicResource TextFillColorPrimaryBrush}"]')).Count -ne 1 -or
            @($icon[0].SelectNodes('./*[local-name()="Path" and @Data]')).Count -ne 1 -or
            $titleBar[0].GetAttribute('Title') -ne 'Contoso.Sample' -or
            $titleBar[0].GetAttribute('IsBackButtonVisible') -ne 'False' -or
            $titleBar[0].GetAttribute('IsPaneToggleButtonVisible') -ne 'False' -or
            $titleBar[0].HasAttribute('Subtitle') -or
            @($titleBar[0].SelectNodes('./*[local-name()="TitleBar.Icon"]')).Count -ne 0 -or
            $window.DocumentElement.GetAttribute('ExtendsContentIntoTitleBar') -ne 'True' -or
            $window.DocumentElement.GetAttribute('MinHeight') -ne '470') {
            throw 'Simple window icon-first 12-DIP title bar contract failed.'
        }
        Assert-TextContains $window.OuterXml 'DisplayMemberPath="Name"' 'window typed ComboBox display'
        Assert-TextContains $window.OuterXml 'Name="StatusText"' 'window visible status'
        if (@($window.SelectNodes('/*/*[local-name()="Grid" and @Margin="28,76,28,28"]')).Count -ne 1) {
            throw 'Simple content must clear the 48-DIP title bar without losing its 28-DIP page inset.'
        }
        Assert-TextContains $code 'SelectedItem as SampleOption' 'window typed selection'
        Assert-TextContains $code 'SetCaptionCommandTarget\("PART_MinimizeButton"\)' 'window caption minimize hookup'
    } elseif ($ShortName -eq 'fluence-nav-mvvm') {
        [xml]$window = Get-Content -Raw -LiteralPath (Join-Path $Output 'MainWindow.xaml')
        $titleHost = @($window.SelectNodes('//*[local-name()="FluenceWindow.TitleBar"]/*[local-name()="Grid"]'))
        $titleBar = @($window.SelectNodes('//*[local-name()="FluenceWindow.TitleBar"]/*[local-name()="Grid"]/*[local-name()="TitleBar"]'))
        $icon = @($window.SelectNodes('//*[local-name()="FluenceWindow.TitleBar"]/*[local-name()="Grid"]/*[local-name()="Grid" and @Width="20" and @Height="20" and @Margin="12,0,8,0"]'))
        if ($titleHost.Count -ne 1 -or $titleBar.Count -ne 1 -or $icon.Count -ne 1 -or
            @($titleHost[0].SelectNodes('./*[local-name()="Grid.ColumnDefinitions"]/*[local-name()="ColumnDefinition" and @Width="40"]')).Count -ne 1 -or
            @($icon[0].SelectNodes('./*[local-name()="Path" and @Data]')).Count -ne 1 -or
            @($icon[0].SelectNodes('./*[local-name()="Ellipse" and @Stroke="{DynamicResource TextFillColorPrimaryBrush}"]')).Count -ne 1 -or
            $titleBar[0].GetAttribute('Title') -ne 'Contoso.Sample' -or
            $titleBar[0].GetAttribute('IsBackButtonVisible') -ne 'True' -or
            $titleBar[0].GetAttribute('BackCommand') -ne '{Binding DataContext.GoBackCommand, ElementName=ShellWindow}' -or
            $titleBar[0].GetAttribute('IsPaneToggleButtonVisible') -ne 'True' -or
            $titleBar[0].HasAttribute('Subtitle') -or
            @($titleBar[0].SelectNodes('./*[local-name()="TitleBar.Icon"]')).Count -ne 0 -or
            $titleBar[0].HasAttribute('BackRequested') -or
            $window.DocumentElement.GetAttribute('ExtendsContentIntoTitleBar') -ne 'True') {
            throw 'Navigation icon-first 12-DIP title bar, Back, or pane toggle contract failed.'
        }
        $navigation = @($window.SelectNodes('//*[local-name()="NavigationView" and @*[local-name()="Name" and .="AppNavigation"]]'))
        if ($navigation.Count -ne 1 -or $navigation[0].GetAttribute('PaneDisplayMode') -ne 'LeftCompact' -or
            $navigation[0].GetAttribute('IsBackButtonVisible') -ne 'False' -or
            $navigation[0].GetAttribute('IsPaneToggleButtonVisible') -ne 'False' -or
            $navigation[0].GetAttribute('IsPaneOpen') -ne '{Binding IsPaneOpen, Mode=TwoWay}') {
            throw 'Navigation compact pane binding contract failed.'
        }
        foreach ($route in @('home', 'activity', 'settings')) {
            $item = @($window.SelectNodes("//*[local-name()='NavigationViewItem' and @Tag='$route']"))
            if ($item.Count -ne 1 -or @($item[0].SelectNodes('.//*[local-name()="FontIcon" and @Glyph]')).Count -ne 1) {
                throw "Navigation item route/icon contract failed: $route"
            }
        }
        if (@($window.SelectNodes("//*[local-name()='NavigationView.FooterMenuItems']/*[local-name()='NavigationViewItem' and @Tag='settings']")).Count -ne 1) {
            throw 'Settings navigation item is not in FooterMenuItems.'
        }
        if (@($window.SelectNodes("//*[local-name()='NavigationView.Content']/*[local-name()='ContentControl' and @Content='{Binding CurrentPage}']")).Count -ne 1) {
            throw 'NavigationView.Content must host CurrentPage.'
        }
        foreach ($viewModel in @('HomeViewModel', 'ActivityViewModel', 'SettingsViewModel')) {
            if (@($window.SelectNodes("//*[local-name()='DataTemplate' and contains(@DataType, '$viewModel')]")).Count -ne 1) {
                throw "Navigation DataTemplate missing: $viewModel"
            }
        }
        $adapter = Get-Content -Raw -LiteralPath (Join-Path $Output 'MainWindow.xaml.cs')
        $shell = Get-Content -Raw -LiteralPath (Join-Path $Output 'ViewModels/ShellViewModel.cs')
        $activity = Get-Content -Raw -LiteralPath (Join-Path $Output 'ViewModels/ActivityViewModel.cs')
        Assert-TextContains $adapter 'InvokedItemContainer\.Tag' 'navigation container route adapter'
        Assert-TextContains $adapter 'NavigateCommand\.Execute' 'navigation command forwarding'
        Assert-TextContains $adapter 'TogglePaneCommand\.Execute' 'navigation title bar pane toggle forwarding'
        Assert-TextContains $adapter 'AppNavigation\.SelectedItem\s*=' 'navigation selected route synchronization'
        Assert-TextContains $adapter 'AppNavigation\.SelectFooterMenuItem\(SettingsNavigationItem\)' 'navigation footer route synchronization'
        Assert-TextContains $adapter 'PART_MinimizeButton' 'navigation caption minimize part hookup'
        Assert-TextContains $adapter 'minimizeButton\.CommandTarget\s*=\s*this' 'navigation caption command owner target'
        Assert-TextContains $shell 'IRelayCommand<string> NavigateCommand' 'typed navigation command'
        Assert-TextContains $shell 'IRelayCommand TogglePaneCommand' 'navigation pane toggle command'
        Assert-TextContains $shell 'IRelayCommand GoBackCommand' 'navigation Back command'
        Assert-TextContains $shell 'Stack<string> _routeHistory' 'navigation route history'
        Assert-TextContains $shell 'GoBackCommand\.NotifyCanExecuteChanged\(\)' 'navigation Back enabled-state refresh'
        Assert-TextContains $shell 'public string CurrentRoute' 'navigation observable selected route'
        Assert-TextContains $shell 'IsPaneOpen\s*=\s*!_settingsPage\.UseCompactNavigation' 'settings preference controls navigation pane'
        Assert-TextContains $shell '_settingsPage\.UseCompactNavigation\s*=\s*!value' 'manual pane toggles update settings preference'
        Assert-TextContains $shell 'ObservableObject CurrentPage' 'observable current page'
        Assert-TextContains $shell '_currentRoute' 'same-route state'
        foreach ($route in @('home', 'activity', 'settings')) {
            Assert-TextContains $shell ('"' + $route + '"') "navigation mapping $route"
        }
        Assert-TextContains $activity 'RecordAction' 'activity sample action'
        Assert-TextContains $activity 'ActionCount' 'activity sample counter'
        [xml]$activityView = Get-Content -Raw -LiteralPath (Join-Path $Output 'Views/ActivityView.xaml')
        $accentActions = @($activityView.SelectNodes('//*[local-name()="Button" and @Appearance="Accent" and @Command="{Binding RecordActionCommand}"]'))
        if ($accentActions.Count -ne 1 -or $accentActions[0].HasAttribute('Width') -or $accentActions[0].HasAttribute('Height')) {
            throw 'Activity action must use a naturally sized accent button.'
        }
    } elseif ($ShortName -eq 'fluence-settings') {
        [xml]$window = Get-Content -Raw -LiteralPath (Join-Path $Output 'MainWindow.xaml')
        $titleBar = @($window.SelectNodes('//*[local-name()="FluenceWindow.TitleBar"]/*[local-name()="TitleBar"]'))
        if ($titleBar.Count -ne 1 -or $titleBar[0].GetAttribute('Title') -ne 'Contoso.Sample settings' -or
            $titleBar[0].HasAttribute('Subtitle') -or
            $titleBar[0].GetAttribute('Margin') -ne '4,0,0,0' -or
            $titleBar[0].GetAttribute('IsPaneToggleButtonVisible') -ne 'True' -or
            $titleBar[0].GetAttribute('IsBackButtonVisible') -ne 'True' -or
            $titleBar[0].GetAttribute('PaneToggleRequested') -ne 'AppTitleBar_PaneToggleRequested' -or
            $titleBar[0].HasAttribute('BackRequested') -or
            @($titleBar[0].SelectNodes('./*[local-name()="TitleBar.Icon"]')).Count -ne 0 -or
            $window.DocumentElement.GetAttribute('ExtendsContentIntoTitleBar') -ne 'True') {
            throw 'Settings Back-first 12-DIP title bar, absent icon/subtitle, or pane toggle contract failed.'
        }
        $navigation = @($window.SelectNodes('//*[local-name()="NavigationView" and @*[local-name()="Name" and .="CategoryNavigation"]]'))
        if ($navigation.Count -ne 1 -or $navigation[0].GetAttribute('PaneDisplayMode') -ne 'LeftCompact' -or
            $navigation[0].GetAttribute('IsBackButtonVisible') -ne 'False' -or
            $navigation[0].GetAttribute('IsPaneToggleButtonVisible') -ne 'False' -or
            $navigation[0].GetAttribute('IsPaneOpen') -ne 'True' -or
            $navigation[0].GetAttribute('Margin') -ne '10,0,0,0') {
            throw 'Settings compact-capable pane or 10-DIP navigation inset contract failed.'
        }
        if (Test-Path -LiteralPath (Join-Path $Output 'Resources/SettingsArtwork.xaml') -PathType Leaf) {
            throw 'Settings title-bar artwork dictionary must be absent.'
        }
        if ($appXaml -match 'SettingsArtwork|SettingsAppArtwork') { throw 'Settings app must not register title-bar artwork.' }
        Assert-TextContains $appXaml 'Resources/SettingRowStyles\.xaml' 'settings row styles registration'
        foreach ($category in @('general', 'appearance', 'notifications', 'about')) {
            $item = @($window.SelectNodes("//*[local-name()='NavigationViewItem' and @Tag='$category']"))
            $name = $category.Substring(0, 1).ToUpperInvariant() + $category.Substring(1)
            if ($item.Count -ne 1 -or
                @($item[0].SelectNodes(".//*[local-name()='SettingsCategoryImage' and @Category='$name' and @Width='20' and @Height='20']")).Count -ne 1) {
                throw "Settings category image contract failed: $category"
            }
        }
        $paneStyle = @($navigation[0].SelectNodes('.//*[local-name()="Style" and contains(@TargetType,"NavigationViewItem")]'))
        if ($paneStyle.Count -ne 1 -or
            @($paneStyle[0].SelectNodes('./*[local-name()="Setter" and @Property="Margin" and @Value="0,0,30,0"]')).Count -ne 1 -or
            @($paneStyle[0].SelectNodes('.//*[local-name()="DataTrigger" and @Value="False"]/*[local-name()="Setter" and @Property="Margin" and @Value="0"]')).Count -ne 1) {
            throw 'Settings expanded and compact item inset contract failed.'
        }
        $pageHost = @($window.SelectNodes('//*[local-name()="NavigationView.Content"]/*[local-name()="ContentControl" and @*[local-name()="Name" and .="PageHost"]]'))
        if ($pageHost.Count -ne 1 -or
            @($pageHost[0].SelectNodes('./*[local-name()="ContentControl.Style"]/*[local-name()="Style"]/*[local-name()="Setter" and @Property="Margin" and @Value="-28,0,0,0"]')).Count -ne 1 -or
            @($pageHost[0].SelectNodes('.//*[local-name()="DataTrigger" and @Value="False"]/*[local-name()="Setter" and @Property="Margin" and @Value="0"]')).Count -ne 1) {
            throw 'Settings expanded and compact PageHost margin contract failed.'
        }
        $adapter = Get-Content -Raw -LiteralPath (Join-Path $Output 'MainWindow.xaml.cs')
        Assert-TextContains $adapter 'InvokedItemContainer\.Tag' 'settings category adapter'
        Assert-TextContains $adapter 'Dictionary<string, UserControl>' 'cached settings pages'
        Assert-TextContains $adapter 'Stack<string> _categoryHistory' 'settings category Back history'
        Assert-TextContains $adapter '_categoryHistory\.Push\(_currentCategory\)' 'settings history push on category visit'
        Assert-TextContains $adapter 'ShowCategory\(_categoryHistory\.Pop\(\)\)' 'settings Back consumes history without re-push'
        Assert-TextContains $adapter 'AppTitleBar\.BackCommand\s*=\s*_backCommand' 'settings Back command wiring'
        Assert-TextContains $adapter 'CanExecute\(object\? parameter\)\s*=>\s*_owner\._categoryHistory\.Count\s*>\s*0' 'settings Back history enabled state'
        Assert-TextContains $adapter '_backCommand\.RaiseCanExecuteChanged\(\)' 'settings Back state refresh'
        Assert-TextContains $adapter 'CategoryNavigation\.SelectedItem\s*=\s*_categoryItems\[category\]' 'settings selected category synchronization'
        Assert-TextContains $adapter 'CategoryNavigation\.IsPaneOpen\s*=\s*!CategoryNavigation\.IsPaneOpen' 'settings pane toggle'
        if ($adapter -match 'AppTitleBar_BackRequested|SettingsAppArtwork|AppTitleBar\.Subtitle') {
            throw 'Settings adapter must not duplicate Back handling, category subtitle, or removed artwork.'
        }
        Assert-TextContains $adapter 'SetCaptionCommandTarget\("PART_MinimizeButton"\)' 'settings caption minimize hookup'
        Assert-TextContains $adapter 'button\.CommandTarget\s*=\s*this' 'settings caption command owner target'
        [xml]$styles = Get-Content -Raw -LiteralPath (Join-Path $Output 'Resources/SettingRowStyles.xaml')
        foreach ($spec in @(@('SettingRowCardStyle', 'MinHeight', '68'), @('SettingRowCardStyle', 'Padding', '18,12'),
            @('SettingRowCardStyle', 'CornerRadius', '5'), @('SettingRowIconStyle', 'Width', '20'),
            @('SettingRowIconStyle', 'Height', '20'), @('SettingRowIconStyle', 'IconFontSize', '20'),
            @('SettingRowIconStyle', 'Foreground', '{DynamicResource TextFillColorPrimaryBrush}'))) {
            $style = @($styles.SelectNodes("//*[local-name()='Style' and @*[local-name()='Key' and .='$($spec[0])']]"))
            if ($style.Count -ne 1 -or @($style[0].SelectNodes("./*[local-name()='Setter' and @Property='$($spec[1])' and @Value='$($spec[2])']")).Count -ne 1) {
                throw "Setting row style contract failed: $($spec -join '/')"
            }
        }
        Assert-TextContains $styles.OuterXml 'DynamicResource' 'theme-aware setting row styles'
        $iconCount = 0
        foreach ($pageName in @('General', 'Appearance', 'Notifications', 'About')) {
            $pagePath = Join-Path $Output "Pages/$($pageName)Page.xaml"
            [xml]$page = Get-Content -Raw -LiteralPath $pagePath
            if (@($page.SelectNodes('//*[local-name()="SmoothScrollViewer"]')).Count -eq 0 -or
                @($page.SelectNodes('//*[local-name()="StackPanel" and @MaxWidth="1000" and @Margin="28,16,28,24"]')).Count -ne 1) {
                throw "Settings page scroll/width/outer spacing failed: $pagePath"
            }
            $markup = Get-Content -Raw -LiteralPath $pagePath
            Assert-TextContains $markup 'Margin="0,24,0,0"' "settings section spacing $pageName"
            Assert-TextContains $markup 'Margin="0,8,0,0"' "settings row spacing $pageName"
            if (@($page.SelectNodes('//*[local-name()="Image"]')).Count -gt 0) { throw "Settings page row icons must be monochrome FontIcons: $pagePath" }
            $cards = @($page.SelectNodes('//*[local-name()="Border" and @Style="{StaticResource SettingRowCardStyle}"]'))
            foreach ($card in $cards) {
                $glyph = @($card.SelectNodes('.//*[local-name()="FontIcon" and @Style="{StaticResource SettingRowIconStyle}" and @Glyph]'))
                if (@($card.SelectNodes('.//*[local-name()="Grid"]')).Count -gt 0 -and $glyph.Count -ne 1) {
                    throw "Settings row needs one theme-aware glyph: $pagePath"
                }
                $iconCount += $glyph.Count
            }
            if (@($page.SelectNodes('//*[local-name()="Border" and @Margin="0,5,0,0" and @Style="{StaticResource SettingRowCardStyle}"]')).Count -eq 0 -and $pageName -ne 'Appearance') {
                throw "Settings stacked rows must have a 5-DIP gap: $pagePath"
            }
            foreach ($grid in $page.SelectNodes('//*[local-name()="Border" and @Style="{StaticResource SettingRowCardStyle}"]/*[local-name()="Grid"]')) {
                $spacers = @($grid.SelectNodes('./*[local-name()="Grid.ColumnDefinitions"]/*[local-name()="ColumnDefinition" and @Width="20"]'))
                if ($spacers.Count -lt 1) { throw "Settings row glyph/text gap must be 20 DIP: $pagePath" }
            }
            $actions = 0
            foreach ($card in $cards) {
                $interactive = @($card.SelectNodes('.//*[local-name()="ToggleSwitch" or local-name()="ComboBox" or local-name()="Button"]'))
                if ($interactive.Count -eq 0) { continue }
                $actions++
                if (@($card.SelectNodes('.//*[local-name()="TextBlock" and @Style="{StaticResource SettingRowTitleStyle}"]')).Count -eq 0 -or
                    @($card.SelectNodes('.//*[local-name()="TextBlock" and @Style="{StaticResource SettingRowDescriptionStyle}"]')).Count -eq 0 -or
                    @($card.SelectNodes('.//*[local-name()="FontIcon" and @Style="{StaticResource SettingRowIconStyle}"]')).Count -ne 1 -or
                    @($interactive | Where-Object { $_.Attributes['AutomationProperties.Name'] }).Count -ne $interactive.Count) {
                    throw "Interactive setting row lacks vector art, label, description, or named action: $pagePath"
                }
            }
            if ($actions -eq 0) { throw "Settings page has no interactive example: $pagePath" }
        }
        if ($iconCount -ne 10) { throw "Settings pages need exactly 10 monochrome row glyphs; found $iconCount" }
        Assert-SettingsAssets $Output
        $resources = @($project.SelectNodes('//*[local-name()="Resource" and @Include="Assets/SettingsIcons/*.png"]'))
        if ($resources.Count -ne 1) { throw 'Settings PNGs must be WPF Resource items.' }
        foreach ($notice in @('ARTWORK-NOTICE.md', 'MICROSOFT-FLUENT-LICENSE.txt')) {
            $content = @($project.SelectNodes("//*[local-name()='Content' and @Include='Assets/SettingsIcons/$notice' and @CopyToOutputDirectory='PreserveNewest']"))
            if ($content.Count -ne 1) { throw "Settings attribution must copy to binary output: $notice" }
        }
        $imageControl = Get-Content -Raw -LiteralPath (Join-Path $Output 'Controls/SettingsCategoryImage.cs')
        Assert-TextContains $imageControl 'OnDpiChanged' 'Settings DPI image refresh'
        Assert-TextContains $imageControl 'AvailableScales\s*=\s*\[100, 125, 150, 200, 400\]' 'Settings DPI image scale choices'
        Assert-TextContains $imageControl 'pack://application:,,,/' 'Settings application-relative image loading'
        Assert-TextContains $imageControl 'bitmap\.Freeze\(\)' 'Settings frozen image cache'
        $general = Get-Content -Raw -LiteralPath (Join-Path $Output 'Pages/GeneralPage.xaml')
        $generalCode = Get-Content -Raw -LiteralPath (Join-Path $Output 'Pages/GeneralPage.xaml.cs')
        $model = Get-Content -Raw -LiteralPath (Join-Path $Output 'Models/ChoiceOption.cs')
        $readme = Get-Content -Raw -LiteralPath (Join-Path $Output 'README.md')
        Assert-TextContains $general 'DisplayMemberPath="Name"' 'model-backed settings ComboBox'
        Assert-TextContains $generalCode 'new ChoiceOption\(' 'typed settings choices'
        Assert-TextContains $generalCode 'DensityChoice\.ItemsSource' 'settings choices ItemsSource'
        Assert-TextContains $model 'public string Name' 'ChoiceOption name'
        if ($model -match 'ToString\s*\(') { throw 'ChoiceOption must not override ToString.' }
        Assert-TextContains $readme '(?i)reset when you restart' 'settings reset semantics'
        Assert-TextContains $readme '(?i)do not read or change Windows or OS settings' 'settings OS boundary'
    }
}

function Assert-Package {
    param([string]$Path, [string]$Id, [bool]$IsTemplate)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing packed package: $Path"
    }
    Add-Type -AssemblyName System.IO.Compression
    $zip = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $entries = @($zip.Entries | ForEach-Object FullName)
        $nuspecEntry = $zip.Entries | Where-Object { $_.FullName -eq "$Id.nuspec" } | Select-Object -First 1
        if ($null -eq $nuspecEntry) { throw "Missing .nuspec in $Path" }
        $reader = [System.IO.StreamReader]::new($nuspecEntry.Open())
        try { [xml]$nuspec = $reader.ReadToEnd() } finally { $reader.Dispose() }
        $metadata = $nuspec.package.metadata
        if ($metadata.id -ne $Id -or $metadata.version -ne $version) {
            throw "Package ID/version mismatch in ${Path}: $($metadata.id) $($metadata.version)"
        }
        if (@($nuspec.SelectNodes('//*[local-name()="dependency"]') | Where-Object { $_.GetAttribute('id') -match '(?i)WindowsAppSDK|WindowsAppRuntime' }).Count -gt 0) {
            throw "Windows App SDK package dependency is forbidden: $Path"
        }
        if ($IsTemplate) {
            if (@($metadata.packageTypes.packageType | Where-Object { $_.name -eq 'Template' }).Count -ne 1) {
                throw "Missing Template package type in $Path"
            }
            if (@($nuspec.SelectNodes('//*[local-name()="dependency"]')).Count -ne 0) {
                throw "Template package has dependency entries: $Path"
            }
            $bad = @($entries | Where-Object { $_ -match '(^|/)(bin|obj)(/|$)|(^|/)packages\.lock\.json$|(^|/)Directory\.(Build|Packages)\.props$|(^|/)LICENSE$|(^|/)BannedSymbols\.txt$|\.ico$|\.pri$|WindowsAppRuntime.*\.dll$|WindowsAppSDK.*\.dll$|^lib/|^analyzers/|^build/' })
            if ($bad.Count -gt 0) { throw "Template package has unwanted entries: $($bad -join ', ')" }
            $png = @($entries | Where-Object { $_ -match '(?i)\.png$' })
            if ($png.Count -ne 20 -or @($png | Where-Object { $_ -notmatch '^content/content/settings-shell/Assets/SettingsIcons/(General|Appearance|Notifications|About)-scale-(100|125|150|200|400)\.png$' }).Count -gt 0) {
                throw 'Template package must contain only the 20 intended Settings category PNGs.'
            }
            foreach ($shortName in $names.Keys) {
                $content = $names[$shortName]
                if ($entries -notcontains "content/content/$content/.template.config/template.json") {
                    throw "Missing hidden metadata for $shortName in $Path"
                }
                if ($entries -notcontains "content/content/$content/.gitignore") {
                    throw "Missing hidden .gitignore for $shortName in $Path"
                }
                $contentRoot = Join-Path $repo "templates/Fluence.Wpf.Templates/content/$content"
                foreach ($sourceFile in Get-ChildItem -LiteralPath $contentRoot -Recurse -File) {
                    $relative = [System.IO.Path]::GetRelativePath($contentRoot, $sourceFile.FullName).Replace('\', '/')
                    if ($relative -match '(^|/)(bin|obj)(/|$)|(^|/)packages\.lock\.json$|\.lock\.json$|\.user$') { continue }
                    $entryName = "content/content/$content/$relative"
                    $entry = $zip.GetEntry($entryName)
                    if ($null -eq $entry) { throw "Template package omitted source file: $entryName" }
                    $buffer = [System.IO.MemoryStream]::new()
                    $stream = $entry.Open()
                    try { $stream.CopyTo($buffer) } finally { $stream.Dispose() }
                    try {
                        $packedHash = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($buffer.ToArray()))
                    } finally { $buffer.Dispose() }
                    $sourceHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $sourceFile.FullName).Hash
                    if ($packedHash -cne $sourceHash) { throw "Template package differs from source: $entryName" }
                }
            }
            $allowedContent = @($names.Keys | ForEach-Object { "content/content/$($names[$_])/" })
            foreach ($entryName in $entries | Where-Object { $_ -like 'content/content/*' -and -not $_.EndsWith('/') }) {
                if (@($allowedContent | Where-Object { $entryName.StartsWith($_, [StringComparison]::Ordinal) }).Count -ne 1) {
                    throw "Unexpected template content path: $entryName"
                }
                $relative = $entryName.Substring('content/content/'.Length)
                $sourcePath = Join-Path (Join-Path $repo 'templates/Fluence.Wpf.Templates/content') $relative
                if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) { throw "Untracked package content: $entryName" }
            }
            foreach ($entry in $zip.Entries | Where-Object { $_.FullName -match '^content/content/.+\.(cs|csproj|xaml|md|txt|json)$' }) {
                $reader = [System.IO.StreamReader]::new($entry.Open())
                try { $packedText = $reader.ReadToEnd() } finally { $reader.Dispose() }
                if ($packedText -match '(?i)[A-Z]:[\\/]Users[\\/]|[\\/]Users[\\/]sintaxasn[\\/]') {
                    throw "Template package contains a machine-specific user path: $($entry.FullName)"
                }
            }
        }
    } finally {
        $zip.Dispose()
    }
}

try {
    if (-not (Test-Path -LiteralPath $run)) {
        New-Item -ItemType Directory -Path $run | Out-Null
    } else {
        throw "Run directory already exists: $run"
    }
    New-Item -ItemType Directory -Path $feed, $packages, $generated, $hive, $logs | Out-Null
    if ($InspectGeneratedPath) {
        $fixture = (Resolve-Path -LiteralPath $InspectGeneratedPath).Path
        Assert-NoStarterBrand $fixture
        $success = $true
        $result.status = 'PASS'
        $result.exitCode = 0
        return
    }
    $escapedFeed = [System.Security.SecurityElement]::Escape($feed)
    $config = '<configuration><packageSources><clear /><add key="local" value="' + $escapedFeed + '" /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources></configuration>'
    [System.IO.File]::WriteAllText($nugetConfig, $config, [System.Text.UTF8Encoding]::new($false))
    $fix = (& git -C $repo rev-parse b3879f16d9e560b106313e7263871e5f949b70a0).Trim()
    if ($LASTEXITCODE -ne 0 -or $fix -ne 'b3879f16d9e560b106313e7263871e5f949b70a0') { throw 'Phase 1 fix commit is unavailable.' }
    [xml]$rootProps = Get-Content -Raw (Join-Path $repo 'Directory.Build.props')
    $rootVersion = @($rootProps.Project.PropertyGroup.VersionPrefix | Where-Object { $_ })
    if ($rootVersion.Count -ne 1 -or $rootVersion[0] -ne '0.9.5') { throw 'Root VersionPrefix does not match the expected 0.9.5 version.' }
    if ($version -ne "0.9.4-starters-dev.g$($fix.Substring(0, 8)).r8") { throw 'Candidate version does not match the three-title-bar local stamp.' }
    if ($version -eq '0.9.4') { throw 'The pre-fix 0.9.4 package is ineligible.' }
    Assert-WorkflowContract
    foreach ($shortName in $selectedTemplates) {
        $content = Join-Path $repo ("templates/Fluence.Wpf.Templates/content/" + $names[$shortName])
        $metadataPath = Join-Path $content '.template.config/template.json'
        if (-not (Test-Path -LiteralPath $metadataPath)) {
            throw "Missing template metadata for ${shortName}: $content"
        }
        $metadata = Get-Content -Raw -LiteralPath $metadataPath | ConvertFrom-Json
        if ($metadata.shortName -ne $shortName -or $metadata.identity -ne $identities[$shortName] -or
            $metadata.sourceName -ne 'FluenceStarterApp' -or $metadata.preferNameDirectory -ne $true -or
            $metadata.tags.type -ne 'project' -or $metadata.tags.language -ne 'C#') {
            throw "Template metadata contract failed: $metadataPath"
        }
        $project = Join-Path $content 'FluenceStarterApp.csproj'
        if (-not (Test-Path -LiteralPath $project)) { throw "Missing template project for ${shortName}: $project" }
        [xml]$projectXml = Get-Content -Raw $project
        $reference = @($projectXml.Project.ItemGroup.PackageReference | Where-Object { $_.Include -eq 'Fluence.Wpf' })
        if ($reference.Count -ne 1 -or $reference[0].Version -ne $version) {
            throw "Fluence.Wpf reference in $project must be exactly $version"
        }
        Assert-NoAppSdkComponents $content
    }
    $templateProject = Join-Path $repo 'templates/Fluence.Wpf.Templates/Fluence.Wpf.Templates.csproj'
    if (-not (Test-Path -LiteralPath $templateProject)) { throw "Missing template package project: $templateProject" }

    $libraryProject = Join-Path $repo 'Fluence.Wpf/Fluence.Wpf.csproj'
    [void](Invoke-Dotnet 'pack-library' @('pack', $libraryProject, '-c', 'Release', '-o', $feed, "-p:PackageVersion=$version"))
    [void](Invoke-Dotnet 'pack-templates' @('pack', $templateProject, '-c', 'Release', '-o', $feed, "-p:PackageVersion=$version", '-p:RestorePackagesWithLockFile=false'))
    $libraryPackage = Join-Path $feed "Fluence.Wpf.$version.nupkg"
    $templatePackage = Join-Path $feed "Fluence.Wpf.Templates.$version.nupkg"
    Assert-Package $libraryPackage 'Fluence.Wpf' $false
    Assert-Package $templatePackage 'Fluence.Wpf.Templates' $true
    $result.packageFiles = @($libraryPackage, $templatePackage)

    [void](Invoke-Dotnet 'new-install' @('new', 'install', $templatePackage, '--debug:custom-hive', $hive))
    $installed = $true
    $listing = Invoke-Dotnet 'new-list' @('new', 'list', 'fluence', '--debug:custom-hive', $hive)
    foreach ($shortName in $selectedTemplates) {
        if ($listing -notmatch [regex]::Escape($shortName)) { throw "Template short name missing from isolated hive: $shortName" }
    }

    foreach ($shortName in $selectedTemplates) {
        $output = Join-Path $generated $names[$shortName]
        [void](Invoke-Dotnet "new-$shortName" @('new', $shortName, '-n', 'Contoso.Sample', '-o', $output, '--debug:custom-hive', $hive))
        $project = Join-Path $output 'Contoso.Sample.csproj'
        if (-not (Test-Path -LiteralPath $project)) { throw "Generated project missing: $project" }
        $files = @(Get-ChildItem -LiteralPath $output -Recurse -File)
        $bad = @($files | Where-Object { $_.FullName -match '(^|[\\/])(bin|obj)([\\/]|$)|packages\.lock\.json$|Directory\.(Build|Packages)\.props$' })
        if ($bad.Count -gt 0) { throw "Generated output contains build/config pollution: $($bad.FullName -join ', ')" }
        $source = ($files | Where-Object Extension -in @('.csproj', '.cs', '.xaml') | ForEach-Object { Get-Content -Raw -LiteralPath $_.FullName }) -join "`n"
        if ($source.Contains('FluenceStarterApp')) { throw "Source token was not replaced in $output" }
        if ($source -match 'ProjectReference|ApplicationIcon|FluenceWindow\.DefaultIcon|Window\.Icon') { throw "Generated project contains forbidden source reference or branded icon: $output" }
        if ($source -notmatch 'Contoso\.Sample\.MainWindow' -or $source -notmatch 'namespace Contoso\.Sample') { throw "Generated XAML class or namespace was not renamed: $output" }
        Assert-GeneratedBytes $output $shortName
        Assert-NoStarterBrand $output
        Assert-GeneratedStructure $output $shortName
        [xml]$generatedXml = Get-Content -Raw $project
        $reference = @($generatedXml.Project.ItemGroup.PackageReference | Where-Object { $_.Include -eq 'Fluence.Wpf' })
        if ($reference.Count -ne 1 -or $reference[0].Version -ne $version) { throw "Generated package version mismatch: $project" }
        [void](Invoke-Dotnet "restore-$shortName" @('restore', $project, '--packages', $packages, '--configfile', $nugetConfig))
        [void](Invoke-Dotnet "build-$shortName" @('build', $project, '--no-restore', '-c', 'Release'))
        if ($shortName -eq 'fluence-settings') { Assert-SettingsAssets $output -CheckBuildOutput }
        Assert-NoAppSdkComponents (Join-Path $output 'bin/Release/net10.0-windows10.0.26100.0')
        $executable = Join-Path $output 'bin/Release/net10.0-windows10.0.26100.0/Contoso.Sample.exe'
        if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) { throw "Generated executable missing after build: $executable" }
        $result.checkedTemplates += [ordered]@{
            shortName = $shortName
            project = $project
            executable = $executable
        }
    }
    $actualNames = @($result.checkedTemplates | ForEach-Object { $_.shortName } | Sort-Object)
    $expectedNames = @($selectedTemplates | Sort-Object)
    if ($actualNames.Count -ne $expectedNames.Count -or ($actualNames -join '|') -ne ($expectedNames -join '|')) {
        throw "Generated build coverage mismatch: expected $($expectedNames -join ', '); got $($actualNames -join ', ')"
    }
    $success = $true
    $result.status = 'PASS'
    $result.exitCode = 0
} catch {
    $result.error = $_.Exception.Message
} finally {
    if ($installed) {
        try { [void](Invoke-Dotnet 'new-uninstall' @('new', 'uninstall', 'Fluence.Wpf.Templates', '--debug:custom-hive', $hive)) }
        catch { $result.error = (($result.error, "Uninstall failed: $($_.Exception.Message)" | Where-Object { $_ }) -join '; '); $success = $false; $result.status = 'FAIL'; $result.exitCode = 1 }
    }
    $expectedParent = [System.IO.Path]::GetFullPath($tempRoot).TrimEnd([System.IO.Path]::DirectorySeparatorChar)
    $actualParent = [System.IO.Path]::GetDirectoryName([System.IO.Path]::GetFullPath($run).TrimEnd([System.IO.Path]::DirectorySeparatorChar))
    if ($success -and -not $KeepArtifacts -and $actualParent -eq $expectedParent -and (Split-Path $run -Leaf) -match '^fluence-consumer-template-check-[0-9a-f]{32}$') {
        try {
            Remove-Item -LiteralPath $run -Recurse -Force
            $result.retained = $false
        } catch {
            $result.error = "Cleanup failed: $($_.Exception.Message)"
            $success = $false
            $result.status = 'FAIL'
            $result.exitCode = 1
        }
    }
    $json = $result | ConvertTo-Json -Depth 5
    if ($ReportPath) { [System.IO.File]::WriteAllText([System.IO.Path]::GetFullPath($ReportPath), $json + "`n", [System.Text.UTF8Encoding]::new($true)) }
    Write-Output $json
    if (-not $success) { exit 1 }
}
