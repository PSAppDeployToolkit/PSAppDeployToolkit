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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Fluence.Wpf.Demo.Pages;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Gallery
{
    /// <summary>
    /// Covers <see cref="DemoCodePresenter"/>, the inline snippet presenter that shares the
    /// <see cref="DemoSampleControl"/> source highlighter.
    /// </summary>
    public sealed class DemoCodePresenterTests : IAsyncLifetime
    {
        private Window? _host;
        private DemoCodePresenter? _presenter;

        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(() =>
            {
                _ = TestApp.EnsureDemoTheme();
                _presenter = new DemoCodePresenter
                {
                    Code = "<TextBlock Text=\"...\" Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\" />",
                    CodeLanguage = DemoSourceLanguage.Xaml,
                };
                _host = DemoTestHost.CreateHostWindow(_presenter);
            }));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(() =>
            {
                if (_host is not null)
                {
                    DemoTestHost.CloseWindow(_host);
                    _host = null;
                }

                _presenter = null;
            }));
        }

        [Fact]
        public Task DemoCodePresenter_RendersColorizedXamlWithoutAPlateAsync()
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                DemoCodePresenter presenter = Presenter();
                RichTextBox viewer = Assert.Single(DemoTestHost.FindVisualChildren<RichTextBox>(presenter));
                Assert.True(viewer.IsReadOnly);
                Assert.Equal(presenter.FontSize, viewer.FontSize);

                // WinUI Gallery SampleCodePresenter (inline): code sits directly on the page, no opaque plate.
                Color transparent = Assert.IsType<SolidColorBrush>(viewer.Background, exactMatch: false).Color;
                Assert.Equal(0, transparent.A);

                Paragraph paragraph = Assert.IsType<Paragraph>(Assert.Single(viewer.Document.Blocks), exactMatch: false);
                List<Run> runs = [.. paragraph.Inlines.OfType<Run>()];
                Assert.True(runs.Count > 3, "The XAML snippet should split into several colorized runs.");
                Assert.Equal(presenter.Code, string.Concat(runs.Select(static run => run.Text)), StringComparer.Ordinal);
                Assert.True(runs.Select(static run => run.Foreground).Distinct().Skip(1).Any(), "Tags, attributes and values should carry different theme brushes.");
            });
        }

        [Fact]
        public Task DemoCodePresenter_CopyButtonFollowsIsCopyButtonVisibleAsync()
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                DemoCodePresenter presenter = Presenter();
                Controls.Button copy = Assert.IsType<Controls.Button>(DemoTestHost.FindByName<Controls.Button>(presenter, "CopyCodeButton"), exactMatch: false);
                FrameworkElement host = Assert.IsType<FrameworkElement>(DemoTestHost.FindByName<FrameworkElement>(presenter, "CopyButtonHost"), exactMatch: false);
                Assert.Equal(Visibility.Visible, host.Visibility);
                Assert.Equal(presenter.Code, copy.Tag as string, StringComparer.Ordinal);

                presenter.IsCopyButtonVisible = false;
                WpfTestSta.DrainDispatcher(presenter.Dispatcher);
                Assert.Equal(Visibility.Collapsed, host.Visibility);
            });
        }

        [Fact]
        public Task DemoCodePresenter_ReRendersWhenCodeOrLanguageChangesAsync()
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                DemoCodePresenter presenter = Presenter();
                presenter.CodeLanguage = DemoSourceLanguage.CSharp;
                presenter.Code = "public void Demo() { }";
                WpfTestSta.DrainDispatcher(presenter.Dispatcher);

                RichTextBox viewer = Assert.Single(DemoTestHost.FindVisualChildren<RichTextBox>(presenter));
                Paragraph paragraph = Assert.IsType<Paragraph>(Assert.Single(viewer.Document.Blocks), exactMatch: false);
                Assert.Equal(presenter.Code, string.Concat(paragraph.Inlines.OfType<Run>().Select(static run => run.Text)), StringComparer.Ordinal);
            });
        }

        private DemoCodePresenter Presenter()
        {
            return _presenter ?? throw new InvalidOperationException("Presenter was not initialized.");
        }
    }
}
