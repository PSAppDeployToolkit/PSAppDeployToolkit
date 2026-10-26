# BreadcrumbBarAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class BreadcrumbBarAutomationPeer : FrameworkElementAutomationPeer
```

Exposes [BreadcrumbBar](../Fluence.Wpf.Controls/BreadcrumbBar.md) to UI Automation as a group named via `AutomationProperties.Name` (the inherited `GetNameCore` behavior). The individual crumbs surface their own focusable elements beneath the group.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The [BreadcrumbBar](../Fluence.Wpf.Controls/BreadcrumbBar.md) control represented by this automation peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/BreadcrumbBarAutomationPeer.cs)

## Constructors

<a id="api-de9ee93c9fba"></a>

### BreadcrumbBarAutomationPeer

```csharp
public BreadcrumbBarAutomationPeer(BreadcrumbBar owner)
```

Exposes [BreadcrumbBar](../Fluence.Wpf.Controls/BreadcrumbBar.md) to UI Automation as a group named via `AutomationProperties.Name` (the inherited `GetNameCore` behavior). The individual crumbs surface their own focusable elements beneath the group.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The [BreadcrumbBar](../Fluence.Wpf.Controls/BreadcrumbBar.md) control represented by this automation peer.

## Methods

<a id="api-d0978d792d3b"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-c163c6cd7301"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Related types

- [Fluence.Wpf.Controls.BreadcrumbBar](../Fluence.Wpf.Controls/BreadcrumbBar.md)
