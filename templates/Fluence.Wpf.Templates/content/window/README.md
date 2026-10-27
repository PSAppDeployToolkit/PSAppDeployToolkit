# FluenceStarterApp

A small WPF window using Fluence.Wpf controls. Build and run it, enter a name, choose an option, toggle notifications, and select **Update status**.

The project starts with the Windows theme and system accent. Its controls update with theme resources. Change the app's title and content to fit your product.

## Build and run with the local package

The generated project references the local Fluence.Wpf candidate used by the starter package. In PowerShell, enter the path to the matching local NuGet feed that contains that exact library package:

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

The config contains only the supplied local feed and nuget.org. Keep the feed and package cache available for later restores. The generated `.gitignore` excludes the project-local `.nuget-packages/` cache, `bin/`, `obj/`, and the temporary `NuGet.Config` containing your machine's feed path. Add your own durable NuGet configuration when you switch to an approved published package.

## Add your own icon

The title bar starts with a generic WPF vector 12 DIP from the client edge, followed by the app name. This simple starter has no Back or pane toggle. It leaves the application and window icons unset. To use your brand, add an `.ico` file to this project, set `<ApplicationIcon>YourIcon.ico</ApplicationIcon>` in the project file, and assign `Icon="YourIcon.ico"` on `MainWindow` if you want a window icon.
