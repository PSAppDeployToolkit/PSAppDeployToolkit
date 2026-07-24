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
using System.Windows.Controls;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [TestMethod]
        public void GlyphButtons_InPickersAndSpinners_HaveAutomationNames()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Controls.NumberBox numberBox = new()
                {
                    SpinButtonPlacementMode = Fluence.Wpf.SpinButtonPlacementMode.Inline,
                    Width = 160,
                };
                Window window = new() { Content = numberBox, Width = 240, Height = 80 };

                try
                {
                    window.Show();
                    _ = numberBox.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);

                    foreach ((string part, string expectedName) in new[]
                    {
                        ("PART_UpButton", "Increase"),
                        ("PART_DownButton", "Decrease"),
                    })
                    {
                        FrameworkElement? btn = FindVisualChildByName<FrameworkElement>(numberBox, part);
                        Assert.IsNotNull(btn, $"{part} should exist in the NumberBox template.");
                        string actualName = AutomationProperties.GetName(btn);
                        Assert.IsTrue(
                            string.Equals(expectedName, actualName, System.StringComparison.Ordinal),
                            $"{part} must expose accessible name for Narrator. Expected: '{expectedName}', actual: '{actualName}'.");
                    }
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void FluenceWindow_CaptionButtons_HaveAutomationNames()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Controls.FluenceWindow window = new();

                try
                {
                    window.Show();
                    _ = window.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);

                    foreach ((string part, string expectedName) in new[]
                    {
                        ("PART_MinimizeButton", "Minimize"),
                        ("PART_CloseButton", "Close"),
                    })
                    {
                        FrameworkElement? button = FindVisualChildByName<FrameworkElement>(window, part);
                        Assert.IsNotNull(button, $"{part} should exist in the FluenceWindow template.");
                        string actualName = AutomationProperties.GetName(button);
                        Assert.IsTrue(
                            string.Equals(expectedName, actualName, System.StringComparison.Ordinal),
                            $"{part} must expose an accessible name for Narrator. Expected: '{expectedName}', actual: '{actualName}'.");
                    }
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void AutoSuggestBox_QueryButton_HasAutomationName()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                // QueryIcon must be non-null so the template trigger does not clear the icon
                // slot; the button is only wired into the visual tree while QueryIcon is set.
                Controls.AutoSuggestBox autoSuggestBox = new()
                {
                    Width = 200,
                    QueryIcon = new Controls.FontIcon { Glyph = "" },
                };
                Window window = new() { Content = autoSuggestBox, Width = 300, Height = 80 };

                try
                {
                    window.Show();
                    _ = autoSuggestBox.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);

                    ControlTemplate? template = autoSuggestBox.Template;
                    Assert.IsNotNull(template, "AutoSuggestBox must receive its themed template.");
                    FrameworkElement? queryButton = template.FindName("PART_QueryButton", autoSuggestBox) as FrameworkElement;
                    Assert.IsNotNull(queryButton, "PART_QueryButton should exist in the AutoSuggestBox template.");
                    string actualName = AutomationProperties.GetName(queryButton);
                    Assert.IsTrue(
                        string.Equals("Search", actualName, System.StringComparison.Ordinal),
                        $"PART_QueryButton must expose accessible name 'Search' for Narrator. Actual: '{actualName}'.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void DatePicker_AcceptCancelButtons_HaveAutomationNames()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Controls.DatePicker picker = new() { Width = 220 };
                Window window = new() { Content = picker, Width = 300, Height = 120 };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "DatePicker must receive its themed template.");

                    foreach ((string part, string expectedName) in new[]
                    {
                        ("PART_AcceptButton", "Accept"),
                        ("PART_CancelButton", "Cancel"),
                    })
                    {
                        FrameworkElement? btn = template.FindName(part, picker) as FrameworkElement;
                        Assert.IsNotNull(btn, $"{part} should exist in the DatePicker template.");
                        string actualName = AutomationProperties.GetName(btn);
                        Assert.IsTrue(
                            string.Equals(expectedName, actualName, System.StringComparison.Ordinal),
                            $"{part} must expose accessible name '{expectedName}' for Narrator. Actual: '{actualName}'.");
                    }
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void TimePicker_AcceptCancelButtons_HaveAutomationNames()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Controls.TimePicker picker = new() { Width = 220 };
                Window window = new() { Content = picker, Width = 300, Height = 120 };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "TimePicker must receive its themed template.");

                    foreach ((string part, string expectedName) in new[]
                    {
                        ("PART_AcceptButton", "Accept"),
                        ("PART_CancelButton", "Cancel"),
                    })
                    {
                        FrameworkElement? btn = template.FindName(part, picker) as FrameworkElement;
                        Assert.IsNotNull(btn, $"{part} should exist in the TimePicker template.");
                        string actualName = AutomationProperties.GetName(btn);
                        Assert.IsTrue(
                            string.Equals(expectedName, actualName, System.StringComparison.Ordinal),
                            $"{part} must expose accessible name '{expectedName}' for Narrator. Actual: '{actualName}'.");
                    }
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void InfoBarAndPipsPager_GlyphButtons_HaveAutomationNames()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                try
                {
                    // --- InfoBar PART_CloseButton ---
                    Controls.InfoBar infoBar = new() { Message = "Test", Width = 400 };
                    Window infoBarWindow = new() { Content = infoBar, Width = 500, Height = 80 };

                    try
                    {
                        infoBarWindow.Show();
                        _ = infoBar.ApplyTemplate();
                        DrainDispatcher(infoBarWindow.Dispatcher);

                        ControlTemplate? infoBarTemplate = infoBar.Template;
                        Assert.IsNotNull(infoBarTemplate, "InfoBar must receive its themed template.");
                        FrameworkElement? closeButton = infoBarTemplate.FindName("PART_CloseButton", infoBar) as FrameworkElement;
                        Assert.IsNotNull(closeButton, "PART_CloseButton should exist in the InfoBar template.");
                        string closeActualName = AutomationProperties.GetName(closeButton);
                        Assert.IsTrue(
                            string.Equals("Close", closeActualName, System.StringComparison.Ordinal),
                            $"InfoBar PART_CloseButton must expose accessible name 'Close' for Narrator. Actual: '{closeActualName}'.");
                    }
                    finally
                    {
                        infoBarWindow.Close();
                    }

                    // --- PipsPager PART_PreviousButton and PART_NextButton ---
                    Controls.PipsPager pipsPager = new()
                    {
                        NumberOfPages = 5,
                        PreviousButtonVisibility = Fluence.Wpf.PipsPagerButtonVisibility.Visible,
                        NextButtonVisibility = Fluence.Wpf.PipsPagerButtonVisibility.Visible,
                        Width = 200,
                    };
                    Window pipsWindow = new() { Content = pipsPager, Width = 300, Height = 80 };

                    try
                    {
                        pipsWindow.Show();
                        _ = pipsPager.ApplyTemplate();
                        DrainDispatcher(pipsWindow.Dispatcher);

                        ControlTemplate? pipsTemplate = pipsPager.Template;
                        Assert.IsNotNull(pipsTemplate, "PipsPager must receive its themed template.");

                        foreach ((string part, string expectedName) in new[]
                        {
                            ("PART_PreviousButton", "Previous page"),
                            ("PART_NextButton", "Next page"),
                        })
                        {
                            FrameworkElement? btn = pipsTemplate.FindName(part, pipsPager) as FrameworkElement;
                            Assert.IsNotNull(btn, $"{part} should exist in the PipsPager template.");
                            string actualName = AutomationProperties.GetName(btn);
                            Assert.IsTrue(
                                string.Equals(expectedName, actualName, System.StringComparison.Ordinal),
                                $"{part} must expose accessible name '{expectedName}' for Narrator. Actual: '{actualName}'.");
                        }
                    }
                    finally
                    {
                        pipsWindow.Close();
                    }
                }
                finally
                {
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void TabViewItem_CloseButton_HasAutomationName()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                // TabViewItem is a ContentControl subclass; it can be templated standalone
                // without a parent TabView. IsClosable defaults to true so PART_CloseButton
                // is rendered (not collapsed by the IsClosable=False trigger).
                Controls.TabViewItem item = new()
                {
                    Header = "Tab 1",
                    IsClosable = true,
                    Width = 160,
                    Height = 40,
                };
                Window window = new() { Content = item, Width = 240, Height = 80 };

                try
                {
                    window.Show();
                    _ = item.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);

                    ControlTemplate? template = item.Template;
                    Assert.IsNotNull(template, "TabViewItem must receive its themed template.");
                    FrameworkElement? closeButton = template.FindName("PART_CloseButton", item) as FrameworkElement;
                    Assert.IsNotNull(closeButton, "PART_CloseButton should exist in the TabViewItem template.");
                    string actualName = AutomationProperties.GetName(closeButton);
                    Assert.IsTrue(
                        string.Equals("Close tab", actualName, System.StringComparison.Ordinal),
                        $"PART_CloseButton must expose accessible name 'Close tab' for Narrator. Actual: '{actualName}'.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void NavigationView_PaneToggleAndBackButtons_HaveAutomationNames()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                try
                {
                    // Default pane mode (Left) instantiates the template block that hosts both buttons.
                    Controls.NavigationView nav = new() { Width = 320, Height = 240 };
                    Window navWindow = new() { Content = nav, Width = 400, Height = 300 };

                    try
                    {
                        navWindow.Show();
                        _ = nav.ApplyTemplate();
                        DrainDispatcher(navWindow.Dispatcher);

                        ControlTemplate? navTemplate = nav.Template;
                        Assert.IsNotNull(navTemplate, "NavigationView must receive its themed template.");

                        foreach ((string part, string expectedName) in new[]
                        {
                            ("PART_BackButton", "Back"),
                            ("PART_PaneToggleButton", "Navigation"),
                        })
                        {
                            FrameworkElement? btn = navTemplate.FindName(part, nav) as FrameworkElement;
                            Assert.IsNotNull(btn, $"{part} should exist in the NavigationView template.");
                            string actualName = AutomationProperties.GetName(btn);
                            Assert.IsTrue(
                                string.Equals(expectedName, actualName, System.StringComparison.Ordinal),
                                $"{part} must expose accessible name '{expectedName}' for Narrator. Actual: '{actualName}'.");
                        }
                    }
                    finally
                    {
                        navWindow.Close();
                    }
                }
                finally
                {
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void NavigationView_TopPane_BackButton_HasAutomationName()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                try
                {
                    Controls.NavigationView nav = new()
                    {
                        PaneDisplayMode = Fluence.Wpf.NavigationViewPaneDisplayMode.Top,
                        Width = 640,
                        Height = 240,
                    };
                    Window navWindow = new() { Content = nav, Width = 700, Height = 300 };

                    try
                    {
                        navWindow.Show();
                        _ = nav.ApplyTemplate();
                        DrainDispatcher(navWindow.Dispatcher);

                        ControlTemplate? navTemplate = nav.Template;
                        Assert.IsNotNull(navTemplate, "NavigationView must receive its themed template in Top mode.");

                        FrameworkElement? backButton = navTemplate.FindName("PART_BackButton", nav) as FrameworkElement;
                        Assert.IsNotNull(backButton, "PART_BackButton should exist in the NavigationView Top template.");
                        string actualName = AutomationProperties.GetName(backButton);
                        Assert.IsTrue(
                            string.Equals("Back", actualName, System.StringComparison.Ordinal),
                            $"PART_BackButton must expose accessible name 'Back' for Narrator in Top mode. Actual: '{actualName}'.");
                    }
                    finally
                    {
                        navWindow.Close();
                    }
                }
                finally
                {
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void FontIcon_AutomationPeer_IsExcludedFromControlTree()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                try
                {
                    Controls.FontIcon icon = new() { Glyph = "" };
                    System.Windows.Automation.Peers.AutomationPeer peer =
                        System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(icon);

                    Assert.IsNotNull(peer, "FontIcon must create an automation peer.");
                    Assert.IsInstanceOfType(peer, typeof(Automation.FontIconAutomationPeer));
                    Assert.IsFalse(
                        peer.IsControlElement(),
                        "Decorative FontIcon must be excluded from the UI Automation control view (AccessibilityView=Raw equivalent).");
                    Assert.IsFalse(
                        peer.IsContentElement(),
                        "Decorative FontIcon must be excluded from the UI Automation content view.");
                }
                finally
                {
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        // TeachingTip PART_AlternateCloseButton is deferred: the button is only visible when
        // the tip is open inside its host Popup (IsOpen=true moves the tip into a detached Popup
        // subtree). Opening the Popup during a headless test requires a visible active window for
        // fallback placement resolution, and FindName / FindVisualChildByName do not cross into
        // a detached Popup root. Reliable coverage needs a popup-aware traversal helper that is
        // outside the current test harness scope; the XAML-level name is verified in the Task 5
        // report (TeachingTip.xaml line 209, AutomationProperties.Name="Close").

        [TestMethod]
        public void AppBarButton_Label_BecomesAccessibleName()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    Controls.AppBarButton button = new() { Label = "Share" };
                    window.Content = button;
                    window.Width = 120;
                    window.Height = 80;
                    window.Show();
                    _ = button.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(button);
                    Assert.IsTrue(
                        string.Equals("Share", peer.GetName(), StringComparison.Ordinal),
                        "AppBarButton Label must be the accessible name when no explicit AutomationProperties.Name is set.");

                    // A later Label change (e.g. via binding) must keep the auto-derived name in sync.
                    button.Label = "Send";
                    Assert.IsTrue(
                        string.Equals("Send", peer.GetName(), StringComparison.Ordinal),
                        "A later Label change must update the auto-derived accessible name.");

                    button.SetValue(AutomationProperties.NameProperty, "Explicit");
                    Assert.IsTrue(
                        string.Equals("Explicit", peer.GetName(), StringComparison.Ordinal),
                        "Explicit AutomationProperties.Name must win over Label.");

                    // Once an explicit name diverges from the label, later Label changes must not clobber it.
                    button.Label = "Forward";
                    Assert.IsTrue(
                        string.Equals("Explicit", peer.GetName(), StringComparison.Ordinal),
                        "An explicit AutomationProperties.Name must survive later Label changes.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }
    }
}
