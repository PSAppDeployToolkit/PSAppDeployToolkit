# FlyoutPresenterAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class FlyoutPresenterAutomationPeer : FrameworkElementAutomationPeer
```

Exposes [FlyoutPresenter](../Fluence.Wpf.Controls/FlyoutPresenter.md) to UI Automation as a group, the container role for the content a flyout presents.

**Remarks:** Initializes a new instance. The Microsoft Learn UI Automation Group control type is the documented role for a container that separates the UI into a logical area, which is what a flyout presenter is: transient, so not a Pane, and holding arbitrary content, so not a Menu.

**Parameter `owner`:** The [FlyoutPresenter](../Fluence.Wpf.Controls/FlyoutPresenter.md) represented by this automation peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/FlyoutPresenterAutomationPeer.cs)

## Constructors

<a id="api-b6f55fd35a1a"></a>

### FlyoutPresenterAutomationPeer

```csharp
public FlyoutPresenterAutomationPeer(FlyoutPresenter owner)
```

Exposes [FlyoutPresenter](../Fluence.Wpf.Controls/FlyoutPresenter.md) to UI Automation as a group, the container role for the content a flyout presents.

**Remarks:** Initializes a new instance. The Microsoft Learn UI Automation Group control type is the documented role for a container that separates the UI into a logical area, which is what a flyout presenter is: transient, so not a Pane, and holding arbitrary content, so not a Menu.

**Parameter `owner`:** The [FlyoutPresenter](../Fluence.Wpf.Controls/FlyoutPresenter.md) represented by this automation peer.

## Methods

<a id="api-ba0ac1298b2e"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-7dd9d5592f03"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Related types

- [Fluence.Wpf.Controls.FlyoutPresenter](../Fluence.Wpf.Controls/FlyoutPresenter.md)
