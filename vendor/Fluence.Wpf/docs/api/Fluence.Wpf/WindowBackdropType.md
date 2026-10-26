# WindowBackdropType

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public enum WindowBackdropType
```

DWM system backdrop material for a top-level window, used with [SystemBackdropType](../Fluence.Wpf.Controls/FluenceWindow.md#api-3e78d59cb2ab). The name follows the .NET 10 WPF Fluent theme, which is this library's authority for window chrome; [Auto](WindowBackdropType.md#api-774eb5bc74f3) is a Fluence addition meaning "the best material this OS supports".

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/WindowBackdropType.cs)

## Values

<a id="api-94b63338af4c"></a>

### Acrylic

```csharp
Acrylic = 3
```

Acrylic blur.

<a id="api-774eb5bc74f3"></a>

### Auto

```csharp
Auto = 1
```

Let the library pick the best supported backdrop for the OS version.

<a id="api-feba43e0152c"></a>

### Mica

```csharp
Mica = 2
```

Mica (layered tint over wallpaper).

<a id="api-6a02046ff591"></a>

### None

```csharp
None = 0
```

No Mica/Acrylic; standard solid backdrop.

<a id="api-fa51d2cc9773"></a>

### Tabbed

```csharp
Tabbed = 4
```

Tabbed Mica for tabbed window groups (Windows 11).
