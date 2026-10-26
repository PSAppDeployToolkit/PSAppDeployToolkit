# SplitButtonAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class SplitButtonAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider, IExpandCollapseProvider
```

Exposes `SplitButton` to UI Automation with the Invoke pattern (primary half) and the ExpandCollapse pattern (flyout half).

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The `SplitButton` control represented by this automation peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/SplitButtonAutomationPeer.cs)

## Constructors

<a id="api-e5632373a9a1"></a>

### SplitButtonAutomationPeer

```csharp
public SplitButtonAutomationPeer(SplitButton owner)
```

Exposes `SplitButton` to UI Automation with the Invoke pattern (primary half) and the ExpandCollapse pattern (flyout half).

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The `SplitButton` control represented by this automation peer.

## Properties

<a id="api-00875447d241"></a>

### ExpandCollapseState

```csharp
public virtual ExpandCollapseState ExpandCollapseState { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Methods

<a id="api-4bce5d65ce6e"></a>

### Collapse

```csharp
public virtual void Collapse()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

**Exception `System.Windows.Automation.ElementNotEnabledException`:** The control is disabled.

<a id="api-c49f86adfb06"></a>

### Expand

```csharp
public virtual void Expand()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

**Exception `System.Windows.Automation.ElementNotEnabledException`:** The control is disabled.

<a id="api-af2ff2c3b4bb"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-072f5c1739f1"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-02d98aacb6ed"></a>

### GetPattern

```csharp
public override object GetPattern(PatternInterface patternInterface)
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-9c4e4c563ddd"></a>

### Invoke

```csharp
public virtual void Invoke()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

**Exception `System.Windows.Automation.ElementNotEnabledException`:** The control is disabled.

## Related types

- [Fluence.Wpf.Controls.SplitButton](../Fluence.Wpf.Controls/SplitButton.md)
