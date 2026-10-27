# MVVM task manager demo

This .NET 10 WPF example uses Fluence.Wpf with CommunityToolkit.Mvvm. It shows a `FluenceWindow`, task filtering, commands, bindings, and progress state.

Run it from the repository root:

```powershell
dotnet run --project Fluence.Wpf.Demo.Mvvm/Fluence.Wpf.Demo.Mvvm.csproj -c Debug
```

`App.xaml.cs` applies the theme before the window opens. `MainWindow.xaml` declares the UI, and `ViewModels/` contains the task and screen state. `Properties/DesignTimeResources.xaml` supplies design time resources for the XAML designer.

Start with the [first WPF application tutorial](https://fluencewpf.com/docs/tutorials/first-wpf-app) for a smaller example. The [documentation index](https://fluencewpf.com/docs) links controls, themes, and API guidance.
