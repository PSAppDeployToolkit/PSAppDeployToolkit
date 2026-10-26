# PersonPicture

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class PersonPicture : Control
```

A Fluent Design circular avatar control that displays a profile photo, initials, or a placeholder glyph, with an optional badge. Authority: WinUI 3 PersonPicture.xaml + PersonPicture_themeresources.xaml. Visual states: Photo, Initials, NoPhotoOrInitials, Group (CommonStates); NoBadge, BadgeWithoutImageSource (BadgeStates).

**Base type:** [`Control`](https://learn.microsoft.com/dotnet/api/system.windows.controls.control) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/PersonPicture.cs)

## Constructors

<a id="api-fec9d9cce1d8"></a>

### PersonPicture

```csharp
public PersonPicture()
```

Creates a new `PersonPicture` instance.

## Properties

<a id="api-c9308db1b828"></a>

### BadgeGlyph

```csharp
public string BadgeGlyph { get; set; }
```

Gets or sets a Segoe Fluent Icons glyph shown in the badge. Takes precedence over [BadgeNumber](PersonPicture.md#api-3714b851e7c0) when both are set. Set to `null` (default) to show no glyph badge.

<a id="api-3714b851e7c0"></a>

### BadgeNumber

```csharp
public int BadgeNumber { get; set; }
```

Gets or sets a numeric badge displayed in the bottom-right corner. Set to 0 (default) to hide the badge.

<a id="api-efcd37e8659e"></a>

### DisplayName

```csharp
public string DisplayName { get; set; }
```

Gets or sets the person's display name. Up to two initials are derived automatically unless [Initials](PersonPicture.md#api-4d46f6d62513) is set explicitly.

<a id="api-4d46f6d62513"></a>

### Initials

```csharp
public string Initials { get; set; }
```

Gets or sets explicit initials to display. When set, overrides the initials derived from [DisplayName](PersonPicture.md#api-efcd37e8659e).

<a id="api-7a41bd978584"></a>

### IsGroup

```csharp
public bool IsGroup { get; set; }
```

Gets or sets whether the avatar represents a group rather than an individual. When `true`, the Group (people) glyph is displayed.

<a id="api-e6e1ebb59820"></a>

### ProfilePicture

```csharp
public ImageSource ProfilePicture { get; set; }
```

Gets or sets the profile photo image source.

## Methods

<a id="api-09e1f4a6f2e9"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

<a id="api-06d15652bb4c"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-35bddf071a27"></a>

### BadgeGlyphProperty

```csharp
public static readonly DependencyProperty BadgeGlyphProperty
```

Identifies the [BadgeGlyph](PersonPicture.md#api-c9308db1b828) dependency property.

<a id="api-b31e2c48084c"></a>

### BadgeNumberProperty

```csharp
public static readonly DependencyProperty BadgeNumberProperty
```

Identifies the [BadgeNumber](PersonPicture.md#api-3714b851e7c0) dependency property.

<a id="api-c25b34fde91e"></a>

### DisplayNameProperty

```csharp
public static readonly DependencyProperty DisplayNameProperty
```

Identifies the [DisplayName](PersonPicture.md#api-efcd37e8659e) dependency property.

<a id="api-175fe1244145"></a>

### InitialsProperty

```csharp
public static readonly DependencyProperty InitialsProperty
```

Identifies the [Initials](PersonPicture.md#api-4d46f6d62513) dependency property.

<a id="api-894cd31ddb14"></a>

### IsGroupProperty

```csharp
public static readonly DependencyProperty IsGroupProperty
```

Identifies the [IsGroup](PersonPicture.md#api-7a41bd978584) dependency property.

<a id="api-8f7046782649"></a>

### ProfilePictureProperty

```csharp
public static readonly DependencyProperty ProfilePictureProperty
```

Identifies the [ProfilePicture](PersonPicture.md#api-e6e1ebb59820) dependency property.
