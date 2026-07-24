The **Fluence.Wpf.Demo** gallery shows every control: `FluenceWindow` chrome with a search box in the title bar, a left `NavigationView` (compact / expanded), and grouped `UserControl` pages under `Fluence.Wpf.Demo/Pages/`:

- Home (clickable hero cards)
- Icons (WinUI Gallery-style Iconography catalog: search, virtualized tile grid, copyable glyph details)
- Typography (Fluent type ramp and TextBlock usage)
- Accessibility (focus order, high contrast, automation, RTL)
- Buttons
- Selection (CheckBox, RadioButton, ToggleSwitch, RatingControl)
- Inputs (TextBox, PasswordBox, NumberBox, ComboBox, Slider, AutoSuggestBox)
- Forms (sign-in, checkout, settings, DatePicker, TimePicker, ColorPicker)
- Data (Card, ListBox, ListView)
- Data Binding (ObservableCollection, selection modes, data templates)
- Trees (TreeView)
- Menus (Menu, ContextMenu, ToolTip, Flyout, ContentDialog, TeachingTip, command buttons)
- Navigation (NavigationView modes)
- Tabs (TabControl, TabView)
- Layout (StackPanel, DockPanel, Border, Separator, Expander)
- Status (InfoBar, InfoBadge, ProgressBar, ProgressRing)
- Settings (theme, navigation style, colors, backdrop, caption buttons)

Most non-Home gallery pages render discrete examples through `DemoSampleControl`. Source tabs read from page-local `XamlSource` and optional `CSharpSource` strings, so you can debug an example next to its page code-behind. Fixed XAML samples keep named live content in page-owned hidden slots and transfer it with the `DemoSamplePageWiring` helper. Reference pages such as Typography mirror WinUI Gallery catalog surfaces and skip the trailing source expander.

**Fluence.Wpf.Demo.Mvvm** is a minimal Task Manager demonstrating `FluenceWindow` + Fluence controls with zero code-behind (CommunityToolkit.Mvvm). See [AGENTS.md](../AGENTS.md) for architecture notes.

## Namespaces

- `Fluence.Wpf` - theme, accent, title-bar and window-control helpers, and UI enums (`ApplicationTheme`, `BackdropType`, `CardVariant`, `NavigationViewPaneDisplayMode`, `TreeViewSelectionMode`, typography enums).
- `Fluence.Wpf.Controls` - styled controls, primitives, and `FluenceWindow`.

Example XML namespace declarations:

```xml
xmlns:fluence="http://schemas.fluencewpf.com"
<!-- or, fully qualified: -->
xmlns:ui="clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"
xmlns:uicore="clr-namespace:Fluence.Wpf;assembly=Fluence.Wpf"
```

`http://schemas.fluencewpf.com` covers both the controls and the core namespaces in a single prefix.

## Catalog (summary)

| Area                | Types                                                                                                                              |
|---------------------|------------------------------------------------------------------------------------------------------------------------------------|
| Window              | `FluenceWindow`, `TitleBar`                                                                                                         |
| Basic actions       | `Button`, `HyperlinkButton`, `DropDownButton`, `SplitButton`, `ToggleSplitButton`, `RepeatButton`, `ToggleButton`                  |
| Selection           | `CheckBox`, `RadioButton`, `ToggleSwitch`, `ComboBox`, `Slider`, `NumberBox`, `DatePicker`, `TimePicker`, `ColorPicker`            |
| Text                | `TextBox`, `PasswordBox`, `AutoSuggestBox`, `TextBlock` + `TextBlockExtensions`                                                    |
| Data                | `ListView`, `ListBox`, `ListBoxItem`, `ListViewItem` (stock container, restyled by the Fluence theme)                              |
| Tabs                | `TabControl`, `TabItem`, `TabView`, `TabViewItem`                                                                                  |
| Feedback            | `ProgressBar`, `ProgressRing`, `InfoBar`, `InfoBadge`, `RatingControl`                                                             |
| Navigation          | `NavigationView`, `NavigationViewItem`, `NavigationViewItemHeader`, `NavigationViewItemSeparator`, `BreadcrumbBar`, `BreadcrumbBarItem`, `PipsPager` |
| Menus & popups      | `ContextMenu`, `MenuItem`, `Menu`, `ToolTip`, `FlyoutBase`, `Flyout`, `FlyoutPresenter`, `TeachingTip`, `CommandBarFlyout`, `CommandBarFlyoutPresenter`, `AppBarButton` |
| Dialogs             | `ContentDialog`                                                                                                                    |
| Trees & collections | `TreeView`, `TreeViewItem`                                                                                                         |
| Layout / surfaces   | `Card`, `Expander`, `Border`, `StackPanel`, `DockPanel`, `SmoothScrollViewer`, `Separator`                                         |
| Person / social     | `PersonPicture`                                                                                                                    |
| Icons               | `FontIcon`                                                                                                                         |

Tab strip and scroll bar styling are provided via merged themes (see `Themes/Generic.xaml`).

## Control API

Each area below lists the public API types it uses; API links point to `/api/` on the documentation site.

### Window and Shell

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.FluenceWindow.html">FluenceWindow</a>
  <a href="../../api/Fluence.Wpf.Controls.TitleBar.html">TitleBar</a>
  <a href="../../api/Fluence.Wpf.BackdropType.html">BackdropType</a>
  <a href="../../api/Fluence.Wpf.CornerPreference.html">CornerPreference</a>
</div>

Primary members include `SystemBackdropType`, `CornerStyle`, `ExtendsContentIntoTitleBar`, `TitleBar`, `TitleBarHeight`, caption-button visibility properties, and title-bar events for back and pane-toggle requests.

`FluenceWindow.DefaultIcon` is a public static `BitmapSource` that exposes the rasterized Fluence brand icon. Every `FluenceWindow` defaults its `Icon` property to this value so the brand mark appears in the title bar and taskbar without any caller setup; a consumer can also apply `DefaultIcon` to its own non-`FluenceWindow` windows. A consumer-assigned `Icon` overrides the default. The value is `null` if the brand resource is unavailable (for example in a headless or session-0 process).

### Basic Actions

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.Button.html">Button</a>
  <a href="../../api/Fluence.Wpf.Controls.HyperlinkButton.html">HyperlinkButton</a>
  <a href="../../api/Fluence.Wpf.Controls.DropDownButton.html">DropDownButton</a>
  <a href="../../api/Fluence.Wpf.Controls.SplitButton.html">SplitButton</a>
  <a href="../../api/Fluence.Wpf.Controls.ToggleSplitButton.html">ToggleSplitButton</a>
  <a href="../../api/Fluence.Wpf.Controls.RepeatButton.html">RepeatButton</a>
  <a href="../../api/Fluence.Wpf.Controls.ToggleButton.html">ToggleButton</a>
  <a href="../../api/Fluence.Wpf.ControlAppearance.html">ControlAppearance</a>
  <a href="../../api/Fluence.Wpf.ToggleSplitButtonIsCheckedChangedEventArgs.html">ToggleSplitButtonIsCheckedChangedEventArgs</a>
