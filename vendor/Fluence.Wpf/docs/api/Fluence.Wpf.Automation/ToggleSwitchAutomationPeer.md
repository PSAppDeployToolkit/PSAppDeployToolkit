# ToggleSwitchAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class ToggleSwitchAutomationPeer : FrameworkElementAutomationPeer, IToggleProvider
```

Exposes `ToggleSwitch` to UI Automation with the Toggle pattern.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The `ToggleSwitch` control represented by this automation peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/ToggleSwitchAutomationPeer.cs)

## Constructors

<a id="api-771a67ea1004"></a>

### ToggleSwitchAutomationPeer

```csharp
public ToggleSwitchAutomationPeer(ToggleSwitch owner)
```

Exposes `ToggleSwitch` to UI Automation with the Toggle pattern.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The `ToggleSwitch` control represented by this automation peer.

## Properties

<a id="api-99a5d3cf4125"></a>

### ToggleState

```csharp
public virtual ToggleState ToggleState { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Methods

<a id="api-0348e3e532d1"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-185f34b74b74"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-133a4bc8f673"></a>

### GetNameCore

```csharp
protected override string GetNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-59522aaf9063"></a>

### GetPattern

```csharp
public override object GetPattern(PatternInterface patternInterface)
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-3bda0791707c"></a>

### Toggle

```csharp
public virtual void Toggle()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

**Exception `System.Windows.Automation.ElementNotEnabledException`:** The control is disabled.

## Related types

- [Fluence.Wpf.Controls.ToggleSwitch](../Fluence.Wpf.Controls/ToggleSwitch.md)
