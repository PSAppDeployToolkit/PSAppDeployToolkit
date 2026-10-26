# HyperlinkButtonAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class HyperlinkButtonAutomationPeer : ButtonAutomationPeer
```

Exposes [HyperlinkButton](../Fluence.Wpf.Controls/HyperlinkButton.md) to UI Automation with the Hyperlink control type.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The [HyperlinkButton](../Fluence.Wpf.Controls/HyperlinkButton.md) control represented by this automation peer.

**Base type:** [`ButtonAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.buttonautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/HyperlinkButtonAutomationPeer.cs)

## Constructors

<a id="api-3a9a5ef275e7"></a>

### HyperlinkButtonAutomationPeer

```csharp
public HyperlinkButtonAutomationPeer(HyperlinkButton owner)
```

Exposes [HyperlinkButton](../Fluence.Wpf.Controls/HyperlinkButton.md) to UI Automation with the Hyperlink control type.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The [HyperlinkButton](../Fluence.Wpf.Controls/HyperlinkButton.md) control represented by this automation peer.

## Methods

<a id="api-94e0e115224e"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`ButtonAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.buttonautomationpeer).

<a id="api-3ed63a21c63d"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`ButtonAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.buttonautomationpeer).

## Related types

- [Fluence.Wpf.Controls.HyperlinkButton](../Fluence.Wpf.Controls/HyperlinkButton.md)
