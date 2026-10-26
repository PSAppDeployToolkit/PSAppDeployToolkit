# TeachingTip

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class TeachingTip : ContentControl
```

A contextual tip surface with a title, subtitle, body content, and optional action and close buttons, mirroring the WinUI 3 `TeachingTip` control. The tip is hosted in an internal light-weight `Popup`: when [Target](TeachingTip.md#api-6c3495e5bd60) is set the popup is anchored to it, centered on the target edge selected by [PreferredPlacement](TeachingTip.md#api-92e93490e59c); untargeted tips dock to the bottom-right corner of the active window content (or center when [PreferredPlacement](TeachingTip.md#api-92e93490e59c) is explicitly Center) and hide the beak. The body uses the inherited `Content` and `ContentTemplate`.

**Base type:** [`ContentControl`](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/TeachingTip.cs)

## Constructors

<a id="api-45923ee3e12c"></a>

### TeachingTip

```csharp
public TeachingTip()
```

Initializes a new instance of the [TeachingTip](TeachingTip.md) class. The tip starts collapsed so a tip declared in page XAML renders nothing inline; it becomes visible once it is re-hosted in its popup the first time it opens. SetCurrentValue keeps an explicit consumer-set `Visibility` authoritative. The open reveal subscribes to `Loaded`: the popup child re-raises Loaded on every open, so the reveal replays each time the tip shows.

## Properties

<a id="api-3593e4c99f02"></a>

### ActionButtonCommand

```csharp
public ICommand? ActionButtonCommand { get; set; }
```

Gets or sets the command executed when the action button is invoked, after the [ActionButtonClick](TeachingTip.md#api-7e34b749ce9e) event has been raised.

<a id="api-4fb41f6c6630"></a>

### ActionButtonCommandParameter

```csharp
public object? ActionButtonCommandParameter { get; set; }
```

Gets or sets the parameter passed to [ActionButtonCommand](TeachingTip.md#api-3593e4c99f02).

<a id="api-0768430a61bb"></a>

### ActionButtonContent

```csharp
public object? ActionButtonContent { get; set; }
```

Gets or sets the content of the accent action button in the tip footer. The button is collapsed while the value is `null`.

<a id="api-bb5b7d6735ba"></a>

### ActualPlacement

```csharp
public TeachingTipPlacementMode ActualPlacement { get; }
```

Gets the placement the tip resolved when it was last opened: [PreferredPlacement](TeachingTip.md#api-92e93490e59c) with [Auto](../Fluence.Wpf/TeachingTipPlacementMode.md#api-af81cca8fba5) resolved to a concrete edge, or [Center](../Fluence.Wpf/TeachingTipPlacementMode.md#api-75a9aeb113d1) for untargeted tips (which dock to the bottom-right corner of the window content but never show a beak). The template positions the beak on the edge facing the target and hides it for [Center](../Fluence.Wpf/TeachingTipPlacementMode.md#api-75a9aeb113d1).

<a id="api-929e62166246"></a>

### CloseButtonContent

```csharp
public object? CloseButtonContent { get; set; }
```

Gets or sets the content of the close button in the tip footer, matching the WinUI close-affordance rules: the footer close button shows only while this value is set. While it is `null` and [IsLightDismissEnabled](TeachingTip.md#api-f2b91895b3ce) is `false`, an alternate X close button is shown in the top-right corner of the tip instead; while it is `null` and light dismiss is enabled, the tip shows no close affordance at all.

<a id="api-f2b91895b3ce"></a>

### IsLightDismissEnabled

```csharp
public bool IsLightDismissEnabled { get; set; }
```

Gets or sets a value indicating whether clicking outside the tip dismisses it. Maps to the inverse of `StaysOpen` on the host popup.

<a id="api-7199b282e925"></a>

### IsOpen

```csharp
public bool IsOpen { get; set; }
```

Gets or sets a value indicating whether the tip is open. Setting `true` shows the tip in its host popup; setting `false` (or a light dismiss) closes it and raises [Closed](TeachingTip.md#api-125b7dee370c).

<a id="api-92e93490e59c"></a>

### PreferredPlacement

```csharp
public TeachingTipPlacementMode PreferredPlacement { get; set; }
```

Gets or sets where the tip opens relative to [Target](TeachingTip.md#api-6c3495e5bd60). When a target is set, [Auto](../Fluence.Wpf/TeachingTipPlacementMode.md#api-af81cca8fba5) currently resolves to [Bottom](../Fluence.Wpf/TeachingTipPlacementMode.md#api-bf80b1ce2d48). When [Target](TeachingTip.md#api-6c3495e5bd60) is `null`, an untargeted [Auto](../Fluence.Wpf/TeachingTipPlacementMode.md#api-af81cca8fba5) tip docks to the bottom-right corner of the window content (the WinUI default position); any other explicit value centers the tip over the window content.

<a id="api-f84572713884"></a>

### Subtitle

```csharp
public string Subtitle { get; set; }
```

Gets or sets the subtitle shown beneath [Title](TeachingTip.md#api-54c32037da31). The subtitle is hidden while the value is empty.

<a id="api-6c3495e5bd60"></a>

### Target

```csharp
public FrameworkElement? Target { get; set; }
```

Gets or sets the element the tip is anchored to. When `null` the tip centers over the active window content and the beak is hidden.

<a id="api-54c32037da31"></a>

### Title

```csharp
public string Title { get; set; }
```

Gets or sets the title shown at the top of the tip. The title is hidden while the value is empty.

## Methods

<a id="api-ca4c350cf7f4"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

<a id="api-6d660bc9c623"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

<a id="api-2c669a7f8b61"></a>

### OnPreviewKeyDown

```csharp
protected override void OnPreviewKeyDown(KeyEventArgs e)
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

