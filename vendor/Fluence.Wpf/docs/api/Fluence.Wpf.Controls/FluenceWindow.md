# FluenceWindow

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class FluenceWindow : Window
```

A top-level window that recreates the Windows 11 Fluent / WinUI 3 chrome on WPF: a DWM system backdrop (Mica, Acrylic, or Tabbed), rounded corners, an extendable title bar, and custom caption buttons that integrate with the Windows 11 snap-layout flyout.

**Remarks:** The window collapses the native non-client frame through `WindowChrome` and drives every caption interaction (drag, resize, snap-layout hover, maximize/restore) from a Win32 message hook so the custom chrome stays authoritative. Theme, accent, and backdrop are applied directly to the HWND via DWM attributes and kept in sync with the shared theme managers for the lifetime of the realised window.

**Base type:** [`Window`](https://learn.microsoft.com/dotnet/api/system.windows.window) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/FluenceWindow.cs)

## Constructors

<a id="api-61740d163149"></a>

### FluenceWindow

```csharp
public FluenceWindow()
```

Initializes a new instance of the [FluenceWindow](FluenceWindow.md) class: loads the default style explicitly, attaches the four `SystemCommands` command bindings, and installs the `WindowChrome` that collapses the native frame.

**Remarks:** The theme-manager subscriptions are intentionally deferred to [OnSourceInitialized](FluenceWindow.md#api-e7a5d65e5102) (HWND realisation) so a constructed-but-never- shown window does not pin itself to the static managers' invocation lists.

## Properties

<a id="api-766d38728cc4"></a>

### CornerStyle

```csharp
public WindowCornerPreference CornerStyle { get; set; }
```

Gets or sets the preferred window corner rounding policy for DWM.

<a id="api-6e6b774ddd39"></a>

### DefaultIcon

```csharp
public static ImageSource? DefaultIcon { get; }
```

The Fluence brand icon embedded in this assembly, loaded once and shared (frozen) as the default `Icon` for every [FluenceWindow](FluenceWindow.md). `null` only if the embedded resource cannot be loaded. Exposed so a consumer can apply the same square, no-background brand mark to its own windows.

<a id="api-748d582858ab"></a>

### ExtendsContentIntoTitleBar

```csharp
public bool ExtendsContentIntoTitleBar { get; set; }
```

Gets or sets whether the window content extends into the title bar area, replacing the system title bar with a custom one rendered by the control template.

<a id="api-c6c3ea392e2c"></a>

### HasShadow

```csharp
public bool HasShadow { get; set; }
```

Gets or sets whether the window has a drop shadow. Defaults to true.

<a id="api-883cff06a937"></a>

### IsClosable

```csharp
public bool IsClosable { get; set; }
```

Gets or sets whether the close button is enabled. When false, the button is visible but grayed out.

<a id="api-c14bf2a29e63"></a>

### IsCloseButtonVisible

```csharp
public Visibility IsCloseButtonVisible { get; set; }
```

Gets or sets the visibility of the close button.

<a id="api-8a5c585e8bb3"></a>

### IsMaximizable

```csharp
public bool IsMaximizable { get; set; }
```

Gets or sets whether the maximize button is enabled. When false, the button is visible but grayed out.

<a id="api-b2d6e1f33b16"></a>

### IsMaximizeButtonVisible

```csharp
public Visibility IsMaximizeButtonVisible { get; set; }
```

Gets or sets the visibility of the maximize button.

<a id="api-0f5f3d11f6a2"></a>

### IsMinimizable

```csharp
public bool IsMinimizable { get; set; }
```

Gets or sets whether the minimize button is enabled. When false, the button is visible but grayed out.

<a id="api-4c65e5349ddb"></a>

### IsMinimizeButtonVisible

```csharp
public Visibility IsMinimizeButtonVisible { get; set; }
```

Gets or sets the visibility of the minimize button.

<a id="api-75c1b2674f9f"></a>

### IsMoveable

```csharp
public bool IsMoveable { get; set; }
```

Gets or sets whether the window can be moved by title-bar dragging or the system move command.

<a id="api-b30556e56367"></a>

### MarginMaximized

```csharp
public Thickness MarginMaximized { get; set; }
```

Gets or sets extra margin applied when the window is maximized to avoid overlap with the work area.

<a id="api-26e15eff65da"></a>

### ShowIcon

```csharp
public bool ShowIcon { get; set; }
```

Gets or sets whether the window icon is shown in the title bar.

<a id="api-686e4b28e02a"></a>

### ShowTitle

```csharp
public bool ShowTitle { get; set; }
```

Gets or sets whether the window title text is shown in the title bar.

<a id="api-3e78d59cb2ab"></a>

### SystemBackdropType

```csharp
public WindowBackdropType SystemBackdropType { get; set; }
```

Gets or sets the requested system backdrop (Mica, Acrylic, Tabbed, or none).

<a id="api-edc3f08e969c"></a>

### TitleBar

```csharp
public UIElement? TitleBar { get; set; }
```

Gets or sets custom content displayed in the title bar region, or `null` to use the default title bar.

**Remarks:** Assigning `null` clears custom title-bar content. When [ExtendsContentIntoTitleBar](FluenceWindow.md#api-748d582858ab) is `true`, the control template falls back to the built-in icon and title presentation.

<a id="api-914b641a0cf1"></a>

### TitleBarHeight

```csharp
public double TitleBarHeight { get; set; }
```

Gets or sets the height of the title bar region. Standard = 48, compact = 32.

## Methods

<a id="api-f41157e071d4"></a>

### OnActivated

```csharp
protected override void OnActivated(EventArgs e)
```

Documentation inherited from the [`Window` API](https://learn.microsoft.com/dotnet/api/system.windows.window).

<a id="api-1f8db09c4722"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`Window` API](https://learn.microsoft.com/dotnet/api/system.windows.window).

<a id="api-c8e59892f15c"></a>

### OnClosed

```csharp
protected override void OnClosed(EventArgs e)
```

Documentation inherited from the [`Window` API](https://learn.microsoft.com/dotnet/api/system.windows.window).

<a id="api-7b0a2f7cf586"></a>

### OnDeactivated

```csharp
protected override void OnDeactivated(EventArgs e)
```

Documentation inherited from the [`Window` API](https://learn.microsoft.com/dotnet/api/system.windows.window).

<a id="api-38c170ea3113"></a>

### OnDpiChanged

```csharp
protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
```

Documentation inherited from the [`Window` API](https://learn.microsoft.com/dotnet/api/system.windows.window).

<a id="api-ff2f6b309fbe"></a>

### OnPropertyChanged

```csharp
protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
```

Documentation inherited from the [`Window` API](https://learn.microsoft.com/dotnet/api/system.windows.window).

<a id="api-e7a5d65e5102"></a>

### OnSourceInitialized

```csharp
protected override void OnSourceInitialized(EventArgs e)
```

Documentation inherited from the [`Window` API](https://learn.microsoft.com/dotnet/api/system.windows.window).

<a id="api-5feb9b3aa58e"></a>

### OnStateChanged

```csharp
protected override void OnStateChanged(EventArgs e)
```

Documentation inherited from the [`Window` API](https://learn.microsoft.com/dotnet/api/system.windows.window).

<a id="api-ec88defd9253"></a>

### SetTitleBar

```csharp
public void SetTitleBar(UIElement? titleBar)
```

Sets custom title-bar content, or clears it to restore the default title bar.

**Parameter `titleBar`:** The custom title-bar element, or `null` to clear custom content.

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-69475ebc3de9"></a>

### CornerStyleProperty

```csharp
public static readonly DependencyProperty CornerStyleProperty
```

Identifies the [CornerStyle](FluenceWindow.md#api-766d38728cc4) dependency property.

<a id="api-3b6ae4db9eb0"></a>

### ExtendsContentIntoTitleBarProperty

```csharp
public static readonly DependencyProperty ExtendsContentIntoTitleBarProperty
```

Identifies the [ExtendsContentIntoTitleBar](FluenceWindow.md#api-748d582858ab) dependency property.

<a id="api-f64229b75131"></a>

### HasShadowProperty

```csharp
public static readonly DependencyProperty HasShadowProperty
```

Identifies the [HasShadow](FluenceWindow.md#api-c6c3ea392e2c) dependency property.

<a id="api-1498dce56e23"></a>

### IsClosableProperty

```csharp
public static readonly DependencyProperty IsClosableProperty
```

Identifies the [IsClosable](FluenceWindow.md#api-883cff06a937) dependency property.

<a id="api-496e30368245"></a>

### IsCloseButtonVisibleProperty

```csharp
public static readonly DependencyProperty IsCloseButtonVisibleProperty
```

Identifies the [IsCloseButtonVisible](FluenceWindow.md#api-c14bf2a29e63) dependency property.

<a id="api-38901baeda1f"></a>

### IsMaximizableProperty

```csharp
public static readonly DependencyProperty IsMaximizableProperty
```

Identifies the [IsMaximizable](FluenceWindow.md#api-8a5c585e8bb3) dependency property.

<a id="api-59d7515075f4"></a>

### IsMaximizeButtonVisibleProperty

```csharp
public static readonly DependencyProperty IsMaximizeButtonVisibleProperty
```

Identifies the [IsMaximizeButtonVisible](FluenceWindow.md#api-b2d6e1f33b16) dependency property.

<a id="api-ba7d25c2ec26"></a>

### IsMinimizableProperty

```csharp
public static readonly DependencyProperty IsMinimizableProperty
```

Identifies the [IsMinimizable](FluenceWindow.md#api-0f5f3d11f6a2) dependency property.

<a id="api-c76ffdcccfd8"></a>

### IsMinimizeButtonVisibleProperty

```csharp
public static readonly DependencyProperty IsMinimizeButtonVisibleProperty
```

Identifies the [IsMinimizeButtonVisible](FluenceWindow.md#api-4c65e5349ddb) dependency property.

<a id="api-b55c8c359afe"></a>

### IsMoveableProperty

```csharp
public static readonly DependencyProperty IsMoveableProperty
```

Identifies the [IsMoveable](FluenceWindow.md#api-75c1b2674f9f) dependency property.

<a id="api-8965bff5284b"></a>

### MarginMaximizedProperty

```csharp
public static readonly DependencyProperty MarginMaximizedProperty
```

Identifies the [MarginMaximized](FluenceWindow.md#api-b30556e56367) dependency property.

<a id="api-efc06af05817"></a>

### ShowIconProperty

```csharp
public static readonly DependencyProperty ShowIconProperty
```

Identifies the [ShowIcon](FluenceWindow.md#api-26e15eff65da) dependency property.

<a id="api-306083b53db4"></a>

### ShowTitleProperty

```csharp
public static readonly DependencyProperty ShowTitleProperty
```

Identifies the [ShowTitle](FluenceWindow.md#api-686e4b28e02a) dependency property.

<a id="api-14057a7f8697"></a>

### SystemBackdropTypeProperty

```csharp
public static readonly DependencyProperty SystemBackdropTypeProperty
```

Identifies the [SystemBackdropType](FluenceWindow.md#api-3e78d59cb2ab) dependency property.

<a id="api-59dd3838f9c0"></a>

### TitleBarHeightProperty

```csharp
public static readonly DependencyProperty TitleBarHeightProperty
```

Identifies the [TitleBarHeight](FluenceWindow.md#api-914b641a0cf1) dependency property.

<a id="api-5cbc811c0282"></a>

### TitleBarProperty

```csharp
public static readonly DependencyProperty TitleBarProperty
```

Identifies the [TitleBar](FluenceWindow.md#api-edc3f08e969c) dependency property.

## Fields

<a id="api-e86aac4fcb41"></a>

### IsNotNullConverter

```csharp
public static readonly IValueConverter IsNotNullConverter
```

Converts a value to `true` when it is not null; used by caption-button visibility bindings in the control template (referenced via `{x:Static}`).

## Related types

- [Fluence.Wpf.WindowBackdropType](../Fluence.Wpf/WindowBackdropType.md)
- [Fluence.Wpf.WindowCornerPreference](../Fluence.Wpf/WindowCornerPreference.md)
