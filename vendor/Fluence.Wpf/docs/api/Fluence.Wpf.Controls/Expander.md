# Expander

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class Expander : System.Windows.Controls.Expander
```

A Fluent Design styled Expander with animated expand/collapse.

**Remarks:** The content reveal mirrors the WinUI 3 Expander: the content presenter slides behind the already-clipped content border by the border's measured height, sliding in over 333 ms on expand and out over 167 ms on collapse. The discrete row swap remains the layout mechanism: the content row takes its space at expand start and releases it at collapse completion, so the slide itself is cosmetic-only inside the clip.

**Base type:** [`System.Windows.Controls.Expander`](https://learn.microsoft.com/dotnet/api/system.windows.controls.expander) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/Expander.cs)

## Constructors

<a id="api-7bd355eacb01"></a>

### Expander

```csharp
public Expander()
```

Creates a new `Expander` instance.

## Properties

<a id="api-204bd1a32bfd"></a>

### CornerRadius

```csharp
public CornerRadius CornerRadius { get; set; }
```

Gets or sets the corner radius of the expander chrome.

<a id="api-0f0921a870e1"></a>

### HeaderBackground

```csharp
public Brush? HeaderBackground { get; set; }
```

Gets or sets the brush that fills the header tier.

**Remarks:** WinUI keys the two tiers separately: the header takes ExpanderHeaderBackground and the content takes the control's own Background (Expander.xaml:111,114). This is the header half of that pair, so a consumer can colour either tier without retemplating. The default style supplies the card default fill, so leaving it unset keeps the shipped look.

## Methods

<a id="api-2ea7ef706d9a"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Resolves the template parts that drive the content slide and applies the steady state for the current `IsExpanded` value without animation.

<a id="api-8ed9c5be5abf"></a>

### OnCollapsed

```csharp
protected override void OnCollapsed()
```

Raises the base Collapsed event and starts the WinUI slide-out of the content behind the clipped content border, releasing the content row's layout space only when the slide completes.

<a id="api-9fad6a29d4e5"></a>

### OnExpanded

```csharp
protected override void OnExpanded()
```

Raises the base Expanded event, gives the content row its layout space, and starts the WinUI slide-in of the content from behind the clipped content border.

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-84296f56b184"></a>

### CornerRadiusProperty

```csharp
public static readonly DependencyProperty CornerRadiusProperty
```

Identifies the [CornerRadius](Expander.md#api-204bd1a32bfd) dependency property.

<a id="api-34e2079ca5e7"></a>

### HeaderBackgroundProperty

```csharp
public static readonly DependencyProperty HeaderBackgroundProperty
```

Identifies the [HeaderBackground](Expander.md#api-0f0921a870e1) dependency property.

## Fields

<a id="api-a132e1c19439"></a>

### BottomCornerRadiusFilter

```csharp
public static readonly IValueConverter BottomCornerRadiusFilter
```

Converter that keeps only the bottom edge of a [CornerRadius](Expander.md#api-204bd1a32bfd) or `Thickness` value and zeroes the top edge. The Expander template in `Themes/Controls/Expander.xaml` consumes this field through `{x:Static controls:Expander.BottomCornerRadiusFilter}`. See [TopCornerRadiusFilter](Expander.md#api-d50ce20a1915) for why the field's declared type is the public `IValueConverter` interface rather than the internal converter type.

<a id="api-d50ce20a1915"></a>

### TopCornerRadiusFilter

```csharp
public static readonly IValueConverter TopCornerRadiusFilter
```

Converter that keeps only the top edge of a [CornerRadius](Expander.md#api-204bd1a32bfd) or `Thickness` value and zeroes the bottom edge. The Expander template in `Themes/Controls/Expander.xaml` consumes this field through `{x:Static controls:Expander.TopCornerRadiusFilter}`. The field is declared as the public `IValueConverter` interface, rather than the internal converter type it constructs, because `{x:Static}` sites inside deferred `Setter.Value` and `ControlTemplate` content are re-resolved at runtime by `StaticExtension` through public-only reflection, which cannot see an internal field regardless of assembly.
