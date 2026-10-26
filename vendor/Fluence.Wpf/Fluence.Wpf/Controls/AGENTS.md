# Control and window guidance

Read this file when adding or changing a control, dependency property, routed event, window chrome behavior, or native interop used by a control.

## Control contract

- Subclass the closest WPF control and set DefaultStyleKey metadata in its static constructor.
- Register dependency properties with DependencyProperty.Register. For read-only state, use RegisterReadOnly and expose the public DependencyProperty.
- A new template part uses a private constant whose identifier and value both read PART_Name, and a matching TemplatePart attribute. Wire it in OnApplyTemplate. Existing controls have older exceptions; do not extend those forms.
- Declare visual-state groups with TemplateVisualState. Match existing common-state names such as Normal, PointerOver, Pressed, and Disabled.
- Use EventHandler<TArgs> for typed events. Use a routed event only when events need to bubble or tunnel through a template. Existing routed events are BreadcrumbBarItem.Click, Card.Click, SplitButton.Click, TabView.AddTabButtonClick, TabView.TabCloseRequested, and TabViewItem.CloseRequested.
- Place every new public event-argument type in its own source file under Fluence.Wpf/.
- When a framework control is sealed, style the native type rather than deriving from it. PasswordBox is the only implicit native framework-type style in the library; its Fluence-only properties live in PasswordBoxExtensions. Other native ScrollBar, ScrollViewer, RepeatButton, and Thumb styles remain keyed.
- New controls need a demo sample, a focused xUnit test, and a CHANGELOG entry. Follow the new-control playbook in .claude/skills/new-control/SKILL.md.

## Window and native behavior

- FluenceWindow owns DWM backdrop, rounded-corner, caption-extension, and title-bar policy. Child controls read OsVersionHelper and follow that policy instead of hard-coding OS-specific metrics or backdrop flags.
- The build checks arithmetic overflow. Wrap Win32 HIWORD/LOWORD bit-mask extraction in unchecked blocks, as in FluenceWindow.HitTestTitleBar.
- Select the immersive dark-mode DWM attribute through NativeMethods.GetImmersiveDarkModeAttribute. Attribute 19 is required below Windows build 18985; attribute 20 applies from build 18985.
- Preserve the auto-hide taskbar work-area adjustment in WM_GETMINMAXINFO for maximized custom-chrome windows.
- Subscribe to static theme and accent manager events in OnSourceInitialized and unsubscribe in OnClosed. Subscribing in a constructor retains windows that are never shown.
- When a DWM backdrop is active, keep HwndSource.CompositionTarget.BackgroundColor aligned with Window.Background. A transparent Window.Background with an opaque black redirection surface can cause a first-paint flash.

## Focused checks

Run the relevant class on both full .NET test targets. For window, native, or theme behavior, also check the root [KNOWN_ISSUES.md](../../KNOWN_ISSUES.md) and test the affected OS capability branch.
