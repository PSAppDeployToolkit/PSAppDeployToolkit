# FlyoutBase

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public abstract class FlyoutBase : DependencyObject
```

Represents the base class for flyout controls that display lightweight UI in a light-dismiss `Popup` anchored to a placement target, mirroring the WinUI 3 `FlyoutBase` contract.

**Remarks:** The popup is created lazily on the first [ShowAt](FlyoutBase.md#api-30da961e26ea) call. It is pinned open and [FlyoutBase](FlyoutBase.md) owns the light dismiss itself: a press outside the flyout, a press on the owning window's caption or borders, the window moving, and the application losing the foreground all close it, as does Escape pressed inside it. A press on the placement target closes the flyout and is swallowed, so the button that opened it toggles it shut rather than reopening it. The popup uses a custom placement callback so the flyout is centered on the facing edge of its placement target, matching WinUI. Derived classes supply the popup child via [CreatePresenter](FlyoutBase.md#api-7aed18979ec4).

**Base type:** [`DependencyObject`](https://learn.microsoft.com/dotnet/api/system.windows.dependencyobject) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/FlyoutBase.cs)

## Constructors

<a id="api-cb648465bd08"></a>

### FlyoutBase

```csharp
protected FlyoutBase()
```

Creates a new `FlyoutBase` instance.

## Properties

<a id="api-995e4cb9606c"></a>

### IsOpen

```csharp
public bool IsOpen { get; }
```

Gets a value indicating whether the flyout is currently open.

<a id="api-d925483a0009"></a>

### Placement

```csharp
public FlyoutPlacementMode Placement { get; set; }
```

Gets or sets where the flyout opens relative to its placement target.

<a id="api-0d71eeb1db68"></a>

### ShouldConstrainToRootBounds

```csharp
public bool ShouldConstrainToRootBounds { get; set; }
```

Gets or sets a value indicating whether the flyout should stay within the bounds of the XAML root. Accepted for WinUI signature compatibility; WPF popups are positioned by the OS, so the value is not currently enforced.

## Methods

<a id="api-7aed18979ec4"></a>

### CreatePresenter

```csharp
protected abstract FrameworkElement CreatePresenter()
```

Creates the element that presents the flyout content as the popup child. Called once when the popup is created; implementations may cache and return the same instance.

**Returns:** The presenter element hosted as the popup child.

<a id="api-2c46a5795dcb"></a>

### GetAttachedFlyout

```csharp
public static FlyoutBase? GetAttachedFlyout(FrameworkElement element)
```

Gets the flyout attached to the specified element via [AttachedFlyoutProperty](FlyoutBase.md#api-06185a4ddc5f).

**Parameter `element`:** The element the flyout is attached to.

**Returns:** The attached flyout, or `null` when none is attached.

**Exception `System.ArgumentNullException`:** `element` is `null`.

<a id="api-90eaaffce1ee"></a>

### Hide

```csharp
public void Hide()
```

Hides the flyout. Raises [Closing](FlyoutBase.md#api-b19856a13e79) first; the close is abandoned when a handler sets [Cancel](../Fluence.Wpf/FlyoutBaseClosingEventArgs.md#api-057e7c6f12d1) to `true`. [Closed](FlyoutBase.md#api-20e3360d0a4f) is raised once the popup has closed.

<a id="api-a058522ea3ad"></a>

### SetAttachedFlyout

```csharp
public static void SetAttachedFlyout(FrameworkElement element, FlyoutBase? value)
```

Sets the flyout attached to the specified element via [AttachedFlyoutProperty](FlyoutBase.md#api-06185a4ddc5f).

**Parameter `element`:** The element to attach the flyout to.

**Parameter `value`:** The flyout to attach, or `null` to detach.

**Exception `System.ArgumentNullException`:** `element` is `null`.

<a id="api-30da961e26ea"></a>

### ShowAt

```csharp
public void ShowAt(FrameworkElement placementTarget)
```

Shows the flyout placed relative to the specified element. Raises [Opening](FlyoutBase.md#api-98561c1a3ace) before the popup opens and [Opened](FlyoutBase.md#api-64a2a27ea97c) after, then moves focus to the presenter. The presenter inherits the placement target's `DataContext` for the lifetime of the popup so bindings inside the flyout content resolve against the anchor's view model.

**Remarks:** The popup is open by the time this returns. When another element holds the mouse capture, which is the case inside a button's Click handler, the flyout takes the light-dismiss capture a second time once that gesture is over: a popup that took it mid-gesture loses it again the moment the button lets go, which closes the flyout as it appears.

**Parameter `placementTarget`:** The element to anchor the flyout to.

**Exception `System.ArgumentNullException`:** `placementTarget` is `null`.

<a id="api-aa4e047c3a70"></a>

### ShowAttachedFlyout

```csharp
public static void ShowAttachedFlyout(FrameworkElement flyoutOwner)
```

Shows the flyout attached to the specified element via [AttachedFlyoutProperty](FlyoutBase.md#api-06185a4ddc5f), anchored to that element. Does nothing when no flyout is attached.

**Parameter `flyoutOwner`:** The element whose attached flyout should be shown.

**Exception `System.ArgumentNullException`:** `flyoutOwner` is `null`.

## Events

<a id="api-20e3360d0a4f"></a>

### Closed

```csharp
public event EventHandler? Closed
```

Occurs after the flyout has closed, whether through [Hide](FlyoutBase.md#api-90eaaffce1ee) or light dismiss.

<a id="api-b19856a13e79"></a>

### Closing

```csharp
public event EventHandler<FlyoutBaseClosingEventArgs>? Closing
```

Occurs before the flyout closes through [Hide](FlyoutBase.md#api-90eaaffce1ee). Set [Cancel](../Fluence.Wpf/FlyoutBaseClosingEventArgs.md#api-057e7c6f12d1) to `true` to keep the flyout open. Light-dismiss closes bypass this event and raise only [Closed](FlyoutBase.md#api-20e3360d0a4f).

<a id="api-64a2a27ea97c"></a>

### Opened

```csharp
public event EventHandler? Opened
```

Occurs after the flyout has opened.

<a id="api-98561c1a3ace"></a>

### Opening

```csharp
public event EventHandler? Opening
```

Occurs before the flyout opens.

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-06185a4ddc5f"></a>

### AttachedFlyoutProperty

```csharp
public static readonly DependencyProperty AttachedFlyoutProperty
```

Identifies the AttachedFlyout attached property, which associates a flyout with an arbitrary element so it can later be opened via [ShowAttachedFlyout](FlyoutBase.md#api-aa4e047c3a70).

<a id="api-5b950e1c156a"></a>

### PlacementProperty

```csharp
public static readonly DependencyProperty PlacementProperty
```

Identifies the [Placement](FlyoutBase.md#api-d925483a0009) dependency property.

<a id="api-358de27b8f72"></a>

### ShouldConstrainToRootBoundsProperty

```csharp
public static readonly DependencyProperty ShouldConstrainToRootBoundsProperty
```

Identifies the [ShouldConstrainToRootBounds](FlyoutBase.md#api-0d71eeb1db68) dependency property.

## Related types

- [Fluence.Wpf.FlyoutBaseClosingEventArgs](../Fluence.Wpf/FlyoutBaseClosingEventArgs.md)
- [Fluence.Wpf.FlyoutPlacementMode](../Fluence.Wpf/FlyoutPlacementMode.md)
