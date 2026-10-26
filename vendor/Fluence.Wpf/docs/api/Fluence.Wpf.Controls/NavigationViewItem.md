# NavigationViewItem

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class NavigationViewItem : ListBoxItem
```

Represents an entry inside a [NavigationView](NavigationView.md) pane.

**Remarks:** Inspired by WinUI3's NavigationView.

**Base type:** [ListBoxItem](ListBoxItem.md)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/NavigationViewItem.cs)

## Constructors

<a id="api-dacaf2a58493"></a>

### NavigationViewItem

```csharp
public NavigationViewItem()
```

Creates a new `NavigationViewItem` instance.

## Properties

<a id="api-54ec1970f298"></a>

### Icon

```csharp
public object Icon { get; set; }
```

Gets or sets the icon content for this item (typically a [FontIcon](FontIcon.md)).

<a id="api-afc33170407e"></a>

### InfoBadge

```csharp
public object InfoBadge { get; set; }
```

Gets or sets an [InfoBadge](InfoBadge.md) element shown on this item.

<a id="api-348715bb88bc"></a>

### IsChildItem

```csharp
public bool IsChildItem { get; set; }
```

Gets or sets whether this item is a child entry in an expanded navigation section. Child entries keep their selection indicator aligned with the content column.

<a id="api-97935b00c7b3"></a>

### IsPressed

```csharp
public bool IsPressed { get; }
```

Gets whether the item is currently being pressed by a pointer.

## Methods

<a id="api-968fc0c404a2"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [ListBoxItem API](../Fluence.Wpf.Controls/ListBoxItem.md).

<a id="api-efe1d36bb9f3"></a>

### OnKeyDown

```csharp
protected override void OnKeyDown(KeyEventArgs e)
```

Documentation inherited from the [ListBoxItem API](../Fluence.Wpf.Controls/ListBoxItem.md).

<a id="api-46b186e26feb"></a>

### OnMouseLeave

```csharp
protected override void OnMouseLeave(MouseEventArgs e)
```

Documentation inherited from the [ListBoxItem API](../Fluence.Wpf.Controls/ListBoxItem.md).

<a id="api-75ef391d0d83"></a>

### OnMouseLeftButtonDown

```csharp
protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
```

Documentation inherited from the [ListBoxItem API](../Fluence.Wpf.Controls/ListBoxItem.md).

<a id="api-517def0cd551"></a>

### OnMouseLeftButtonUp

```csharp
protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
```

Documentation inherited from the [ListBoxItem API](../Fluence.Wpf.Controls/ListBoxItem.md).

<a id="api-de3adc40d544"></a>

### OnPreviewMouseLeftButtonDown

```csharp
protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
```

Documentation inherited from the [ListBoxItem API](../Fluence.Wpf.Controls/ListBoxItem.md).

**Remarks:** Parent [NavigationView](NavigationView.md) derives from `Selector` (not [ListBox](ListBox.md)). [ListBoxItem](ListBoxItem.md) handles mouse on the bubbling route and may mark the event handled before selection sync runs; we handle preview mouse and sync selection on the parent so clicks always update selection.

<a id="api-56b6b85ddbe5"></a>

### OnPropertyChanged

```csharp
protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
```

Documentation inherited from the [ListBoxItem API](../Fluence.Wpf.Controls/ListBoxItem.md).

**Remarks:** Evicts the parent [NavigationView](NavigationView.md)'s cached Top-overflow natural width when a measure-affecting property changes while a width is cached. An item sitting collapsed in the overflow menu is never measured, so it never raises the `SizeChanged` that is the ordinary eviction path; a content or font change while overflowed would otherwise keep the stale width forever. `VisibilityProperty` is excluded because the overflow pass itself toggles it, and that toggle carries no width change.

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-9f3a02ddf304"></a>

### IconProperty

```csharp
public static readonly DependencyProperty IconProperty
```

Identifies the [Icon](NavigationViewItem.md#api-54ec1970f298) dependency property.

<a id="api-8c3255593b40"></a>

### InfoBadgeProperty

```csharp
public static readonly DependencyProperty InfoBadgeProperty
```

Identifies the [InfoBadge](NavigationViewItem.md#api-afc33170407e) dependency property.

<a id="api-ab43480c0e03"></a>

### IsChildItemProperty

```csharp
public static readonly DependencyProperty IsChildItemProperty
```

Identifies the [IsChildItem](NavigationViewItem.md#api-348715bb88bc) dependency property.

<a id="api-c97b200db275"></a>

### IsPressedProperty

```csharp
public static readonly DependencyProperty IsPressedProperty
```

Identifies the read-only [IsPressed](NavigationViewItem.md#api-97935b00c7b3) dependency property.

## Related types

- [Fluence.Wpf.Controls.ListBoxItem](ListBoxItem.md)
