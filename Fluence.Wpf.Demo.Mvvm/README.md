# Fluence.Wpf.Demo.Mvvm

A small MVVM Task Manager demo for anyone who wants to see Fluence.Wpf controls used without page-level code-behind. It targets `net10.0-windows10.0.26100.0` and uses CommunityToolkit.Mvvm.

## What Lives Here

- `App.xaml.cs` - applies Fluence resources at startup before showing the main window.
- `MainWindow.xaml` - a `FluenceWindow` with task filtering, list content, task input, and progress/status controls.
- `ViewModels/` - `MainViewModel` and `TaskItemViewModel` using `[ObservableProperty]` and `[RelayCommand]`.
- `Converters/EnumToBoolConverter.cs` - radio-button binding support for the `FilterMode` enum.

## Run

From the repository root:

```powershell
dotnet run --project Fluence.Wpf.Demo.Mvvm/Fluence.Wpf.Demo.Mvvm.csproj -c Debug
```

## Maintenance Notes

Keep `App.xaml` free of manual merged dictionaries; `ApplicationThemeManager.Apply(...)` owns the Fluence resource slots. `MainViewModel.Refresh()` intentionally rebuilds `DisplayedTasks` before notifying derived status/progress properties, so avoid adding notification attributes that fire before the collection has been refreshed.

## Design-time preview

`Properties/DesignTimeResources.xaml` merges the library's `DesignTime.Dark.xaml`, so the XAML
designer renders this window in **Dark** (the running app uses `ApplicationTheme.Auto`).
`MainWindow.xaml` uses `d:DataContext="{d:DesignInstance vm:MainViewModel, IsDesignTimeCreatable=True}"`,
so the designer instantiates the real (seeded) `MainViewModel` and shows sample rows.
