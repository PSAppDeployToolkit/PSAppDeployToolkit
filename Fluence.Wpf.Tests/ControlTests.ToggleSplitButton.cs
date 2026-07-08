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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

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
        // the MSTest worker thread throws) and shows it so the template applies.
        private static void RunToggleSplitButtonTest(Func<Controls.ToggleSplitButton> createButton, Action<Application?, Controls.ToggleSplitButton> verify)
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
                Controls.ToggleSplitButton button = createButton();
                Window window = new();

                try
                {
                    window.Content = button;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
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

        private static System.Windows.Controls.Button GetPrimaryButtonPart(Controls.ToggleSplitButton button)
        {
            System.Windows.Controls.Button? primary = button.Template?.FindName("PART_PrimaryButton", button) as System.Windows.Controls.Button;
            Assert.IsNotNull(primary, "PART_PrimaryButton should exist in the template.");
            return primary;
        }

        private static ToggleButton GetSecondaryButtonPart(Controls.ToggleSplitButton button)
        {
            ToggleButton? secondary = button.Template?.FindName("PART_SecondaryButton", button) as ToggleButton;
            Assert.IsNotNull(secondary, "PART_SecondaryButton should exist in the template.");
            return secondary;
        }

        [TestMethod]
        public void ToggleSplitButton_Defaults_AreWinUiCanon()
        {
            RunToggleSplitButtonTest(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                    IsHitTestVisible = false,
                },
                (_, button) =>
                {
                    Assert.IsFalse(button.IsChecked);
                    Assert.AreEqual(new CornerRadius(4), button.CornerRadius);
                    Assert.AreEqual(new CornerRadius(8), button.DropdownCornerRadius);
                    Assert.IsFalse(button.IsFlyoutOpen);
                    Assert.AreEqual(ControlAppearance.Standard, button.Appearance);
                    Assert.IsNull(button.Command);
                    Assert.IsNull(button.Flyout);
                });
        }

        [TestMethod]
        public void ToggleSplitButton_DefaultStyle_ExposesTemplateParts()
        {
            RunToggleSplitButtonTest(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                    IsHitTestVisible = false,
                },
                (_, button) =>
                {
                    Assert.IsNotNull(GetPrimaryButtonPart(button));
                    Assert.IsNotNull(GetSecondaryButtonPart(button));

                    Popup? popup = button.Template?.FindName("PART_Popup", button) as Popup;
                    Rectangle? divider = FindVisualChildByName<Rectangle>(button, "Divider");

                    Assert.IsNotNull(popup, "PART_Popup should exist in the template.");
                    Assert.IsFalse(popup.StaysOpen, "The flyout popup should light-dismiss.");
                    Assert.IsNotNull(divider, "The divider rectangle should exist in the template.");
                });
        }

        [TestMethod]
        public void ToggleSplitButton_PrimaryClick_TogglesThenRaisesClick()
        {
            RunToggleSplitButtonTest(
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
                    DrainDispatcher(button.Dispatcher);

                    Assert.AreEqual(1, clickCount, "A primary click should raise Click exactly once.");
                    Assert.AreEqual(true, checkedInsideHandler,
                        "The Click handler must observe the already-toggled state (WinUI toggles before raising Click).");
                    Assert.IsTrue(button.IsChecked);
                });
        }

        [TestMethod]
        public void ToggleSplitButton_PrimaryClick_SecondClickTogglesOff()
        {
            RunToggleSplitButtonTest(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                },
                (_, button) =>
                {
                    int clickCount = 0;
                    button.Click += (_, _) => clickCount++;

                    System.Windows.Controls.Button primary = GetPrimaryButtonPart(button);
                    primary.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    primary.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    DrainDispatcher(button.Dispatcher);

                    Assert.AreEqual(2, clickCount);
                    Assert.IsFalse(button.IsChecked, "A second primary click should toggle back off.");
                });
        }

        [TestMethod]
        public void ToggleSplitButton_PrimaryClick_StillExecutesCommand()
        {
            RunToggleSplitButtonTest(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                },
                (_, button) =>
                {
                    int executeCount = 0;
                    button.Command = new ToggleSplitButtonRelayCommand(_ => executeCount++);

                    GetPrimaryButtonPart(button).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    DrainDispatcher(button.Dispatcher);

                    Assert.AreEqual(1, executeCount, "The inherited command plumbing must still execute.");
                    Assert.IsTrue(button.IsChecked, "The primary click must also toggle the checked state.");
                });
        }

        [TestMethod]
        public void ToggleSplitButton_IsCheckedChanged_RaisesWithNewValue()
        {
            RunToggleSplitButtonTest(
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
                    Assert.AreEqual(1, raiseCount);
                    Assert.AreSame(button, lastSender);
                    Assert.AreEqual(true, lastValue);

                    button.IsChecked = false;
                    Assert.AreEqual(2, raiseCount);
                    Assert.AreEqual(false, lastValue);
                });
        }

        [TestMethod]
        public void ToggleSplitButton_SecondaryToggle_OpensFlyoutWithoutToggling()
        {
            RunToggleSplitButtonTest(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                    Flyout = "Flyout content",
                },
                (_, button) =>
                {
                    ToggleButton secondary = GetSecondaryButtonPart(button);
                    Popup? popup = button.Template?.FindName("PART_Popup", button) as Popup;
                    Assert.IsNotNull(popup);

                    secondary.IsChecked = true;
                    DrainDispatcher(button.Dispatcher);

                    Assert.IsTrue(popup.IsOpen, "Checking the secondary half should open the flyout popup.");
                    Assert.IsTrue(button.IsFlyoutOpen);
                    Assert.IsFalse(button.IsChecked, "Opening the flyout must not toggle the primary checked state.");

                    secondary.IsChecked = false;
                    DrainDispatcher(button.Dispatcher);

                    Assert.IsFalse(popup.IsOpen, "Unchecking the secondary half should close the flyout popup.");
                    Assert.IsFalse(button.IsFlyoutOpen);
                });
        }

        [TestMethod]
        public void ToggleSplitButton_Checked_AccentFillsBothHalvesAndBackdrops()
        {
            RunToggleSplitButtonTest(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                    IsChecked = true,
                    IsHitTestVisible = false,
                },
                (application, button) =>
                {
                    Border? primaryFill = FindVisualChildByName<Border>(button, "PrimaryFill");
                    Border? secondaryFill = FindVisualChildByName<Border>(button, "SecondaryFill");
                    Border? primaryBackdrop = FindVisualChildByName<Border>(button, "PrimaryAccentFillBackdrop");
                    Border? secondaryBackdrop = FindVisualChildByName<Border>(button, "SecondaryAccentFillBackdrop");

                    Assert.IsNotNull(primaryFill);
                    Assert.IsNotNull(secondaryFill);
                    Assert.IsNotNull(primaryBackdrop);
                    Assert.IsNotNull(secondaryBackdrop);

                    Color accentDefault = GetResolvedBrushColor(application, "AccentFillColorDefaultBrush");
                    Assert.AreEqual(accentDefault, GetSolidColor(primaryFill.Background, "Checked PrimaryFill"));
                    Assert.AreEqual(accentDefault, GetSolidColor(secondaryFill.Background, "Checked SecondaryFill"));
                    Assert.AreEqual(1.0, primaryBackdrop.Opacity, "Checked state should reveal the primary accent backdrop.");
                    Assert.AreEqual(1.0, secondaryBackdrop.Opacity, "Checked state should reveal the secondary accent backdrop.");
                    Assert.AreEqual(GetResolvedBrushColor(application, "TextOnAccentFillColorPrimaryBrush"), GetSolidColor(button.Foreground, "Checked foreground"));
                });
        }

        [TestMethod]
        public void ToggleSplitButton_Checked_DividerUsesCheckedDividerBrush()
        {
            RunToggleSplitButtonTest(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                    IsHitTestVisible = false,
                },
                (application, button) =>
                {
                    Rectangle? divider = FindVisualChildByName<Rectangle>(button, "Divider");
                    Assert.IsNotNull(divider);

                    Color uncheckedDivider = GetSolidColor(divider.Fill, "Unchecked divider");

                    button.IsChecked = true;
                    DrainDispatcher(button.Dispatcher);
                    button.UpdateLayout();

                    Color checkedDivider = GetSolidColor(divider.Fill, "Checked divider");

                    Assert.AreEqual(GetResolvedBrushColor(application, "ControlStrokeColorOnAccentTertiaryBrush"), checkedDivider,
                        "The checked divider must use the WinUI SplitButtonBorderBrushCheckedDivider mapping.");
                    Assert.AreNotEqual(uncheckedDivider, checkedDivider,
                        "The checked divider color must differ from the unchecked ControlStrokeColorDefaultBrush divider.");
                });
        }

        [TestMethod]
        public void ToggleSplitButton_CheckedFlyoutOpen_TintsBothHalvesPressedFill()
        {
            RunToggleSplitButtonTest(
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
                    DrainDispatcher(button.Dispatcher);
                    button.UpdateLayout();

                    Border? primaryFill = FindVisualChildByName<Border>(button, "PrimaryFill");
                    Border? secondaryFill = FindVisualChildByName<Border>(button, "SecondaryFill");

                    Assert.IsNotNull(primaryFill);
                    Assert.IsNotNull(secondaryFill);

                    Color accentTertiary = GetResolvedBrushColor(application, "AccentFillColorTertiaryBrush");
                    Assert.AreEqual(accentTertiary, GetSolidColor(primaryFill.Background, "CheckedFlyoutOpen PrimaryFill"));
                    Assert.AreEqual(accentTertiary, GetSolidColor(secondaryFill.Background, "CheckedFlyoutOpen SecondaryFill"));

                    secondary.IsChecked = false;
                    DrainDispatcher(button.Dispatcher);
                });
        }

        [TestMethod]
        public void ToggleSplitButton_CheckedDisabled_UsesAccentDisabledPalette()
        {
            RunToggleSplitButtonTest(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                    IsChecked = true,
                    IsEnabled = false,
                },
                (application, button) =>
                {
                    Border? primaryFill = FindVisualChildByName<Border>(button, "PrimaryFill");
                    Border? secondaryFill = FindVisualChildByName<Border>(button, "SecondaryFill");

                    Assert.IsNotNull(primaryFill);
                    Assert.IsNotNull(secondaryFill);

                    Color accentDisabled = GetResolvedBrushColor(application, "AccentFillColorDisabledBrush");
                    Assert.AreEqual(accentDisabled, GetSolidColor(primaryFill.Background, "Checked disabled PrimaryFill"));
                    Assert.AreEqual(accentDisabled, GetSolidColor(secondaryFill.Background, "Checked disabled SecondaryFill"));
                    Assert.AreEqual(GetResolvedBrushColor(application, "TextFillColorDisabledBrush"), GetSolidColor(button.Foreground, "Checked disabled foreground"));
                });
        }

        [TestMethod]
        public void ToggleSplitButton_AutomationPeer_ExposesToggleAndExpandCollapse()
        {
            RunToggleSplitButtonTest(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                    IsHitTestVisible = false,
                },
                (_, button) =>
                {
                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(button);

                    Assert.AreEqual("ToggleSplitButton", peer.GetClassName());
                    Assert.AreEqual(AutomationControlType.SplitButton, peer.GetAutomationControlType());
                    Assert.IsNotNull(peer.GetPattern(PatternInterface.Toggle), "The peer must expose the Toggle pattern.");
                    Assert.IsNotNull(peer.GetPattern(PatternInterface.ExpandCollapse), "The peer must expose the ExpandCollapse pattern.");
                    Assert.IsNull(peer.GetPattern(PatternInterface.Invoke),
                        "The peer must not expose Invoke (WinUI parity; Invoke would raise Click without toggling).");
                });
        }

        [TestMethod]
        public void ToggleSplitButton_TogglePattern_TogglesStateAndRaisesEvent()
        {
            RunToggleSplitButtonTest(
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
                    IToggleProvider? toggleProvider = peer.GetPattern(PatternInterface.Toggle) as IToggleProvider;
                    Assert.IsNotNull(toggleProvider);

                    toggleProvider.Toggle();
                    Assert.IsTrue(button.IsChecked);
                    Assert.AreEqual(ToggleState.On, toggleProvider.ToggleState);
                    Assert.AreEqual(1, raiseCount);

                    toggleProvider.Toggle();
                    Assert.IsFalse(button.IsChecked);
                    Assert.AreEqual(ToggleState.Off, toggleProvider.ToggleState);
                    Assert.AreEqual(2, raiseCount);
                });
        }

        [TestMethod]
        public void ToggleSplitButton_ExpandCollapsePattern_OpensAndClosesFlyout()
        {
            RunToggleSplitButtonTest(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                    Flyout = "Flyout content",
                },
                (_, button) =>
                {
                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(button);
                    IExpandCollapseProvider? expandProvider = peer.GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider;
                    Assert.IsNotNull(expandProvider);
                    Assert.AreEqual(ExpandCollapseState.Collapsed, expandProvider.ExpandCollapseState);

                    expandProvider.Expand();
                    DrainDispatcher(button.Dispatcher);

                    Assert.IsTrue(button.IsFlyoutOpen, "Expand must open the flyout.");
                    Assert.AreEqual(ExpandCollapseState.Expanded, expandProvider.ExpandCollapseState);

                    expandProvider.Collapse();
                    DrainDispatcher(button.Dispatcher);

                    Assert.IsFalse(button.IsFlyoutOpen, "Collapse must close the flyout.");
                    Assert.AreEqual(ExpandCollapseState.Collapsed, expandProvider.ExpandCollapseState);
                });
        }

        [TestMethod]
        public void ToggleSplitButton_FocusVisuals_UseKeyboardOnlyFocusVisualStyle()
        {
            // Mirrors the SplitButton contract: focus rings come from the
            // DefaultControlFocusVisualStyle adorner on each half, which WPF shows only
            // for keyboard navigation (Tab), never on mouse click - matching DropDownButton.
            RunToggleSplitButtonTest(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                    IsHitTestVisible = false,
                },
                (application, button) =>
                {
                    System.Windows.Controls.Button primary = GetPrimaryButtonPart(button);
                    ToggleButton secondary = GetSecondaryButtonPart(button);
                    Style? focusVisualStyle = application?.TryFindResource("DefaultControlFocusVisualStyle") as Style;

                    Assert.IsNotNull(focusVisualStyle, "DefaultControlFocusVisualStyle should resolve from the computed dictionary.");
                    Assert.AreSame(focusVisualStyle, primary.FocusVisualStyle,
                        "The primary half must use the FocusVisualStyle adorner so the focus ring shows only for keyboard navigation, never on click.");
                    Assert.AreSame(focusVisualStyle, secondary.FocusVisualStyle,
                        "The secondary half must use the FocusVisualStyle adorner so the focus ring shows only for keyboard navigation, never on click.");
                    Assert.IsNull(FindVisualChildByName<Border>(button, "PrimaryFocusOuter"),
                        "The always-on in-template primary focus borders must be gone; they rendered on mouse click.");
                    Assert.IsNull(FindVisualChildByName<Border>(button, "SecondaryFocusOuter"),
                        "The always-on in-template secondary focus borders must be gone; they rendered on mouse click.");
                });
        }

        [TestMethod]
        public void ToggleSplitButton_ThemeCycle_CheckedBrushesReResolve()
        {
            RunToggleSplitButtonTest(
                () => new Controls.ToggleSplitButton
                {
                    Content = "Toggle",
                    IsChecked = true,
                    IsHitTestVisible = false,
                },
                (application, button) =>
                {
                    ThemeTestHelpers.ApplyStandardThemeCycle();
                    DrainDispatcher(button.Dispatcher);
                    button.UpdateLayout();

                    Border? primaryFill = FindVisualChildByName<Border>(button, "PrimaryFill");
                    Rectangle? divider = FindVisualChildByName<Rectangle>(button, "Divider");

                    Assert.IsNotNull(primaryFill);
                    Assert.IsNotNull(divider);
                    Assert.AreEqual(GetResolvedBrushColor(application, "AccentFillColorDefaultBrush"), GetSolidColor(primaryFill.Background, "Checked PrimaryFill after theme cycle"));
                    Assert.AreEqual(GetResolvedBrushColor(application, "ControlStrokeColorOnAccentTertiaryBrush"), GetSolidColor(divider.Fill, "Checked divider after theme cycle"));
                    ThemeTestHelpers.AssertKeyThemeBrushesResolve(application);
                });
        }
    }
}
