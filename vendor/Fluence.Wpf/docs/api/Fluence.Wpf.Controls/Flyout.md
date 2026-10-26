# Flyout

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class Flyout : FlyoutBase
```

Represents a flyout that displays arbitrary content on the Fluent flyout surface, mirroring the WinUI 3 `Flyout` control. The content is hosted in a [FlyoutPresenter](FlyoutPresenter.md) inside a light-dismiss popup.

**Base type:** [FlyoutBase](FlyoutBase.md)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/Flyout.cs)

## Constructors

<a id="api-5cc835ef5ecc"></a>

### Flyout

```csharp
public Flyout()
```

Creates a new `Flyout` instance.

## Properties

<a id="api-6a9021f0c04c"></a>

### Content

```csharp
public object? Content { get; set; }
```

Gets or sets the content shown in the flyout.

**Remarks:** The content is hosted in an unparented popup, so it does not live in the placement target's name scope: `ElementName` (and `RelativeSource FindAncestor` walks above the presenter) bindings inside the content do not resolve. The presenter inherits the placement target's `DataContext` while the flyout is open, so plain `Binding` paths against the anchor's view model work as expected.

<a id="api-71cef41ad0e8"></a>

### FlyoutPresenterStyle

```csharp
public Style? FlyoutPresenterStyle { get; set; }
```

Gets or sets the style applied to the [FlyoutPresenter](FlyoutPresenter.md) that hosts [Content](Flyout.md#api-6a9021f0c04c). When `null`, the default themed presenter style is used.

## Methods

<a id="api-e1778c91a2e6"></a>

### CreatePresenter

```csharp
protected override FrameworkElement CreatePresenter()
```

Creates (or returns the cached) [FlyoutPresenter](FlyoutPresenter.md) with its `Content` bound to [Content](Flyout.md#api-6a9021f0c04c) and [FlyoutPresenterStyle](Flyout.md#api-71cef41ad0e8) applied when set.

**Returns:** The presenter hosting the flyout content.

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-53a50df6a713"></a>

### ContentProperty

```csharp
public static readonly DependencyProperty ContentProperty
```

Identifies the [Content](Flyout.md#api-6a9021f0c04c) dependency property.

<a id="api-28b2c9f517db"></a>

### FlyoutPresenterStyleProperty

```csharp
public static readonly DependencyProperty FlyoutPresenterStyleProperty
```

Identifies the [FlyoutPresenterStyle](Flyout.md#api-71cef41ad0e8) dependency property.

## Related types

- [Fluence.Wpf.Controls.FlyoutBase](FlyoutBase.md)
