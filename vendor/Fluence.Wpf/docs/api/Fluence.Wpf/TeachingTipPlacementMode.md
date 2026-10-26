# TeachingTipPlacementMode

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public enum TeachingTipPlacementMode
```

Defines where a [TeachingTip](../Fluence.Wpf.Controls/TeachingTip.md) opens relative to its target, mirroring the WinUI 3 `TeachingTipPlacementMode` contract.

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/TeachingTipPlacementMode.cs)

## Values

<a id="api-af81cca8fba5"></a>

### Auto

```csharp
Auto = 0
```

The system chooses the position. Currently maps to [Bottom](TeachingTipPlacementMode.md#api-bf80b1ce2d48) popup placement.

<a id="api-bf80b1ce2d48"></a>

### Bottom

```csharp
Bottom = 2
```

The tip opens below the target.

<a id="api-75a9aeb113d1"></a>

### Center

```csharp
Center = 5
```

The tip is centered over the target, or over the active window content when no target is set. The beak is hidden in this mode.

<a id="api-c53c1183793a"></a>

### Left

```csharp
Left = 3
```

The tip opens to the left of the target.

<a id="api-3265832e5c89"></a>

### Right

```csharp
Right = 4
```

The tip opens to the right of the target.

<a id="api-f86d2a8855f5"></a>

### Top

```csharp
Top = 1
```

The tip opens above the target.
