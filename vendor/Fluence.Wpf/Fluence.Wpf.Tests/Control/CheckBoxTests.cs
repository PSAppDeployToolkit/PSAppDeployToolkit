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
using System.Windows.Automation;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Fluent <see cref="CheckBox"/> control: Description surfaces as
    /// AutomationProperties.HelpText.
    /// </summary>
    public sealed class CheckBoxTests : IClassFixture<LightThemeFixture>
    {
        public CheckBoxTests(LightThemeFixture fixture)
        {
            _ = fixture;
        }

        // ---------------------------------------------------------------------------
        // CheckBox Description -> HelpText
        // ---------------------------------------------------------------------------

        [Fact]
        public Task CheckBox_Description_SetsAutomationHelpTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                CheckBox checkBox = new()
                {
                    Content = "Enable feature",
                    Description = "Enables the optional feature for this session.",
                };

                string helpText = AutomationProperties.GetHelpText(checkBox);
                Assert.True(
                    string.Equals("Enables the optional feature for this session.", helpText, StringComparison.Ordinal),
                    $"CheckBox.Description must be surfaced as AutomationProperties.HelpText. Actual: '{helpText}'.");
            });
        }

        [Fact]
        public Task CheckBox_DescriptionChanges_UpdatesAutomationHelpTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                CheckBox checkBox = new()
                {
                    Content = "Enable feature",
                    Description = "First description.",
                };

                checkBox.Description = "Updated description.";
                string helpText = AutomationProperties.GetHelpText(checkBox);
                Assert.True(
                    string.Equals("Updated description.", helpText, StringComparison.Ordinal),
                    $"CheckBox.Description change must update AutomationProperties.HelpText. Actual: '{helpText}'.");
            });
        }

        [Fact]
        public Task CheckBox_NullDescription_ClearsAutomationHelpTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                CheckBox checkBox = new()
                {
                    Content = "Enable feature",
                    Description = "Some description.",
                };
                checkBox.Description = null;

                string helpText = AutomationProperties.GetHelpText(checkBox);
                Assert.True(
                    string.IsNullOrWhiteSpace(helpText),
                    $"Null CheckBox.Description must clear AutomationProperties.HelpText. Actual: '{helpText}'.");
            });
        }

        [Fact]
        public Task Stage3_CheckBox_Content_RoundtripsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                CheckBox cb = new() { Content = "Test" };
                Assert.Equal("Test", cb.Content as string, StringComparer.Ordinal);
            });
        }
    }
}
