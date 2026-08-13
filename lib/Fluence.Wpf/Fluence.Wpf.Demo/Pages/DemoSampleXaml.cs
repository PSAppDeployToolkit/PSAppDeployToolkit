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

namespace Fluence.Wpf.Demo.Pages
{
    /// <summary>
    /// Builds the displayed XAML source for gallery samples. Every sample presents a complete
    /// UserControl file, so the canonical root element and xmlns preamble live here once
    /// instead of being repeated in every sample's source string.
    /// </summary>
    internal static class DemoSampleXaml
    {
        /// <summary>
        /// Wraps a sample body in the canonical UserControl root: the given <c>x:Class</c>,
        /// the WPF presentation and XAML namespaces, and the single
        /// <c>xmlns:fluence="http://schemas.fluencewpf.com"</c> declaration the library
        /// documents for consumers.
        /// </summary>
        /// <param name="className">The full <c>x:Class</c> name the sample displays.</param>
        /// <param name="body">The sample body markup, newline-terminated.</param>
        internal static string UserControl(string className, string body)
        {
            return "<UserControl\n" +
                "    x:Class=\"" + className + "\"\n" +
                "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                "    xmlns:fluence=\"http://schemas.fluencewpf.com\">\n" +
                body +
                "</UserControl>\n";
        }
    }
}
