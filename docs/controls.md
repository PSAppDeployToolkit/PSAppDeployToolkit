# Control catalog

This catalog lists the public controls in `Fluence.Wpf.Controls`. Add `xmlns:fluence="http://schemas.fluencewpf.com"` in XAML and call `ApplicationThemeManager.Apply` before creating the first window. The [gallery](../Fluence.Wpf.Demo/README.md) groups live examples by task. The [C# API reference](api/index.md) documents each type's constructors, properties, methods, and events. Follow the [Basic walkthrough](csharp/usage.md) for a runnable XAML example.

## Window and application shell

| Type | Use it for | Start here |
| --- | --- | --- |
| [FluenceWindow](controls/fluence-window.md) | WPF window with a Fluent title bar, caption controls, backdrop policy, and corner preference | [Window guide](how-to/window-and-title-bar.md) |
| [TitleBar](controls/fluence-window.md#title-bar) | Title, back request, pane toggle request, and custom title-area content | [Gallery shell](../Fluence.Wpf.Demo/MainWindow.xaml) |

## Actions and selection

| Types | Use them for | Main behavior |
| --- | --- | --- |
| [Button](controls/button.md), [HyperlinkButton](controls/hyperlink-button.md), [AppBarButton](controls/app-bar-button.md), [RepeatButton](controls/repeat-button.md) | Commands and repeated actions | WPF command and click patterns |
| [ToggleButton](controls/toggle-button.md), [ToggleSwitch](controls/toggle-switch.md) | On/off and optional indeterminate state | Checked state; [ToggleSwitch](controls/toggle-switch.md) exposes a switch presentation |
| [DropDownButton](controls/drop-down-button.md), [SplitButton](controls/split-button.md), [ToggleSplitButton](controls/toggle-split-button.md) | Primary action with a flyout | Flyout opening; toggle split button also reports `IsCheckedChanged` |
| [CheckBox](controls/check-box.md), [RadioButton](controls/radio-button.md) | Independent or exclusive choices | Standard WPF checked state |
| [Slider](controls/slider.md), [RatingControl](controls/rating-control.md) | Select a value or give a rating | Value and change events |

## Text, forms, and pickers

| Types | Use them for | Key detail |
| --- | --- | --- |
| [TextBox](controls/text-box.md), [AutoSuggestBox](controls/auto-suggest-box.md) | Text entry and suggestions | `PlaceholderText`; query and suggestion events on [AutoSuggestBox](controls/auto-suggest-box.md) |
| Native WPF `PasswordBox` with [PasswordBoxExtensions](controls/password-box-extensions.md) | Password entry | The native sealed control gets an implicit Fluence style and attached properties |
| [NumberBox](controls/number-box.md) | Numeric entry with spin behavior | `Value`, `ValueChanged`, spin placement options |
| [ComboBox](controls/combo-box.md) | Choose from a drop-down list | `ItemsSource` collections, selection, editable text, and placeholder support |
| [DatePicker](controls/date-picker.md), [TimePicker](controls/time-picker.md) | Choose a date or time | Dedicated selected-value properties and changed events |
| [ColorPicker](controls/color-picker.md) | Choose a color | `Color`, `ColorChanged` |

## Navigation and tabs

| Types | Use them for | Main behavior |
| --- | --- | --- |
| [NavigationView](controls/navigation-view.md), [NavigationViewItem](controls/navigation-view.md#navigation-view-item), [NavigationViewItemHeader](controls/navigation-view.md#navigation-view-item-header), [NavigationViewItemSeparator](controls/navigation-view.md#navigation-view-item-separator) | Application destinations in a left or top pane | `ItemInvoked`, `BackRequested`, `PaneDisplayMode`, `FooterMenuItems` |
| [BreadcrumbBar](controls/breadcrumb-bar.md), [BreadcrumbBarItem](controls/breadcrumb-bar.md#breadcrumb-bar-item) | Location hierarchy | `ItemClicked` |
| [PipsPager](controls/pips-pager.md) | A compact sequence of pages | `SelectedIndexChanged` |
| [SelectorBar](controls/selector-bar.md), [SelectorBarItem](controls/selector-bar.md#selector-bar-item) | A compact choice among views | WPF selection model |
| [SlideNavigationPresenter](controls/slide-navigation-presenter.md) | Transition between navigation content | Slide transition effect |
| [TabView](controls/tab-view.md), [TabViewItem](controls/tab-view.md#tab-view-item) | Document-like tabs | Add and close requests; `TabWidthMode` |

## Data, collections, and people

| Types | Use them for | Key detail |
| --- | --- | --- |
| [ListBox](controls/list-box.md), [ListBoxItem](controls/list-box.md#list-box-item), [ListView](controls/list-view.md) | Data-bound item lists | Familiar WPF items, templates, and selection; `ListView.ItemsLayout` chooses layout |
| [TreeView](controls/tree-view.md), [TreeViewItem](controls/tree-view.md#tree-view-item) | Hierarchical collections | Expansion and selection; `TreeView.SelectedItems` for multiple selection |
| [PersonPicture](controls/person-picture.md) | Initials or avatar imagery | Person representation |

## Menus, flyouts, and dialogs

| Types | Use them for | Main behavior |
| --- | --- | --- |
| [Menu](controls/menu.md), [MenuItem](controls/menu.md#menu-item), [ContextMenu](controls/context-menu.md), [ToolTip](controls/tool-tip.md) | Conventional menus and hints | WPF menu and popup interactions |
| [FlyoutBase](controls/flyout.md#flyout-base), [Flyout](controls/flyout.md), [FlyoutPresenter](controls/flyout.md#flyout-presenter) | Attached or programmatic anchored content | `ShowAt`, `Hide`, opening and closing events |
| [CommandBarFlyout](controls/command-bar-flyout.md), [CommandBarFlyoutPresenter](controls/command-bar-flyout.md#command-bar-flyout-presenter) | A flyout with command actions | Provides command buttons in a popup |
| [TeachingTip](controls/teaching-tip.md) | Contextual instruction anchored to UI | `IsOpen`, action and close events |
| [ContentDialog](controls/content-dialog.md) | Modal decision inside an owner window | `ShowAsync`, result enum, button click events |

## Status and layout

| Types | Use them for | Main behavior |
| --- | --- | --- |
| [InfoBar](controls/info-bar.md), [InfoBadge](controls/info-badge.md) | Inline message or compact status | Severity/style and open or close behavior |
| [ProgressBar](controls/progress-bar.md), [ProgressRing](controls/progress-ring.md) | Determinate or indeterminate progress | Progress value or ring state |
| [Card](controls/card.md) | Passive or clickable content panel | `IsClickable` enables its `Click` event |
| [Border](controls/border.md), [StackPanel](controls/stack-panel.md), [DockPanel](controls/dock-panel.md), [Separator](controls/separator.md), [Expander](controls/expander.md) | Layout and grouped content | WPF layout patterns with Fluent defaults |
| [SmoothScrollViewer](controls/smooth-scroll-viewer.md), [ScrollBarExtensions](controls/smooth-scroll-viewer.md#scroll-bar-extensions) | Scrolling and native scroll bar behavior | Smooth scrolling and attached extensions |

## Text, icons, and images

| Types | Use them for | Key detail |
| --- | --- | --- |
| [TextBlock](controls/text-block.md), [TextBlockExtensions](controls/text-block.md#text-block-extensions) | Fluent typography | Fluence [TextBlock](controls/text-block.md) is a templated `ContentControl`; extensions also serve native text |
| [FontIcon](controls/font-icon.md) | Segoe Fluent icon glyphs | Set `Glyph` and optional icon font size |
| [Image](controls/image.md) | Images with templated corner clipping | Not a subclass of native WPF [Image](https://learn.microsoft.com/dotnet/api/system.windows.controls.image) |

## Accessibility and extensibility

Custom controls expose UI Automation peers under `Fluence.Wpf.Automation`. The gallery includes focus, keyboard, high contrast, and right-to-left examples. Use the [accessibility reference](reference/accessibility.md) when checking an interaction. `Fluence.Wpf.Markup` adds `ThemeResourceExtension` and theme dictionaries for app-authored theme values; see the [theme guide](theming.md#theme-aware-markup).

The `Fluence.Wpf` namespace also contains theme managers, enums, and typed event arguments used by these controls. The [API reference](api/index.md) explains where to look them up.
