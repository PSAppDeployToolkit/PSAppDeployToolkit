# ScrollingIndicatorMode

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public enum ScrollingIndicatorMode
```

Identifies which scrolling indicator a `ScrollBar` is currently showing.

**Remarks:** Mirrors the WinUI 3 `ScrollingIndicatorMode` enumeration that the WinUI ScrollViewer template pushes into its two scroll bars. A Fluence scroll bar reads the value from [IndicatorModeProperty](../Fluence.Wpf.Controls/ScrollBarExtensions.md#api-908b204fd3fe) and moves its `ScrollingIndicatorStates` visual state group to the matching state.

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/ScrollingIndicatorMode.cs)

## Values

<a id="api-68be13de4869"></a>

### MouseIndicator

```csharp
MouseIndicator = 2
```

The mouse indicator is shown: the interactive rail that expands to its full width while the pointer is over it.

<a id="api-c5704f37b42a"></a>

### None

```csharp
None = 0
```

No indicator is shown. The scroll bar is fully faded out and does not accept input.

<a id="api-3fc938710121"></a>

### TouchIndicator

```csharp
TouchIndicator = 1
```

The touch panning indicator is shown: a thin, non interactive bar that reports the scroll position without offering a drag target.
