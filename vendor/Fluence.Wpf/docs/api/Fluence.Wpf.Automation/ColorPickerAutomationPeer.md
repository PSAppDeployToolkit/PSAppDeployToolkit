# ColorPickerAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class ColorPickerAutomationPeer : FrameworkElementAutomationPeer
```

Exposes `ColorPicker` to UI Automation as a group whose name reflects the selected color as a hex string (eight digits when alpha editing is enabled, six otherwise), unless an explicit `AutomationProperties.Name` is set. The inner sliders and hex text box surface their own interaction patterns.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The `ColorPicker` control represented by this automation peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/ColorPickerAutomationPeer.cs)

## Constructors

<a id="api-9bca244df0f0"></a>

### ColorPickerAutomationPeer

```csharp
public ColorPickerAutomationPeer(ColorPicker owner)
```

Exposes `ColorPicker` to UI Automation as a group whose name reflects the selected color as a hex string (eight digits when alpha editing is enabled, six otherwise), unless an explicit `AutomationProperties.Name` is set. The inner sliders and hex text box surface their own interaction patterns.

**Remarks:** Initializes a new instance.

**Parameter `owner`:** The `ColorPicker` control represented by this automation peer.

## Methods

<a id="api-e31b8cf5a750"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-39343a8da263"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-da0648ae6cf4"></a>

### GetNameCore

```csharp
protected override string GetNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Related types

- [Fluence.Wpf.Controls.ColorPicker](../Fluence.Wpf.Controls/ColorPicker.md)
