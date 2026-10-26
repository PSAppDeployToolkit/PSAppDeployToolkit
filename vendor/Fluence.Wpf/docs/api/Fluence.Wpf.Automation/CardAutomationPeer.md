# CardAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class CardAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
```

Exposes [Card](../Fluence.Wpf.Controls/Card.md) to UI Automation. When the card is clickable it presents as a `Button` with the Invoke pattern; otherwise it presents as a `Group` with no action pattern.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The [Card](../Fluence.Wpf.Controls/Card.md) control represented by this automation peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/CardAutomationPeer.cs)

## Constructors

<a id="api-edf2d0ccffff"></a>

### CardAutomationPeer

```csharp
public CardAutomationPeer(Card owner)
```

Exposes [Card](../Fluence.Wpf.Controls/Card.md) to UI Automation. When the card is clickable it presents as a `Button` with the Invoke pattern; otherwise it presents as a `Group` with no action pattern.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The [Card](../Fluence.Wpf.Controls/Card.md) control represented by this automation peer.

## Methods

<a id="api-d21076eab5e0"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-7c22a5e1b2d7"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-a912c2ca97b5"></a>

### GetPattern

```csharp
public override object GetPattern(PatternInterface patternInterface)
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-9d8aa9c23965"></a>

### Invoke

```csharp
public void Invoke()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Related types

- [Fluence.Wpf.Controls.Card](../Fluence.Wpf.Controls/Card.md)
