# AutoSuggestBoxAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class AutoSuggestBoxAutomationPeer : FrameworkElementAutomationPeer, IValueProvider
```

Exposes `AutoSuggestBox` to UI Automation as an edit control with the Value pattern surfacing its [Text](../Fluence.Wpf.Controls/AutoSuggestBox.md#api-0f249b6a936a).

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The `AutoSuggestBox` control represented by this automation peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/AutoSuggestBoxAutomationPeer.cs)

## Constructors

<a id="api-33946b364ec9"></a>

### AutoSuggestBoxAutomationPeer

```csharp
public AutoSuggestBoxAutomationPeer(AutoSuggestBox owner)
```

Exposes `AutoSuggestBox` to UI Automation as an edit control with the Value pattern surfacing its [Text](../Fluence.Wpf.Controls/AutoSuggestBox.md#api-0f249b6a936a).

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The `AutoSuggestBox` control represented by this automation peer.

## Properties

<a id="api-99f15f8aee3c"></a>

### IsReadOnly

```csharp
public virtual bool IsReadOnly { get; }
```

Always `false`. `AutoSuggestBox` has no read-only mode; disabled state is conveyed via `IsEnabled`, not `IsReadOnly`.

<a id="api-bbbbf1150354"></a>

### Value

```csharp
public virtual string Value { get; }
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Methods

<a id="api-8094399b5fe3"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-406839b1a04b"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-e4f38cd4d68d"></a>

### GetNameCore

```csharp
protected override string GetNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-7b7e9db058f5"></a>

### GetPattern

```csharp
public override object GetPattern(PatternInterface patternInterface)
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-0b2ddb860422"></a>

### SetValue

```csharp
public virtual void SetValue(string value)
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

**Exception `System.Windows.Automation.ElementNotEnabledException`:** The control is disabled.

## Related types

- [Fluence.Wpf.Controls.AutoSuggestBox](../Fluence.Wpf.Controls/AutoSuggestBox.md)