</div>

The action controls keep standard WPF command, content, and click patterns. Use `Appearance="Accent"` for the primary action on a page, and standard or subtle styling for lower-emphasis commands.

`ToggleButton` holds a checked state with the WinUI accent visuals: checked renders as the accent state with distinct hover and pressed tints, and `IsThreeState="True"` adds the indeterminate state (`IsChecked` of `null`) with the WinUI neutral palette. The default template renders the same canonical toggle visual for every `Appearance` value; the property exists primarily for derived controls such as `DropDownButton`.

`ToggleSplitButton` derives from `SplitButton` and turns the primary half into a toggle: a primary click flips `IsChecked` and then raises `Click`, so handlers observe the already-toggled state, while the chevron half still opens the `Flyout`. Subscribe to `IsCheckedChanged` (carrying the new state in `ToggleSplitButtonIsCheckedChangedEventArgs`) for state updates from clicks, bindings, or the Toggle automation pattern. Checked renders both halves in the accent palette with the WinUI checked divider stroke. Use it when the primary half switches a mode on or off and the flyout chooses which variant of that mode applies, such as list formatting.

### Selection

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.CheckBox.html">CheckBox</a>
  <a href="../../api/Fluence.Wpf.Controls.RadioButton.html">RadioButton</a>
  <a href="../../api/Fluence.Wpf.Controls.ToggleSwitch.html">ToggleSwitch</a>
  <a href="../../api/Fluence.Wpf.Controls.RatingControl.html">RatingControl</a>
</div>

Selection controls follow WPF checked-state APIs (`IsChecked`, groups, and selection). `RatingControl` adds value-based selection for simple scoring UI.

### Inputs

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.ComboBox.html">ComboBox</a>
  <a href="../../api/Fluence.Wpf.Controls.Slider.html">Slider</a>
  <a href="../../api/Fluence.Wpf.Controls.NumberBox.html">NumberBox</a>
  <a href="../../api/Fluence.Wpf.Controls.TextBox.html">TextBox</a>
  <a href="../../api/Fluence.Wpf.Controls.PasswordBox.html">PasswordBox</a>
  <a href="../../api/Fluence.Wpf.Controls.AutoSuggestBox.html">AutoSuggestBox</a>
  <a href="../../api/Fluence.Wpf.SpinButtonPlacementMode.html">SpinButtonPlacementMode</a>
  <a href="../../api/Fluence.Wpf.NumberBoxValueChangedEventArgs.html">NumberBoxValueChangedEventArgs</a>
  <a href="../../api/Fluence.Wpf.AutoSuggestBoxTextChangedEventArgs.html">AutoSuggestBoxTextChangedEventArgs</a>
  <a href="../../api/Fluence.Wpf.AutoSuggestBoxSuggestionChosenEventArgs.html">AutoSuggestBoxSuggestionChosenEventArgs</a>
  <a href="../../api/Fluence.Wpf.AutoSuggestBoxQuerySubmittedEventArgs.html">AutoSuggestBoxQuerySubmittedEventArgs</a>
  <a href="../../api/Fluence.Wpf.AutoSuggestionBoxTextChangeReason.html">AutoSuggestionBoxTextChangeReason</a>
</div>

Input controls keep standard WPF editing, selection, command, and binding behavior. `NumberBox` adds numeric parsing, range, increment, and spin-button placement. Text inputs get placeholder, validation, and focus visuals from the shared templates. `AutoSuggestBox` pairs a text input with a light-dismiss suggestion list the application fills through `TextChanged`, `SuggestionChosen`, and `QuerySubmitted`.

### Forms

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.TextBox.html">TextBox</a>
  <a href="../../api/Fluence.Wpf.Controls.PasswordBox.html">PasswordBox</a>
  <a href="../../api/Fluence.Wpf.Controls.ComboBox.html">ComboBox</a>
  <a href="../../api/Fluence.Wpf.Controls.Button.html">Button</a>
  <a href="../../api/Fluence.Wpf.Controls.DatePicker.html">DatePicker</a>
  <a href="../../api/Fluence.Wpf.Controls.TimePicker.html">TimePicker</a>
  <a href="../../api/Fluence.Wpf.Controls.ColorPicker.html">ColorPicker</a>
  <a href="../../api/Fluence.Wpf.ValidationState.html">ValidationState</a>
  <a href="../../api/Fluence.Wpf.DatePickerSelectedValueChangedEventArgs.html">DatePickerSelectedValueChangedEventArgs</a>
  <a href="../../api/Fluence.Wpf.TimePickerSelectedValueChangedEventArgs.html">TimePickerSelectedValueChangedEventArgs</a>
  <a href="../../api/Fluence.Wpf.ColorPickerColorChangedEventArgs.html">ColorPickerColorChangedEventArgs</a>
</div>

The form page combines input controls with card surfaces, status text, validation states, and primary actions. Start here for sign-in, checkout, and settings forms. `DatePicker`, `TimePicker`, and `ColorPicker` add flyout-based date, time, and color selection for form fields.

### Data and Collections

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.Card.html">Card</a>
  <a href="../../api/Fluence.Wpf.Controls.ListBox.html">ListBox</a>
  <a href="../../api/Fluence.Wpf.Controls.ListBoxItem.html">ListBoxItem</a>
  <a href="../../api/Fluence.Wpf.Controls.ListView.html">ListView</a>
  <a href="../../api/Fluence.Wpf.Controls.PersonPicture.html">PersonPicture</a>
  <a href="../../api/Fluence.Wpf.CardVariant.html">CardVariant</a>
  <a href="../../api/Fluence.Wpf.ListViewState.html">ListViewState</a>
</div>

Collection controls keep WPF item-source and template behavior. `Card` adds header, footer, icon, clickable, and pressed-state APIs.

### Data Binding

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.ListView.html">ListView</a>
  <a href="../../api/Fluence.Wpf.Controls.ListBox.html">ListBox</a>
  <a href="../../api/Fluence.Wpf.Controls.TreeView.html">TreeView</a>
  <a href="../../api/Fluence.Wpf.Controls.Card.html">Card</a>
  <a href="../../api/Fluence.Wpf.ListViewState.html">ListViewState</a>
</div>

The data-binding page shows standard WPF `ItemsSource`, `SelectedItem`, `SelectedItems`, item-template, and command-binding patterns with Fluence controls. The control API is WPF-native; view models need no Fluence-specific base classes.

### Icons

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.FontIcon.html">FontIcon</a>
</div>

`FontIcon` renders Segoe Fluent Symbols glyphs and adds icon-size, foreground, and alignment properties. It works inside buttons, navigation items, tab headers, cards, and standalone icon lists.

