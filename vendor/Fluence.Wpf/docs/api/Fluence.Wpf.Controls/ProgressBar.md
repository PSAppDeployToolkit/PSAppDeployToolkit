# ProgressBar

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class ProgressBar : System.Windows.Controls.ProgressBar
```

Fluent-styled progress bar with determinate, indeterminate, error, and paused states plus an optional step mode.

**Base type:** [`System.Windows.Controls.ProgressBar`](https://learn.microsoft.com/dotnet/api/system.windows.controls.progressbar) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/ProgressBar.cs)

## Constructors

<a id="api-b71f54d69834"></a>

### ProgressBar

```csharp
public ProgressBar()
```

Initializes a new instance of the [ProgressBar](ProgressBar.md) class and subscribes to size changes for layout updates. Loaded and Unloaded are also wired so the repeat-forever indeterminate animation only runs while the control is in a live visual tree, mirroring [ProgressRing](ProgressRing.md).

## Properties

<a id="api-e08c3ac57474"></a>

### CornerRadius

```csharp
public CornerRadius CornerRadius { get; set; }
```

Gets or sets the corner radius of the progress indicator (the determinate fill and the indeterminate bars), matching the WinUI 3 default of 1.5.

<a id="api-5d40697c6a78"></a>

### CurrentStep

```csharp
public int CurrentStep { get; set; }
```

Gets or sets the current step in StepProgress mode.

<a id="api-13ad8a209569"></a>

### ProgressMode

```csharp
public ProgressBarMode ProgressMode { get; set; }
```

Gets or sets the progress mode. This is a one-way backward-compatibility alias that maps onto the WinUI-orthogonal primitives `IsIndeterminate`, [ShowError](ProgressBar.md#api-46090dea5574), and [ShowPaused](ProgressBar.md#api-3dc8755f5a35); changing those primitives does not write back to this property.

<a id="api-46090dea5574"></a>

### ShowError

```csharp
public bool ShowError { get; set; }
```

Gets or sets a value indicating whether the progress bar reports an error state. When set, the determinate fill and the indeterminate bars use the critical system brush, matching the WinUI 3 ProgressBar.ShowError contract. Takes precedence over [ShowPaused](ProgressBar.md#api-3dc8755f5a35).

<a id="api-3dc8755f5a35"></a>

### ShowPaused

```csharp
public bool ShowPaused { get; set; }
```

Gets or sets a value indicating whether the progress bar reports a paused state. When set (and [ShowError](ProgressBar.md#api-46090dea5574) is not), the determinate fill and the indeterminate bars use the caution system brush, matching the WinUI 3 ProgressBar.ShowPaused contract.

<a id="api-c301bc520e7e"></a>

### ShowStepMarkers

```csharp
public bool ShowStepMarkers { get; set; }
```

Gets or sets whether the bar is notched at each step boundary while it is in step progress mode. Off by default: a step bar reads as one travelling fill unless a consumer asks for the segmented look. The notches cut through the track and the fill alike, so a step bar reads as a row of segments rather than one continuous bar. Ignored outside step progress mode, and by a bar with fewer than two steps, which has no interior boundary to mark.

<a id="api-4b8cd9bf494c"></a>

### Steps

```csharp
public int Steps { get; set; }
```

Gets or sets the total number of steps in StepProgress mode.

<a id="api-6e8446c58861"></a>

### TrackHeight

```csharp
public double TrackHeight { get; set; }
```

Gets or sets the height of the thin baseline track behind the progress indicator, matching the WinUI 3 default of 1.

## Methods

<a id="api-679a3f54e21b"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`ProgressBar` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.progressbar).

<a id="api-6f816f992de4"></a>

### OnMaximumChanged

```csharp
protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
```

Documentation inherited from the [`ProgressBar` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.progressbar).

<a id="api-f4dc7626f84b"></a>

### OnMinimumChanged

```csharp
protected override void OnMinimumChanged(double oldMinimum, double newMinimum)
```

Documentation inherited from the [`ProgressBar` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.progressbar).

<a id="api-3a452ec56541"></a>

### OnValueChanged

```csharp
protected override void OnValueChanged(double oldValue, double newValue)
```

Documentation inherited from the [`ProgressBar` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.progressbar).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-19c26fe39479"></a>

### CornerRadiusProperty

```csharp
public static readonly DependencyProperty CornerRadiusProperty
```

Identifies the [CornerRadius](ProgressBar.md#api-e08c3ac57474) dependency property.

<a id="api-716f4ab2ee66"></a>

### CurrentStepProperty

```csharp
public static readonly DependencyProperty CurrentStepProperty
```

Identifies the [CurrentStep](ProgressBar.md#api-5d40697c6a78) dependency property.

<a id="api-abceace48530"></a>

### ProgressModeProperty

```csharp
public static readonly DependencyProperty ProgressModeProperty
```

Identifies the [ProgressMode](ProgressBar.md#api-13ad8a209569) dependency property.

<a id="api-a2884b507082"></a>

### ShowErrorProperty

```csharp
public static readonly DependencyProperty ShowErrorProperty
```

Identifies the [ShowError](ProgressBar.md#api-46090dea5574) dependency property.

<a id="api-0076aba0bc02"></a>

### ShowPausedProperty

```csharp
public static readonly DependencyProperty ShowPausedProperty
```

Identifies the [ShowPaused](ProgressBar.md#api-3dc8755f5a35) dependency property.

<a id="api-2bfd3e047753"></a>

### ShowStepMarkersProperty

```csharp
public static readonly DependencyProperty ShowStepMarkersProperty
```

Identifies the [ShowStepMarkers](ProgressBar.md#api-c301bc520e7e) dependency property.

<a id="api-ce3a52dd3b6d"></a>

### StepsProperty

```csharp
public static readonly DependencyProperty StepsProperty
```

Identifies the [Steps](ProgressBar.md#api-4b8cd9bf494c) dependency property.

<a id="api-81c6a96ecda1"></a>

### TrackHeightProperty

```csharp
public static readonly DependencyProperty TrackHeightProperty
```

Identifies the [TrackHeight](ProgressBar.md#api-6e8446c58861) dependency property.

## Related types

- [Fluence.Wpf.ProgressBarMode](../Fluence.Wpf/ProgressBarMode.md)
