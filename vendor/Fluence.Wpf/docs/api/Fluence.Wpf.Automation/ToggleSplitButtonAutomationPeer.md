# ToggleSplitButtonAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class ToggleSplitButtonAutomationPeer : FrameworkElementAutomationPeer, IToggleProvider, IExpandCollapseProvider
```

Exposes `ToggleSplitButton` to UI Automation with the Toggle pattern (primary half) and the ExpandCollapse pattern (flyout half). The Invoke pattern is deliberately not offered: WinUI exposes only Toggle and ExpandCollapse on its ToggleSplitButton peer, and an Invoke routed through the SplitButton peer would raise Click without toggling, diverging from a real primary-half click.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The `ToggleSplitButton` control represented by this automation peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/ToggleSplitButtonAutomationPeer.cs)

## Constructors

<a id="api-73b251d7c671"></a>

### ToggleSplitButtonAutomationPeer

```csharp
public ToggleSplitButtonAutomationPeer(ToggleSplitButton owner)
```

Exposes `ToggleSplitButton` to UI Automation with the Toggle pattern (primary half) and the ExpandCollapse pattern (flyout half). The Invoke pattern is deliberately not offered: WinUI exposes only Toggle and ExpandCollapse on its ToggleSplitButton peer, and an Invoke routed through the SplitButton peer would raise Click without toggling, diverging from a real primary-half click.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The `ToggleSplitButton` control represented by this automation peer.

## Properties

<a id="api-77a0f9c28e09"></a>

### ExpandCollapseState

```csharp
public virtual ExpandCollapseState ExpandCollapseState { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-7e39503e1aba"></a>

### ToggleState

```csharp
public virtual ToggleState ToggleState { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Methods

<a id="api-7bbccdbd9f8e"></a>

### Collapse

```csharp
public virtual void Collapse()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

**Exception `System.Windows.Automation.ElementNotEnabledException`:** The control is disabled.

<a id="api-8d995474bec4"></a>

### Expand

```csharp
public virtual void Expand()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

**Exception `System.Windows.Automation.ElementNotEnabledException`:** The control is disabled.

<a id="api-220696b1573f"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-14a96f8cceab"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-72b99cac99ee"></a>

### GetPattern

```csharp
public override object GetPattern(PatternInterface patternInterface)
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-97ef188428f2"></a>

### Toggle

```csharp
public virtual void Toggle()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

**Exception `System.Windows.Automation.ElementNotEnabledException`:** The control is disabled.

## Related types

- [Fluence.Wpf.Controls.ToggleSplitButton](../Fluence.Wpf.Controls/ToggleSplitButton.md)
