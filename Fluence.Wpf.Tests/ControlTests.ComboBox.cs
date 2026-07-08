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
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-3 C18 tests: ComboBox FocusedStates VSM port.
    /// Authority: WinUI 3 ComboBox_themeresources.xaml (FocusedStates / EditableFocusedStates groups).
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-3 C18  ComboBox FocusedStates VSM
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void ComboBox_FocusedStates_GroupExistsInTemplate()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ComboBox cb = new();
                _ = cb.Items.Add("One");
                Window w = new() { Content = cb, Width = 300, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // VSM groups are attached to the root Grid of the template
                Grid? root = FindVisualChild<Grid>(cb);
                Assert.IsNotNull(root, "TemplateRoot Grid must be present.");
                IList groups = VisualStateManager.GetVisualStateGroups(root);
                bool hasFocusedStates = groups
                    .Cast<VisualStateGroup>()
                    .Any(static g => string.Equals(g.Name, "FocusedStates", System.StringComparison.Ordinal));
                Assert.IsTrue(hasFocusedStates,
                    "ComboBox template root must have a FocusedStates VSM group per WI-3 C18.");
                w.Close();
            });
        }

        [TestMethod]
        public void ComboBox_EditableFocusedStates_GroupExistsInTemplate()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ComboBox cb = new();
                _ = cb.Items.Add("One");
                Window w = new() { Content = cb, Width = 300, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Grid? root = FindVisualChild<Grid>(cb);
                Assert.IsNotNull(root, "TemplateRoot Grid must be present.");
                IList groups = VisualStateManager.GetVisualStateGroups(root);
                bool hasEditableFocusedStates = groups
                    .Cast<VisualStateGroup>()
                    .Any(static g => string.Equals(g.Name, "EditableFocusedStates", System.StringComparison.Ordinal));
                Assert.IsTrue(hasEditableFocusedStates,
                    "ComboBox template root must have an EditableFocusedStates VSM group per WI-3 C18.");
                w.Close();
            });
        }

        [TestMethod]
        public void ComboBox_FocusedState_DoesNotShowFocusAccentLine()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ComboBox cb = new();
                _ = cb.Items.Add("Alpha");
                Window w = new() { Content = cb, Width = 300, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                bool transitioned = VisualStateManager.GoToState(cb, "Focused", useTransitions: false);
                Assert.IsTrue(transitioned, "GoToState('Focused') must return true.");
                DrainDispatcher(w.Dispatcher);

                Border? accentLine = FindVisualChildByName<Border>(cb, "FocusAccentLine");
                Assert.IsNotNull(accentLine, "FocusAccentLine border must be present in template.");
                Assert.AreEqual(Visibility.Collapsed, accentLine.Visibility,
                    "ComboBox should never show a focus underline; dropdown items own the selection indicator.");
                Assert.AreEqual(0.0, accentLine.Opacity, 0.01,
                    "FocusAccentLine opacity must stay 0 in Focused state.");
                w.Close();
            });
        }

        [TestMethod]
        public void ComboBox_UnfocusedState_FocusAccentLineIsHidden()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ComboBox cb = new();
                _ = cb.Items.Add("Beta");
                Window w = new() { Content = cb, Width = 300, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // Focused first, then Unfocused
                _ = VisualStateManager.GoToState(cb, "Focused", useTransitions: false);
                DrainDispatcher(w.Dispatcher);
                bool transitioned = VisualStateManager.GoToState(cb, "Unfocused", useTransitions: false);
                Assert.IsTrue(transitioned, "GoToState('Unfocused') must return true.");
                DrainDispatcher(w.Dispatcher);

                Border? accentLine = FindVisualChildByName<Border>(cb, "FocusAccentLine");
                Assert.IsNotNull(accentLine, "FocusAccentLine border must be present in template.");
                Assert.AreEqual(0.0, accentLine.Opacity, 0.01,
                    "FocusAccentLine opacity must be 0.0 in Unfocused state.");
                Assert.AreEqual(Visibility.Collapsed, accentLine.Visibility,
                    "ComboBox focus underline should remain collapsed when unfocused.");
                w.Close();
            });
        }

        [TestMethod]
        public void ComboBox_InitialTemplate_DoesNotShowFocusAccentLine()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ComboBox cb = new();
                _ = cb.Items.Add("Alpha");
                cb.SelectedIndex = 0;
                Window w = new() { Content = cb, Width = 300, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Border? accentLine = FindVisualChildByName<Border>(cb, "FocusAccentLine");
                Assert.IsNotNull(accentLine, "FocusAccentLine border must be present in template.");
                Assert.AreEqual(Visibility.Collapsed, accentLine.Visibility,
                    "ComboBox should not expose the focused underline visually.");
                Assert.AreEqual(0.0, accentLine.Opacity, 0.01,
                    "ComboBox should not show the focused underline before it receives focus.");

                w.Close();
            });
        }

        [TestMethod]
        public void ComboBox_ThemeCycle_FocusedStateKeepsAccentLineHidden()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ComboBox cb = new();
                _ = cb.Items.Add("Gamma");
                Window w = new() { Content = cb, Width = 300, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                DrainDispatcher(w.Dispatcher);
                _ = cb.ApplyTemplate();
                w.UpdateLayout();
                DrainDispatcher(w.Dispatcher);

                bool transitioned = VisualStateManager.GoToState(cb, "Focused", useTransitions: false);
                Assert.IsTrue(transitioned, "GoToState('Focused') must return true after theme cycle.");
                DrainDispatcher(w.Dispatcher);

                Border? accentLine = FindVisualChildByName<Border>(cb, "FocusAccentLine");
                Assert.IsNotNull(accentLine,
                    "FocusAccentLine must be present after theme cycle.");
                Assert.AreEqual(Visibility.Collapsed, accentLine.Visibility,
                    "ComboBox focus underline must remain collapsed after theme cycle.");
                Assert.AreEqual(0.0, accentLine.Opacity, 0.01,
                    "FocusAccentLine must stay hidden in Focused state after theme cycle.");
                w.Close();
            });
        }
    }
}
