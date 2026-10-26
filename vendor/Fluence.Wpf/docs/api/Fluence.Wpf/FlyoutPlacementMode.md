# FlyoutPlacementMode

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public enum FlyoutPlacementMode
```

Defines where a [FlyoutBase](../Fluence.Wpf.Controls/FlyoutBase.md) opens relative to its placement target.

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/FlyoutPlacementMode.cs)

## Values

<a id="api-44bd39fe0499"></a>

### Auto

```csharp
Auto = 5
```

The system chooses the position. Currently maps to [Bottom](FlyoutPlacementMode.md#api-f47027caef3d) popup placement.

<a id="api-f47027caef3d"></a>

### Bottom

```csharp
Bottom = 1
```

The flyout opens below the placement target.

<a id="api-d1ff915d1f64"></a>

### Full

```csharp
Full = 4
```

The flyout is intended to fill the window. Currently maps to [Bottom](FlyoutPlacementMode.md#api-f47027caef3d) popup placement.

<a id="api-66021bcacecd"></a>

### Left

```csharp
Left = 2
```

The flyout opens to the left of the placement target.

<a id="api-96c88cf7172f"></a>

### Right

```csharp
Right = 3
```

The flyout opens to the right of the placement target.

<a id="api-b101bf1a0c18"></a>

### Top

```csharp
Top = 0
```

The flyout opens above the placement target.
