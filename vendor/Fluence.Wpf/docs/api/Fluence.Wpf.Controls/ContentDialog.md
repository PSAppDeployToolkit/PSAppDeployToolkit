# ContentDialog

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class ContentDialog : ContentControl
```

A modal dialog with a title area, arbitrary body content, and up to three command buttons, mirroring the WinUI 3 `ContentDialog` control. While open the dialog sits above a smoke layer that dims and blocks everything behind it: over a window that exposes a full-window overlay host (a FluenceWindow `PART_DialogOverlayHost`) the smoke covers the entire window, title bar included; over a plain window it is hosted in the content adorner layer. A tunneling input guard additionally blocks any press outside the dialog. The owner's visual tree is never restructured. The body uses the inherited `Content` and `ContentTemplate`.

**Base type:** [`ContentControl`](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/ContentDialog.cs)

## Constructors

<a id="api-26c03d004bb0"></a>

### ContentDialog

```csharp
public ContentDialog()
```

Initializes a new instance of the [ContentDialog](ContentDialog.md) class. The dialog starts collapsed so a dialog declared in window XAML renders nothing inline at rest; it becomes visible while it is hosted in its modal overlay during [ShowAsync](ContentDialog.md#api-8c20dda5a3cd) and collapses again when it closes. SetCurrentValue keeps an explicit consumer-set `Visibility` authoritative.

## Properties

<a id="api-4df21cb281ea"></a>

### CloseButtonCommand

```csharp
public ICommand? CloseButtonCommand { get; set; }
```

Gets or sets the command executed when the close button is invoked (or the dialog is dismissed with the Escape key) and the [CloseButtonClick](ContentDialog.md#api-7b12b3276bc9) event is not canceled.

<a id="api-119ffc8ae2f6"></a>

### CloseButtonCommandParameter

```csharp
public object? CloseButtonCommandParameter { get; set; }
```

Gets or sets the parameter passed to [CloseButtonCommand](ContentDialog.md#api-4df21cb281ea).

<a id="api-38e7a8ad00be"></a>

### CloseButtonText

```csharp
public string CloseButtonText { get; set; }
```

Gets or sets the text of the close button. The button is collapsed while the text is empty.

<a id="api-595f77184284"></a>

### DefaultButton

```csharp
public ContentDialogButton DefaultButton { get; set; }
```

Gets or sets which command button receives initial keyboard focus when the dialog opens and is invoked by the Enter key while focus is not on another command button.

<a id="api-429a6ce3aae0"></a>

### IsPrimaryButtonEnabled

```csharp
public bool IsPrimaryButtonEnabled { get; set; }
```

Gets or sets a value indicating whether the primary button is enabled.

<a id="api-b5cc41309676"></a>

### IsSecondaryButtonEnabled

```csharp
public bool IsSecondaryButtonEnabled { get; set; }
```

Gets or sets a value indicating whether the secondary button is enabled.

<a id="api-9eb8a33cf217"></a>

### PrimaryButtonCommand

```csharp
public ICommand? PrimaryButtonCommand { get; set; }
```

Gets or sets the command executed when the primary button is invoked and the [PrimaryButtonClick](ContentDialog.md#api-e548fff3243f) event is not canceled.

<a id="api-22804a6b9a00"></a>

### PrimaryButtonCommandParameter

```csharp
public object? PrimaryButtonCommandParameter { get; set; }
```

Gets or sets the parameter passed to [PrimaryButtonCommand](ContentDialog.md#api-9eb8a33cf217).

<a id="api-0654c36525b8"></a>

### PrimaryButtonText

```csharp
public string PrimaryButtonText { get; set; }
```

Gets or sets the text of the primary button. The button is collapsed while the text is empty.

<a id="api-c3f73c40f9e7"></a>

### SecondaryButtonCommand

```csharp
public ICommand? SecondaryButtonCommand { get; set; }
```

Gets or sets the command executed when the secondary button is invoked and the [SecondaryButtonClick](ContentDialog.md#api-2853a2ed535e) event is not canceled.

<a id="api-75814454c363"></a>

### SecondaryButtonCommandParameter

```csharp
public object? SecondaryButtonCommandParameter { get; set; }
```

Gets or sets the parameter passed to [SecondaryButtonCommand](ContentDialog.md#api-c3f73c40f9e7).

<a id="api-a2723d4bf6ac"></a>

### SecondaryButtonText

```csharp
public string SecondaryButtonText { get; set; }
```

Gets or sets the text of the secondary button. The button is collapsed while the text is empty.

<a id="api-1b91b716e5d6"></a>

### Title

```csharp
public object? Title { get; set; }
```

Gets or sets the title shown at the top of the dialog.

<a id="api-a5e737c3af5d"></a>

### TitleTemplate

```csharp
public DataTemplate? TitleTemplate { get; set; }
```

Gets or sets the template used to display [Title](ContentDialog.md#api-1b91b716e5d6).

## Methods

<a id="api-8a83aeb73ca6"></a>

### Hide

```csharp
public void Hide()
```

Closes the dialog with [None](../Fluence.Wpf/ContentDialogResult.md#api-ffdcc3d4bda8). Does nothing when the dialog is not open.

<a id="api-91c51d685ef3"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

<a id="api-7e0e88379e74"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

<a id="api-2bc9b865a38a"></a>

### OnKeyDown

```csharp
protected override void OnKeyDown(KeyEventArgs e)
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

