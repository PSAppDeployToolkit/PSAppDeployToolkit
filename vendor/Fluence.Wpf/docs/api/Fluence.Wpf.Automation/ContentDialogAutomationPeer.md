# ContentDialogAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class ContentDialogAutomationPeer : FrameworkElementAutomationPeer
```

Exposes [ContentDialog](../Fluence.Wpf.Controls/ContentDialog.md) to UI Automation as a modal window element.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The [ContentDialog](../Fluence.Wpf.Controls/ContentDialog.md) control represented by this automation peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/ContentDialogAutomationPeer.cs)

## Constructors

<a id="api-9b3e41c472f5"></a>

### ContentDialogAutomationPeer

```csharp
public ContentDialogAutomationPeer(ContentDialog owner)
```

Exposes [ContentDialog](../Fluence.Wpf.Controls/ContentDialog.md) to UI Automation as a modal window element.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The [ContentDialog](../Fluence.Wpf.Controls/ContentDialog.md) control represented by this automation peer.

## Methods

<a id="api-5b2f5b782af6"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-08ded42e864b"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-3641a78f1c5c"></a>

### GetNameCore

```csharp
protected override string GetNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Related types

- [Fluence.Wpf.Controls.ContentDialog](../Fluence.Wpf.Controls/ContentDialog.md)
