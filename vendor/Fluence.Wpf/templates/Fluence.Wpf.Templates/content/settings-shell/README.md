# FluenceStarterApp settings shell

This project is a small WPF settings layout built with Fluence.Wpf. Run it, choose General, Appearance, Notifications, or About on the left, and try the controls in each category.

## Build and run

Install the .NET 10 SDK on Windows. Open PowerShell in the generated project directory. Enter the path to the local feed that contains the exact Fluence.Wpf candidate version in this project file (the repository template checker reports its feed path), then create an explicit NuGet config, restore, build, and run:

    $feed = Read-Host 'Path to the local candidate NuGet feed'
    if (-not (Test-Path -LiteralPath $feed -PathType Container)) { throw 'Local feed directory not found.' }
    $feed = (Resolve-Path -LiteralPath $feed).Path
    $escapedFeed = [System.Security.SecurityElement]::Escape($feed)
    $nugetConfig = Join-Path (Get-Location) 'NuGet.Config'
    $config = @"
    <configuration>
      <packageSources>
        <clear />
        <add key="fluence-local" value="$escapedFeed" />
        <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
      </packageSources>
    </configuration>
    "@
    [System.IO.File]::WriteAllText($nugetConfig, $config, [System.Text.UTF8Encoding]::new($false))
    $project = (Get-ChildItem -Filter '*.csproj' -File | Select-Object -First 1).FullName
    $packages = Join-Path (Get-Location) '.nuget-packages'
    dotnet restore $project --packages $packages --configfile $nugetConfig
    dotnet build $project -c Release --no-restore
    dotnet run --project $project -c Release --no-build

Keep the local feed and restored package folder available for this session. The generated `.gitignore` excludes `.nuget-packages/`, `bin/`, `obj/`, and the temporary `NuGet.Config` containing your machine's feed path. The template currently references a local validation candidate. Replace that version with an approved published version when one becomes available.

## Make it yours

- Replace the sample setting rows and their code-behind actions with your app's values and behavior.
- The example controls update only local, in-memory state. Values stay while you switch categories in this window and reset when you restart. They do not read or change Windows or OS settings.
- The Appearance choice changes only this app's Fluence theme. Auto follows the Windows app theme.
- The left categories are cached UserControls. The always-visible Back button is disabled until category history exists, then retraces visits while keeping page state. The pane toggle switches between the expanded categories and a compact icon rail. Add a category in MainWindow.xaml and its view in MainWindow.xaml.cs.
- The title bar starts with Back at 12 DIP from the client edge, followed by the pane toggle and app name. It has no title-bar icon or repeated category subtitle. The four sidebar categories use bundled color PNGs, while all setting rows use monochrome, theme-aware FontIcons. Replace the sample imagery as appropriate for your app.
- `Assets/SettingsIcons/ARTWORK-NOTICE.md` records the source and terms of each category image. General and Appearance use Windows Settings artwork with no explicit redistribution grant found during this template's research. Notifications and About use Microsoft Fluent Emoji derivatives under the included `MICROSOFT-FLUENT-LICENSE.txt`. Both notices copy beside the built app; assess the Windows image rights before redistributing it.
- Category images are WPF resources loaded from this app's assembly. The app has no Windows App SDK package, bootstrap code, runtime component, or PRI reader.
- Resources/SettingRowStyles.xaml holds the page and row styles. Theme colors use Fluence dynamic resources so they follow Light, Dark, and High Contrast.
- This project does not set an application or window icon. Supply your own .ico as ApplicationIcon in the project file and, if desired, set the window Icon to your own asset.

The example ChoiceOption objects drive the General page ComboBox through DisplayMemberPath="Name".
