# Control catalog

This catalog lists the public controls in `Fluence.Wpf.Controls`. Add `xmlns:fluence="http://schemas.fluencewpf.com"` in XAML and call `ApplicationThemeManager.Apply` before creating the first window. The [gallery](../Fluence.Wpf.Demo/README.md) groups live examples by task. The [C# API reference](api/index.md) documents each type's constructors, properties, methods, and events. Follow the [Basic walkthrough](https://fluencewpf.com/docs/csharp/usage) for a runnable XAML example.

## Window and application shell

| Type | Use it for | Start here |
| --- | --- | --- |
| [FluenceWindow](controls/fluence-window.md) | WPF window with a Fluent title bar, caption controls, backdrop policy, and corner preference | [Window guide](how-to/window-and-title-bar.md) |
| [TitleBar](controls/title-bar.md) | Title, back request, pane toggle request, and custom title-area content | [Gallery shell](../Fluence.Wpf.Demo/MainWindow.xaml) |

[FluenceWindow](controls/fluence-window.md) extends `Window`. `SystemBackdropType` requests Mica, Acrylic, Tabbed, Auto, or None. Its [TitleBar](controls/title-bar.md) property accepts a UI element; the [TitleBar](controls/title-bar.md) control raises `BackRequested` and `PaneToggleRequested`.

## Actions and selection

| Types | Use them for | Main behavior |
| --- | --- | --- |
| [Button](controls/button.md), [HyperlinkButton](controls/hyperlink-button.md), [AppBarButton](controls/app-bar-button.md), [RepeatButton](controls/repeat-button.md) | Commands and repeated actions | WPF command and click patterns |
| [ToggleButton](controls/toggle-button.md), [ToggleSwitch](controls/toggle-switch.md) | On/off and optional indeterminate state | Checked state; [ToggleSwitch](controls/toggle-switch.md) exposes a switch presentation |
| [DropDownButton](controls/drop-down-button.md), [SplitButton](controls/split-button.md), [ToggleSplitButton](controls/toggle-split-button.md) | Primary action with a flyout | Flyout opening; toggle split button also reports `IsCheckedChanged` |
| [CheckBox](controls/check-box.md), [RadioButton](controls/radio-button.md) | Independent or exclusive choices | Standard WPF checked state |
| [Slider](controls/slider.md), [RatingControl](controls/rating-control.md) | Continuous or discrete ratings | Value and change events |

`ControlAppearance` selects the visual emphasis for supported controls. The gallery's Buttons and Selection pages show the variants. For a primary command, `Appearance="Accent"` is the common choice.

The `CheckBox` keeps native WPF selection, input, and automation behavior. Its indicator is a 20-DIP box with a 1-DIP outline. The animated check path uses a source-derived 1.225-DIP stroke; checking draws it over 316.7 ms in normal and hover states, while pressing from unchecked draws it over 150 ms. Unchecking erases the path over 66.7 ms. The indeterminate dash remains static. State changes use a bounded FIFO route queue and coalesce changes received before layout. Initial load and retemplating snap to the current glyph; unloading cancels active motion and clears queued routes. Disabled motion also snaps to the resolved state. The animation uses path trim only, without fade or scale effects. The check geometry and route data are adapted from Microsoft WinUI's [`AnimatedAcceptVisualSource`](https://github.com/microsoft/microsoft-ui-xaml/blob/7b12709347527238494ca28cb9fed326d513e779/controls/dev/AnimatedIcon/AnimatedVisuals/AnimatedAcceptVisualSource.cpp) and [`CheckBox` theme resources](https://github.com/microsoft/microsoft-ui-xaml/blob/7b12709347527238494ca28cb9fed326d513e779/controls/dev/CommonStyles/CheckBox_themeresources.xaml), under the upstream repository's [MIT license](https://github.com/microsoft/microsoft-ui-xaml/blob/7b12709347527238494ca28cb9fed326d513e779/LICENSE).

The `RadioButton` keeps native WPF grouping, keyboard navigation, and UI Automation behavior. Its Fluent indicator has a 20-DIP outer ring with a 1-DIP stroke and a 12-DIP checked dot. The dot changes to 14 DIP on hover or when disabled, and to 10 DIP while pressed. The checked dot has no inner stroke in any state. Checking and unchecking change dot opacity immediately. Hover and press size transitions take 250 ms, while disabled sizing takes 167 ms. A separate pressed glyph grows from 4 to 10 DIP in 167 ms. The 32-DIP glyph row uses a 20-DIP icon column, 8-DIP left and 6-DIP top content padding, and a 120-DIP minimum width.

## Text, forms, and pickers

| Types | Use them for | Key detail |
| --- | --- | --- |
| [TextBox](controls/text-box.md), [AutoSuggestBox](controls/auto-suggest-box.md) | Text entry and suggestions | `PlaceholderText`; query and suggestion events on [AutoSuggestBox](controls/auto-suggest-box.md) |
| Native WPF `PasswordBox` with [PasswordBoxExtensions](controls/password-box-extensions.md) | Password entry | The native sealed control gets an implicit Fluence style and attached properties |
| [NumberBox](controls/number-box.md) | Numeric entry with spin behavior | `Value`, `ValueChanged`, spin placement options |
| [ComboBox](controls/combo-box.md) | Choose from a drop-down list | `ItemsSource` collections, selection, editable text, and placeholder support |
| [DatePicker](controls/date-picker.md), [TimePicker](controls/time-picker.md) | Choose a date or time | Dedicated selected-value properties and changed events |
| [ColorPicker](controls/color-picker.md) | Choose a color | `Color`, `ColorChanged` |

ColorPicker's Hex field omits the character counter while retaining its RGB/ARGB input-length limit. Its RGB, HSV, and alpha fields keep their existing counters.

The Fluence [DatePicker](controls/date-picker.md) is a `Control` with a three-column picker, not a subclass of the native WPF calendar picker. Use its own `SelectedDate` API. See [inputs and data](how-to/inputs-and-data.md) and the gallery's Inputs and Forms pages.

`AutoSuggestBox` observes collection changes when its current `ItemsSource` implements `INotifyCollectionChanged`. Adding the first suggestion or removing the last one updates the popup without replacing the collection or the consumer's open-state binding.

## Navigation and tabs

| Types | Use them for | Main behavior |
| --- | --- | --- |
| [NavigationView](controls/navigation-view.md), [NavigationViewItem](controls/navigation-view-item.md), [NavigationViewItemHeader](controls/navigation-view-item-header.md), [NavigationViewItemSeparator](controls/navigation-view-item-separator.md) | Application destinations in a left or top pane | `ItemInvoked`, `BackRequested`, `PaneDisplayMode`, `FooterMenuItems` |
| [BreadcrumbBar](controls/breadcrumb-bar.md), [BreadcrumbBarItem](controls/breadcrumb-bar-item.md) | Location hierarchy | `ItemClicked` |
| [PipsPager](controls/pips-pager.md) | A compact sequence of pages | `SelectedIndexChanged` |
| [SelectorBar](controls/selector-bar.md), [SelectorBarItem](controls/selector-bar-item.md) | A compact choice among views | WPF selection model |
| [SlideNavigationPresenter](controls/slide-navigation-presenter.md) | Transition between navigation content | Slide transition effect |
| [TabView](controls/tab-view.md), [TabViewItem](controls/tab-view-item.md) | Document-like tabs | Add and close requests; `TabWidthMode` |

[NavigationView](controls/navigation-view.md) derives from `Selector`. Put its page host in `NavigationView.Content`, because an unnamed child enters the item collection. The application controls its navigation history. [TabView](controls/tab-view.md) extends WPF `TabControl`; a close request does not remove the item for you. See [navigation and tabs](how-to/navigation-and-tabs.md).

Reordering `NavigationView.FooterMenuItems` preserves a selected item that remains present. Removing or clearing that selected footer item clears its footer selection; it does not select an unrelated main item.

In `TabView`, the trailing arrow from the last visible, enabled, focusable header reaches the visible enabled add button; the reverse arrow returns to the header. When no header can receive keyboard focus, Tab reaches the add button directly. Tab then leaves the strip. Arrow navigation and overflow-button glyphs follow right-to-left layout. Ctrl+F4 requests closure of the selected closable tab from its header or content, unless the content handles the key first. Selection and layout changes keep the selected header visible within the strip. A header wider than the viewport aligns its physical leading edge with the viewport's leading edge.

## Data, collections, and people

| Types | Use them for | Key detail |
| --- | --- | --- |
| [ListBox](controls/list-box.md), [ListBoxItem](controls/list-box-item.md), [ListView](controls/list-view.md) | Data-bound item lists | Familiar WPF items, templates, and selection; `ListView.ItemsLayout` chooses layout |
| [TreeView](controls/tree-view.md), [TreeViewItem](controls/tree-view-item.md) | Hierarchical collections | Expansion and selection; `TreeView.SelectedItems` for multiple selection |
| [Card](controls/card.md) | Passive or clickable content panel | `IsClickable` enables its `Click` event |
| [PersonPicture](controls/person-picture.md) | Initials or avatar imagery | Person representation |

The theme also styles some native framework elements used inside these controls. A `ListViewItem` is the WPF container, not a separate public Fluence class.

`TreeView` retains physical scrolling by default. With `SelectionMode="Single"` or `"None"`, opt in using `VirtualizingPanel.IsVirtualizing="True"`; `VirtualizingPanel.VirtualizationMode="Recycling"` can reuse containers. Keep selection and expansion state in model-backed item bindings when it must survive recycling. `Multiple` mode uses physical scrolling in the shipped template even when logical scrolling and a virtualizing panel are requested, because checked selection and parent tri-state currently depend on realized containers. A fully custom control template is responsible for its own behavior. See the [tree-selection limitations](../KNOWN_ISSUES.md#tree-selection-and-unrealized-data-bound-branches).

## Menus, flyouts, and dialogs

| Types | Use them for | Main behavior |
| --- | --- | --- |
| [Menu](controls/menu.md), [MenuItem](controls/menu-item.md), [ContextMenu](controls/context-menu.md), [ToolTip](controls/tool-tip.md) | Conventional menus and hints | WPF menu and popup interactions |
| [FlyoutBase](controls/flyout-base.md), [Flyout](controls/flyout.md), [FlyoutPresenter](controls/flyout-presenter.md) | Attached or programmatic anchored content | `ShowAt`, `Hide`, opening and closing events |
| [CommandBarFlyout](controls/command-bar-flyout.md), [CommandBarFlyoutPresenter](controls/command-bar-flyout-presenter.md) | A flyout with command actions | Provides command buttons in a popup |
| [TeachingTip](controls/teaching-tip.md) | Contextual instruction anchored to UI | `IsOpen`, action and close events |
| [ContentDialog](controls/content-dialog.md) | Modal decision inside an owner window | `ShowAsync`, result enum, button click events |

[FlyoutBase](controls/flyout-base.md) is an abstract support type. `ContentDialog.ShowAsync` needs an active owner and must run on the UI dispatcher. See [dialogs and feedback](how-to/dialogs-and-feedback.md).

## Status and layout

| Types | Use them for | Main behavior |
| --- | --- | --- |
| [InfoBar](controls/info-bar.md), [InfoBadge](controls/info-badge.md) | Inline message or compact status | Severity/style and open or close behavior |
| [ProgressBar](controls/progress-bar.md), [ProgressRing](controls/progress-ring.md) | Determinate or indeterminate progress | Progress value or ring state |
| [Border](controls/border.md), [StackPanel](controls/stack-panel.md), [DockPanel](controls/dock-panel.md), [Separator](controls/separator.md), [Expander](controls/expander.md) | Layout and grouped content | WPF layout patterns with Fluent defaults |
| [SmoothScrollViewer](controls/smooth-scroll-viewer.md), [ScrollBarExtensions](controls/scroll-bar-extensions.md) | Scrolling and native scroll bar behavior | Smooth scrolling and attached extensions |

The ProgressBar uses a 1 DIP track and a 3 DIP indicator by default. Their vertical centerlines align in the determinate presentation. Custom `TrackHeight` or `MinHeight` values can have half-pixel offsets from device-pixel rounding.

## Text, icons, and images

| Types | Use them for | Key detail |
| --- | --- | --- |
| [TextBlock](controls/text-block.md), [TextBlockExtensions](controls/text-block-extensions.md) | Fluent typography | Fluence [TextBlock](controls/text-block.md) is a templated `ContentControl`; extensions also serve native text |
| [FontIcon](controls/font-icon.md) | Segoe Fluent icon glyphs | Set `Glyph` and optional icon font size |
| [Image](controls/image.md) | Images with templated corner clipping | Not a subclass of native WPF [Image](controls/image.md) |

Typography styles such as `BodyTextBlockStyle` and `TitleLargeTextBlockStyle` are application resources. See [theming](theming.md).

## Accessibility and extensibility

Custom controls expose UI Automation peers under `Fluence.Wpf.Automation`. The gallery includes focus, keyboard, high contrast, and right-to-left examples. Use the [accessibility reference](reference/accessibility.md) when checking an interaction. `Fluence.Wpf.Markup` adds `ThemeResourceExtension` and theme dictionaries for app-authored theme values; see the [theme guide](theming.md#theme-aware-markup).

The `Fluence.Wpf` namespace also contains theme managers, enums, and typed event arguments used by these controls. The [API reference map](reference/api.md) explains where to look them up.
