# PipsPagerAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class PipsPagerAutomationPeer : FrameworkElementAutomationPeer
```

Exposes [PipsPager](../Fluence.Wpf.Controls/PipsPager.md) to UI Automation as a group named via `AutomationProperties.Name` (the inherited `GetNameCore` behavior). The generated pip buttons and the navigation chevrons surface their own focusable elements beneath the group, each pip named "Page N".

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The [PipsPager](../Fluence.Wpf.Controls/PipsPager.md) control represented by this automation peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/PipsPagerAutomationPeer.cs)

## Constructors

<a id="api-d91a0c9dd075"></a>

### PipsPagerAutomationPeer

```csharp
public PipsPagerAutomationPeer(PipsPager owner)
```

Exposes [PipsPager](../Fluence.Wpf.Controls/PipsPager.md) to UI Automation as a group named via `AutomationProperties.Name` (the inherited `GetNameCore` behavior). The generated pip buttons and the navigation chevrons surface their own focusable elements beneath the group, each pip named "Page N".

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The [PipsPager](../Fluence.Wpf.Controls/PipsPager.md) control represented by this automation peer.

## Methods

<a id="api-a5bb5f629056"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-958c3d4d3572"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Related types

- [Fluence.Wpf.Controls.PipsPager](../Fluence.Wpf.Controls/PipsPager.md)
