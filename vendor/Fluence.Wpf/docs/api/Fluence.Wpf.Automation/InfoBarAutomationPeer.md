# InfoBarAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class InfoBarAutomationPeer : FrameworkElementAutomationPeer
```

Exposes [InfoBar](../Fluence.Wpf.Controls/InfoBar.md) to UI Automation as a status bar element.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The [InfoBar](../Fluence.Wpf.Controls/InfoBar.md) control represented by this automation peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/InfoBarAutomationPeer.cs)

## Constructors

<a id="api-d6e9969fbc50"></a>

### InfoBarAutomationPeer

```csharp
public InfoBarAutomationPeer(InfoBar owner)
```

Exposes [InfoBar](../Fluence.Wpf.Controls/InfoBar.md) to UI Automation as a status bar element.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The [InfoBar](../Fluence.Wpf.Controls/InfoBar.md) control represented by this automation peer.

## Methods

<a id="api-525be988fad8"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-678f673a7f31"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-28f993e578cf"></a>

### GetNameCore

```csharp
protected override string GetNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Related types

- [Fluence.Wpf.Controls.InfoBar](../Fluence.Wpf.Controls/InfoBar.md)
