# TeachingTipAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class TeachingTipAutomationPeer : FrameworkElementAutomationPeer
```

Exposes [TeachingTip](../Fluence.Wpf.Controls/TeachingTip.md) to UI Automation as a pane element.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The [TeachingTip](../Fluence.Wpf.Controls/TeachingTip.md) control represented by this automation peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/TeachingTipAutomationPeer.cs)

## Constructors

<a id="api-2aa2c9d7ec6b"></a>

### TeachingTipAutomationPeer

```csharp
public TeachingTipAutomationPeer(TeachingTip owner)
```

Exposes [TeachingTip](../Fluence.Wpf.Controls/TeachingTip.md) to UI Automation as a pane element.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The [TeachingTip](../Fluence.Wpf.Controls/TeachingTip.md) control represented by this automation peer.

## Methods

<a id="api-289b55053268"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-5a0d362557db"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-5f3c370c5936"></a>

### GetNameCore

```csharp
protected override string GetNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Related types

- [Fluence.Wpf.Controls.TeachingTip](../Fluence.Wpf.Controls/TeachingTip.md)
