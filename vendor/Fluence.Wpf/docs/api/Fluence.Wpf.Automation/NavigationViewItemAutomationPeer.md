# NavigationViewItemAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public sealed class NavigationViewItemAutomationPeer : FrameworkElementAutomationPeer, ISelectionItemProvider, IInvokeProvider
```

Exposes `NavigationViewItem` to UI Automation as a selectable list item.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The `NavigationViewItem` control represented by this automation peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/NavigationViewItemAutomationPeer.cs)

## Constructors

<a id="api-b3a02c483a71"></a>

### NavigationViewItemAutomationPeer

```csharp
public NavigationViewItemAutomationPeer(NavigationViewItem owner)
```

Exposes `NavigationViewItem` to UI Automation as a selectable list item.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The `NavigationViewItem` control represented by this automation peer.

## Properties

<a id="api-a10417a51a06"></a>

### IsSelected

```csharp
public bool IsSelected { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-425f858f1fc0"></a>

### SelectionContainer

```csharp
public IRawElementProviderSimple? SelectionContainer { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Methods

<a id="api-041de54753b5"></a>

### AddToSelection

```csharp
public void AddToSelection()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

**Exception `System.Windows.Automation.ElementNotEnabledException`:** The item is disabled.

<a id="api-3baf45156ec3"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-8e18095709d4"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-d70cb09dd8ac"></a>

### GetPattern

```csharp
public override object GetPattern(PatternInterface patternInterface)
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-67019ea063b0"></a>

### Invoke

```csharp
public void Invoke()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

**Exception `System.Windows.Automation.ElementNotEnabledException`:** The item is disabled.

<a id="api-2e26b1a9aad3"></a>

### RemoveFromSelection

```csharp
public void RemoveFromSelection()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

**Exception `System.Windows.Automation.ElementNotEnabledException`:** The item is disabled.

<a id="api-33b160665309"></a>

### SelectItem

```csharp
public void SelectItem()
```

Selects the associated navigation view item.

**Exception `System.Windows.Automation.ElementNotEnabledException`:** The item is disabled.

## Related types

- [Fluence.Wpf.Controls.NavigationViewItem](../Fluence.Wpf.Controls/NavigationViewItem.md)
