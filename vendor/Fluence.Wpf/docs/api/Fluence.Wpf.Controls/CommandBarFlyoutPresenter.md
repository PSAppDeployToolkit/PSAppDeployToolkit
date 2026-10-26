# CommandBarFlyoutPresenter

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class CommandBarFlyoutPresenter : Control
```

Displays the commands of a [CommandBarFlyout](CommandBarFlyout.md) on the canonical Fluent flyout surface: a horizontal primary bar, a more button shown while secondary commands exist, and a collapsible overflow menu below the bar toggled via [IsExpanded](CommandBarFlyoutPresenter.md#api-68270cfaefd6). The themed template lives in `Themes/Controls/CommandBarFlyout.xaml`.

**Remarks:** Clicking any [AppBarButton](AppBarButton.md) hosted in the presenter (other than the more button) dismisses the owning flyout after the button's normal `Click` handling, matching the WinUI command-dismiss behavior. The WinUI `AlwaysExpanded` option is omitted for v1, so the overflow collapses again whenever the flyout closes.

**Base type:** [`Control`](https://learn.microsoft.com/dotnet/api/system.windows.controls.control) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/CommandBarFlyoutPresenter.cs)

## Constructors

<a id="api-e2973f20c81e"></a>

### CommandBarFlyoutPresenter

```csharp
public CommandBarFlyoutPresenter()
```

Initializes a new instance of the [CommandBarFlyoutPresenter](CommandBarFlyoutPresenter.md) class and subscribes a handled-too `ClickEvent` handler so any command invoked inside the presenter also dismisses the owning flyout.

## Properties

<a id="api-68270cfaefd6"></a>

### IsExpanded

```csharp
public bool IsExpanded { get; set; }
```

Gets or sets a value indicating whether the secondary (overflow) command area below the primary bar is visible. Toggled by the more button; reset to `false` whenever the owning flyout closes.

## Methods

<a id="api-5819bc557893"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-923e6768addb"></a>

### IsExpandedProperty

```csharp
public static readonly DependencyProperty IsExpandedProperty
```

Identifies the [IsExpanded](CommandBarFlyoutPresenter.md#api-68270cfaefd6) dependency property.
