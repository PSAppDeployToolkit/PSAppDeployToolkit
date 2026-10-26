# ColorPicker

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class ColorPicker : Control
```

A control that lets the user pick a color from a saturation/value spectrum, a hue slider, an optional alpha slider, a current/previous preview swatch row, and a text-entry area with an RGB/HSV representation selector, per-channel inputs, an alpha percentage input, and a hex input, mirroring the WinUI 3 `ColorPicker`. The picker keeps hue, saturation, value, and alpha as its internal source of truth so dragging across the grey axis does not accumulate RGB round-trip drift, the same approach WinUI uses.

**Remarks:** Scope notes: WinUI's `ColorSpectrumShape` (the Ring spectrum), the `ColorSpectrumComponents` permutations, `Orientation`, and the Min/Max channel range properties are deliberately omitted. The spectrum is fixed to saturation on the x axis by value on the y axis at the selected hue, with hue on a horizontal slider serving as the third-dimension color slider. Channel and alpha text inputs commit live on every valid keystroke like WinUI; the hex input commits on Enter or focus loss, a deliberate deviation from WinUI's live hex commit.

**Base type:** [`Control`](https://learn.microsoft.com/dotnet/api/system.windows.controls.control) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/ColorPicker.cs)

## Constructors

<a id="api-d4dfee459081"></a>

### ColorPicker

```csharp
public ColorPicker()
```

Creates a new `ColorPicker` instance.

## Properties

<a id="api-bb28b2f87dbb"></a>

### Color

```csharp
public Color Color { get; set; }
```

Gets or sets the currently selected color. Defaults to opaque red. Changing this property raises [ColorChanged](ColorPicker.md#api-9fe48fc0fc23); values assigned from outside the picker re-derive the internal hue/saturation/value model.

<a id="api-e2f8029b8654"></a>

### IsAlphaEnabled

```csharp
public bool IsAlphaEnabled { get; set; }
```

Gets or sets a value indicating whether the alpha channel can be edited. When `false` (the default) the alpha slider row and the alpha text input collapse, the hex input parses and displays six digits, and turning the property off pins the picker's alpha back to 255. Programmatic [Color](ColorPicker.md#api-bb28b2f87dbb) assignments keep whatever alpha they carry.

<a id="api-75e7d0ba7382"></a>

### IsAlphaSliderVisible

```csharp
public bool IsAlphaSliderVisible { get; set; }
```

Gets or sets a value indicating whether the alpha slider is shown. The slider renders only when both this property and [IsAlphaEnabled](ColorPicker.md#api-e2f8029b8654) are `true`, matching WinUI.

<a id="api-2eb9eacb9404"></a>

### IsAlphaTextInputVisible

```csharp
public bool IsAlphaTextInputVisible { get; set; }
```

Gets or sets a value indicating whether the alpha percentage text input is shown. The input renders only when both this property and [IsAlphaEnabled](ColorPicker.md#api-e2f8029b8654) are `true`, matching WinUI.

<a id="api-f9900104674b"></a>

### IsColorChannelTextInputVisible

```csharp
public bool IsColorChannelTextInputVisible { get; set; }
```

Gets or sets a value indicating whether the RGB/HSV representation selector and the per-channel text inputs are shown. The hex input is governed separately by [IsHexInputVisible](ColorPicker.md#api-c02ebdc93a4f) and the alpha input by [IsAlphaTextInputVisible](ColorPicker.md#api-2eb9eacb9404), matching WinUI. Channel input commits live on every valid keystroke; Enter or focus loss normalizes the text and restores it after invalid input.

<a id="api-4c0d605819e5"></a>

### IsColorPreviewVisible

```csharp
public bool IsColorPreviewVisible { get; set; }
```

Gets or sets a value indicating whether the current/previous preview swatch row is shown.

<a id="api-05f433c58dc2"></a>

### IsColorSliderVisible

```csharp
public bool IsColorSliderVisible { get; set; }
```

Gets or sets a value indicating whether the third-dimension color slider is shown. With the spectrum fixed to saturation by value, the third dimension is the hue slider, matching how WinUI assigns its color slider for that spectrum component pairing.

<a id="api-5ba453bab270"></a>

### IsColorSpectrumVisible

```csharp
public bool IsColorSpectrumVisible { get; set; }
```

Gets or sets a value indicating whether the saturation/value spectrum square is shown. The hue and alpha sliders remain available when it is hidden.

<a id="api-c02ebdc93a4f"></a>

### IsHexInputVisible

```csharp
public bool IsHexInputVisible { get; set; }
```

Gets or sets a value indicating whether the hex text input is shown. The input accepts `#RRGGBB` and `#AARRGGBB` (the leading `#` is optional) and commits on Enter or when keyboard focus leaves the box.

<a id="api-46f64e383937"></a>

### IsMoreButtonVisible

