# DatePickerAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class DatePickerAutomationPeer : FrameworkElementAutomationPeer
```

Exposes `DatePicker` to UI Automation as a group whose name reflects the selected date in the current culture's short date format, falling back to [PlaceholderText](../Fluence.Wpf.Controls/DatePicker.md#api-10c873bde915) while no date is selected. The inner field button and selector columns surface their own interaction patterns.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The `DatePicker` control represented by this automation peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/DatePickerAutomationPeer.cs)

## Constructors

<a id="api-795e2342de31"></a>

### DatePickerAutomationPeer

```csharp
public DatePickerAutomationPeer(DatePicker owner)
```

Exposes `DatePicker` to UI Automation as a group whose name reflects the selected date in the current culture's short date format, falling back to [PlaceholderText](../Fluence.Wpf.Controls/DatePicker.md#api-10c873bde915) while no date is selected. The inner field button and selector columns surface their own interaction patterns.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The `DatePicker` control represented by this automation peer.

## Methods

<a id="api-5cc3ab97fe09"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-0625237f0af9"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-da47f8090e02"></a>

### GetNameCore

```csharp
protected override string GetNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Related types

- [Fluence.Wpf.Controls.DatePicker](../Fluence.Wpf.Controls/DatePicker.md)
