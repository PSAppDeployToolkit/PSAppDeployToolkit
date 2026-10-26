# FlyoutPresenter

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class FlyoutPresenter : ContentControl
```

Displays the content of a [Flyout](Flyout.md) on the canonical Fluent flyout surface (flyout background fill, flyout stroke, overlay corner radius). The themed template lives in `Themes/Controls/FlyoutPresenter.xaml`.

**Base type:** [`ContentControl`](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/FlyoutPresenter.cs)

## Constructors

<a id="api-d9236c886b07"></a>

### FlyoutPresenter

```csharp
public FlyoutPresenter()
```

Initializes a new instance of the [FlyoutPresenter](FlyoutPresenter.md) class and subscribes the open reveal to `Loaded`. The presenter instance is reused across popup opens and re-raises Loaded on every open, so the reveal replays each time the flyout shows.

## Methods

<a id="api-02f9fec2c16e"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).
