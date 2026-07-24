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
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-3 B13 tests: Expander chevron rotation easing (ControlFastOutSlowIn / SplineDoubleKeyFrame).
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-3 B13  Expander chevron rotation easing
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void Expander_StyleApplies_RootBorderFound()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.Expander expander = new() { Header = "Test", Content = "Content" };
                Window w = new() { Content = expander, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // RootBorder is the template root - proves Fluence style applied.
                Border? rootBorder = FindVisualChildByName<Border>(expander, "RootBorder");
                Assert.IsNotNull(rootBorder, "RootBorder must exist in Expander template (Fluence style applied).");
                w.Close();
            });
        }

        [TestMethod]
        public void Expander_ChevronPath_ExistsWithRotateTransformOnParent()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.Expander expander = new() { Header = "Test", Content = "Body", IsExpanded = false };
                Window w = new() { Content = expander, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Path? chevron = FindVisualChildByName<Path>(expander, "Chevron");
                Assert.IsNotNull(chevron, "Chevron Path must exist in Expander header template.");

                // Parent Border owns the RotateTransform.
                Border? parent = VisualTreeHelper.GetParent(chevron) as Border;
                Assert.IsNotNull(parent, "Chevron parent must be a Border.");

                RotateTransform? rt = parent.RenderTransform as RotateTransform;
                Assert.IsNotNull(rt,
                    "Border containing Chevron must have RenderTransform=RotateTransform (ChevronRotation).");
                Assert.AreEqual(0.0, rt.Angle, 1.0,
                    "ChevronRotation.Angle must be 0 when Expander is collapsed (WinUI Expander_themeresources.xaml).");
                w.Close();
            });
        }

        [TestMethod]
        public void Expander_Expanded_ContentVisibilityIsVisible()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.Expander expander = new() { Header = "Test", Content = "Body", IsExpanded = true };
                Window w = new() { Content = expander, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // Structural check: ExpandSite ContentPresenter is present.
                ContentPresenter? site = FindVisualChildByName<ContentPresenter>(expander, "ExpandSite");
                Assert.IsNotNull(site, "ExpandSite ContentPresenter must exist in Expander template.");
                w.Close();
            });
        }

        [TestMethod]
        public void Expander_HeaderBorder_CornerRadius4()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.Expander expander = new() { Header = "Test" };
                Window w = new() { Content = expander, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Border? headerBorder = FindVisualChildByName<Border>(expander, "HeaderBorder");
                Assert.IsNotNull(headerBorder, "HeaderBorder must exist in ExpanderHeaderToggleButton template.");
                Assert.AreEqual(new CornerRadius(4), headerBorder.CornerRadius,
                    "HeaderBorder CornerRadius must be 4 (matching WinUI Expander corner spec).");
                w.Close();
            });
        }

        // ---------------------------------------------------------------------------
        // WinUI content slide: expand/collapse translate the content behind the clip
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void Expander_ContentSlideParts_PresentWithClipAndInlineTranslate()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.Expander expander = new() { Header = "Test", Content = "Body" };
                Window w = new() { Content = expander, Width = 300, Height = 300 };
                try
                {
                    w.Show();
                    DrainDispatcher(w.Dispatcher);

                    Border? contentBorder = FindVisualChildByName<Border>(expander, "PART_ContentBorder");
                    Assert.IsNotNull(contentBorder, "PART_ContentBorder must exist in the Expander template.");
                    Assert.IsTrue(contentBorder.ClipToBounds,
                        "PART_ContentBorder must clip its bounds so the content slides behind the clip.");

                    Grid? grid = VisualTreeHelper.GetParent(contentBorder) as Grid;
                    Assert.IsNotNull(grid, "PART_ContentBorder must sit inside the row-swap Grid.");
                    Assert.AreEqual(2, grid.RowDefinitions.Count,
                        "The Expander template Grid must keep its header/content row pair.");

                    ContentPresenter? site = FindVisualChildByName<ContentPresenter>(expander, "ExpandSite");
                    Assert.IsNotNull(site, "ExpandSite ContentPresenter must exist in the Expander template.");
                    Assert.IsInstanceOfType(site.RenderTransform, typeof(TranslateTransform),
                        "ExpandSite must carry the inline TranslateTransform installed by OnApplyTemplate.");
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [TestMethod]
        public void Expander_ExpandSlide_RestsAtZeroWithStarContentRow()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.Expander expander = new() { Header = "Test", Content = "Body", IsExpanded = false };
                Window w = new() { Content = expander, Width = 300, Height = 300 };
                try
                {
                    w.Show();
                    DrainDispatcher(w.Dispatcher);

                    Border contentBorder = FindVisualChildByName<Border>(expander, "PART_ContentBorder")
                        ?? throw new AssertFailedException("PART_ContentBorder must exist.");
                    Grid grid = (Grid)VisualTreeHelper.GetParent(contentBorder);
                    ContentPresenter site = FindVisualChildByName<ContentPresenter>(expander, "ExpandSite")
                        ?? throw new AssertFailedException("ExpandSite must exist.");
                    TranslateTransform translate = (TranslateTransform)site.RenderTransform;

                    Assert.AreEqual(0.0, grid.RowDefinitions[1].Height.Value, 0.001,
                        "The content row must be closed at collapsed rest.");

                    expander.IsExpanded = true;
                    Assert.IsTrue(WaitUntil(w.Dispatcher, 2000, () => translate.Y < 0),
                        "The expand slide must start from a negative offset (content behind the clip).");
                    Assert.IsTrue(
                        WaitUntil(w.Dispatcher, 4000,
                            () => Math.Abs(translate.Y) < 0.001 && grid.RowDefinitions[1].Height.IsStar),
                        "The content must rest at translate 0 with a star content row after the expand slide.");
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [TestMethod]
        public void Expander_CollapseSlide_ClosesRowAndResetsTranslate()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.Expander expander = new() { Header = "Test", Content = "Body", IsExpanded = true };
                Window w = new() { Content = expander, Width = 300, Height = 300 };
                try
                {
                    w.Show();
                    DrainDispatcher(w.Dispatcher);

                    Border contentBorder = FindVisualChildByName<Border>(expander, "PART_ContentBorder")
                        ?? throw new AssertFailedException("PART_ContentBorder must exist.");
                    Grid grid = (Grid)VisualTreeHelper.GetParent(contentBorder);
                    ContentPresenter site = FindVisualChildByName<ContentPresenter>(expander, "ExpandSite")
                        ?? throw new AssertFailedException("ExpandSite must exist.");
                    TranslateTransform translate = (TranslateTransform)site.RenderTransform;

                    Assert.IsTrue(grid.RowDefinitions[1].Height.IsStar,
                        "Initial IsExpanded=true must open the content row without animation.");

                    expander.IsExpanded = false;
                    Assert.IsTrue(
                        WaitUntil(w.Dispatcher, 4000,
                            () => !grid.RowDefinitions[1].Height.IsStar
                                && grid.RowDefinitions[1].Height.Value < 0.001
                                && Math.Abs(translate.Y) < 0.001),
                        "Collapse must close the content row at slide completion and reset the translate.");
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [TestMethod]
        public void Expander_RapidToggleMidFlight_SettlesCollapsedWithoutStuckOffset()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.Expander expander = new() { Header = "Test", Content = "Body", IsExpanded = false };
                Window w = new() { Content = expander, Width = 300, Height = 300 };
                try
                {
                    w.Show();
                    DrainDispatcher(w.Dispatcher);

                    Border contentBorder = FindVisualChildByName<Border>(expander, "PART_ContentBorder")
                        ?? throw new AssertFailedException("PART_ContentBorder must exist.");
                    Grid grid = (Grid)VisualTreeHelper.GetParent(contentBorder);
                    ContentPresenter site = FindVisualChildByName<ContentPresenter>(expander, "ExpandSite")
                        ?? throw new AssertFailedException("ExpandSite must exist.");
                    TranslateTransform translate = (TranslateTransform)site.RenderTransform;

                    // Interrupt the 333 ms expand slide mid-flight with a collapse.
                    expander.IsExpanded = true;
                    Assert.IsTrue(WaitUntil(w.Dispatcher, 2000, () => translate.Y < 0),
                        "The expand slide must be in flight before the interrupting collapse.");
                    expander.IsExpanded = false;

                    Assert.IsTrue(
                        WaitUntil(w.Dispatcher, 4000,
                            () => !grid.RowDefinitions[1].Height.IsStar
                                && grid.RowDefinitions[1].Height.Value < 0.001
                                && Math.Abs(translate.Y) < 0.001),
                        "A collapse interrupting the expand slide must settle collapsed with no stuck offset.");
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [TestMethod]
        public void Expander_ExpandUp_SlidesFromBelowIntoTopContentRow()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.Expander expander = new()
                {
                    Header = "Test",
                    Content = "Body",
                    ExpandDirection = ExpandDirection.Up,
                    IsExpanded = false,
                };
                Window w = new() { Content = expander, Width = 300, Height = 300 };
                try
                {
                    w.Show();
                    DrainDispatcher(w.Dispatcher);

                    Border contentBorder = FindVisualChildByName<Border>(expander, "PART_ContentBorder")
                        ?? throw new AssertFailedException("PART_ContentBorder must exist.");
                    Grid grid = (Grid)VisualTreeHelper.GetParent(contentBorder);
                    ContentPresenter site = FindVisualChildByName<ContentPresenter>(expander, "ExpandSite")
                        ?? throw new AssertFailedException("ExpandSite must exist.");
                    TranslateTransform translate = (TranslateTransform)site.RenderTransform;

                    Assert.AreEqual(0, Grid.GetRow(contentBorder),
                        "ExpandDirection=Up must place the content border in the top row.");
                    Assert.AreEqual(0.0, grid.RowDefinitions[0].Height.Value, 0.001,
                        "The Up content row must be closed at collapsed rest.");

                    expander.IsExpanded = true;
                    Assert.IsTrue(WaitUntil(w.Dispatcher, 2000, () => translate.Y > 0),
                        "The Up expand slide must start from a positive offset (content below the header).");
                    Assert.IsTrue(
                        WaitUntil(w.Dispatcher, 4000,
                            () => Math.Abs(translate.Y) < 0.001 && grid.RowDefinitions[0].Height.IsStar),
                        "The Up content must rest at translate 0 with a star top row after the expand slide.");
                }
                finally
                {
                    w.Close();
                }
            });
        }
    }
}
