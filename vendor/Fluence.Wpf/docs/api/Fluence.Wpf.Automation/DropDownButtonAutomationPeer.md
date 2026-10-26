# DropDownButtonAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class DropDownButtonAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
```

Exposes `DropDownButton` to UI Automation with the ExpandCollapse pattern.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The `DropDownButton` control represented by this automation peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/DropDownButtonAutomationPeer.cs)

## Constructors

<a id="api-635fd0aecc0e"></a>

### DropDownButtonAutomationPeer

```csharp
public DropDownButtonAutomationPeer(DropDownButton owner)
```

Exposes `DropDownButton` to UI Automation with the ExpandCollapse pattern.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The `DropDownButton` control represented by this automation peer.

## Properties

<a id="api-03f0a3d10882"></a>

### ExpandCollapseState

```csharp
public virtual ExpandCollapseState ExpandCollapseState { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Methods

<a id="api-050e50146209"></a>

### Collapse

```csharp
public virtual void Collapse()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

**Exception `System.Windows.Automation.ElementNotEnabledException`:** The control is disabled.

<a id="api-2f6e52bb3893"></a>

### Expand

```csharp
public virtual void Expand()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

**Exception `System.Windows.Automation.ElementNotEnabledException`:** The control is disabled.

<a id="api-7e438ce7eef3"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-a60967e11fe3"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-7c5ef15c69e9"></a>

### GetPattern

```csharp
public override object GetPattern(PatternInterface patternInterface)
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Related types

- [Fluence.Wpf.Controls.DropDownButton](../Fluence.Wpf.Controls/DropDownButton.md)