### Typography

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.TextBlock.html">TextBlock</a>
  <a href="../../api/Fluence.Wpf.Controls.TextBlockExtensions.html">TextBlockExtensions</a>
  <a href="../../api/Fluence.Wpf.FluentTypography.html">FluentTypography</a>
</div>

`TextBlockExtensions.Typography` maps text to the Fluent type ramp, so app text and control templates share the same typography tokens.

### Navigation

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.NavigationView.html">NavigationView</a>
  <a href="../../api/Fluence.Wpf.Controls.NavigationViewItem.html">NavigationViewItem</a>
  <a href="../../api/Fluence.Wpf.Controls.NavigationViewItemHeader.html">NavigationViewItemHeader</a>
  <a href="../../api/Fluence.Wpf.Controls.NavigationViewItemSeparator.html">NavigationViewItemSeparator</a>
  <a href="../../api/Fluence.Wpf.NavigationViewPaneDisplayMode.html">NavigationViewPaneDisplayMode</a>
  <a href="../../api/Fluence.Wpf.NavigationViewBackRequestedEventArgs.html">NavigationViewBackRequestedEventArgs</a>
  <a href="../../api/Fluence.Wpf.NavigationViewItemInvokedEventArgs.html">NavigationViewItemInvokedEventArgs</a>
  <a href="../../api/Fluence.Wpf.Controls.BreadcrumbBar.html">BreadcrumbBar</a>
  <a href="../../api/Fluence.Wpf.Controls.BreadcrumbBarItem.html">BreadcrumbBarItem</a>
  <a href="../../api/Fluence.Wpf.BreadcrumbBarItemClickedEventArgs.html">BreadcrumbBarItemClickedEventArgs</a>
  <a href="../../api/Fluence.Wpf.Controls.PipsPager.html">PipsPager</a>
  <a href="../../api/Fluence.Wpf.PipsPagerButtonVisibility.html">PipsPagerButtonVisibility</a>
  <a href="../../api/Fluence.Wpf.PipsPagerSelectedIndexChangedEventArgs.html">PipsPagerSelectedIndexChangedEventArgs</a>
</div>

`NavigationView` owns pane layout, selection, back-button state, top overflow, and item invocation events. Application route history remains app-owned.

`BreadcrumbBar` shows the path to the current location as clickable crumbs separated by chevrons; the last crumb renders emphasized with no trailing chevron. Long trails clip when constrained rather than collapsing leading crumbs into an ellipsis (there is no overflow flyout in this version). Bind `ItemsSource` and handle `ItemClicked` (which carries the clicked `Item` and `Index`) to trim the path:

```xml
<ui:BreadcrumbBar x:Name="Trail" ItemClicked="Trail_ItemClicked" />
```

`PipsPager` is a compact page indicator for carousels and onboarding flows: a row (or column) of clickable pip dots with the selected page emphasized, optional previous/next chevron buttons (`PipsPagerButtonVisibility`), and a sliding window of at most `MaxVisiblePips` dots for large page counts. Handle `SelectedIndexChanged` to switch content:

```xml
<ui:PipsPager
    NumberOfPages="8"
    PreviousButtonVisibility="Visible"
    NextButtonVisibility="Visible"
    SelectedIndexChanged="Pager_SelectedIndexChanged" />
```

### Tabs

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.TabView.html">TabView</a>
  <a href="../../api/Fluence.Wpf.Controls.TabViewItem.html">TabViewItem</a>
  <a href="../../api/Fluence.Wpf.TabViewWidthMode.html">TabViewWidthMode</a>
  <a href="../../api/Fluence.Wpf.TabViewCloseButtonOverlayMode.html">TabViewCloseButtonOverlayMode</a>
  <a href="../../api/Fluence.Wpf.TabViewTabCloseRequestedEventArgs.html">TabViewTabCloseRequestedEventArgs</a>
</div>

`TabView` adds add-tab, close-request, width-mode, and close-button overlay APIs. Standard `TabControl` and `TabItem` pick up Fluent styling through the merged control templates.

### Layout and Surfaces

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.Border.html">Border</a>
  <a href="../../api/Fluence.Wpf.Controls.StackPanel.html">StackPanel</a>
  <a href="../../api/Fluence.Wpf.Controls.DockPanel.html">DockPanel</a>
  <a href="../../api/Fluence.Wpf.Controls.Expander.html">Expander</a>
  <a href="../../api/Fluence.Wpf.Controls.Separator.html">Separator</a>
  <a href="../../api/Fluence.Wpf.Controls.SmoothScrollViewer.html">SmoothScrollViewer</a>
  <a href="../../api/Fluence.Wpf.BorderVariant.html">BorderVariant</a>
</div>

Layout primitives preserve WPF layout behavior. `SmoothScrollViewer` is the scroll host used throughout the demo gallery.

### Menus and Popups

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.Menu.html">Menu</a>
  <a href="../../api/Fluence.Wpf.Controls.MenuItem.html">MenuItem</a>
  <a href="../../api/Fluence.Wpf.Controls.ContextMenu.html">ContextMenu</a>
  <a href="../../api/Fluence.Wpf.Controls.ToolTip.html">ToolTip</a>
  <a href="../../api/Fluence.Wpf.Controls.FlyoutBase.html">FlyoutBase</a>
  <a href="../../api/Fluence.Wpf.Controls.Flyout.html">Flyout</a>
  <a href="../../api/Fluence.Wpf.Controls.FlyoutPresenter.html">FlyoutPresenter</a>
  <a href="../../api/Fluence.Wpf.Controls.ContentDialog.html">ContentDialog</a>
  <a href="../../api/Fluence.Wpf.Controls.TeachingTip.html">TeachingTip</a>
  <a href="../../api/Fluence.Wpf.FlyoutPlacementMode.html">FlyoutPlacementMode</a>
  <a href="../../api/Fluence.Wpf.TeachingTipPlacementMode.html">TeachingTipPlacementMode</a>
  <a href="../../api/Fluence.Wpf.Controls.CommandBarFlyout.html">CommandBarFlyout</a>
  <a href="../../api/Fluence.Wpf.Controls.CommandBarFlyoutPresenter.html">CommandBarFlyoutPresenter</a>
  <a href="../../api/Fluence.Wpf.Controls.AppBarButton.html">AppBarButton</a>
  <a href="../../api/Fluence.Wpf.ContentDialogResult.html">ContentDialogResult</a>
  <a href="../../api/Fluence.Wpf.ContentDialogButton.html">ContentDialogButton</a>
  <a href="../../api/Fluence.Wpf.ContentDialogButtonClickEventArgs.html">ContentDialogButtonClickEventArgs</a>
</div>

