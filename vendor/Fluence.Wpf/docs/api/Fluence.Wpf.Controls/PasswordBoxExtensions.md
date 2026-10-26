# PasswordBoxExtensions

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public static class PasswordBoxExtensions
```

Provides attached properties that extend the sealed `PasswordBox` with the Fluent Design chrome this library ships: a placeholder, a rounded corner radius, a reveal (peek) button, a Caps Lock indicator, and a password strength meter.

**Remarks:** `PasswordBox` is sealed, so this library cannot subclass it the way it subclasses `TextBox`. Sealing blocks inheritance but not templating, so the library styles the native control instead. Everything that would otherwise be a dependency property on a derived type lives here as an attached property. The control a consumer places in their tree stays a real `PasswordBox`: one focus target, one automation element, the operating system secure store behind `SecurePassword`, and the native clipboard, IME, and context menu policy.



The implicit Fluent style sets [IsFluentDecoratedProperty](PasswordBoxExtensions.md#api-6302e3307c05), which is what attaches the behavior. An application that wants the native behavior back can clear it.

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/PasswordBoxExtensions.cs)

## Methods

<a id="api-557fc1356e5b"></a>

### GetCornerRadius

```csharp
public static CornerRadius GetCornerRadius(this PasswordBox obj)
```

Gets the corner radius of the password box chrome.

**Parameter `obj`:** The password box to read from or write to.

**Returns:** The corner radius.

<a id="api-ca9757a33fc1"></a>

### GetHasPassword

```csharp
public static bool GetHasPassword(this PasswordBox obj)
```

Gets whether the field currently holds a password. The Fluent template uses this to hide the placeholder and the reveal button, because the native `Password` is not a dependency property and so cannot be observed by a trigger.

**Parameter `obj`:** The password box to read from or write to.

**Returns:** `true` when the field holds a password; otherwise `false`.

<a id="api-f405615dce51"></a>

### GetIsFluentDecorated

```csharp
public static bool GetIsFluentDecorated(this PasswordBox obj)
```

Gets whether the Fluent password box behavior is attached to the specified element.

**Parameter `obj`:** The password box to read from or write to.

**Returns:** `true` when the behavior is attached; otherwise `false`.

<a id="api-94baa5d83182"></a>

### GetIsPasswordRevealed

```csharp
public static bool GetIsPasswordRevealed(this PasswordBox obj)
```

Gets whether the password is currently revealed (peeked at).

**Parameter `obj`:** The password box to read from or write to.

**Returns:** `true` while the password is revealed; otherwise `false`.

<a id="api-b1281b9dd214"></a>

### GetPasswordStrength

```csharp
public static int GetPasswordStrength(this PasswordBox obj)
```

Gets the strength score from 0 (weakest) to 4 (strongest). The behavior recomputes it whenever the password changes; a caller may overwrite it to score the password itself.

**Parameter `obj`:** The password box to read from or write to.

**Returns:** The strength score.

<a id="api-d0dfdbef4ee9"></a>

### GetPlaceholderText

```csharp
public static string GetPlaceholderText(this PasswordBox obj)
```

Gets the placeholder text shown while the field is empty.

**Parameter `obj`:** The password box to read from or write to.

**Returns:** The placeholder text.

<a id="api-dd3cd679180d"></a>

### GetRevealButtonEnabled

```csharp
public static bool GetRevealButtonEnabled(this PasswordBox obj)
```

Gets whether the reveal button is offered once the field holds a password.

**Parameter `obj`:** The password box to read from or write to.

**Returns:** `true` when the reveal button is enabled; otherwise `false`.

<a id="api-82e6cccf9247"></a>

### GetShowCapsLockIndicator

```csharp
public static bool GetShowCapsLockIndicator(this PasswordBox obj)
```

Gets whether the Caps Lock indicator is shown while Caps Lock is active. Opt in per box.

**Parameter `obj`:** The password box to read from or write to.

**Returns:** `true` when the indicator is enabled; otherwise `false`.

<a id="api-c32dedbd19b9"></a>

### GetShowPasswordStrength

```csharp
public static bool GetShowPasswordStrength(this PasswordBox obj)
```

Gets whether the password strength meter is displayed. Opt in per box.

**Parameter `obj`:** The password box to read from or write to.

**Returns:** `true` when the meter is shown; otherwise `false`.

<a id="api-1a1a8331cba4"></a>

### SetCornerRadius

```csharp
public static void SetCornerRadius(this PasswordBox obj, CornerRadius value)
```

Sets the corner radius of the password box chrome.

**Parameter `obj`:** The password box to read from or write to.

**Parameter `value`:** The corner radius to apply.

<a id="api-9c6d3c80a022"></a>

### SetIsFluentDecorated

```csharp
public static void SetIsFluentDecorated(this PasswordBox obj, bool value)
```

Sets whether the Fluent password box behavior is attached to the specified element. The implicit Fluent style sets this to `true`; clearing it restores the native behavior.

**Parameter `obj`:** The password box to read from or write to.

**Parameter `value`:** `true` to attach the behavior; otherwise `false`.

<a id="api-3f6754c80192"></a>

### SetPasswordStrength

```csharp
public static void SetPasswordStrength(this PasswordBox obj, int value)
```

Sets the strength score from 0 (weakest) to 4 (strongest).

**Parameter `obj`:** The password box to read from or write to.

**Parameter `value`:** The strength score to store.

<a id="api-f67a9a0fabd6"></a>

### SetPlaceholderText

```csharp
public static void SetPlaceholderText(this PasswordBox obj, string value)
```

Sets the placeholder text shown while the field is empty.

**Parameter `obj`:** The password box to read from or write to.

**Parameter `value`:** The placeholder text to store.

<a id="api-c54f1298af40"></a>

### SetRevealButtonEnabled

```csharp
public static void SetRevealButtonEnabled(this PasswordBox obj, bool value)
```

Sets whether the reveal button is offered once the field holds a password.

**Parameter `obj`:** The password box to read from or write to.

**Parameter `value`:** `true` to enable the reveal button; otherwise `false`.

<a id="api-5eb150cf499e"></a>

### SetShowCapsLockIndicator

```csharp
public static void SetShowCapsLockIndicator(this PasswordBox obj, bool value)
```

Sets whether the Caps Lock indicator is shown while Caps Lock is active.

**Parameter `obj`:** The password box to read from or write to.

**Parameter `value`:** `true` to enable the indicator; otherwise `false`.

<a id="api-ffeb44dc1c1a"></a>

### SetShowPasswordStrength

```csharp
public static void SetShowPasswordStrength(this PasswordBox obj, bool value)
```

Sets whether the password strength meter is displayed.

**Parameter `obj`:** The password box to read from or write to.

**Parameter `value`:** `true` to show the meter; otherwise `false`.

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-2e1cbcfe8316"></a>

### CornerRadiusProperty

```csharp
public static readonly DependencyProperty CornerRadiusProperty
```

Identifies the CornerRadius attached property.

<a id="api-090467a1a0be"></a>

### HasPasswordProperty

```csharp
public static readonly DependencyProperty HasPasswordProperty
```

Identifies the read-only HasPassword attached property.

<a id="api-6302e3307c05"></a>

### IsFluentDecoratedProperty

```csharp
public static readonly DependencyProperty IsFluentDecoratedProperty
```

Identifies the IsFluentDecorated attached property.

<a id="api-aad212355a16"></a>

### IsPasswordRevealedProperty

```csharp
public static readonly DependencyProperty IsPasswordRevealedProperty
```

Identifies the read-only IsPasswordRevealed attached property.

<a id="api-0abe1f33e8fc"></a>

### PasswordStrengthProperty

```csharp
public static readonly DependencyProperty PasswordStrengthProperty
```

Identifies the PasswordStrength attached property.

<a id="api-1f7f1e51a6d9"></a>

### PlaceholderTextProperty

```csharp
public static readonly DependencyProperty PlaceholderTextProperty
```

Identifies the PlaceholderText attached property.

<a id="api-725154c9e236"></a>

### RevealButtonEnabledProperty

```csharp
public static readonly DependencyProperty RevealButtonEnabledProperty
```

Identifies the RevealButtonEnabled attached property.

<a id="api-4e0122fd15c3"></a>

### ShowCapsLockIndicatorProperty

```csharp
public static readonly DependencyProperty ShowCapsLockIndicatorProperty
```

Identifies the ShowCapsLockIndicator attached property.

<a id="api-307910852396"></a>

### ShowPasswordStrengthProperty

```csharp
public static readonly DependencyProperty ShowPasswordStrengthProperty
```

Identifies the ShowPasswordStrength attached property.
