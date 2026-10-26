# ScrollBarExtensions

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public static class ScrollBarExtensions
```

Provides the attached properties that drive the WinUI style scrolling indicator on the `ScrollBar` instances inside a Fluence scroll viewer template.

**Remarks:** WinUI 3 gives its ScrollViewer template a `ScrollingIndicatorStates` visual state group and pushes an `IndicatorMode` value down into both scroll bars, so a bar stays hidden until the pointer enters the scroller or the content actually scrolls, then fades out again after a delay. WPF has no equivalent framework plumbing: `ScrollBar` carries no indicator property and nothing calls `GoToState` for those states. This class supplies the missing driver.



The keyed Fluence scroll bar styles set [IsIndicatorEnabledProperty](ScrollBarExtensions.md#api-5e3ef1157835), which attaches a per instance behavior. The behavior watches the templated parent scroll viewer and the bar's own pointer state, writes [IndicatorModeProperty](ScrollBarExtensions.md#api-908b204fd3fe), and moves both the `ScrollingIndicatorStates` and `ConsciousStates` groups. A bar that is not hosted in a `ScrollViewer` has no auto hide source, so it stays on [MouseIndicator](../Fluence.Wpf/ScrollingIndicatorMode.md#api-68be13de4869) and remains visible.

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/ScrollBarExtensions.cs)

## Methods

<a id="api-fec988b9b627"></a>

### GetIndicatorMode

```csharp
public static ScrollingIndicatorMode GetIndicatorMode(this ScrollBar obj)
```

Gets the scrolling indicator the specified scroll bar is currently showing.

**Parameter `obj`:** The scroll bar to read from.

**Returns:** The active [ScrollingIndicatorMode](../Fluence.Wpf/ScrollingIndicatorMode.md).

<a id="api-9e17e31cd9b6"></a>

### GetIsIndicatorEnabled

```csharp
public static bool GetIsIndicatorEnabled(this ScrollBar obj)
```

Gets whether the scrolling indicator behavior is attached to the specified scroll bar.

**Parameter `obj`:** The scroll bar to read from.

**Returns:** `true` when the behavior is attached; otherwise `false`.

<a id="api-13bdfc083300"></a>

### SetIndicatorMode

```csharp
public static void SetIndicatorMode(this ScrollBar obj, ScrollingIndicatorMode value)
```

Sets the scrolling indicator the specified scroll bar shows. The attached behavior writes this as the pointer and scroll state change; an application can also drive it directly.

**Parameter `obj`:** The scroll bar to write to.

**Parameter `value`:** The indicator to show.

<a id="api-dbbdde2d276d"></a>

### SetIsIndicatorEnabled

```csharp
public static void SetIsIndicatorEnabled(this ScrollBar obj, bool value)
```

Sets whether the scrolling indicator behavior is attached to the specified scroll bar. The Fluence scroll bar styles set this to `true`; clearing it leaves the bar permanently visible.

**Parameter `obj`:** The scroll bar to write to.

**Parameter `value`:** `true` to attach the behavior; otherwise `false`.

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-908b204fd3fe"></a>

### IndicatorModeProperty

```csharp
public static readonly DependencyProperty IndicatorModeProperty
```

Identifies the IndicatorMode attached property.

<a id="api-5e3ef1157835"></a>

### IsIndicatorEnabledProperty

```csharp
public static readonly DependencyProperty IsIndicatorEnabledProperty
```

Identifies the IsIndicatorEnabled attached property.

## Related types

- [Fluence.Wpf.ScrollingIndicatorMode](../Fluence.Wpf/ScrollingIndicatorMode.md)
