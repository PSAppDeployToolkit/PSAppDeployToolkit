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

// Slot-wiring infrastructure for demo sample pages.
//
// WPF raises error MC3093 ("Cannot set Name attribute value on element ... inside a template")
// when you try to give an x:Name to a control declared directly inside a DemoSampleControl
// property element, because the property element is treated like a template scope. To work
// around this, each gallery page declares hidden ContentControl "slots" at the page root
// (outside DemoSampleControl) and calls DemoSamplePageWiring.Apply to move the live controls
// into the right DemoSampleControl zones at startup.

using System;

namespace Fluence.Wpf.Demo.Pages
{
    /// <summary>
    /// Carries the XAML and C# source text for one <c>DemoSampleControl</c> card.
    /// </summary>
    /// <remarks>
    /// The <see cref="Slot"/> number is 1-based and must match the position of its corresponding
    /// <c>DemoSampleControl</c> in document order on the page.
    /// </remarks>
    internal readonly struct DemoSampleSource
    {
        public DemoSampleSource(int slot, string xamlSource, string cSharpSource)
        {
            if (slot <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slot), "Slot must be greater than zero.");
            }

            Slot = slot;
            XamlSource = xamlSource ?? throw new ArgumentNullException(nameof(xamlSource));
            CSharpSource = cSharpSource ?? throw new ArgumentNullException(nameof(cSharpSource));
        }

        /// <summary>
        /// Gets the 1-based index that identifies which <c>DemoSampleControl</c> on the page receives this source.
        /// </summary>
        public int Slot { get; }

        /// <summary>
        /// Gets the XAML source text displayed in the XAML tab of the source expander.
        /// </summary>
        public string XamlSource { get; }

        /// <summary>
        /// Gets the C# source text displayed in the C# tab of the source expander.
        /// </summary>
        public string CSharpSource { get; }
    }
}
