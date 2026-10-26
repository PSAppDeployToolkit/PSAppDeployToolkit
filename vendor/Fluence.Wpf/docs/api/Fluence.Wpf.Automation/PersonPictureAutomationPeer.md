# PersonPictureAutomationPeer

[C# API](../index.md) / [Fluence.Wpf.Automation](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Automation`

```csharp
public class PersonPictureAutomationPeer : FrameworkElementAutomationPeer
```

Exposes `PersonPicture` to UI Automation as an image element with an accessible name derived from the control's identity text. Narrator announces who the avatar represents using [DisplayName](../Fluence.Wpf.Controls/PersonPicture.md#api-efcd37e8659e) when set, falling back to [Initials](../Fluence.Wpf.Controls/PersonPicture.md#api-4d46f6d62513), and always deferring to an explicit `NameProperty` value first.

**Remarks:** Initializes a new instance of the [PersonPictureAutomationPeer](PersonPictureAutomationPeer.md) class.

**Parameter `owner`:** The `PersonPicture` control represented by this automation peer.

**Base type:** [`FrameworkElementAutomationPeer`](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Automation/PersonPictureAutomationPeer.cs)

## Constructors

<a id="api-93660865d40d"></a>

### PersonPictureAutomationPeer

```csharp
public PersonPictureAutomationPeer(PersonPicture owner)
```

Exposes `PersonPicture` to UI Automation as an image element with an accessible name derived from the control's identity text. Narrator announces who the avatar represents using [DisplayName](../Fluence.Wpf.Controls/PersonPicture.md#api-efcd37e8659e) when set, falling back to [Initials](../Fluence.Wpf.Controls/PersonPicture.md#api-4d46f6d62513), and always deferring to an explicit `NameProperty` value first.

**Remarks:** Initializes a new instance of the [PersonPictureAutomationPeer](PersonPictureAutomationPeer.md) class.

**Parameter `owner`:** The `PersonPicture` control represented by this automation peer.

## Methods

<a id="api-ae148163f19c"></a>

### GetAutomationControlTypeCore

```csharp
protected override AutomationControlType GetAutomationControlTypeCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-2ad982d7e2c5"></a>

### GetClassNameCore

```csharp
protected override string GetClassNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

<a id="api-8ff157509c72"></a>

### GetNameCore

```csharp
protected override string GetNameCore()
```

Documentation inherited from the [`FrameworkElementAutomationPeer` API](https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.frameworkelementautomationpeer).

## Related types

- [Fluence.Wpf.Controls.PersonPicture](../Fluence.Wpf.Controls/PersonPicture.md)
