# InfoBar

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class InfoBar : ContentControl
```

An inline notification bar for displaying status messages with severity levels.

**Base type:** [`ContentControl`](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/InfoBar.cs)

## Constructors

<a id="api-6ad6c4ac063a"></a>

### InfoBar

```csharp
public InfoBar()
```

Creates a new `InfoBar` instance.

## Properties

<a id="api-23335c2cbbc1"></a>

### ActionButton

```csharp
public object ActionButton { get; set; }
```

Gets or sets the content placed in the action button slot.

<a id="api-9c2ada1e3687"></a>

### CloseButtonCommand

```csharp
public ICommand? CloseButtonCommand { get; set; }
```

Gets or sets the command invoked when the close button is clicked, WinUI's `InfoBar.CloseButtonCommand`. The command runs in addition to the close itself: the bar still closes unless a [Closing](InfoBar.md#api-17ca0f3be614) handler cancels it.

<a id="api-536087b845aa"></a>

### CloseButtonCommandParameter

```csharp
public object? CloseButtonCommandParameter { get; set; }
```

Gets or sets the parameter passed to [CloseButtonCommand](InfoBar.md#api-9c2ada1e3687).

<a id="api-cb9c21563681"></a>

### CloseButtonStyle

```csharp
public Style? CloseButtonStyle { get; set; }
```

Gets or sets the style applied to the close button, WinUI's `InfoBar.CloseButtonStyle`.

<a id="api-3b67066c27b2"></a>

### CornerRadius

```csharp
public CornerRadius CornerRadius { get; set; }
```

Gets or sets the corner radius of the info bar.

<a id="api-f18f10f83ce9"></a>

### Icon

```csharp
public object Icon { get; set; }
```

Gets or sets a custom icon that overrides the default severity icon.

<a id="api-1dce80f6814b"></a>

### IsClosable

```csharp
public bool IsClosable { get; set; }
```

Gets or sets a value indicating whether the close button is displayed.

<a id="api-a52764ffe1ab"></a>

### IsIconVisible

```csharp
public bool IsIconVisible { get; set; }
```

Gets or sets a value indicating whether the severity icon is displayed.

<a id="api-8afd9c710efd"></a>

### IsOpen

```csharp
public bool IsOpen { get; set; }
```

Gets or sets a value indicating whether the info bar is visible. The default is `false`, as WinUI's is: a bar is declared closed and opened when the condition it reports actually arises.

<a id="api-227d4462c62f"></a>

### Message

```csharp
public string Message { get; set; }
```

Gets or sets the message text displayed in the info bar.

<a id="api-a7b85a106708"></a>

### Severity

```csharp
public InfoBarSeverity Severity { get; set; }
```

Gets or sets the severity level that determines the visual style of the info bar.

<a id="api-a04af2a783df"></a>

### Title

```csharp
public string Title { get; set; }
```

Gets or sets the title text displayed in the info bar.

## Methods

<a id="api-001ab80370eb"></a>

### GetSeverityBrushKey

```csharp
public static string GetSeverityBrushKey(InfoBarSeverity severity)
```

Returns the theme brush resource key (for a `DynamicResource` reference) that colors `severity`. Mirrors the `Severity` triggers in Themes/Controls/InfoBar.xaml.

**Parameter `severity`:** The severity to map.

**Returns:** A brush key resolvable against the Fluence theme dictionaries.

**Exception `System.ArgumentOutOfRangeException`:** Thrown when `severity` is not a defined [InfoBarSeverity](../Fluence.Wpf/InfoBarSeverity.md) value.

<a id="api-e33a7c6c2c79"></a>

### GetSeverityGlyph

```csharp
public static string GetSeverityGlyph(InfoBarSeverity severity)
```

Returns the Segoe Fluent Icons glyph that represents `severity`. This is the single programmatic source for the severity glyphs; it mirrors the `StandardIcon` severity triggers in Themes/Controls/InfoBar.xaml (WPF property triggers cannot call this method, so keep both in sync). These are the WinUI InfoBar*IconGlyph codes (InfoBar_themeresources.xaml), the glyph drawn on top of the IconBackground circle, not a standalone icon.

**Parameter `severity`:** The severity to map.

**Returns:** A single-character glyph string in the Segoe Fluent Icons font.

**Exception `System.ArgumentOutOfRangeException`:** Thrown when `severity` is not a defined [InfoBarSeverity](../Fluence.Wpf/InfoBarSeverity.md) value.

<a id="api-d7adf0a3d350"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

<a id="api-4c5042f1f9e1"></a>

### OnCloseButtonClick

```csharp
protected virtual void OnCloseButtonClick()
```

Stamps [CloseButton](../Fluence.Wpf/InfoBarCloseReason.md#api-dde777cee7ed) as the reason for the close the button is about to start, then drives [IsOpen](InfoBar.md#api-8afd9c710efd) to `false`. The [Closing](InfoBar.md#api-17ca0f3be614) and [Closed](InfoBar.md#api-23b2f7b0587b) events are raised by the property's changed callback, so a cancelled close leaves the bar open and a completed one raises [Closed](InfoBar.md#api-23b2f7b0587b) once, with this reason.

<a id="api-1627722d6981"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

## Events

<a id="api-34d373f8e19f"></a>

### CloseButtonClick

```csharp
public event EventHandler? CloseButtonClick
```

Occurs when the close button is clicked, before the close itself runs, matching WinUI's `InfoBar.CloseButtonClick`. WinUI passes no arguments of its own here, so neither does this. Cancel the close from [Closing](InfoBar.md#api-17ca0f3be614), not from here.

<a id="api-23b2f7b0587b"></a>

### Closed

```csharp
public event EventHandler<InfoBarClosedEventArgs>? Closed
```

Occurs after the info bar has closed.

<a id="api-17ca0f3be614"></a>

### Closing

```csharp
public event EventHandler<InfoBarClosingEventArgs>? Closing
```

Occurs before the info bar closes, whichever way the close started: the close button or [IsOpen](InfoBar.md#api-8afd9c710efd) set to `false` in code. Set [Cancel](../Fluence.Wpf/InfoBarClosingEventArgs.md#api-b3041dfb2e75) to `true` to prevent closing; [IsOpen](InfoBar.md#api-8afd9c710efd) goes back to `true` and [Closed](InfoBar.md#api-23b2f7b0587b) is not raised.

<a id="api-a2b39605dbb9"></a>

### Opened

```csharp
public event EventHandler<InfoBarOpenedEventArgs>? Opened
```

Occurs when the info bar opens, which is [IsOpen](InfoBar.md#api-8afd9c710efd) going from `false` to `true`. WinUI raises the same event from the same transition. A [Closing](InfoBar.md#api-17ca0f3be614) handler that cancels puts [IsOpen](InfoBar.md#api-8afd9c710efd) back without the bar ever having closed, and that revert is not a fresh open, so it raises nothing here.

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-a8303aa18827"></a>

### ActionButtonProperty

```csharp
public static readonly DependencyProperty ActionButtonProperty
```

Identifies the [ActionButton](InfoBar.md#api-23335c2cbbc1) dependency property.

<a id="api-5590b342830e"></a>

### CloseButtonCommandParameterProperty

```csharp
public static readonly DependencyProperty CloseButtonCommandParameterProperty
```

Identifies the [CloseButtonCommandParameter](InfoBar.md#api-536087b845aa) dependency property.

<a id="api-c01a9f5900d5"></a>

### CloseButtonCommandProperty

```csharp
public static readonly DependencyProperty CloseButtonCommandProperty
```

Identifies the [CloseButtonCommand](InfoBar.md#api-9c2ada1e3687) dependency property.

<a id="api-5c62e5c3d219"></a>

### CloseButtonStyleProperty

```csharp
public static readonly DependencyProperty CloseButtonStyleProperty
```

Identifies the [CloseButtonStyle](InfoBar.md#api-cb9c21563681) dependency property.

<a id="api-df5ca43d4df3"></a>

### CornerRadiusProperty

```csharp
public static readonly DependencyProperty CornerRadiusProperty
```

Identifies the [CornerRadius](InfoBar.md#api-3b67066c27b2) dependency property.

<a id="api-d88d9e2484d2"></a>

### IconProperty

```csharp
public static readonly DependencyProperty IconProperty
```

Identifies the [Icon](InfoBar.md#api-f18f10f83ce9) dependency property.

<a id="api-b951dda3556c"></a>

### IsClosableProperty

```csharp
public static readonly DependencyProperty IsClosableProperty
```

Identifies the [IsClosable](InfoBar.md#api-1dce80f6814b) dependency property.

<a id="api-4adaeb264f41"></a>

### IsIconVisibleProperty

```csharp
public static readonly DependencyProperty IsIconVisibleProperty
```

Identifies the [IsIconVisible](InfoBar.md#api-a52764ffe1ab) dependency property.

<a id="api-99af2c7b4750"></a>

### IsOpenProperty

```csharp
public static readonly DependencyProperty IsOpenProperty
```

Identifies the [IsOpen](InfoBar.md#api-8afd9c710efd) dependency property.

<a id="api-53289c49b9bc"></a>

### MessageProperty

```csharp
public static readonly DependencyProperty MessageProperty
```

Identifies the [Message](InfoBar.md#api-227d4462c62f) dependency property.

<a id="api-f1e78b6574b6"></a>

### SeverityProperty

```csharp
public static readonly DependencyProperty SeverityProperty
```

Identifies the [Severity](InfoBar.md#api-a7b85a106708) dependency property.

<a id="api-4e34917df573"></a>

### TitleProperty

```csharp
public static readonly DependencyProperty TitleProperty
```

Identifies the [Title](InfoBar.md#api-a04af2a783df) dependency property.

## Related types

- [Fluence.Wpf.InfoBarClosedEventArgs](../Fluence.Wpf/InfoBarClosedEventArgs.md)
- [Fluence.Wpf.InfoBarClosingEventArgs](../Fluence.Wpf/InfoBarClosingEventArgs.md)
- [Fluence.Wpf.InfoBarOpenedEventArgs](../Fluence.Wpf/InfoBarOpenedEventArgs.md)
- [Fluence.Wpf.InfoBarSeverity](../Fluence.Wpf/InfoBarSeverity.md)
