# ColorPickerColorChangedEventArgs

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public sealed class ColorPickerColorChangedEventArgs : EventArgs
```

Event data for [ColorChanged](../Fluence.Wpf.Controls/ColorPicker.md#api-9fe48fc0fc23).

**Remarks:** Initializes a new instance of the [ColorPickerColorChangedEventArgs](ColorPickerColorChangedEventArgs.md) class.

**Parameter `oldColor`:** The color before the change.

**Parameter `newColor`:** The color after the change.

**Base type:** [`EventArgs`](https://learn.microsoft.com/dotnet/api/system.eventargs) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/ColorPickerColorChangedEventArgs.cs)

## Constructors

<a id="api-dfe972152ab9"></a>

### ColorPickerColorChangedEventArgs

```csharp
public ColorPickerColorChangedEventArgs(Color oldColor, Color newColor)
```

Event data for [ColorChanged](../Fluence.Wpf.Controls/ColorPicker.md#api-9fe48fc0fc23).

**Remarks:** Initializes a new instance of the [ColorPickerColorChangedEventArgs](ColorPickerColorChangedEventArgs.md) class.

**Parameter `oldColor`:** The color before the change.

**Parameter `newColor`:** The color after the change.

## Properties

<a id="api-029349ba2890"></a>

### NewColor

```csharp
public Color NewColor { get; }
```

Gets the color after the change.

<a id="api-dd7fb762d301"></a>

### OldColor

```csharp
public Color OldColor { get; }
```

Gets the color before the change.
