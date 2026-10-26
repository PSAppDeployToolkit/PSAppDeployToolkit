# SystemThemeWatcher

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public static class SystemThemeWatcher
```

Subscribes a `Window` to settings change notifications, coalescing pending theme and accent refreshes.

**Remarks:** Used with [ApplicationThemeManager](ApplicationThemeManager.md) to refresh resources when the user changes Windows light/dark or contrast while the app runs.

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/SystemThemeWatcher.cs)

## Methods

<a id="api-567a7985924c"></a>

### UnWatch

```csharp
public static void UnWatch(Window window)
```

Stops watching the specified window and removes Win32 hooks.

**Parameter `window`:** The window previously passed to [Watch](SystemThemeWatcher.md#api-5771729ced60).

**Exception `System.ArgumentNullException`:** `window` is `null`.

<a id="api-5771729ced60"></a>

### Watch

```csharp
public static void Watch(Window window)
```

Begins watching the specified window for system theme and accent changes.

**Parameter `window`:** The WPF window to associate with the watcher. Must not be `null`.

**Exception `System.ArgumentNullException`:** `window` is `null`.