```csharp
public bool IsMoreButtonVisible { get; set; }
```

Gets or sets a value indicating whether the text-entry area collapses behind a More/Less toggle button. When `false` (the default) the text inputs are always visible and no toggle is shown; when `true` the toggle appears and the text-entry area stays collapsed until it is checked, matching WinUI.

<a id="api-4edff306ba03"></a>

### PreviousColor

```csharp
public Color? PreviousColor { get; set; }
```

Gets or sets the comparison color shown next to the current color in the preview swatch row, or `null` to hide the comparison swatch.

## Methods

<a id="api-07826bdda39b"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

<a id="api-837fa9cc9ed7"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

<a id="api-c4b847a6571f"></a>

### OnKeyDown

```csharp
protected override void OnKeyDown(KeyEventArgs e)
```

Handles arrow and page key presses when keyboard focus is on the spectrum area, adjusting saturation (Left/Right) or value (Up/Down/PageUp/PageDown) and routing through the HSV funnel so there is no RGB round-trip drift.

**Parameter `e`:** The key event arguments.

## Events

<a id="api-9fe48fc0fc23"></a>

### ColorChanged

```csharp
public event EventHandler<ColorPickerColorChangedEventArgs>? ColorChanged
```

Occurs after [Color](ColorPicker.md#api-bb28b2f87dbb) changes, whether through the spectrum, the sliders, the channel or hex text inputs, or a programmatic update. Channel and alpha text inputs commit live, so the event can fire once per keystroke while the user types (for example "1", "12", "120"), matching WinUI.

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-ca98f12cf5d0"></a>

### ColorProperty

```csharp
public static readonly DependencyProperty ColorProperty
```

Identifies the [Color](ColorPicker.md#api-bb28b2f87dbb) dependency property.

<a id="api-bf7d2e202beb"></a>

### IsAlphaEnabledProperty

```csharp
public static readonly DependencyProperty IsAlphaEnabledProperty
```

Identifies the [IsAlphaEnabled](ColorPicker.md#api-e2f8029b8654) dependency property.

<a id="api-c3dcb3e5b807"></a>

### IsAlphaSliderVisibleProperty

```csharp
public static readonly DependencyProperty IsAlphaSliderVisibleProperty
```

Identifies the [IsAlphaSliderVisible](ColorPicker.md#api-75e7d0ba7382) dependency property.

<a id="api-ffc6b8290d47"></a>

### IsAlphaTextInputVisibleProperty

```csharp
public static readonly DependencyProperty IsAlphaTextInputVisibleProperty
```

Identifies the [IsAlphaTextInputVisible](ColorPicker.md#api-2eb9eacb9404) dependency property.

<a id="api-ece309909d30"></a>

### IsColorChannelTextInputVisibleProperty

```csharp
public static readonly DependencyProperty IsColorChannelTextInputVisibleProperty
```

Identifies the [IsColorChannelTextInputVisible](ColorPicker.md#api-f9900104674b) dependency property.

<a id="api-54f9292d4a01"></a>

### IsColorPreviewVisibleProperty

```csharp
public static readonly DependencyProperty IsColorPreviewVisibleProperty
```

Identifies the [IsColorPreviewVisible](ColorPicker.md#api-4c0d605819e5) dependency property.

<a id="api-044d13c4ffdf"></a>

### IsColorSliderVisibleProperty

```csharp
public static readonly DependencyProperty IsColorSliderVisibleProperty
```

Identifies the [IsColorSliderVisible](ColorPicker.md#api-05f433c58dc2) dependency property.

<a id="api-cf98778daf5e"></a>

### IsColorSpectrumVisibleProperty

```csharp
public static readonly DependencyProperty IsColorSpectrumVisibleProperty
```

Identifies the [IsColorSpectrumVisible](ColorPicker.md#api-5ba453bab270) dependency property.

<a id="api-47950e4dbf32"></a>

### IsHexInputVisibleProperty

```csharp
public static readonly DependencyProperty IsHexInputVisibleProperty
```

Identifies the [IsHexInputVisible](ColorPicker.md#api-c02ebdc93a4f) dependency property.

<a id="api-3779fcf0bf14"></a>

### IsMoreButtonVisibleProperty

```csharp
public static readonly DependencyProperty IsMoreButtonVisibleProperty
```

Identifies the [IsMoreButtonVisible](ColorPicker.md#api-46f64e383937) dependency property.

<a id="api-95ac2bfa9caf"></a>

### PreviousColorProperty

```csharp
public static readonly DependencyProperty PreviousColorProperty
```

Identifies the [PreviousColor](ColorPicker.md#api-4edff306ba03) dependency property.

## Related types

- [Fluence.Wpf.ColorPickerColorChangedEventArgs](../Fluence.Wpf/ColorPickerColorChangedEventArgs.md)
