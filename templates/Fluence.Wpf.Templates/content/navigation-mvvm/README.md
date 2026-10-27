# Fluence NavigationView MVVM starter

This project gives you a small WPF application shell with Fluence `NavigationView` and CommunityToolkit.Mvvm. Home, Activity, and Settings each have a cached view model, so returning to a page keeps its in-memory state.

## Build and run

```powershell
$feed = (Resolve-Path -LiteralPath (Read-Host 'Path to the local Fluence.Wpf NuGet feed')).Path
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
```

The project targets `net10.0-windows10.0.26100.0`. The config contains only the matching local feed with the referenced `Fluence.Wpf` candidate package and nuget.org for the public toolkit dependency. The project references both packages directly and copies no library project files or repository build settings. The build and run commands reuse the explicit restore result. The generated `.gitignore` excludes `.nuget-packages/`, `bin/`, `obj/`, and the temporary `NuGet.Config` containing your machine's feed path.

## Navigation and MVVM

`MainWindow` forwards the invoked navigation item's `Tag` to `ShellViewModel.NavigateCommand`. The shell maps `home`, `activity`, and `settings` to stable view-model instances, while XAML data templates select each `UserControl` view. The Activity page includes a sample command, counter, and observable activity list.

The title bar starts with a generic vector at 12 DIP, followed by an always-visible Back button and the pane toggle. Back is disabled until a different page is visited. It retraces page visits while retaining each cached page's state. The hamburger switches between the expanded pane and a compact icon rail. The Settings page's compact-navigation option controls the same pane state for this session. Activity has a normal-size accent action button. Settings are in-memory examples for this app session. They do not read or change Windows settings.

## App identity

The title bar uses generic sample vector artwork and keeps the app name without a page subtitle. The starter does not set an application or window icon and contains no Fluence brand icon. Add your own `.ico` as the application icon and set your own window icon if your app needs one.
