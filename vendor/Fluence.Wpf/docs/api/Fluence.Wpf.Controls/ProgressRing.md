# ProgressRing

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class ProgressRing : Control
```

A circular progress indicator that supports both determinate and indeterminate modes.

**Remarks:** Indeterminate animation follows the WinUI 3 motion model: a private sweep-fraction dependency property pulses the arc length from zero to half the circumference and back over a 2 second linear cycle, while the template's `PART_IndeterminateRotate` transform spins the arc from 90 to 1170 degrees over the same 2 second cycle.



The legacy orbit-dot template settings ([EllipseDiameter](ProgressRing.md#api-031ab32ce817) / [EllipseOffset](ProgressRing.md#api-ee72b01eae55)) are retained for custom templates and compatibility with earlier Fluence versions. The default template no longer consumes them.



Determinate mode renders a stroked arc through `ArcSegment`; the arc end-angle tweens for 367 ms with a WinUI-style decelerating key spline when [Value](ProgressRing.md#api-9f1fc6671c98) changes.

**Base type:** [`Control`](https://learn.microsoft.com/dotnet/api/system.windows.controls.control) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/ProgressRing.cs)

## Constructors

<a id="api-a4f04ba6492f"></a>

### ProgressRing

```csharp
public ProgressRing()
```

Initializes a new instance of the [ProgressRing](ProgressRing.md) class.

## Properties

<a id="api-031ab32ce817"></a>

### EllipseDiameter

```csharp
public double EllipseDiameter { get; }
```

Gets the diameter that earlier orbit-dot templates use for each indeterminate-mode dot. The default Fluence template now uses an arc, but this value is retained for custom templates and compatibility.

<a id="api-ee72b01eae55"></a>

### EllipseOffset

```csharp
public Thickness EllipseOffset { get; }
```

Gets the top-margin offset used by earlier orbit-dot templates to position each dot on the orbit radius. The default Fluence template now uses an arc, but this value is retained for custom templates and compatibility.

<a id="api-7d3d37f99ec0"></a>

### IsActive

```csharp
public bool IsActive { get; set; }
```

Gets or sets whether the progress ring is active and visible.

<a id="api-1a595eacea59"></a>

### IsIndeterminate

```csharp
public bool IsIndeterminate { get; set; }
```

Gets or sets whether the ring operates in indeterminate (spinning) mode.

<a id="api-dcdfb184efe0"></a>

### Maximum

```csharp
public double Maximum { get; set; }
```

Gets or sets the maximum value.

<a id="api-54e79ebbc874"></a>

### Minimum

```csharp
public double Minimum { get; set; }
```

Gets or sets the minimum value.

<a id="api-f2596add8c54"></a>

### ProgressState

```csharp
public ProgressRingState ProgressState { get; set; }
```

Gets or sets the visual state used to color the progress arc.

**Remarks:** Backward-compatibility alias for the orthogonal [ShowPaused](ProgressRing.md#api-197bc1579bc1) and [ShowError](ProgressRing.md#api-63ff0f4ecb80) flags. Setting this property maps one way onto the flags ([Normal](../Fluence.Wpf/ProgressRingState.md#api-06efa8419c43) clears both); setting the flags directly leaves this property unchanged.

<a id="api-63ff0f4ecb80"></a>

### ShowError

```csharp
public bool ShowError { get; set; }
```

Gets or sets whether the ring renders its arcs in the error state using the system critical brush.

**Remarks:** In indeterminate mode the arc keeps spinning while the error state is shown. Takes precedence over [ShowPaused](ProgressRing.md#api-197bc1579bc1) when both flags are set. Setting this flag directly does not change the legacy [ProgressState](ProgressRing.md#api-f2596add8c54) alias.

<a id="api-197bc1579bc1"></a>

### ShowPaused

```csharp
public bool ShowPaused { get; set; }
```

Gets or sets whether the ring renders its arcs in the paused state using the system caution brush.

**Remarks:** In indeterminate mode the spinning animation stops and a static half-circle arc is rendered instead. [ShowError](ProgressRing.md#api-63ff0f4ecb80) takes precedence when both flags are set. Setting this flag directly does not change the legacy [ProgressState](ProgressRing.md#api-f2596add8c54) alias.

<a id="api-f812fbd41bfc"></a>

### StrokeThickness

```csharp
public double StrokeThickness { get; set; }
```

Gets or sets the thickness of the progress arc stroke.

**Remarks:** Applies to both determinate and indeterminate arc visuals in the default template.

<a id="api-9f1fc6671c98"></a>

### Value

```csharp
public double Value { get; set; }
```

Gets or sets the current progress value in determinate mode.

## Methods

<a id="api-cd03b2744a20"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

<a id="api-a087424e9d1d"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

<a id="api-0c76a23c681d"></a>

### OnRenderSizeChanged

```csharp
protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-71af0834b3d8"></a>

### EllipseDiameterProperty

```csharp
public static readonly DependencyProperty EllipseDiameterProperty
```

Identifies the read-only [EllipseDiameter](ProgressRing.md#api-031ab32ce817) dependency property.

<a id="api-4fbd9cbbcdd8"></a>

### EllipseOffsetProperty

```csharp
public static readonly DependencyProperty EllipseOffsetProperty
```

Identifies the read-only [EllipseOffset](ProgressRing.md#api-ee72b01eae55) dependency property.

<a id="api-1bc8c790e2c6"></a>

### IsActiveProperty

```csharp
public static readonly DependencyProperty IsActiveProperty
```

Identifies the [IsActive](ProgressRing.md#api-7d3d37f99ec0) dependency property.

<a id="api-69dfa5e4dc54"></a>

### IsIndeterminateProperty

```csharp
public static readonly DependencyProperty IsIndeterminateProperty
```

Identifies the [IsIndeterminate](ProgressRing.md#api-1a595eacea59) dependency property.

<a id="api-d36ccc9f61a7"></a>

### MaximumProperty

```csharp
public static readonly DependencyProperty MaximumProperty
```

Identifies the [Maximum](ProgressRing.md#api-dcdfb184efe0) dependency property.

<a id="api-633f83e31c71"></a>

### MinimumProperty

```csharp
public static readonly DependencyProperty MinimumProperty
```

Identifies the [Minimum](ProgressRing.md#api-54e79ebbc874) dependency property.

<a id="api-7f131dfc75c8"></a>

### ProgressStateProperty

```csharp
public static readonly DependencyProperty ProgressStateProperty
```

Identifies the [ProgressState](ProgressRing.md#api-f2596add8c54) dependency property.

<a id="api-3a49b60f78cc"></a>

### ShowErrorProperty

```csharp
public static readonly DependencyProperty ShowErrorProperty
```

Identifies the [ShowError](ProgressRing.md#api-63ff0f4ecb80) dependency property.

<a id="api-88673c612fcc"></a>

### ShowPausedProperty

```csharp
public static readonly DependencyProperty ShowPausedProperty
```

Identifies the [ShowPaused](ProgressRing.md#api-197bc1579bc1) dependency property.

<a id="api-d632290a7594"></a>

### StrokeThicknessProperty

```csharp
public static readonly DependencyProperty StrokeThicknessProperty
```

Identifies the [StrokeThickness](ProgressRing.md#api-f812fbd41bfc) dependency property.

<a id="api-a0ce6a820112"></a>

### ValueProperty

```csharp
public static readonly DependencyProperty ValueProperty
```

Identifies the [Value](ProgressRing.md#api-9f1fc6671c98) dependency property.

## Related types

- [Fluence.Wpf.ProgressRingState](../Fluence.Wpf/ProgressRingState.md)
