# CommandBarFlyout

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class CommandBarFlyout : FlyoutBase
```

Represents a flyout that shows a horizontal bar of primary commands with an optional expandable overflow menu of secondary commands, mirroring the WinUI 3 `CommandBarFlyout` control.

**Remarks:** Commands are usually [AppBarButton](AppBarButton.md) instances. Clicking a command inside the flyout runs its normal `Click` handling and then dismisses the flyout, matching the WinUI behavior. The flyout participates in the full [FlyoutBase](FlyoutBase.md) contract: it can be opened via [ShowAt](FlyoutBase.md#api-30da961e26ea) or attached to an element with [SetAttachedFlyout](FlyoutBase.md#api-a058522ea3ad) and opened via [ShowAttachedFlyout](FlyoutBase.md#api-aa4e047c3a70). The WinUI `AlwaysExpanded` option is omitted for v1; the flyout always opens collapsed and the overflow menu is toggled by the presenter's more button.

**Base type:** [FlyoutBase](FlyoutBase.md)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/CommandBarFlyout.cs)

## Constructors

<a id="api-33b0bf323f9c"></a>

### CommandBarFlyout

```csharp
public CommandBarFlyout()
```

Creates a new `CommandBarFlyout` instance.

## Properties

<a id="api-b7c204502534"></a>

### PrimaryCommands

```csharp
public ObservableCollection<UIElement> PrimaryCommands { get; }
```

Gets the commands shown in the always-visible horizontal primary bar.

<a id="api-4a9e57edb115"></a>

### SecondaryCommands

```csharp
public ObservableCollection<UIElement> SecondaryCommands { get; }
```

Gets the commands shown in the expandable overflow menu below the primary bar. The presenter's more button is only visible while this collection is non-empty.

## Methods

<a id="api-f9b0b7897286"></a>

### CreatePresenter

```csharp
protected override FrameworkElement CreatePresenter()
```

Creates (or returns the cached) [CommandBarFlyoutPresenter](CommandBarFlyoutPresenter.md) bound to this flyout's [PrimaryCommands](CommandBarFlyout.md#api-b7c204502534) and [SecondaryCommands](CommandBarFlyout.md#api-4a9e57edb115).

**Returns:** The presenter hosting the command bar.

## Related types

- [Fluence.Wpf.Controls.FlyoutBase](FlyoutBase.md)
