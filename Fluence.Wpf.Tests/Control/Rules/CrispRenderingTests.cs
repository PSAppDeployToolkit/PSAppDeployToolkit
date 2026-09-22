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

using System.Threading.Tasks;
using System.Windows;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Control.Rules
{
    /// <summary>
    /// A themed control's <c language="csharp">UseLayoutRounding</c> setter must keep crisp,
    /// non-blurry edges, and must survive a theme switch.
    /// </summary>
    public sealed class CrispRenderingTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        private static void AssertCrispRenderingSetters(FrameworkElement element)
        {
            Assert.True(element.UseLayoutRounding, "UseLayoutRounding should be true from default style.");
        }

        [Fact]
        public Task ThemedButton_HasCrispLayoutRoundingSettersAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                Button button = new();
                _ = new Window { Content = button };
                _ = button.ApplyTemplate();
                AssertCrispRenderingSetters(button);
            });
        }

        [Fact]
        public Task ThemedTextBox_HasCrispLayoutRoundingSettersAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                TextBox textBox = new();
                _ = new Window { Content = textBox };
                _ = textBox.ApplyTemplate();
                AssertCrispRenderingSetters(textBox);
            });
        }

        [Fact]
        public Task CrispRendering_PreservedAcrossThemeSwitchesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark, ApplicationTheme.HighContrast })
                {
                    ApplicationThemeManager.Apply(theme, WindowBackdropType.None);

                    CheckBox checkBox = new();
                    _ = new Window { Content = checkBox };
                    _ = checkBox.ApplyTemplate();
                    AssertCrispRenderingSetters(checkBox);
                }
            });
        }
    }
}
