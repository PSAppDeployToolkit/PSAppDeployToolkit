# TimePickerAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class TimePickerAutomationPeer : FrameworkElementAutomationPeer
```

Exposes `TimePicker` to UI Automation as a group whose name reflects the selected time in the current culture's short time format, falling back to [PlaceholderText](../Fluence.Wpf.Controls/TimePicker.md#api-02164da3aede) while no time is selected. The inner field button and selector columns surface their own interaction patterns.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The `TimePicker` control represented by this automation peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/TimePickerAutomationPeer.cs)

## Constructors

<a id="api-a84febae72ab"></a>

### TimePickerAutomationPeer

```csharp
public TimePickerAutomationPeer(TimePicker owner)
```

Exposes `TimePicker` to UI Automation as a group whose name reflects the selected time in the current culture's short time format, falling back to [PlaceholderText](../Fluence.Wpf.Controls/TimePicker.md#api-02164da3aede) while no time is selected. The inner field button and selector columns surface their own interaction patterns.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The `TimePicker` control represented by this automation peer.

## Methods

<a id="api-91cd0e1de6b8"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-8e11aa90bf66"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-16c03cce1c64"></a>

### GetNameCore

```csharp
protected override string GetNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Related types

- [Fluence.Wpf.Controls.TimePicker](../Fluence.Wpf.Controls/TimePicker.md)
