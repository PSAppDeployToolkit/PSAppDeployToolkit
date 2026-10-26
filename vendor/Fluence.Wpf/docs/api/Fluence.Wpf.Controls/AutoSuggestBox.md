# AutoSuggestBox

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class AutoSuggestBox : Control
```

A text input control that shows a light-dismiss list of suggestions while the user types, mirroring the WinUI 3 `AutoSuggestBox`. The application drives filtering by handling [TextChanged](AutoSuggestBox.md#api-e7db91ba4290) and updating [ItemsSource](AutoSuggestBox.md#api-50a8615da178); the control opens the suggestion list while it has keyboard focus and suggestions exist.

**Base type:** [`Control`](https://learn.microsoft.com/dotnet/api/system.windows.controls.control) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/AutoSuggestBox.cs)

## Constructors

<a id="api-20befa5579b4"></a>

### AutoSuggestBox

```csharp
public AutoSuggestBox()
```

Creates a new `AutoSuggestBox` instance.

## Properties

<a id="api-47e6b095cdf6"></a>

### Header

```csharp
public object? Header { get; set; }
```

Gets or sets the optional header content shown above the text box.

<a id="api-e9644941e4b9"></a>

### IsSuggestionListOpen

```csharp
public bool IsSuggestionListOpen { get; set; }
```

Gets or sets whether the suggestion list popup is open. The control opens the list while it has keyboard focus and [ItemsSource](AutoSuggestBox.md#api-50a8615da178) has items, and closes it on light dismiss, focus loss, Escape, or query submission.

<a id="api-50a8615da178"></a>

### ItemsSource

```csharp
public IEnumerable? ItemsSource { get; set; }
```

Gets or sets the collection of suggestions shown in the suggestion list. Replace this collection from a [TextChanged](AutoSuggestBox.md#api-e7db91ba4290) handler to filter suggestions as the user types.

<a id="api-d4894bcaf06e"></a>

### MaxSuggestionListHeight

```csharp
public double MaxSuggestionListHeight { get; set; }
```

Gets or sets the maximum height of the suggestion list popup.

<a id="api-03a797f6ff7b"></a>

### PlaceholderText

```csharp
public string PlaceholderText { get; set; }
```

Gets or sets the placeholder text displayed when the text box is empty.

<a id="api-6788dcb4b65f"></a>

### QueryIcon

```csharp
public object? QueryIcon { get; set; }
```

Gets or sets the icon shown at the right edge of the text box, typically a search glyph. The default template hosts it in a clickable subtle button in the text box icon slot; clicking it submits the current text through the same [QuerySubmitted](AutoSuggestBox.md#api-3eca4afc52ef) pipeline as the Enter key. No button is shown while the value is `null`.

<a id="api-0f249b6a936a"></a>

### Text

```csharp
public string Text { get; set; }
```

Gets or sets the text shown in the text box portion of the control. Setting this property programmatically raises [TextChanged](AutoSuggestBox.md#api-e7db91ba4290) with [ProgrammaticChange](../Fluence.Wpf/AutoSuggestionBoxTextChangeReason.md#api-95adfffba47a).

<a id="api-a9f614f9cdc5"></a>

### TextMemberPath

```csharp
public string TextMemberPath { get; set; }
```

Gets or sets the property path on a suggestion item that supplies the display text and, when [UpdateTextOnSelect](AutoSuggestBox.md#api-387b2b88fb29) is enabled, the value written into [Text](AutoSuggestBox.md#api-0f249b6a936a) when the suggestion is chosen. When empty, the item's string representation is used.

<a id="api-387b2b88fb29"></a>

### UpdateTextOnSelect

```csharp
public bool UpdateTextOnSelect { get; set; }
```

Gets or sets whether choosing a suggestion updates [Text](AutoSuggestBox.md#api-0f249b6a936a) with the value resolved through [TextMemberPath](AutoSuggestBox.md#api-a9f614f9cdc5).

## Methods

<a id="api-951672e6d1f7"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

<a id="api-b83c9ac7e44a"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

<a id="api-a9912461cecf"></a>

### OnGotKeyboardFocus

```csharp
protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

<a id="api-729a1b588a5b"></a>

### OnIsKeyboardFocusWithinChanged