Menu and popup controls use WinUI-style flyout visuals. Command text, separators, nested menu items, context menus, and tooltips all follow the same visual contract. `Flyout` hosts arbitrary content in a light-dismiss popup, `ContentDialog` raises a modal prompt with up to three command buttons, and `TeachingTip` anchors a coaching callout to a target element.

### Trees

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.TreeView.html">TreeView</a>
  <a href="../../api/Fluence.Wpf.Controls.TreeViewItem.html">TreeViewItem</a>
  <a href="../../api/Fluence.Wpf.TreeViewSelectionMode.html">TreeViewSelectionMode</a>
</div>

`TreeView` supports single and multiple selection modes, a live `SelectedItems` list, expandable hierarchy, and tri-state item selection through `TreeViewItem.IsSelectionChecked`.

### Status and Feedback

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.InfoBar.html">InfoBar</a>
  <a href="../../api/Fluence.Wpf.Controls.InfoBadge.html">InfoBadge</a>
  <a href="../../api/Fluence.Wpf.Controls.ProgressBar.html">ProgressBar</a>
  <a href="../../api/Fluence.Wpf.Controls.ProgressRing.html">ProgressRing</a>
  <a href="../../api/Fluence.Wpf.Controls.RatingControl.html">RatingControl</a>
  <a href="../../api/Fluence.Wpf.InfoBarSeverity.html">InfoBarSeverity</a>
  <a href="../../api/Fluence.Wpf.InfoBadgeStyle.html">InfoBadgeStyle</a>
  <a href="../../api/Fluence.Wpf.ProgressBarMode.html">ProgressBarMode</a>
  <a href="../../api/Fluence.Wpf.ProgressRingState.html">ProgressRingState</a>
</div>

Status controls cover severity, closable state, determinate and indeterminate progress, paused and error states, and count or attention badge styling.

`InfoBar` exposes two public static helpers for use outside the bar itself:

- `InfoBar.GetSeverityGlyph(InfoBarSeverity)` - returns the canonical Segoe Fluent glyph character for the given severity, so a consumer can render the severity icon in any `FontIcon` without hardcoding codepoints.
- `InfoBar.GetSeverityBrushKey(InfoBarSeverity)` - returns the theme brush resource key for the given severity, suitable for a `SetResourceReference` call or a `DynamicResource` binding.

Both helpers stay in sync with the `InfoBar` control template; use them instead of hardcoding glyph or brush values.

### Accessibility

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Automation.html">Automation namespace</a>
  <a href="../../api/Fluence.Wpf.Automation.NavigationViewAutomationPeer.html">NavigationViewAutomationPeer</a>
  <a href="../../api/Fluence.Wpf.Automation.ToggleSwitchAutomationPeer.html">ToggleSwitchAutomationPeer</a>
  <a href="../../api/Fluence.Wpf.Automation.InfoBarAutomationPeer.html">InfoBarAutomationPeer</a>
  <a href="../../api/Fluence.Wpf.Automation.RatingControlAutomationPeer.html">RatingControlAutomationPeer</a>
  <a href="../../api/Fluence.Wpf.Automation.PasswordBoxAutomationPeer.html">PasswordBoxAutomationPeer</a>
  <a href="../../api/Fluence.Wpf.Automation.PersonPictureAutomationPeer.html">PersonPictureAutomationPeer</a>
  <a href="../../api/Fluence.Wpf.Automation.HyperlinkButtonAutomationPeer.html">HyperlinkButtonAutomationPeer</a>
  <a href="../../api/Fluence.Wpf.Automation.CardAutomationPeer.html">CardAutomationPeer</a>
</div>

Accessibility coverage includes focus visuals, high-contrast resources, automation peers, keyboard navigation, and right-to-left layout.

#### Reduced motion

Fluence controls respect the Windows **"Show animations in Windows"** accessibility setting
(Settings > Accessibility > Visual effects > Animation effects, surfaced to WPF as
`SystemParameters.ClientAreaAnimation`). When the setting is off, code-driven control animations
do not play and controls jump straight to their final visual state: the indeterminate
`ProgressRing` and `ProgressBar` render a static resting frame instead of spinning or sweeping,
`FontIcon.IsSpinning` holds its static rotation, and the `ContentDialog` entrance and close,
`NavigationView` pane and selection-indicator motion, `Flyout`, `TeachingTip`, and `ComboBox`
dropdown reveals, `Expander` content slide, `ToggleSwitch` knob and thumb motion, `ListView`
insert/remove tweens, and `SmoothScrollViewer` wheel tween all apply their end states
immediately. Template-level hover and press micro-feedback storyboards are not yet gated, and
flipping the OS setting mid-session takes effect at each animation's next natural start.

#### Per-control roles, names, and patterns

| Control | UIA Role | Name source | Patterns |
|---|---|---|---|
| `Button` | Button | `Content` or `AutomationProperties.Name` | Invoke |
| `HyperlinkButton` | Hyperlink | `Content` or `AutomationProperties.Name` | Invoke |
| `DropDownButton` | Button | `Content` or `AutomationProperties.Name` | Invoke, ExpandCollapse |
| `SplitButton` | SplitButton | `Content` or `AutomationProperties.Name` | Invoke (primary), ExpandCollapse (chevron) |
| `ToggleSplitButton` | SplitButton | `Content` or `AutomationProperties.Name` | Toggle (primary), ExpandCollapse (chevron) |
| `ToggleButton` | Button | `Content` or `AutomationProperties.Name` | Toggle |
| `CheckBox` | CheckBox | `Content`; `AutomationProperties.HelpText` carries description | Toggle |
| `RadioButton` | RadioButton | `Content`; `AutomationProperties.HelpText` carries description | Selection |
| `ToggleSwitch` | Button | `Header` via automation peer `GetNameCore` / `AutomationProperties.Name` | Toggle |
| `RatingControl` | Slider | `AutomationProperties.Name` (current and maximum value are exposed through the RangeValue pattern, not the name) | RangeValue |
| `ComboBox` | ComboBox | `PlaceholderText` or `AutomationProperties.Name` | ExpandCollapse, Selection |
| `Slider` | Slider | `AutomationProperties.Name` or labeled by adjacent `TextBlock` | RangeValue |
| `NumberBox` | Spinner | `Header` via automation peer `GetNameCore` / `AutomationProperties.Name` | RangeValue |
| `TextBox` | Edit | `Header` or `AutomationProperties.Name` | Value, Text |
| `PasswordBox` | Edit | `AutomationProperties.Name` (app-provided label); reveal button announces its state | none |
| `AutoSuggestBox` | Edit | `Header` via automation peer `GetNameCore` / `AutomationProperties.Name` | Value |
| `NavigationView` | Navigation | `AutomationProperties.Name` on the control | Selection |
| `NavigationViewItem` | ListItem | `Content` | SelectionItem |
| `TabView` | Tab | `AutomationProperties.Name` on the control | Selection |
| `TabViewItem` | TabItem | `Header` | SelectionItem |
| `InfoBar` | StatusBar | `Title` (or `AutomationProperties.Name`); message is announced via the live region | none (live region) |
| `ProgressBar` | ProgressBar | `AutomationProperties.Name` | RangeValue |
| `ProgressRing` | ProgressBar | `AutomationProperties.Name` | RangeValue |
| `Card` | Button (when clickable) / Group (non-clickable) | `AutomationProperties.Name` (or its content) | Invoke (when clickable) |
| `PersonPicture` | Image | Display name or initials | none |
| `ContentDialog` | Window | `Title` | none (focus trapped inside; assertive live region announces `Title` on open) |
| `TeachingTip` | ToolTip | `Title` and `Subtitle` (live region) | none |
| `AppBarButton` | Button | `Label` (surfaced as `AutomationProperties.Name`) or an explicit `AutomationProperties.Name` | Invoke |

