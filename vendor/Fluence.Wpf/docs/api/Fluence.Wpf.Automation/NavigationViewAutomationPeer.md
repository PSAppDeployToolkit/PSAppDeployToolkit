# NavigationViewAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class NavigationViewAutomationPeer : FrameworkElementAutomationPeer, ISelectionProvider
```

Exposes `NavigationView` to UI Automation as a selection list.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The `NavigationView` control represented by this automation peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/NavigationViewAutomationPeer.cs)

## Constructors

<a id="api-b9d05a735df7"></a>

### NavigationViewAutomationPeer

```csharp
public NavigationViewAutomationPeer(NavigationView owner)
```

Exposes `NavigationView` to UI Automation as a selection list.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The `NavigationView` control represented by this automation peer.

## Properties

<a id="api-b564611e467c"></a>

### CanSelectMultiple

```csharp
public virtual bool CanSelectMultiple { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-0ca880d36da1"></a>

### IsSelectionRequired

```csharp
public virtual bool IsSelectionRequired { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Methods

<a id="api-aa74ac85a39f"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-554ba5a898d6"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-7fcb3e2bdddf"></a>

### GetPattern

```csharp
public override object GetPattern(PatternInterface patternInterface)
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-b15be501f618"></a>

### GetSelection

```csharp
public virtual IRawElementProviderSimple[] GetSelection()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Related types

- [Fluence.Wpf.Controls.NavigationView](../Fluence.Wpf.Controls/NavigationView.md)
