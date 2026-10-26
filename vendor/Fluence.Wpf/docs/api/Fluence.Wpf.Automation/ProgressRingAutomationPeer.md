# ProgressRingAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class ProgressRingAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider
```

Exposes `ProgressRing` to UI Automation as a progress indicator.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The `ProgressRing` control represented by this automation peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/ProgressRingAutomationPeer.cs)

## Constructors

<a id="api-86396e1c8f3d"></a>

### ProgressRingAutomationPeer

```csharp
public ProgressRingAutomationPeer(ProgressRing owner)
```

Exposes `ProgressRing` to UI Automation as a progress indicator.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The `ProgressRing` control represented by this automation peer.

## Properties

<a id="api-64e123f97a3f"></a>

### IsReadOnly

```csharp
public virtual bool IsReadOnly { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-c2c5880273cc"></a>

### LargeChange

```csharp
public virtual double LargeChange { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-aef3002f76da"></a>

### Maximum

```csharp
public virtual double Maximum { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-0275db9b9a55"></a>

### Minimum

```csharp
public virtual double Minimum { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-c5e9e86b48c4"></a>

### SmallChange

```csharp
public virtual double SmallChange { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-af9ef44de101"></a>

### Value

```csharp
public virtual double Value { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Methods

<a id="api-d406b56463fb"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-1be8c0576f4a"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-c6763e9d64d0"></a>

### GetPattern

```csharp
public override object GetPattern(PatternInterface patternInterface)
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-fed2a8b7aed6"></a>

### SetValue

```csharp
public virtual void SetValue(double value)
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

**Exception `System.Windows.Automation.ElementNotEnabledException`:** The ring is disabled.

**Exception `System.InvalidOperationException`:** Always, for an enabled ring: progress is reported by the application, so the pattern is read-only and a client that sets a value is told so rather than left to assume it took.

## Related types

- [Fluence.Wpf.Controls.ProgressRing](../Fluence.Wpf.Controls/ProgressRing.md)