#### Live regions on net472

`AutomationPeer.RaiseNotificationEvent` (which pushes ad-hoc announcements to
screen readers) requires .NET Framework 4.8 and is not available on `net472`. All
status-change announcements in this library instead use the net472-safe pattern:

1. The control template or peer constructor sets `AutomationProperties.LiveSetting`
   to `Polite` or `Assertive` on the announcing element.
2. When the relevant state changes (severity, message, progress state, validation
   message, or coaching text), the automation peer calls
   `RaiseAutomationEvent(AutomationEvents.LiveRegionChanged)`.
3. Screen readers that honour `LiveRegionChanged` (Narrator, NVDA, JAWS) then
   read the element's current `GetNameCore` text.

Controls that use this approach: `ContentDialog` (announces its `Title` when the
dialog opens), `InfoBar`, `ProgressBar`, `ProgressRing`, `TeachingTip`, and
`TextBox` validation feedback. This pattern requires no .NET 4.8 surface and works
on both `net472` and `net10.0-windows10.0.26100.0`.

#### Keyboard operability

Beyond standard WPF focus and tab traversal, Fluence adds keyboard operability for
controls that WPF does not natively make fully keyboard-accessible:

- `ColorPicker` spectrum: arrow keys adjust the selected point on the hue-saturation-value
  spectrum canvas; the spectrum canvas receives keyboard focus as a named focusable element.
- `RatingControl`: Left/Right arrow keys decrement and increment the rating; Home and End
  jump to the minimum and maximum values.
- `PasswordBox` reveal button: Space and Enter toggle the reveal state when the reveal button
  is keyboard-focused; the reveal button’s accessible name updates to announce the current state.
- `NumberBox` spin buttons: the increment and decrement buttons are keyboard-accessible tab
  stops with accessible names; the `LargeChange` value reported by the automation peer
  matches the `NumberBox.LargeChange` property.

#### Known net472 gaps

Four UI Automation features available from .NET Framework 4.8 are absent on `net472`.
See `KNOWN_ISSUES.md` for the full rationale and chosen fallbacks:

- `AutomationPeer.RaiseNotificationEvent` - substituted with `LiveRegionChanged` (described above).
- `AutomationProperties.IsDialog` - `ContentDialog` uses `ControlType.Window`, Tab focus trapping, and an assertive live-region announcement of its `Title` on open as the fallback.
- `AutomationProperties.HeadingLevel` - not used by the library; app-layer concern.
- Automatic `PositionInSet`/`SizeOfSet` for `ItemsControl` - peers do not override `GetPositionInSetCore`/`GetSizeOfSetCore` on either TFM; apps set `AutomationProperties.PositionInSet`/`SizeOfSet` explicitly on items where meaningful.

## FluenceWindow

`FluenceWindow` gives you title-bar styling, caption buttons, backdrop support, and a title-bar content slot. `MinWidth` is caller-controlled and unset by default; the default title bar height is 48 px (the WinUI 3 canonical expanded title-bar height). When `ExtendsContentIntoTitleBar="True"`, app content renders behind the title bar. A `NavigationView` left pane reserves title-bar height before its first item when no explicit header is provided.

`CaptionButtonChrome` and `WindowPolicy` are internal types behind `FluenceWindow` caption-button and DWM policy decisions. Tests cover them, but they are not consumer controls.

`TitleBar` is the shell title-bar control the gallery uses. It provides back and pane-toggle buttons (`BackRequested`, `PaneToggleRequested`, and matching command properties), icon/title/subtitle presentation, and left/right/content slots. Interactive template buttons set `WindowChrome.IsHitTestVisibleInChrome`; app content such as search boxes should do the same.

## NavigationView

Three pane display modes ship out of the box:

| `PaneDisplayMode` | Rail                                            | Labels                         | Template                                |
|-------------------|-------------------------------------------------|--------------------------------|-----------------------------------------|
| `Left` (default)  | 48 / 320 px                                     | Shown when `IsPaneOpen="True"` | `NavigationViewLeftPaneTemplate`        |
| `LeftCompact`     | 48 px (overlay 320 px when `IsPaneOpen="True"`) | Overlay only                   | `NavigationViewLeftCompactPaneTemplate` |
| `Top`             | 48 px horizontal strip                          | Always shown                   | `NavigationViewTopPaneTemplate`         |

Left and LeftCompact share the same visual contract:

- Pane toggle (`PART_PaneToggleButton`, glyph `E700`) and back button (`PART_BackButton`, glyph `E72B`) appear in WinUI order at the top of a 48 px rail, each 48×40 px.
- When a closed compact-left pane shows both an enabled back button and pane toggle, the pane reserves two 48 px chrome slots so the pane toggle remains visible to the right of back.
- Selection indicator (`PART_SelectionIndicator`) is a single `Border` that animates between items - 3 × 16 px vertical in `Left` / `LeftCompact`, 16 × 3 px horizontal in `Top`.
- Content region is a `Border` with `CornerRadius="8,0,0,0"`, `BorderThickness="1,1,0,0"`, and `BorderBrush="{DynamicResource NavigationViewContentSeparatorBrush}"`, wrapping `PART_ContentPresenter`.
- `IsBackButtonVisible` / `IsBackEnabled` drive back button visibility and enabled state. The back button shows only when both are `true`; a disabled back route collapses the button and reserves no glyph slot. Route the `BackRequested` event to your own history stack.
- `IsPaneToggleButtonVisible` controls pane toggle visibility. It defaults to `true` for left pane modes and does not show in top mode.
- When hosted inside a `FluenceWindow`, `Left` mode sets `ExtendsContentIntoTitleBar=True`; `Top` mode sets it `False`.
- `Top` mode coerces `IsPaneOpen=True` and `IsPaneToggleButtonVisible=False`, keeps top-level item icons and text visible, lays items out horizontally without scrolling, and moves overflowed items to the `PART_TopOverflowButton` menu.
- In `Top` mode, right-docked `FooterMenuItems` (e.g. **Settings**) render icon-only - the label is collapsed so the entry shows just its glyph, matching WinUI. `Left` / `LeftCompact` keep footer labels. The footer selection indicator (`PART_FooterSelectionIndicator`) centres under the selected footer item and fades/scales in and out as the footer selection changes.
- Item invocation raises `ItemInvoked` before WPF `SelectionChanged`, matching WinUI ordering. Navigation content belongs to the app layer: set `NavigationView.Content` or route through your own frame or service when handling `ItemInvoked`.

