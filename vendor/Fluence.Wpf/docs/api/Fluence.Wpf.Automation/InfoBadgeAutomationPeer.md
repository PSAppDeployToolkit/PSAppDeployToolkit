# InfoBadgeAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class InfoBadgeAutomationPeer : FrameworkElementAutomationPeer
```

Exposes [InfoBadge](../Fluence.Wpf.Controls/InfoBadge.md) to UI Automation as a text element whose name is the badge value.

**Remarks:** Initializes a new instance. WinUI ships no InfoBadge peer, so the control type follows the Microsoft Learn UI Automation Text control type, which covers a small piece of status text that conveys information but takes no input.

**Parameter `owner`:** The [InfoBadge](../Fluence.Wpf.Controls/InfoBadge.md) control represented by this automation peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/InfoBadgeAutomationPeer.cs)

## Constructors

<a id="api-a5b798cbbea4"></a>

### InfoBadgeAutomationPeer

```csharp
public InfoBadgeAutomationPeer(InfoBadge owner)
```

Exposes [InfoBadge](../Fluence.Wpf.Controls/InfoBadge.md) to UI Automation as a text element whose name is the badge value.

**Remarks:** Initializes a new instance. WinUI ships no InfoBadge peer, so the control type follows the Microsoft Learn UI Automation Text control type, which covers a small piece of status text that conveys information but takes no input.

**Parameter `owner`:** The [InfoBadge](../Fluence.Wpf.Controls/InfoBadge.md) control represented by this automation peer.

## Methods

<a id="api-15c82a70bbfb"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-eeb36ca725d9"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-bba1ecc38bc4"></a>

### GetNameCore

```csharp
protected override string GetNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

**Remarks:** An explicit automation name wins. Otherwise the numeric value is the name, except for the sentinel -1, which renders as a dot and carries no number to announce.

## Related types

- [Fluence.Wpf.Controls.InfoBadge](../Fluence.Wpf.Controls/InfoBadge.md)
