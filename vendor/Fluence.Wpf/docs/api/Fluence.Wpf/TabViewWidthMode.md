# TabViewWidthMode

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public enum TabViewWidthMode
```

Controls how tab item widths are distributed inside a [TabView](../Fluence.Wpf.Controls/TabView.md).

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/TabViewWidthMode.cs)

## Values

<a id="api-52379d9229a4"></a>

### Compact

```csharp
Compact = 2
```

Selected tab sizes to content; unselected tabs collapse to a compact width showing the icon only when present.

<a id="api-a68d47e945e8"></a>

### Equal

```csharp
Equal = 1
```

All tabs share the available horizontal space equally.

<a id="api-ec0598f57b9e"></a>

### SizeToContent

```csharp
SizeToContent = 0
```

Each tab sizes to its content up to a maximum width.
