# RatingControlAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class RatingControlAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider
```

Exposes `RatingControl` to UI Automation as a slider with range value. Implements `IRangeValueProvider` so assistive technologies such as Narrator can read and set the rating value.

**Remarks:** Initializes a new instance of the [RatingControlAutomationPeer](RatingControlAutomationPeer.md) class.

**Parameter `owner`:** The `RatingControl` control represented by this automation peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/RatingControlAutomationPeer.cs)

## Constructors

<a id="api-f3f4ba18f1ea"></a>

### RatingControlAutomationPeer

```csharp
public RatingControlAutomationPeer(RatingControl owner)
```

Exposes `RatingControl` to UI Automation as a slider with range value. Implements `IRangeValueProvider` so assistive technologies such as Narrator can read and set the rating value.

**Remarks:** Initializes a new instance of the [RatingControlAutomationPeer](RatingControlAutomationPeer.md) class.

**Parameter `owner`:** The `RatingControl` control represented by this automation peer.

## Properties

<a id="api-02f5ef6667fb"></a>

### IsReadOnly

```csharp
public virtual bool IsReadOnly { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-34165dcc692e"></a>

### LargeChange

```csharp
public virtual double LargeChange { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-55911024688d"></a>

### Maximum

```csharp
public virtual double Maximum { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-984c3128b1d5"></a>

### Minimum

```csharp
public virtual double Minimum { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-f814baf4ac06"></a>

### SmallChange

```csharp
public virtual double SmallChange { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-8dac0b382bf8"></a>

### Value

```csharp
public virtual double Value { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Methods

<a id="api-7b760322355d"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-2fd0068fe27b"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-3e2011e9c186"></a>

### GetPattern

```csharp
public override object GetPattern(PatternInterface patternInterface)
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-41d10f0aef0c"></a>

### SetValue

```csharp
public virtual void SetValue(double value)
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

**Exception `System.Windows.Automation.ElementNotEnabledException`:** The control is disabled.

**Exception `System.InvalidOperationException`:** The control is read-only.

## Related types

- [Fluence.Wpf.Controls.RatingControl](../Fluence.Wpf.Controls/RatingControl.md)