## Cards

`Card` is a `ContentControl` with optional `Header`, `Footer`, and `Icon` slots. Set `IsClickable="True"` to opt into click semantics:

```xml
<fluence:Card Padding="16"
              IsClickable="True"
              Click="OnCardClicked"
              Variant="{x:Static uicore:CardVariant.Default}">
    <fluence:Button Content="Accent" Appearance="Accent" />
</fluence:Card>
```

When `IsClickable` is true:

- The read-only `IsPressed` dependency property mirrors the left-button press state.
- A left-button press inside the card followed by a matching release raises the `Click` routed event (`RoutingStrategy.Bubble`).
- `OnMouseLeave` and `OnLostMouseCapture` cancel the pending press without raising `Click`, matching WinUI button semantics.

## Typography

`TextBlockExtensions` exposes attached properties for the WinUI type ramp:

```xml
<TextBlock fluence:TextBlockExtensions.Typography="TitleLarge"
           Text="Fluence.Wpf" />
```

Supported values: `Caption`, `Body`, `BodyStrong`, `BodyLarge`, `Subtitle`, `Title`, `TitleLarge`, `Display` (see `Fluence.Wpf/FluentTypography.cs`).

The attached property applies the matching named style from `Themes/Typography/Typography.xaml` (`BodyTextBlockStyle`, `TitleTextBlockStyle`, and so on). Keep type-ramp metrics in that dictionary so code, templates, and app XAML all pull from one place.

## Tabs

`TabControl` / `TabItem` pick up WinUI 3 styling automatically through `Themes/Generic.xaml`. The animated selection indicator, typography, and strip padding match the rest of the library.

`TabView` / `TabViewItem` add multi-document features on top of the standard `TabControl` contract:

```xml
<ui:TabView AddTabButtonClick="OnAddTabButtonClick"
            CloseButtonOverlayMode="Auto"
            TabCloseRequested="OnTabCloseRequested">
    <ui:TabViewItem Header="Document 1" IsSelected="True">
        <ui:TabViewItem.Icon>
            <ui:FontIcon Glyph="" IconFontSize="16" />
        </ui:TabViewItem.Icon>
        <!-- tab body -->
    </ui:TabViewItem>

    <ui:TabViewItem Header="Welcome" IsClosable="False">
        <!-- pinned tab: close button hidden -->
    </ui:TabViewItem>
</ui:TabView>
```

Key members:

| Member                           | Type                                               | Notes                                                                  |
| -------------------------------- | -------------------------------------------------- | ---------------------------------------------------------------------- |
| `TabView.IsAddTabButtonVisible`  | `bool`                                             | Toggles the trailing `+` button (`PART_AddTabButton`). Default `true`. |
| `TabView.TabWidthMode`           | `TabViewWidthMode`                                 | `SizeToContent` (default), `Equal`, or `Compact`.                      |
| `TabView.CloseButtonOverlayMode` | `TabViewCloseButtonOverlayMode`                    | `Auto` (default), `OnPointerOver`, or `Always`.                        |
| `TabView.AddTabButtonClick`      | Routed event                                       | Raised when the trailing `+` button is invoked.                        |
| `TabView.TabCloseRequested`      | Routed event (`TabViewTabCloseRequestedEventArgs`) | Bubbled from the originating `TabViewItem.CloseRequested`.             |
| `TabViewItem.IsClosable`         | `bool`                                             | Default `true`. Set `false` to pin a tab and hide its close button.    |
| `TabViewItem.Icon`               | `object`                                           | Any visual (typically `FontIcon`); rendered to the left of `Header`.   |
| `TabViewItem.CloseRequested`     | Routed event (`RoutingStrategy.Bubble`)            | Raised by the per-tab close button (`PART_CloseButton`).               |

You remove the tab from the source collection yourself; the control does not auto-remove items. See `Fluence.Wpf.Demo/Pages/GalleryTabsPage.xaml(.cs)` for an example.

## Feedback

`ProgressBar` follows the WinUI 3 contract: `IsIndeterminate` toggles the sliding two-segment indeterminate animation, and `ShowError` / `ShowPaused` switch the indicator to the system critical and caution brushes. The three flags are orthogonal, so an indeterminate bar can also carry the error or paused color.

```xml
<ui:ProgressBar Value="62" ShowPaused="True" />
<ui:ProgressBar Value="78" ShowError="True" />
<ui:ProgressBar IsIndeterminate="True" />
```

The legacy `ProgressMode` enum (`Standard`, `Indeterminate`, `Error`, `Paused`, `StepProgress`) is retained as a backward-compatible alias that maps onto those flags. `StepProgress` (with `Steps` / `CurrentStep`) is a Fluence-only determinate sub-mode with no WinUI equivalent.

`ProgressRing` uses the same `IsActive` / `IsIndeterminate` / `Value` contract as WinUI, plus `ShowError` / `ShowPaused` for the critical and caution colors. The indeterminate ring is a pulsing arc that rotates, matching the WinUI motion.

```xml
<ui:ProgressRing IsActive="True" IsIndeterminate="True" />
<ui:ProgressRing IsActive="True"
                 IsIndeterminate="True"
                 ShowPaused="True" />
<ui:ProgressRing IsActive="True"
                 IsIndeterminate="False"
                 ShowError="True"
                 Value="70" />
```

`ProgressState` (`Normal`, `Paused`, `Error`) remains as a backward-compatible alias for `ShowPaused` / `ShowError`: `Normal` uses the accent brush, `Paused` uses `SystemFillColorCautionBrush`, and `Error` uses `SystemFillColorCriticalBrush`.

## Menus & Popups

`ContextMenu`, `MenuItem`, and `Menu` use the WinUI 3 MenuFlyout visual vocabulary.

```xml
<!-- Attach a Fluent ContextMenu to any element -->
<ui:Button Content="Right-click me">
    <ui:Button.ContextMenu>
        <ui:ContextMenu>
            <ui:MenuItem Header="Cut"  InputGestureText="Ctrl+X" />
            <ui:MenuItem Header="Copy" InputGestureText="Ctrl+C" />
            <Separator />
            <ui:MenuItem Header="Paste" InputGestureText="Ctrl+V" />
        </ui:ContextMenu>
    </ui:Button.ContextMenu>
</ui:Button>

<!-- Top-level menu bar -->
<ui:Menu>
    <ui:MenuItem Header="_File">
        <ui:MenuItem Header="_New"  InputGestureText="Ctrl+N" />
        <ui:MenuItem Header="_Open" InputGestureText="Ctrl+O" />
        <Separator />
        <ui:MenuItem Header="E_xit" />
    </ui:MenuItem>
</ui:Menu>
```

