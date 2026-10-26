# TitleBarAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class TitleBarAutomationPeer : FrameworkElementAutomationPeer
```

Exposes [TitleBar](../Fluence.Wpf.Controls/TitleBar.md) to UI Automation as a title bar element.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The [TitleBar](../Fluence.Wpf.Controls/TitleBar.md) control represented by this automation peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/TitleBarAutomationPeer.cs)

## Constructors

<a id="api-a244ae1066ad"></a>

### TitleBarAutomationPeer

```csharp
public TitleBarAutomationPeer(TitleBar owner)
```

Exposes [TitleBar](../Fluence.Wpf.Controls/TitleBar.md) to UI Automation as a title bar element.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The [TitleBar](../Fluence.Wpf.Controls/TitleBar.md) control represented by this automation peer.

## Methods

<a id="api-e78a30f28d44"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-dfd16e76bd68"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-c84257d7fbaa"></a>

### GetNameCore

```csharp
protected override string GetNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

**Remarks:** An explicit automation name wins; otherwise the displayed title is the name. This is the order WinUI's own TitleBarAutomationPeer uses (microsoft-ui-xaml, src/controls/dev/TitleBar/TitleBarAutomationPeer.cpp, GetNameCore).

## Related types

- [Fluence.Wpf.Controls.TitleBar](../Fluence.Wpf.Controls/TitleBar.md)