**Remarks:** The Enter/[DefaultButton](ContentDialog.md#api-595f77184284) shortcut runs on the bubbling key event (not the tunneling preview) so focused body controls that consume Enter themselves, such as an AcceptsReturn TextBox or an open ComboBox, win over the default button.

<a id="api-f7fab7ba1603"></a>

### OnPreviewKeyDown

```csharp
protected override void OnPreviewKeyDown(KeyEventArgs e)
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

<a id="api-8c20dda5a3cd"></a>

### ShowAsync

```csharp
public Task<ContentDialogResult> ShowAsync()
```

Shows the dialog modally over the active window (or the application main window) and returns a task that completes with the [ContentDialogResult](../Fluence.Wpf/ContentDialogResult.md) once the dialog closes. While open, a smoke layer dims the owner window content and blocks mouse input, Tab navigation is trapped inside the dialog, Escape dismisses as if the close button were invoked, and Enter invokes [DefaultButton](ContentDialog.md#api-595f77184284). Must be called on the dialog's dispatcher thread.

**Returns:** A task that completes with the dialog result when the dialog closes.

**Exception `System.InvalidOperationException`:** This dialog or another dialog on the same owner is already open, no owner window could be resolved, the owner window has no `UIElement` content root, or no adorner layer exists above the owner window content.

## Events

<a id="api-7b12b3276bc9"></a>

### CloseButtonClick

```csharp
public event EventHandler<ContentDialogButtonClickEventArgs>? CloseButtonClick
```

Occurs when the close button is invoked or the dialog is dismissed with the Escape key. Set [Cancel](../Fluence.Wpf/ContentDialogButtonClickEventArgs.md#api-bd49d9c01b88) to `true` to keep the dialog open and skip [CloseButtonCommand](ContentDialog.md#api-4df21cb281ea).

<a id="api-ce53999dec5b"></a>

### Closed

```csharp
public event EventHandler<ContentDialogClosedEventArgs>? Closed
```

Occurs after the dialog has been removed from the owner window's adorner layer.

<a id="api-413bbefe7c4a"></a>

### Opened

```csharp
public event EventHandler<ContentDialogOpenedEventArgs>? Opened
```

Occurs after the dialog has been added to the owner window's adorner layer.

<a id="api-e548fff3243f"></a>

### PrimaryButtonClick

```csharp
public event EventHandler<ContentDialogButtonClickEventArgs>? PrimaryButtonClick
```

Occurs when the primary button is invoked. Set [Cancel](../Fluence.Wpf/ContentDialogButtonClickEventArgs.md#api-bd49d9c01b88) to `true` to keep the dialog open and skip [PrimaryButtonCommand](ContentDialog.md#api-9eb8a33cf217).

<a id="api-2853a2ed535e"></a>

### SecondaryButtonClick

```csharp
public event EventHandler<ContentDialogButtonClickEventArgs>? SecondaryButtonClick
```

Occurs when the secondary button is invoked. Set [Cancel](../Fluence.Wpf/ContentDialogButtonClickEventArgs.md#api-bd49d9c01b88) to `true` to keep the dialog open and skip [SecondaryButtonCommand](ContentDialog.md#api-c3f73c40f9e7).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-cb284ce90380"></a>

### CloseButtonCommandParameterProperty

```csharp
public static readonly DependencyProperty CloseButtonCommandParameterProperty
```

Identifies the [CloseButtonCommandParameter](ContentDialog.md#api-119ffc8ae2f6) dependency property.

<a id="api-52fa07f178b4"></a>

### CloseButtonCommandProperty

```csharp
public static readonly DependencyProperty CloseButtonCommandProperty
```

Identifies the [CloseButtonCommand](ContentDialog.md#api-4df21cb281ea) dependency property.

<a id="api-2af954f6eb85"></a>

### CloseButtonTextProperty

```csharp
public static readonly DependencyProperty CloseButtonTextProperty
```

Identifies the [CloseButtonText](ContentDialog.md#api-38e7a8ad00be) dependency property.

<a id="api-2b8a750c2749"></a>

### DefaultButtonProperty

```csharp
public static readonly DependencyProperty DefaultButtonProperty
```

Identifies the [DefaultButton](ContentDialog.md#api-595f77184284) dependency property.

<a id="api-9964320ab920"></a>

### IsPrimaryButtonEnabledProperty

```csharp
public static readonly DependencyProperty IsPrimaryButtonEnabledProperty
```

Identifies the [IsPrimaryButtonEnabled](ContentDialog.md#api-429a6ce3aae0) dependency property.

<a id="api-0d71bf132d98"></a>

### IsSecondaryButtonEnabledProperty

```csharp
public static readonly DependencyProperty IsSecondaryButtonEnabledProperty
```

Identifies the [IsSecondaryButtonEnabled](ContentDialog.md#api-b5cc41309676) dependency property.

<a id="api-cb6b42296692"></a>

### PrimaryButtonCommandParameterProperty

```csharp
public static readonly DependencyProperty PrimaryButtonCommandParameterProperty
```

Identifies the [PrimaryButtonCommandParameter](ContentDialog.md#api-22804a6b9a00) dependency property.

<a id="api-a6c9a6b39fe4"></a>

### PrimaryButtonCommandProperty

```csharp
public static readonly DependencyProperty PrimaryButtonCommandProperty
```

Identifies the [PrimaryButtonCommand](ContentDialog.md#api-9eb8a33cf217) dependency property.

<a id="api-6894fe6116ca"></a>

### PrimaryButtonTextProperty

```csharp
public static readonly DependencyProperty PrimaryButtonTextProperty
```

Identifies the [PrimaryButtonText](ContentDialog.md#api-0654c36525b8) dependency property.

<a id="api-ff64b039ea24"></a>

### SecondaryButtonCommandParameterProperty

```csharp
public static readonly DependencyProperty SecondaryButtonCommandParameterProperty
```

Identifies the [SecondaryButtonCommandParameter](ContentDialog.md#api-75814454c363) dependency property.

<a id="api-680e4d1fdc96"></a>

### SecondaryButtonCommandProperty

```csharp
public static readonly DependencyProperty SecondaryButtonCommandProperty
```

Identifies the [SecondaryButtonCommand](ContentDialog.md#api-c3f73c40f9e7) dependency property.

<a id="api-6664363591f5"></a>

### SecondaryButtonTextProperty

```csharp
public static readonly DependencyProperty SecondaryButtonTextProperty
```

Identifies the [SecondaryButtonText](ContentDialog.md#api-a2723d4bf6ac) dependency property.

<a id="api-3325b4f87b10"></a>

### TitleProperty

```csharp
public static readonly DependencyProperty TitleProperty
```

Identifies the [Title](ContentDialog.md#api-1b91b716e5d6) dependency property.

<a id="api-c7ec5683b8ae"></a>

### TitleTemplateProperty

```csharp
public static readonly DependencyProperty TitleTemplateProperty
```

Identifies the [TitleTemplate](ContentDialog.md#api-a5e737c3af5d) dependency property.

## Related types

- [Fluence.Wpf.ContentDialogButton](../Fluence.Wpf/ContentDialogButton.md)
- [Fluence.Wpf.ContentDialogButtonClickEventArgs](../Fluence.Wpf/ContentDialogButtonClickEventArgs.md)
- [Fluence.Wpf.ContentDialogClosedEventArgs](../Fluence.Wpf/ContentDialogClosedEventArgs.md)
- [Fluence.Wpf.ContentDialogOpenedEventArgs](../Fluence.Wpf/ContentDialogOpenedEventArgs.md)
- [Fluence.Wpf.ContentDialogResult](../Fluence.Wpf/ContentDialogResult.md)