`ToolTip` is applied automatically to any element with a `ToolTipService.ToolTip` property when the `ToolTip` style is merged:

```xml
<ui:Button Content="Save" ToolTipService.ToolTip="Save the document" />
```

`Flyout` shows arbitrary content in a light-dismiss popup anchored to a placement target. Any element can carry one through the `FlyoutBase.AttachedFlyout` attached property and open it with `FlyoutBase.ShowAttachedFlyout` (or call `ShowAt` directly). `Placement` picks the side via `FlyoutPlacementMode`, `FlyoutPresenterStyle` restyles the themed `FlyoutPresenter` chrome, and `Opening` / `Opened` / `Closing` / `Closed` track the lifecycle - `Closing` is cancelable through `FlyoutBaseClosingEventArgs.Cancel`.

```xml
<ui:Button Click="ShowNoteFlyout_Click" Content="Show flyout">
    <ui:FlyoutBase.AttachedFlyout>
        <ui:Flyout Placement="Bottom">
            <ui:Flyout.Content>
                <TextBlock Text="A lightweight, light-dismiss popup." />
            </ui:Flyout.Content>
        </ui:Flyout>
    </ui:FlyoutBase.AttachedFlyout>
</ui:Button>
```

```csharp
private void ShowNoteFlyout_Click(object sender, RoutedEventArgs e)
{
    FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
}
```

`ContentDialog` raises a modal prompt with a title, arbitrary body content, and up to three command buttons (`PrimaryButtonText`, `SecondaryButtonText`, `CloseButtonText`; buttons with no text collapse). `ShowAsync()` returns a `Task<ContentDialogResult>` that completes when the dialog closes, `Hide()` dismisses it programmatically, and `DefaultButton` picks the button that takes initial focus and answers Enter; Escape routes through the close button, and Tab navigation is trapped inside the dialog. Over a `FluenceWindow` the smoke layer dims the entire window, title bar included; over a plain `Window` the dialog is hosted in the content adorner layer.

A dialog declared inline in XAML starts collapsed and is detached from its declared parent when `ShowAsync()` runs, then re-hosted inside the modal overlay (it becomes visible only while overlay-hosted). The supported parent types are `Panel`, `Decorator` (for example `Border`), and `ContentControl`; `ShowAsync()` throws an `InvalidOperationException` with a descriptive message for any other parent type, so remove the dialog from an unsupported parent before showing it.

```csharp
ContentDialog dialog = new()
{
    Title = "Delete file?",
    Content = "Roadmap.md will be permanently deleted. This cannot be undone.",
    PrimaryButtonText = "Delete",
    CloseButtonText = "Cancel",
    DefaultButton = ContentDialogButton.Close,
};

ContentDialogResult result = await dialog.ShowAsync();
```

The cancelable `PrimaryButtonClick` / `SecondaryButtonClick` / `CloseButtonClick` events run before the dialog closes - set `ContentDialogButtonClickEventArgs.Cancel` to keep it open (for example when validation fails) - and the matching `*ButtonCommand` / `*ButtonCommandParameter` properties execute alongside them. `Opened` and `Closed` report the dialog lifecycle.

`TeachingTip` is a non-blocking coaching callout. Set `Target` to anchor the tip to an element - the beak points at the target and `PreferredPlacement` (`TeachingTipPlacementMode`) picks the side - or leave it unset to center the tip over the window content with the beak hidden. `IsOpen` shows and hides the tip, `IsLightDismissEnabled` lets a click elsewhere dismiss it, and `ActionButtonContent` / `ActionButtonCommand` plus `CloseButtonContent` drive the optional buttons, reported through `ActionButtonClick`, `CloseButtonClick`, and `Closed`.

```xml
<Grid>
    <ui:Button x:Name="SaveButton" Click="ShowTipButton_Click" Content="Save" />
    <ui:TeachingTip x:Name="SaveTip"
                    Title="Autosave is on"
                    Subtitle="Your changes are saved as you type."
                    CloseButtonContent="Got it"
                    IsLightDismissEnabled="True"
                    PreferredPlacement="Bottom" />
</Grid>
```

```csharp
private void ShowTipButton_Click(object sender, RoutedEventArgs e)
{
    SaveTip.Target = SaveButton;
    SaveTip.IsOpen = true;
}
```

`CommandBarFlyout` shows a compact horizontal strip of `AppBarButton` commands with an optional overflow menu of secondary commands behind a "see more" button. Invoking any command dismisses the flyout. It derives from `FlyoutBase`, so `ShowAt`, `Hide`, and the attached `FlyoutBase.AttachedFlyout` pattern all apply:

```xml
<ui:CommandBarFlyout>
    <ui:CommandBarFlyout.PrimaryCommands>
        <ui:AppBarButton Click="Copy_Click" Label="Copy">
            <ui:AppBarButton.Icon>
                <ui:FontIcon Glyph="&#xE8C8;" IconFontSize="16" />
            </ui:AppBarButton.Icon>
        </ui:AppBarButton>
    </ui:CommandBarFlyout.PrimaryCommands>
    <ui:CommandBarFlyout.SecondaryCommands>
        <ui:AppBarButton Click="Delete_Click" Label="Delete" />
    </ui:CommandBarFlyout.SecondaryCommands>
</ui:CommandBarFlyout>
```

Primary commands render icon-only with the `Label` as a tooltip; secondary commands render as icon-plus-label menu rows.

## Trees

`TreeView` and `TreeViewItem` render a hierarchical list matching the WinUI 3 `TreeView` visual contract.

```xml
<ui:TreeView SelectionMode="{x:Static uicore:TreeViewSelectionMode.Multiple}">
    <ui:TreeViewItem Header="Documents" IsExpanded="True">
        <ui:TreeViewItem Header="Reports">
            <ui:TreeViewItem Header="Q1.xlsx" />
            <ui:TreeViewItem Header="Q2.xlsx" />
        </ui:TreeViewItem>
        <ui:TreeViewItem Header="Presentations" />
    </ui:TreeViewItem>
    <ui:TreeViewItem Header="Pictures" />
</ui:TreeView>
```

Selection members:

| Member                         | Type                    | Notes                                                           |
| ------------------------------ | ----------------------- | --------------------------------------------------------------- |
| `TreeView.SelectionMode`       | `TreeViewSelectionMode` | `Single` by default; use `Multiple` for checkbox selection.     |
| `TreeView.SelectedItems`       | `IList`                 | Live selected item list.                                        |
| `TreeViewItem.IsSelectionChecked` | `bool?`               | Mirrors checked, unchecked, and indeterminate checkbox state.    |

