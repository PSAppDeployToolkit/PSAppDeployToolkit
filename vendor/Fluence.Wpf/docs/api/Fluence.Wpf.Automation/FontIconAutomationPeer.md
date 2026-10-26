# FontIconAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class FontIconAutomationPeer : FrameworkElementAutomationPeer
```

Automation peer for [FontIcon](../Fluence.Wpf.Controls/FontIcon.md) that excludes the purely decorative glyph from the UI Automation control and content views, matching WinUI's `AccessibilityView="Raw"` behavior. The glyph carries no meaning of its own; the labelled parent control (for example a `Button` with an `AutomationProperties.Name`) is what screen readers announce.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The [FontIcon](../Fluence.Wpf.Controls/FontIcon.md) that owns this peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/FontIconAutomationPeer.cs)

## Constructors

<a id="api-ac4017558a1e"></a>

### FontIconAutomationPeer

```csharp
public FontIconAutomationPeer(FontIcon owner)
```

Automation peer for [FontIcon](../Fluence.Wpf.Controls/FontIcon.md) that excludes the purely decorative glyph from the UI Automation control and content views, matching WinUI's `AccessibilityView="Raw"` behavior. The glyph carries no meaning of its own; the labelled parent control (for example a `Button` with an `AutomationProperties.Name`) is what screen readers announce.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The [FontIcon](../Fluence.Wpf.Controls/FontIcon.md) that owns this peer.

## Methods

<a id="api-ca1497caae52"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-bc0e50cef5de"></a>

### IsContentElementCore

```csharp
protected override bool IsContentElementCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-eab82cb202c9"></a>

### IsControlElementCore

```csharp
protected override bool IsControlElementCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Related types

- [Fluence.Wpf.Controls.FontIcon](../Fluence.Wpf.Controls/FontIcon.md)