**Remarks:** Escape pressed inside the open tip dismisses it, mirroring the WinUI keyboard contract, which treats Escape as a light dismiss. The close runs through the [IsOpen](TeachingTip.md#api-7199b282e925) pipeline so [Closed](TeachingTip.md#api-125b7dee370c) is raised as usual, and the reason is staged so it reports [LightDismiss](../Fluence.Wpf/TeachingTipCloseReason.md#api-a10999473a78) rather than the default [Programmatic](../Fluence.Wpf/TeachingTipCloseReason.md#api-e066cb2a0f70).

## Events

<a id="api-7e34b749ce9e"></a>

### ActionButtonClick

```csharp
public event EventHandler? ActionButtonClick
```

Occurs when the action button is invoked, before [ActionButtonCommand](TeachingTip.md#api-3593e4c99f02) executes. Invoking the action button does not close the tip.

<a id="api-80592fc1dd30"></a>

### CloseButtonClick

```csharp
public event EventHandler? CloseButtonClick
```

Occurs when the close button is invoked, before the tip closes.

<a id="api-125b7dee370c"></a>

### Closed

```csharp
public event EventHandler<TeachingTipClosedEventArgs>? Closed
```

Occurs after the tip has closed, whether through [IsOpen](TeachingTip.md#api-7199b282e925), the close button, or a light dismiss.

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-30e0a3b2a6cf"></a>

### ActionButtonCommandParameterProperty

```csharp
public static readonly DependencyProperty ActionButtonCommandParameterProperty
```

Identifies the [ActionButtonCommandParameter](TeachingTip.md#api-4fb41f6c6630) dependency property.

<a id="api-d0a48c1773a5"></a>

### ActionButtonCommandProperty

```csharp
public static readonly DependencyProperty ActionButtonCommandProperty
```

Identifies the [ActionButtonCommand](TeachingTip.md#api-3593e4c99f02) dependency property.

<a id="api-850814527396"></a>

### ActionButtonContentProperty

```csharp
public static readonly DependencyProperty ActionButtonContentProperty
```

Identifies the [ActionButtonContent](TeachingTip.md#api-0768430a61bb) dependency property.

<a id="api-661422e97ec5"></a>

### ActualPlacementProperty

```csharp
public static readonly DependencyProperty ActualPlacementProperty
```

Identifies the [ActualPlacement](TeachingTip.md#api-bb5b7d6735ba) dependency property.

<a id="api-f070956c8d2b"></a>

### CloseButtonContentProperty

```csharp
public static readonly DependencyProperty CloseButtonContentProperty
```

Identifies the [CloseButtonContent](TeachingTip.md#api-929e62166246) dependency property.

<a id="api-68609fcddb90"></a>

### IsLightDismissEnabledProperty

```csharp
public static readonly DependencyProperty IsLightDismissEnabledProperty
```

Identifies the [IsLightDismissEnabled](TeachingTip.md#api-f2b91895b3ce) dependency property.

<a id="api-340b2dd9fd29"></a>

### IsOpenProperty

```csharp
public static readonly DependencyProperty IsOpenProperty
```

Identifies the [IsOpen](TeachingTip.md#api-7199b282e925) dependency property.

<a id="api-6ebe0f91b429"></a>

### PreferredPlacementProperty

```csharp
public static readonly DependencyProperty PreferredPlacementProperty
```

Identifies the [PreferredPlacement](TeachingTip.md#api-92e93490e59c) dependency property.

<a id="api-e17a54524c13"></a>

### SubtitleProperty

```csharp
public static readonly DependencyProperty SubtitleProperty
```

Identifies the [Subtitle](TeachingTip.md#api-f84572713884) dependency property.

<a id="api-918ce0ccb472"></a>

### TargetProperty

```csharp
public static readonly DependencyProperty TargetProperty
```

Identifies the [Target](TeachingTip.md#api-6c3495e5bd60) dependency property.

<a id="api-1ced3dedf827"></a>

### TitleProperty

```csharp
public static readonly DependencyProperty TitleProperty
```

Identifies the [Title](TeachingTip.md#api-54c32037da31) dependency property.

## Related types

- [Fluence.Wpf.TeachingTipClosedEventArgs](../Fluence.Wpf/TeachingTipClosedEventArgs.md)
- [Fluence.Wpf.TeachingTipPlacementMode](../Fluence.Wpf/TeachingTipPlacementMode.md)