Visual contract:

- Per-level indent via `LevelToIndentConverter` (16 px per level).
- Chevron (`U+E76C`) rotates 90° on expand - 100 ms `ControlFastOutSlowInKeySpline` easing.
- `SubtleFillColorSecondaryBrush` on hover, `SubtleFillColorTertiaryBrush` on press, `AccentFillColorDefaultBrush` when selected.
- VSM groups: `CommonStates`, `SelectionStates`, `ExpansionStates`.

## AutoSuggestBox

`AutoSuggestBox` is a text input with a light-dismiss suggestion list that follows the WinUI 3 contract: the application owns filtering. Handle `TextChanged`, ignore stale notifications (a `Reason` other than `UserInput`, or `CheckCurrent()` returning false), and update `ItemsSource`; the list opens while the box has keyboard focus and suggestions exist.

```xml
<ui:AutoSuggestBox x:Name="SearchBox"
                   Width="280"
                   Header="Fruit"
                   PlaceholderText="Search fruit"
                   QuerySubmitted="SearchBox_QuerySubmitted"
                   TextChanged="SearchBox_TextChanged" />
```

```csharp
private void SearchBox_TextChanged(object sender, AutoSuggestBoxTextChangedEventArgs e)
{
    if (e.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
    {
        return;
    }

    SearchBox.ItemsSource = FindMatches(SearchBox.Text);
}

private void SearchBox_QuerySubmitted(object sender, AutoSuggestBoxQuerySubmittedEventArgs e)
{
    string submitted = e.ChosenSuggestion as string ?? e.QueryText;
    // Act on the query.
}
```

Up and Down move through the suggestions, Enter raises `QuerySubmitted` (with `ChosenSuggestion` set when a suggestion is selected), and Escape closes the list. `UpdateTextOnSelect` (default `true`) writes a chosen suggestion back into `Text` through `TextMemberPath`, and `Header`, `PlaceholderText`, `QueryIcon`, and `MaxSuggestionListHeight` shape the field.

## Pickers

`DatePicker` and `TimePicker` show a button-styled field that opens a light-dismiss selector flyout; the flyout's accept button commits the pending column selection and cancel discards it.

`DatePicker` binds `SelectedDate` (`DateTime?`, `null` until the user picks) and raises `SelectedDateChanged` with the old and new dates. The field orders its day, month, and year segments by the current culture's short date pattern; `MinYear` / `MaxYear` (defaults 1900 and 2100) bound the year column, `DayVisible` / `MonthVisible` / `YearVisible` hide individual segments, and the day column rebuilds for the pending month and year, so 29 February is offered only in leap years.

```xml
<ui:DatePicker Header="Due date"
               PlaceholderText="Pick a date"
               SelectedDate="{Binding DueDate}" />
```

`TimePicker` binds `SelectedTime` (`TimeSpan?`) and raises `SelectedTimeChanged`. `ClockIdentifier` selects `12HourClock` (hours 1-12 plus an AM/PM column using the culture's designators, with an invariant AM/PM fallback when the culture defines none) or `24HourClock` (hours 0-23, no designator column); the default follows the current culture's short time pattern. `MinuteIncrement` steps the minute column (for example 5 offers 00, 05, 10, and so on).

```xml
<ui:TimePicker Header="Reminder time"
               ClockIdentifier="24HourClock"
               MinuteIncrement="5"
               SelectedTime="{Binding ReminderTime}" />
```

`ColorPicker` combines a saturation/value spectrum at the selected hue, a hue slider, an optional alpha slider, current/previous preview swatches, and a text-entry area with an RGB/HSV representation selector, per-channel inputs, an alpha percentage input, and a hex input. `Color` (two-way bindable, opaque red by default) raises `ColorChanged` with the old and new values; `PreviousColor` fills the comparison swatch and `IsAlphaEnabled` (default `false`) adds the alpha editing surfaces and 8-digit hex editing. The WinUI visibility options trim the layout: `IsColorSpectrumVisible`, `IsColorPreviewVisible`, `IsColorSliderVisible`, `IsColorChannelTextInputVisible`, `IsAlphaSliderVisible`, `IsAlphaTextInputVisible`, `IsHexInputVisible`, and `IsMoreButtonVisible` (default `false`; when `true` the text-entry area collapses behind a More/Less toggle). Channel and alpha inputs commit live per keystroke; the hex input commits on Enter or focus loss. The picker keeps hue, saturation, value, and alpha as its internal source of truth, so dragging along the spectrum's grey axis does not accumulate RGB round-trip drift.

```xml
<ui:ColorPicker Color="{Binding AccentColor, Mode=TwoWay}"
                IsAlphaEnabled="True"
                IsMoreButtonVisible="True" />
```

## Screenshots

Ten reference captures live under `docs/screenshots/`:

- `gallery-home-{light,dark}.png` - gallery shell on the Home page with the expanded `Left` navigation pane.
- `gallery-buttons-{light,dark}.png` - Buttons page with the `LeftCompact` navigation rail.
- `gallery-status-{light,dark}.png` - Status page with the `Top` navigation bar.
- `mvvm-{light,dark}.png` - the MVVM Task Manager demo.
- `powershell-{light,dark}.png` - the PowerShell controls-tour window (`03-ControlsTour.ps1`).

Capture is opt-in: the `GalleryScreenshotHarness` tests skip unless the `FLUENCE_CAPTURE_SCREENSHOTS` environment variable is set, so an ordinary test run never overwrites the committed images. To regenerate them (use the .NET 10 target so the MVVM capture is included):

```powershell
$env:FLUENCE_CAPTURE_SCREENSHOTS = '1'
dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net10.0-windows10.0.26100.0 --filter "TestCategory=Screenshots"
```

The harness uses `RenderTargetBitmap` and flattens transparent WPF layers over `SolidBackgroundFillColorBaseBrush`. It cannot capture DWM Mica / Acrylic, which compose outside WPF, so the screenshots show WPF control and shell surfaces only. `FluenceWindowTitleBarTests` verifies `FluenceWindow` caption styling.

Marketing images live under `docs/images/` (for example `docs/images/Banner.png`). Capture control screenshots at 100% and 150% scaling and record the reference OS build, theme, and accent when adding them.

## Tests

MSTest exercises templates, theme stability, and control behavior on .NET Framework 4.7.2 and .NET 10 for Windows. A new public control needs at minimum:

- A default-style / template smoke test that confirms the control applies the expected template.
- A theme-cycle pass if the control leans on `DynamicResource` (`ThemeTestHelpers.ApplyStandardThemeCycle`).
- Interaction or state assertions where the control exposes behavior (see `ControlTests.NavigationView.cs` and `ControlTests.FluentStroke.cs`).
