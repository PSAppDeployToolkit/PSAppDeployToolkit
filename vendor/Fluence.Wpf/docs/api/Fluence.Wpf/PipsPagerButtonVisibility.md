# PipsPagerButtonVisibility

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public enum PipsPagerButtonVisibility
```

Defines when the previous and next navigation buttons of a [PipsPager](../Fluence.Wpf.Controls/PipsPager.md) are shown, mirroring the WinUI 3 `PipsPagerButtonVisibility` enumeration.

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/PipsPagerButtonVisibility.cs)

## Values

<a id="api-f8b11852cb9f"></a>

### Collapsed

```csharp
Collapsed = 2
```

The navigation button is never shown. This is the WinUI default.

<a id="api-adf662bf3bb1"></a>

### Visible

```csharp
Visible = 0
```

The navigation button is always visible.

<a id="api-3708244955ac"></a>

### VisibleOnPointerOver

```csharp
VisibleOnPointerOver = 1
```

The navigation button is visible only while the pointer is over the pager.