```csharp
protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

<a id="api-89b20fa4b47d"></a>

### OnPreviewKeyDown

```csharp
protected override void OnPreviewKeyDown(KeyEventArgs e)
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

## Events

<a id="api-3eca4afc52ef"></a>

### QuerySubmitted

```csharp
public event EventHandler<AutoSuggestBoxQuerySubmittedEventArgs>? QuerySubmitted
```

Occurs when the user submits a query with the Enter key or by choosing a suggestion from the suggestion list.

<a id="api-ad5b21807785"></a>

### SuggestionChosen

```csharp
public event EventHandler<AutoSuggestBoxSuggestionChosenEventArgs>? SuggestionChosen
```

Occurs when the user chooses a suggestion from the suggestion list, before [Text](AutoSuggestBox.md#api-0f249b6a936a) is updated and [QuerySubmitted](AutoSuggestBox.md#api-3eca4afc52ef) is raised.

<a id="api-e7db91ba4290"></a>

### TextChanged

```csharp
public event EventHandler<AutoSuggestBoxTextChangedEventArgs>? TextChanged
```

Occurs after the text changes. Inspect [Reason](../Fluence.Wpf/AutoSuggestBoxTextChangedEventArgs.md#api-e4026a76e522) to distinguish user input, programmatic changes, and suggestion-driven updates.

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-09d34010e603"></a>

### HeaderProperty

```csharp
public static readonly DependencyProperty HeaderProperty
```

Identifies the [Header](AutoSuggestBox.md#api-47e6b095cdf6) dependency property.

<a id="api-c2150583c3cc"></a>

### IsSuggestionListOpenProperty

```csharp
public static readonly DependencyProperty IsSuggestionListOpenProperty
```

Identifies the [IsSuggestionListOpen](AutoSuggestBox.md#api-e9644941e4b9) dependency property.

<a id="api-9b759a1fa275"></a>

### ItemsSourceProperty

```csharp
public static readonly DependencyProperty ItemsSourceProperty
```

Identifies the [ItemsSource](AutoSuggestBox.md#api-50a8615da178) dependency property.

<a id="api-d040817614d8"></a>

### MaxSuggestionListHeightProperty

```csharp
public static readonly DependencyProperty MaxSuggestionListHeightProperty
```

Identifies the [MaxSuggestionListHeight](AutoSuggestBox.md#api-d4894bcaf06e) dependency property.

<a id="api-5810fdeb13ca"></a>

### PlaceholderTextProperty

```csharp
public static readonly DependencyProperty PlaceholderTextProperty
```

Identifies the [PlaceholderText](AutoSuggestBox.md#api-03a797f6ff7b) dependency property.

<a id="api-2ba90dc573d9"></a>

### QueryIconProperty

```csharp
public static readonly DependencyProperty QueryIconProperty
```

Identifies the [QueryIcon](AutoSuggestBox.md#api-6788dcb4b65f) dependency property.

<a id="api-ee5aea031a3e"></a>

### TextMemberPathProperty

```csharp
public static readonly DependencyProperty TextMemberPathProperty
```

Identifies the [TextMemberPath](AutoSuggestBox.md#api-a9f614f9cdc5) dependency property.

<a id="api-84fdb6b03469"></a>

### TextProperty

```csharp
public static readonly DependencyProperty TextProperty
```

Identifies the [Text](AutoSuggestBox.md#api-0f249b6a936a) dependency property.

<a id="api-4fa1288f2abd"></a>

### UpdateTextOnSelectProperty

```csharp
public static readonly DependencyProperty UpdateTextOnSelectProperty
```

Identifies the [UpdateTextOnSelect](AutoSuggestBox.md#api-387b2b88fb29) dependency property.

## Related types

- [Fluence.Wpf.AutoSuggestBoxQuerySubmittedEventArgs](../Fluence.Wpf/AutoSuggestBoxQuerySubmittedEventArgs.md)
- [Fluence.Wpf.AutoSuggestBoxSuggestionChosenEventArgs](../Fluence.Wpf/AutoSuggestBoxSuggestionChosenEventArgs.md)
- [Fluence.Wpf.AutoSuggestBoxTextChangedEventArgs](../Fluence.Wpf/AutoSuggestBoxTextChangedEventArgs.md)
