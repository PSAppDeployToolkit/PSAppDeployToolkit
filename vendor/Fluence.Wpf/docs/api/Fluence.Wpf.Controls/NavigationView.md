# NavigationView

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class NavigationView : Selector
```

A navigation control with a collapsible pane and content area, similar to WinUI NavigationView. Uses a single shared selection indicator that animates between items.

**Base type:** [`Selector`](https://learn.microsoft.com/dotnet/api/system.windows.controls.primitives.selector) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/NavigationView.cs)

## Constructors

<a id="api-2e6a49745ac6"></a>

### NavigationView

```csharp
public NavigationView()
```

Initializes a new instance of the [NavigationView](NavigationView.md) class.

## Properties

<a id="api-c7d34a4afead"></a>

### Content

```csharp
public object Content { get; set; }
```

Gets or sets the content hosted in the main area.

<a id="api-413a1e3283b9"></a>

### ContentBackground

```csharp
public Brush ContentBackground { get; set; }
```

Gets or sets the background brush for the content area.

<a id="api-2f79492e7b8f"></a>

### FooterMenuItems

```csharp
public ObservableCollection<object> FooterMenuItems { get; }
```

Gets the collection of pinned footer entries, rendered below the main menu items. Footer entries are [NavigationViewItem](NavigationViewItem.md) instances that participate in the same single-selection model and selection indicator as the main menu, mirroring the WinUI `NavigationView.FooterMenuItems` region.

<a id="api-4ad31733e7b6"></a>

### Header

```csharp
public object Header { get; set; }
```

Gets or sets header content displayed beside the navigation chrome.

<a id="api-8908782781b6"></a>

### HeaderTemplate

```csharp
public DataTemplate HeaderTemplate { get; set; }
```

Gets or sets the DataTemplate used to display the [Header](NavigationView.md#api-4ad31733e7b6).

<a id="api-fca109d9a2ed"></a>

### IsBackButtonVisible

```csharp
public bool IsBackButtonVisible { get; set; }
```

Gets or sets whether the back button is shown.

<a id="api-1ffea1914c16"></a>

### IsBackEnabled

```csharp
public bool IsBackEnabled { get; set; }
```

Gets or sets whether the back button can be invoked.

<a id="api-72dfd6c4ca54"></a>

### IsPaneOpen

```csharp
public bool IsPaneOpen { get; set; }
```

Gets or sets whether the left pane is expanded.

<a id="api-d3f6b3e0755f"></a>

### IsPaneToggleButtonVisible

```csharp
public bool IsPaneToggleButtonVisible { get; set; }
```

Gets or sets whether the pane collapse/expand toggle button is shown in left pane modes.

<a id="api-fb0de3e53519"></a>

### PaneDisplayMode

```csharp
public NavigationViewPaneDisplayMode PaneDisplayMode { get; set; }
```

Gets or sets whether the pane is shown on the left or across the top.

<a id="api-349022dc7403"></a>

### PaneFooter

```csharp
public object PaneFooter { get; set; }
```

Gets or sets content at the end of the pane (footer).

<a id="api-2eb110cd1bca"></a>

### PaneHeader

```csharp
public object PaneHeader { get; set; }
```

Gets or sets content at the start of the pane chrome (title area).

<a id="api-4509849723d4"></a>

### SelectionFollowsFocus

```csharp
public bool SelectionFollowsFocus { get; set; }
```

Gets or sets whether keyboard focus on an item selects it immediately.

## Methods

<a id="api-a86c03342d88"></a>

### ClearContainerForItemOverride

```csharp
protected override void ClearContainerForItemOverride(DependencyObject element, object item)
```

Documentation inherited from the [`Selector` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.primitives.selector).

<a id="api-43e79979a3f9"></a>

### GetContainerForItemOverride

```csharp
protected override DependencyObject GetContainerForItemOverride()
```

Documentation inherited from the [`Selector` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.primitives.selector).

<a id="api-2440a1db373e"></a>

### IsItemItsOwnContainerOverride

```csharp
protected override bool IsItemItsOwnContainerOverride(object item)
```

Documentation inherited from the [`Selector` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.primitives.selector).

<a id="api-a0b2be7d263b"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`Selector` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.primitives.selector).

<a id="api-dba5d12b9cd7"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [`Selector` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.primitives.selector).

<a id="api-85cd8bca4199"></a>

### OnItemsChanged

```csharp
protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
```

Documentation inherited from the [`Selector` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.primitives.selector).

<a id="api-2efdefe6ff17"></a>

### OnPreviewGotKeyboardFocus

```csharp
protected override void OnPreviewGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
```

Documentation inherited from the [`Selector` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.primitives.selector).

<a id="api-e182cf1d0c61"></a>

### OnSelectionChanged

```csharp
protected override void OnSelectionChanged(SelectionChangedEventArgs e)
```

Documentation inherited from the [`Selector` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.primitives.selector).

<a id="api-e887b81a2644"></a>

### PrepareContainerForItemOverride

```csharp
protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
```

Documentation inherited from the [`Selector` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.primitives.selector).

<a id="api-8950dc619fb9"></a>

### SelectFooterMenuItem

```csharp
public void SelectFooterMenuItem(NavigationViewItem item)
```

Programmatically selects a [FooterMenuItems](NavigationView.md#api-2f79492e7b8f) entry as if the user had invoked it: clears any main-menu selection, marks the footer item selected, moves the footer selection indicator, and raises [ItemInvoked](NavigationView.md#api-90edd0f83051). No-op if the item is not a current footer entry.

**Parameter `item`:** The footer item to select.

## Events

<a id="api-316ab5cd3396"></a>

### BackRequested

```csharp
public event EventHandler<NavigationViewBackRequestedEventArgs>? BackRequested
```

Occurs when the back button is invoked.

<a id="api-90edd0f83051"></a>

### ItemInvoked

```csharp
public event EventHandler<NavigationViewItemInvokedEventArgs>? ItemInvoked
```

Occurs when a navigation item is invoked before selection changes.

<a id="api-084f8d7959c2"></a>

### PaneClosed

```csharp
public event EventHandler? PaneClosed
```

Occurs when the pane has closed (collapsed in left mode).

<a id="api-785de7c7043d"></a>

### PaneOpening

```csharp
public event EventHandler? PaneOpening
```

Occurs when the pane is opening (expanded in left mode).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-15502a8d8c4c"></a>

### ContentBackgroundProperty

```csharp
public static readonly DependencyProperty ContentBackgroundProperty
```

Identifies the [ContentBackground](NavigationView.md#api-413a1e3283b9) dependency property.

<a id="api-8ed566b89334"></a>

### ContentProperty

```csharp
public static readonly DependencyProperty ContentProperty
```

Identifies the [Content](NavigationView.md#api-c7d34a4afead) dependency property.

<a id="api-e6daa1f65b61"></a>

### FooterMenuItemsProperty

```csharp
public static readonly DependencyProperty FooterMenuItemsProperty
```

Identifies the [FooterMenuItems](NavigationView.md#api-2f79492e7b8f) dependency property.

<a id="api-8310b792d2d1"></a>

### HeaderProperty

```csharp
public static readonly DependencyProperty HeaderProperty
```

Identifies the [Header](NavigationView.md#api-4ad31733e7b6) dependency property.

<a id="api-4f4bb15dfbd9"></a>

### HeaderTemplateProperty

```csharp
public static readonly DependencyProperty HeaderTemplateProperty
```

Identifies the [HeaderTemplate](NavigationView.md#api-8908782781b6) dependency property.

<a id="api-1ac03a93aeae"></a>

### IsBackButtonVisibleProperty

```csharp
public static readonly DependencyProperty IsBackButtonVisibleProperty
```

Identifies the [IsBackButtonVisible](NavigationView.md#api-fca109d9a2ed) dependency property.

<a id="api-a649f879184b"></a>

### IsBackEnabledProperty

```csharp
public static readonly DependencyProperty IsBackEnabledProperty
```

Identifies the [IsBackEnabled](NavigationView.md#api-1ffea1914c16) dependency property.

<a id="api-85f79bfc8bc6"></a>

### IsPaneOpenProperty

```csharp
public static readonly DependencyProperty IsPaneOpenProperty
```

Identifies the [IsPaneOpen](NavigationView.md#api-72dfd6c4ca54) dependency property.

<a id="api-71a27b8e3056"></a>

### IsPaneToggleButtonVisibleProperty

```csharp
public static readonly DependencyProperty IsPaneToggleButtonVisibleProperty
```

Identifies the [IsPaneToggleButtonVisible](NavigationView.md#api-d3f6b3e0755f) dependency property.

<a id="api-7782ea8fce59"></a>

### PaneDisplayModeProperty

```csharp
public static readonly DependencyProperty PaneDisplayModeProperty
```

Identifies the [PaneDisplayMode](NavigationView.md#api-fb0de3e53519) dependency property.

<a id="api-844659aec6c7"></a>

### PaneFooterProperty

```csharp
public static readonly DependencyProperty PaneFooterProperty
```

Identifies the [PaneFooter](NavigationView.md#api-349022dc7403) dependency property.

<a id="api-feffe0f6433c"></a>

### PaneHeaderProperty

```csharp
public static readonly DependencyProperty PaneHeaderProperty
```

Identifies the [PaneHeader](NavigationView.md#api-2eb110cd1bca) dependency property.

<a id="api-72ccba5278f2"></a>

### SelectionFollowsFocusProperty

```csharp
public static readonly DependencyProperty SelectionFollowsFocusProperty
```

Identifies the [SelectionFollowsFocus](NavigationView.md#api-4509849723d4) dependency property.

## Related types

- [Fluence.Wpf.Controls.NavigationViewItem](NavigationViewItem.md)
- [Fluence.Wpf.NavigationViewBackRequestedEventArgs](../Fluence.Wpf/NavigationViewBackRequestedEventArgs.md)
- [Fluence.Wpf.NavigationViewItemInvokedEventArgs](../Fluence.Wpf/NavigationViewItemInvokedEventArgs.md)
- [Fluence.Wpf.NavigationViewPaneDisplayMode](../Fluence.Wpf/NavigationViewPaneDisplayMode.md)
