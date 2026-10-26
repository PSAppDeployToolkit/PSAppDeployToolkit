# TextBlockAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class TextBlockAutomationPeer : FrameworkElementAutomationPeer
```

Exposes [TextBlock](../Fluence.Wpf.Controls/TextBlock.md) to UI Automation as a text element.

**Remarks:** [TextBlock](../Fluence.Wpf.Controls/TextBlock.md) wraps a `TextBlock` inside a `ContentControl` template to support the Fluent typography ramp. Without this peer, WPF creates a generic peer that reports `ControlType.Pane`, placing a spurious container in the UIA tree and breaking `AutomationProperties.LabeledBy` relationships that expect `ControlType.Text`.



Only [TextBlock](../Fluence.Wpf.Controls/TextBlock.md) instances with an explicit `NameProperty` are visible in the UIA control view. Instances without a name are excluded so decorative body-copy text is not announced; the name is never derived from [Text](../Fluence.Wpf.Controls/TextBlock.md#api-55f86c94798b) automatically.

**Parameter `owner`:** The [TextBlock](../Fluence.Wpf.Controls/TextBlock.md) control represented by this automation peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/TextBlockAutomationPeer.cs)

## Constructors

<a id="api-b13285eced05"></a>

### TextBlockAutomationPeer

```csharp
public TextBlockAutomationPeer(TextBlock owner)
```

Exposes [TextBlock](../Fluence.Wpf.Controls/TextBlock.md) to UI Automation as a text element.

**Remarks:** [TextBlock](../Fluence.Wpf.Controls/TextBlock.md) wraps a `TextBlock` inside a `ContentControl` template to support the Fluent typography ramp. Without this peer, WPF creates a generic peer that reports `ControlType.Pane`, placing a spurious container in the UIA tree and breaking `AutomationProperties.LabeledBy` relationships that expect `ControlType.Text`.



Only [TextBlock](../Fluence.Wpf.Controls/TextBlock.md) instances with an explicit `NameProperty` are visible in the UIA control view. Instances without a name are excluded so decorative body-copy text is not announced; the name is never derived from [Text](../Fluence.Wpf.Controls/TextBlock.md#api-55f86c94798b) automatically.

**Parameter `owner`:** The [TextBlock](../Fluence.Wpf.Controls/TextBlock.md) control represented by this automation peer.

## Methods

<a id="api-efbe138a65f3"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-9474bca6cb43"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-c5752389ee9c"></a>

### IsControlElementCore

```csharp
protected override bool IsControlElementCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Related types

- [Fluence.Wpf.Controls.TextBlock](../Fluence.Wpf.Controls/TextBlock.md)
