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

using System.Windows;
using System.Windows.Media;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Infrastructure
{
    /// <summary>
    /// Template-part transform lookups shared by the <see cref="Controls.ToggleSwitch"/>,
    /// <see cref="Controls.ProgressRing"/> and reduced-motion test classes: each control's
    /// motion-relevant transform must be reached the same way everywhere a test needs to
    /// inspect whether it is animating.
    /// </summary>
    internal static class TemplatePartTransforms
    {
        /// <summary>
        /// Returns the <see cref="TranslateTransform"/> that positions a
        /// <see cref="Controls.ToggleSwitch"/> knob.
        /// </summary>
        /// <param name="toggleSwitch">The toggle switch whose knob transform is read.</param>
        internal static TranslateTransform GetToggleSwitchKnobTranslate(Controls.ToggleSwitch toggleSwitch)
        {
            FrameworkElement knob = Assert.IsType<FrameworkElement>(FindVisualChildByName<FrameworkElement>(toggleSwitch, "SwitchKnob"), exactMatch: false);
            return Assert.IsType<TranslateTransform>(knob.RenderTransform);
        }

        /// <summary>
        /// Returns the <see cref="RotateTransform"/> that drives a
        /// <see cref="Controls.ProgressRing"/> indeterminate arc, or null if the template has
        /// not applied.
        /// </summary>
        /// <param name="ring">The progress ring whose indeterminate rotation is read.</param>
        internal static RotateTransform? GetIndeterminateRotateTransform(Controls.ProgressRing ring)
        {
            return ring.Template?.FindName("PART_IndeterminateRotate", ring) as RotateTransform;
        }
    }
}
