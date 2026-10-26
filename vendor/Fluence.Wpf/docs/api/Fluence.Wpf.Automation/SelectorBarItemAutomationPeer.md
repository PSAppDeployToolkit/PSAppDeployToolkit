# SelectorBarItemAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class SelectorBarItemAutomationPeer : ListBoxItemWrapperAutomationPeer
```

Exposes [SelectorBarItem](../Fluence.Wpf.Controls/SelectorBarItem.md) to UI Automation with the item's [Text](../Fluence.Wpf.Controls/SelectorBarItem.md#api-8a00a2b51d96) as its name.

**Remarks:** The base `ListBoxItemWrapperAutomationPeer` derives the name from the item's content, which a SelectorBar item leaves unused: the label lives on [Text](../Fluence.Wpf.Controls/SelectorBarItem.md#api-8a00a2b51d96) instead, following WinUI's own item surface. Without this peer a text-only item announces nothing.

**Parameter `owner`:** The [SelectorBarItem](../Fluence.Wpf.Controls/SelectorBarItem.md) represented by this automation peer.

**Base type:** [`ListBoxItemWrapperAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.listboxitemwrapperautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/SelectorBarItemAutomationPeer.cs)

## Constructors

<a id="api-904e7a45b733"></a>

### SelectorBarItemAutomationPeer

```csharp
public SelectorBarItemAutomationPeer(SelectorBarItem owner)
```

Exposes [SelectorBarItem](../Fluence.Wpf.Controls/SelectorBarItem.md) to UI Automation with the item's [Text](../Fluence.Wpf.Controls/SelectorBarItem.md#api-8a00a2b51d96) as its name.

**Remarks:** The base `ListBoxItemWrapperAutomationPeer` derives the name from the item's content, which a SelectorBar item leaves unused: the label lives on [Text](../Fluence.Wpf.Controls/SelectorBarItem.md#api-8a00a2b51d96) instead, following WinUI's own item surface. Without this peer a text-only item announces nothing.

**Parameter `owner`:** The [SelectorBarItem](../Fluence.Wpf.Controls/SelectorBarItem.md) represented by this automation peer.

## Methods

<a id="api-b6f03c03d3e7"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`ListBoxItemWrapperAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.listboxitemwrapperautomationpeer).

<a id="api-cf26f0169787"></a>

### GetLocalizedControlTypeCore

```csharp
protected override string GetLocalizedControlTypeCore()
```

Documentation inherited from the [`ListBoxItemWrapperAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.listboxitemwrapperautomationpeer).

**Remarks:** WinUI reports "SelectorBarItem" as the localized control type rather than letting the item fall back to the generic list-item announcement.

<a id="api-9f0a83610499"></a>

### GetNameCore

```csharp
protected override string GetNameCore()
```

Documentation inherited from the [`ListBoxItemWrapperAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.listboxitemwrapperautomationpeer).

**Remarks:** WinUI's own peer resolves the name in four steps (SelectorBarItemAutomationPeer.cpp): an explicit automation name, then [Text](../Fluence.Wpf.Controls/SelectorBarItem.md#api-8a00a2b51d96), then the string form of the item's child content, then the control type name. This peer follows the same order, so an icon-only item still announces something.

## Related types

- [Fluence.Wpf.Controls.SelectorBarItem](../Fluence.Wpf.Controls/SelectorBarItem.md)
