# ImageAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class ImageAutomationPeer : FrameworkElementAutomationPeer
```

Exposes [Image](../Fluence.Wpf.Controls/Image.md) to UI Automation as an image element, but only once the consumer has given it an accessible name. An unnamed image is treated as decorative and is dropped from both the control and content views, so assistive technology never announces a bare unlabelled image element. Authority: WinUI keeps `Image` at `AccessibilityView="Raw"` until it is named, and Microsoft's UI Automation guidance is that decorative graphics stay out of the tree entirely. In-tree precedent: [FontIconAutomationPeer](FontIconAutomationPeer.md) for the both-views exclusion, and [TextBlockAutomationPeer](TextBlockAutomationPeer.md) for keying that exclusion off the accessible name.

**Remarks:** Initializes a new instance of the [ImageAutomationPeer](ImageAutomationPeer.md) class.

**Parameter `owner`:** The [Image](../Fluence.Wpf.Controls/Image.md) control represented by this automation peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/ImageAutomationPeer.cs)

## Constructors

<a id="api-310a17bc8408"></a>

### ImageAutomationPeer

```csharp
public ImageAutomationPeer(Image owner)
```

Exposes [Image](../Fluence.Wpf.Controls/Image.md) to UI Automation as an image element, but only once the consumer has given it an accessible name. An unnamed image is treated as decorative and is dropped from both the control and content views, so assistive technology never announces a bare unlabelled image element. Authority: WinUI keeps `Image` at `AccessibilityView="Raw"` until it is named, and Microsoft's UI Automation guidance is that decorative graphics stay out of the tree entirely. In-tree precedent: [FontIconAutomationPeer](FontIconAutomationPeer.md) for the both-views exclusion, and [TextBlockAutomationPeer](TextBlockAutomationPeer.md) for keying that exclusion off the accessible name.

**Remarks:** Initializes a new instance of the [ImageAutomationPeer](ImageAutomationPeer.md) class.

**Parameter `owner`:** The [Image](../Fluence.Wpf.Controls/Image.md) control represented by this automation peer.

## Methods

<a id="api-a164120bd029"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-9495f817b870"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-5fbac0edd577"></a>

### IsContentElementCore

```csharp
protected override bool IsContentElementCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-2395f3fac66c"></a>

### IsControlElementCore

```csharp
protected override bool IsControlElementCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Related types

- [Fluence.Wpf.Controls.Image](../Fluence.Wpf.Controls/Image.md)
