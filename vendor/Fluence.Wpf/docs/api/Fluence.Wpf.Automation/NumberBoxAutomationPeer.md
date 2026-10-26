# NumberBoxAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class NumberBoxAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider
```

Exposes `NumberBox` to UI Automation as a spinner with range value.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The `NumberBox` control represented by this automation peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/NumberBoxAutomationPeer.cs)

## Constructors

<a id="api-9e68936331a7"></a>

### NumberBoxAutomationPeer

```csharp
public NumberBoxAutomationPeer(NumberBox owner)
```

Exposes `NumberBox` to UI Automation as a spinner with range value.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The `NumberBox` control represented by this automation peer.

## Properties

<a id="api-56a758fae96f"></a>

### IsReadOnly

```csharp
public virtual bool IsReadOnly { get; }
```

Always `false`. `NumberBox` has no read-only mode; disabled state is conveyed via `IsEnabled`, not `IsReadOnly`.

<a id="api-9273f7a9c7b5"></a>

### LargeChange

```csharp
public virtual double LargeChange { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-c04a1a1eaa15"></a>

### Maximum

```csharp
public virtual double Maximum { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-9ba832f0240b"></a>

### Minimum

```csharp
public virtual double Minimum { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-6c2d5ca86a3e"></a>

### SmallChange

```csharp
public virtual double SmallChange { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-af2a7d2f41cf"></a>

### Value

```csharp
public virtual double Value { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Methods

<a id="api-a7e4824028f3"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-4d107a93c07a"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-76552d7f8943"></a>

### GetNameCore

```csharp
protected override string GetNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-abaad0ab62df"></a>

### GetPattern

```csharp
public override object GetPattern(PatternInterface patternInterface)
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-de2d3ba60960"></a>

### SetValue

```csharp
public virtual void SetValue(double value)
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

**Exception `System.Windows.Automation.ElementNotEnabledException`:** The control is disabled.

## Related types

- [Fluence.Wpf.Controls.NumberBox](../Fluence.Wpf.Controls/NumberBox.md)
