/*
 * Copyright 2026 Dan Cunningham
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// ToggleSplitButton tests: the WinUI toggle-then-Click primary contract,
    /// IsCheckedChanged, flyout behavior inherited from SplitButton, the checked
    /// accent visuals including the checked divider stroke and CheckedFlyoutOpen,
    /// and the Toggle + ExpandCollapse automation surface (deliberately no Invoke).
    /// </summary>
    public partial class ControlTests
    {
        private sealed class ToggleSplitButtonRelayCommand(Action<object?> execute) : ICommand
        {
            private readonly Action<object?> _execute = execute;

            public bool CanExecute(object? parameter) { return true; }
            public void Execute(object? parameter) { _execute(parameter); }
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S108:Nested blocks of code should not be left empty", Justification = "This is just test code.")]
            public event EventHandler? CanExecuteChanged { add { } remove { } }
        }

        // Constructs the control inside the STA action (FrameworkElement creation on
        // the xUnit worker thread throws) and shows it so the template applies.
        private static Task RunToggleSplitButtonTestAsync(Func<Controls.ToggleSplitButton> createButton, Action<Application, Controls.ToggleSplitButton> verify)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
                Controls.ToggleSplitButton button = createButton();
                Window window = new();

                try
                {
                    window.Content = button;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    verify(application, button);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        private static Button GetPrimaryButtonPart(Controls.ToggleSplitButton button)
        {
            return Assert.IsType<Button>(button.Template?.FindName("PART_PrimaryButton", button));
        }

        private static ToggleButton GetSecondaryButtonPart(Controls.ToggleSplitButton button)
        {
            return Assert.IsAssignableFrom<ToggleButton>(button.Template?.FindName("PART_SecondaryButton", button));
        }

        [Fact]
        public Task ToggleSplitButton_Defaults_AreWinUiCanonAsync()
        {
            return RunToggleSplitButtonTestAsync(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                    IsHitTestVisible = false,
                },
                (_, button) =>
                {
                    Assert.False(button.IsChecked);
                    Assert.Equal(new CornerRadius(4), button.CornerRadius);
                    Assert.Equal(new CornerRadius(8), button.DropdownCornerRadius);
                    Assert.False(button.IsFlyoutOpen);
                    Assert.Equal(ControlAppearance.Standard, button.Appearance);
                    Assert.Null(button.Command);
                    Assert.Null(button.Flyout);
                });
        }

        [Fact]
        public Task ToggleSplitButton_DefaultStyle_ExposesTemplatePartsAsync()
        {
            return RunToggleSplitButtonTestAsync(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                    IsHitTestVisible = false,
                },
                (_, button) =>
                {
                    Assert.NotNull(GetPrimaryButtonPart(button));
                    Assert.NotNull(GetSecondaryButtonPart(button));

                    Popup popup = Assert.IsType<Popup>(button.Template?.FindName("PART_Popup", button));
                    Rectangle divider = Assert.IsAssignableFrom<Rectangle>(FindVisualChildByName<Rectangle>(button, "Divider"));

                    Assert.False(popup.StaysOpen, "The flyout popup should light-dismiss.");
                });
        }

        [Fact]
        public Task ToggleSplitButton_PrimaryClick_TogglesThenRaisesClickAsync()
        {
            return RunToggleSplitButtonTestAsync(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                },
                (_, button) =>
                {
                    int clickCount = 0;
                    bool? checkedInsideHandler = null;
                    button.Click += (_, _) =>
                    {
                        clickCount++;
                        checkedInsideHandler = button.IsChecked;
                    };

                    GetPrimaryButtonPart(button).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    WpfTestSta.DrainDispatcher(button.Dispatcher);

                    Assert.Equal(1, clickCount);
                    Assert.Equal(true, checkedInsideHandler);
                    Assert.True(button.IsChecked);
                });
        }

        [Fact]
        public Task ToggleSplitButton_PrimaryClick_SecondClickTogglesOffAsync()
        {
            return RunToggleSplitButtonTestAsync(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                },
                (_, button) =>
                {
                    int clickCount = 0;
                    button.Click += (_, _) => clickCount++;

                    Button primary = GetPrimaryButtonPart(button);
                    primary.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    primary.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    WpfTestSta.DrainDispatcher(button.Dispatcher);

                    Assert.Equal(2, clickCount);
                    Assert.False(button.IsChecked, "A second primary click should toggle back off.");
                });
        }

        [Fact]
        public Task ToggleSplitButton_PrimaryClick_StillExecutesCommandAsync()
        {
            return RunToggleSplitButtonTestAsync(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                },
                (_, button) =>
                {
                    int executeCount = 0;
                    button.Command = new ToggleSplitButtonRelayCommand(_ => executeCount++);

                    GetPrimaryButtonPart(button).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    WpfTestSta.DrainDispatcher(button.Dispatcher);

                    Assert.Equal(1, executeCount);
                    Assert.True(button.IsChecked, "The primary click must also toggle the checked state.");
                });
        }

        [Fact]
        public Task ToggleSplitButton_IsCheckedChanged_RaisesWithNewValueAsync()
        {
            return RunToggleSplitButtonTestAsync(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                    IsHitTestVisible = false,
                },
                (_, button) =>
                {
                    int raiseCount = 0;
                    object? lastSender = null;
                    bool? lastValue = null;
                    button.IsCheckedChanged += (sender, args) =>
                    {
                        raiseCount++;
                        lastSender = sender;
                        lastValue = args.IsChecked;
                    };

                    button.IsChecked = true;
                    Assert.Equal(1, raiseCount);
                    Assert.Same(button, lastSender);
                    Assert.Equal(true, lastValue);

                    button.IsChecked = false;
                    Assert.Equal(2, raiseCount);
                    Assert.Equal(false, lastValue);
                });
        }

        [Fact]
        public Task ToggleSplitButton_SecondaryToggle_OpensFlyoutWithoutTogglingAsync()
        {
            return RunToggleSplitButtonTestAsync(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                    Flyout = "Flyout content",
                },
                (_, button) =>
                {
                    ToggleButton secondary = GetSecondaryButtonPart(button);
                    Popup popup = Assert.IsType<Popup>(button.Template?.FindName("PART_Popup", button));

                    secondary.IsChecked = true;
                    WpfTestSta.DrainDispatcher(button.Dispatcher);

                    Assert.True(popup.IsOpen, "Checking the secondary half should open the flyout popup.");
                    Assert.True(button.IsFlyoutOpen);
                    Assert.False(button.IsChecked, "Opening the flyout must not toggle the primary checked state.");

                    secondary.IsChecked = false;
                    WpfTestSta.DrainDispatcher(button.Dispatcher);

                    Assert.False(popup.IsOpen, "Unchecking the secondary half should close the flyout popup.");
                    Assert.False(button.IsFlyoutOpen);
                });
        }

        [Fact]
        public Task ToggleSplitButton_Checked_AccentFillsBothHalvesAndBackdropsAsync()
        {
            return RunToggleSplitButtonTestAsync(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                    IsChecked = true,
                    IsHitTestVisible = false,
                },
                (application, button) =>
                {
                    Border primaryFill = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(button, "PrimaryFill"));
                    Border secondaryFill = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(button, "SecondaryFill"));
                    Border primaryBackdrop = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(button, "PrimaryAccentFillBackdrop"));
                    Border secondaryBackdrop = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(button, "SecondaryAccentFillBackdrop"));


                    Color accentDefault = GetResolvedBrushColor(application, "AccentFillColorDefaultBrush");
                    Assert.Equal(accentDefault, GetSolidColor(primaryFill.Background));
                    Assert.Equal(accentDefault, GetSolidColor(secondaryFill.Background));
                    Assert.Equal(1.0, primaryBackdrop.Opacity);
                    Assert.Equal(1.0, secondaryBackdrop.Opacity);
                    Assert.Equal(GetResolvedBrushColor(application, "TextOnAccentFillColorPrimaryBrush"), GetSolidColor(button.Foreground));
                });
        }

        [Fact]
        public Task ToggleSplitButton_Checked_DividerUsesCheckedDividerBrushAsync()
        {
            return RunToggleSplitButtonTestAsync(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                    IsHitTestVisible = false,
                },
                (application, button) =>
                {
                    Rectangle divider = Assert.IsAssignableFrom<Rectangle>(FindVisualChildByName<Rectangle>(button, "Divider"));

                    Color uncheckedDivider = GetSolidColor(divider.Fill);

                    button.IsChecked = true;
                    WpfTestSta.DrainDispatcher(button.Dispatcher);
                    button.UpdateLayout();

                    Color checkedDivider = GetSolidColor(divider.Fill);

                    Assert.Equal(GetResolvedBrushColor(application, "ControlStrokeColorOnAccentTertiaryBrush"), checkedDivider);
                    Assert.NotEqual(uncheckedDivider, checkedDivider);
                });
        }

        [Fact]
        public Task ToggleSplitButton_CheckedFlyoutOpen_TintsBothHalvesPressedFillAsync()
        {
            return RunToggleSplitButtonTestAsync(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                    IsChecked = true,
                    Flyout = "Flyout content",
                },
                (application, button) =>
                {
                    ToggleButton secondary = GetSecondaryButtonPart(button);
                    secondary.IsChecked = true;
                    WpfTestSta.DrainDispatcher(button.Dispatcher);
                    button.UpdateLayout();

                    Border primaryFill = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(button, "PrimaryFill"));
                    Border secondaryFill = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(button, "SecondaryFill"));


                    Color accentTertiary = GetResolvedBrushColor(application, "AccentFillColorTertiaryBrush");
                    Assert.Equal(accentTertiary, GetSolidColor(primaryFill.Background));
                    Assert.Equal(accentTertiary, GetSolidColor(secondaryFill.Background));

                    secondary.IsChecked = false;
                    WpfTestSta.DrainDispatcher(button.Dispatcher);
                });
        }

        [Fact]
        public Task ToggleSplitButton_CheckedDisabled_UsesAccentDisabledPaletteAsync()
        {
            return RunToggleSplitButtonTestAsync(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                    IsChecked = true,
                    IsEnabled = false,
                },
                (application, button) =>
                {
                    Border primaryFill = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(button, "PrimaryFill"));
                    Border secondaryFill = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(button, "SecondaryFill"));


                    Color accentDisabled = GetResolvedBrushColor(application, "AccentFillColorDisabledBrush");
                    Assert.Equal(accentDisabled, GetSolidColor(primaryFill.Background));
                    Assert.Equal(accentDisabled, GetSolidColor(secondaryFill.Background));
                    Assert.Equal(GetResolvedBrushColor(application, "TextFillColorDisabledBrush"), GetSolidColor(button.Foreground));
                });
        }

        [Fact]
        public Task ToggleSplitButton_AutomationPeer_ExposesToggleAndExpandCollapseAsync()
        {
            return RunToggleSplitButtonTestAsync(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                    IsHitTestVisible = false,
                },
                (_, button) =>
                {
                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(button);

                    Assert.Equal("ToggleSplitButton", peer.GetClassName(), StringComparer.Ordinal);
                    Assert.Equal(AutomationControlType.SplitButton, peer.GetAutomationControlType());
                    Assert.NotNull(peer.GetPattern(PatternInterface.Toggle));
                    Assert.NotNull(peer.GetPattern(PatternInterface.ExpandCollapse));
                    Assert.Null(peer.GetPattern(PatternInterface.Invoke));
                });
        }

        [Fact]
        public Task ToggleSplitButton_TogglePattern_TogglesStateAndRaisesEventAsync()
        {
            return RunToggleSplitButtonTestAsync(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                    IsHitTestVisible = false,
                },
                (_, button) =>
                {
                    int raiseCount = 0;
                    button.IsCheckedChanged += (_, _) => raiseCount++;

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(button);
                    IToggleProvider toggleProvider = Assert.IsAssignableFrom<IToggleProvider>(peer.GetPattern(PatternInterface.Toggle));

                    toggleProvider.Toggle();
                    Assert.True(button.IsChecked);
                    Assert.Equal(ToggleState.On, toggleProvider.ToggleState);
                    Assert.Equal(1, raiseCount);

                    toggleProvider.Toggle();
                    Assert.False(button.IsChecked);
                    Assert.Equal(ToggleState.Off, toggleProvider.ToggleState);
                    Assert.Equal(2, raiseCount);
                });
        }

        [Fact]
        public Task ToggleSplitButton_ExpandCollapsePattern_OpensAndClosesFlyoutAsync()
        {
            return RunToggleSplitButtonTestAsync(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                    Flyout = "Flyout content",
                },
                (_, button) =>
                {
                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(button);
                    IExpandCollapseProvider expandProvider = Assert.IsAssignableFrom<IExpandCollapseProvider>(peer.GetPattern(PatternInterface.ExpandCollapse));
                    Assert.Equal(ExpandCollapseState.Collapsed, expandProvider.ExpandCollapseState);

                    expandProvider.Expand();
                    WpfTestSta.DrainDispatcher(button.Dispatcher);

                    Assert.True(button.IsFlyoutOpen, "Expand must open the flyout.");
                    Assert.Equal(ExpandCollapseState.Expanded, expandProvider.ExpandCollapseState);

                    expandProvider.Collapse();
                    WpfTestSta.DrainDispatcher(button.Dispatcher);

                    Assert.False(button.IsFlyoutOpen, "Collapse must close the flyout.");
                    Assert.Equal(ExpandCollapseState.Collapsed, expandProvider.ExpandCollapseState);
                });
        }

        [Fact]
        public async Task ToggleSplitButton_FocusVisuals_UseKeyboardOnlyFocusVisualStyleAsync()
        {
            // Mirrors the SplitButton contract: focus rings come from the
            // DefaultControlFocusVisualStyle adorner on each half, which WPF shows only
            // for keyboard navigation (Tab), never on mouse click - matching DropDownButton.
            await RunToggleSplitButtonTestAsync(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                    IsHitTestVisible = false,
                },
                (application, button) =>
                {
                    Button primary = GetPrimaryButtonPart(button);
                    ToggleButton secondary = GetSecondaryButtonPart(button);
                    Style focusVisualStyle = Assert.IsType<Style>(application?.TryFindResource("DefaultControlFocusVisualStyle"));

                    Assert.Same(focusVisualStyle, primary.FocusVisualStyle);
                    Assert.Same(focusVisualStyle, secondary.FocusVisualStyle);
                    Assert.Null(FindVisualChildByName<Border>(button, "PrimaryFocusOuter"));
                    Assert.Null(FindVisualChildByName<Border>(button, "SecondaryFocusOuter"));
                }).ConfigureAwait(true);
        }

        [Fact]
        public Task ToggleSplitButton_ThemeCycle_CheckedBrushesReResolveAsync()
        {
            return RunToggleSplitButtonTestAsync(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                    IsChecked = true,
                    IsHitTestVisible = false,
                },
                (application, button) =>
                {
                    ThemeTestHelpers.ApplyStandardThemeCycle();
                    WpfTestSta.DrainDispatcher(button.Dispatcher);
                    button.UpdateLayout();

                    Border primaryFill = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(button, "PrimaryFill"));
                    Rectangle divider = Assert.IsAssignableFrom<Rectangle>(FindVisualChildByName<Rectangle>(button, "Divider"));

                    Assert.Equal(GetResolvedBrushColor(application, "AccentFillColorDefaultBrush"), GetSolidColor(primaryFill.Background));
                    Assert.Equal(GetResolvedBrushColor(application, "ControlStrokeColorOnAccentTertiaryBrush"), GetSolidColor(divider.Fill));
                    ThemeTestHelpers.AssertKeyThemeBrushesResolve(application);
                });
        }
    }
}
