# TitleBar

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class TitleBar : ContentControl
```

A Fluent Design shell title bar with optional navigation buttons (back and pane toggle), an icon slot, title and subtitle text, left/right header slots, and a centred custom-content slot.

**Remarks:** Place this control inside [TitleBar](FluenceWindow.md#api-edc3f08e969c) and configure visibility of the back/pane-toggle buttons via [IsBackButtonVisible](TitleBar.md#api-df1e9858fc9b) and [IsPaneToggleButtonVisible](TitleBar.md#api-e94aeffcb720). Respond to navigation gestures through the [BackRequested](TitleBar.md#api-32f7aefbf796) and [PaneToggleRequested](TitleBar.md#api-fd4155bc710f) events or the command properties [BackCommand](TitleBar.md#api-7e68f294ec9e) and [PaneToggleCommand](TitleBar.md#api-05ab69f3c73f).

**Base type:** [`ContentControl`](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/TitleBar.cs)

## Constructors

<a id="api-0457c035f630"></a>

### TitleBar

```csharp
public TitleBar()
```

Initializes a new instance of the [TitleBar](TitleBar.md) class.

## Properties

<a id="api-7e68f294ec9e"></a>

### BackCommand

```csharp
public ICommand BackCommand { get; set; }
```

Gets or sets the command invoked when the back button is clicked. When `null` the back button click still raises [BackRequested](TitleBar.md#api-32f7aefbf796).

<a id="api-316c256f8833"></a>

### BackCommandParameter

```csharp
public object BackCommandParameter { get; set; }
```

Gets or sets the command parameter passed to [BackCommand](TitleBar.md#api-7e68f294ec9e).

<a id="api-2354bf7511f0"></a>

### CustomContent

```csharp
public object CustomContent { get; set; }
```

Gets or sets custom content displayed in the centred content slot of the title bar. Setting this also assigns `Content` unless `Content` has been independently set to a different value.

<a id="api-2a082a73dfe5"></a>

### Icon

```csharp
public object Icon { get; set; }
```

Gets or sets the title bar icon content.

<a id="api-df1e9858fc9b"></a>

### IsBackButtonVisible

```csharp
public bool IsBackButtonVisible { get; set; }
```

Gets or sets a value indicating whether the back navigation button is visible.

<a id="api-786a5710e166"></a>

### IsCompact

```csharp
public bool IsCompact { get; set; }
```

Gets or sets a value indicating whether the title bar uses compact height (32 px) instead of the default 48 px.

<a id="api-e94aeffcb720"></a>

### IsPaneToggleButtonVisible

```csharp
public bool IsPaneToggleButtonVisible { get; set; }
```

Gets or sets a value indicating whether the pane toggle button is visible.

<a id="api-1d406e72bdfd"></a>

### LeftHeader

```csharp
public object LeftHeader { get; set; }
```

Gets or sets content displayed in the left header slot, before the icon and title text.

<a id="api-05ab69f3c73f"></a>

### PaneToggleCommand

```csharp
public ICommand PaneToggleCommand { get; set; }
```

Gets or sets the command invoked when the pane toggle button is clicked. When `null` the pane toggle click still raises [PaneToggleRequested](TitleBar.md#api-fd4155bc710f).

<a id="api-300567242b96"></a>

### PaneToggleCommandParameter

```csharp
public object PaneToggleCommandParameter { get; set; }
```

Gets or sets the command parameter passed to [PaneToggleCommand](TitleBar.md#api-05ab69f3c73f).

<a id="api-cba6ab472720"></a>

### RightHeader

```csharp
public object RightHeader { get; set; }
```

Gets or sets content displayed in the right header slot, after the central stretch column.

<a id="api-1e569228c3fb"></a>

### Subtitle

```csharp
public string Subtitle { get; set; }
```

Gets or sets the subtitle text displayed after the title.

<a id="api-4d2bf342d9a8"></a>

### Title

```csharp
public string Title { get; set; }
```

Gets or sets the title text displayed in the title bar.

## Methods

<a id="api-897f5992061a"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

<a id="api-64a4a4d1bb8a"></a>

### OnBackRequested

```csharp
protected virtual void OnBackRequested(EventArgs e)
```

Raises the [BackRequested](TitleBar.md#api-32f7aefbf796) event.

**Parameter `e`:** Event data. Pass `Empty` for a plain notification.

<a id="api-d16f2a2e1405"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

<a id="api-30a8c76ac0d8"></a>

### OnPaneToggleRequested

```csharp
protected virtual void OnPaneToggleRequested(EventArgs e)
```

Raises the [PaneToggleRequested](TitleBar.md#api-fd4155bc710f) event.

**Parameter `e`:** Event data. Pass `Empty` for a plain notification.

<a id="api-52edb71aadad"></a>

### OnPropertyChanged

```csharp
protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

