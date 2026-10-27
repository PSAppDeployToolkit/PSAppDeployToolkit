# Control gallery

The gallery is a runnable WPF application with examples of Fluence.Wpf controls, themes, accents, and window chrome. Its pages show live controls alongside example source. It targets `net472` and `net10.0-windows10.0.26100.0`.

From the repository root, run a specific target:

```powershell
dotnet run --project Fluence.Wpf.Demo/Fluence.Wpf.Demo.csproj -c Debug -f net10.0-windows10.0.26100.0
```

Use `-f net472` to run the .NET Framework build. In `App.xaml.cs`, `OnStartup` applies the Fluence theme and system accent before creating `MainWindow`. `MainWindow` hosts the navigation shell; examples live under `Pages/`, with shared sample presentation in `Controls/`.

For the setup behind the example, see the [first WPF application tutorial](https://fluencewpf.com/docs/tutorials/first-wpf-app). Browse the [control catalog](https://fluencewpf.com/docs/controls), [theming guide](https://fluencewpf.com/docs/theming), or full [documentation index](https://fluencewpf.com/docs) for consumer guidance.

## Visual examples

The captures show the gallery home, button samples, and status controls in both available themes.

| Gallery home | Buttons | Status controls |
| --- | --- | --- |
| ![Gallery home in light mode](https://raw.githubusercontent.com/sintaxasn/Fluence.Wpf.Website/main/docs/screenshots/gallery-home-light.png)<br>Light | ![Button controls in light mode](https://raw.githubusercontent.com/sintaxasn/Fluence.Wpf.Website/main/docs/screenshots/gallery-buttons-light.png)<br>Light | ![Status controls in light mode](https://raw.githubusercontent.com/sintaxasn/Fluence.Wpf.Website/main/docs/screenshots/gallery-status-light.png)<br>Light |
| ![Gallery home in dark mode](https://raw.githubusercontent.com/sintaxasn/Fluence.Wpf.Website/main/docs/screenshots/gallery-home-dark.png)<br>Dark | ![Button controls in dark mode](https://raw.githubusercontent.com/sintaxasn/Fluence.Wpf.Website/main/docs/screenshots/gallery-buttons-dark.png)<br>Dark | ![Status controls in dark mode](https://raw.githubusercontent.com/sintaxasn/Fluence.Wpf.Website/main/docs/screenshots/gallery-status-dark.png)<br>Dark |