## Events

<a id="api-32f7aefbf796"></a>

### BackRequested

```csharp
public event EventHandler? BackRequested
```

Occurs when the back button is invoked after command execution has been processed.

<a id="api-fd4155bc710f"></a>

### PaneToggleRequested

```csharp
public event EventHandler? PaneToggleRequested
```

Occurs when the pane toggle button is invoked after command execution has been processed.

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-19c89a50a776"></a>

### BackCommandParameterProperty

```csharp
public static readonly DependencyProperty BackCommandParameterProperty
```

Identifies the [BackCommandParameter](TitleBar.md#api-316c256f8833) dependency property.

<a id="api-fe7f720a4149"></a>

### BackCommandProperty

```csharp
public static readonly DependencyProperty BackCommandProperty
```

Identifies the [BackCommand](TitleBar.md#api-7e68f294ec9e) dependency property.

<a id="api-da1f15990130"></a>

### CustomContentProperty

```csharp
public static readonly DependencyProperty CustomContentProperty
```

Identifies the [CustomContent](TitleBar.md#api-2354bf7511f0) dependency property.

<a id="api-b686368f71af"></a>

### IconProperty

```csharp
public static readonly DependencyProperty IconProperty
```

Identifies the [Icon](TitleBar.md#api-2a082a73dfe5) dependency property.

<a id="api-0a695d4d3848"></a>

### IsBackButtonVisibleProperty

```csharp
public static readonly DependencyProperty IsBackButtonVisibleProperty
```

Identifies the [IsBackButtonVisible](TitleBar.md#api-df1e9858fc9b) dependency property.

<a id="api-c330abcd1d32"></a>

### IsCompactProperty

```csharp
public static readonly DependencyProperty IsCompactProperty
```

Identifies the [IsCompact](TitleBar.md#api-786a5710e166) dependency property.

<a id="api-6e26bc4d06dd"></a>

### IsPaneToggleButtonVisibleProperty

```csharp
public static readonly DependencyProperty IsPaneToggleButtonVisibleProperty
```

Identifies the [IsPaneToggleButtonVisible](TitleBar.md#api-e94aeffcb720) dependency property.

<a id="api-f5501fc12c37"></a>

### LeftHeaderProperty

```csharp
public static readonly DependencyProperty LeftHeaderProperty
```

Identifies the [LeftHeader](TitleBar.md#api-1d406e72bdfd) dependency property.

<a id="api-5a24c078ad8c"></a>

### PaneToggleCommandParameterProperty

```csharp
public static readonly DependencyProperty PaneToggleCommandParameterProperty
```

Identifies the [PaneToggleCommandParameter](TitleBar.md#api-300567242b96) dependency property.

<a id="api-2ee7084d11dc"></a>

### PaneToggleCommandProperty

```csharp
public static readonly DependencyProperty PaneToggleCommandProperty
```

Identifies the [PaneToggleCommand](TitleBar.md#api-05ab69f3c73f) dependency property.

<a id="api-cf89d43c77ae"></a>

### RightHeaderProperty

```csharp
public static readonly DependencyProperty RightHeaderProperty
```

Identifies the [RightHeader](TitleBar.md#api-cba6ab472720) dependency property.

<a id="api-0c0a54578e0e"></a>

### SubtitleProperty

```csharp
public static readonly DependencyProperty SubtitleProperty
```

Identifies the [Subtitle](TitleBar.md#api-1e569228c3fb) dependency property.

<a id="api-b0b3b8b1002f"></a>

### TitleProperty

```csharp
public static readonly DependencyProperty TitleProperty
```

Identifies the [Title](TitleBar.md#api-4d2bf342d9a8) dependency property.
