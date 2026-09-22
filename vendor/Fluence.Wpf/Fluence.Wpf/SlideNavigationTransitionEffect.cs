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

namespace Fluence.Wpf
{
    /// <summary>
    /// The direction the incoming content slides in from during a
    /// <see cref="Controls.SlideNavigationPresenter"/> content change, mirroring the WinUI 3
    /// <c language="csharp">SlideNavigationTransitionEffect</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// WinUI's enumeration also carries <c language="text">FromBottom</c>, which its own
    /// implementation animates with a different curve family (an exponential ease over the
    /// vertical axis rather than the two horizontal key splines). Only the two horizontal
    /// effects are ported, so the enumeration does not advertise a value the presenter cannot
    /// play. The gap is recorded in docs/winui-parity.md.
    /// </para>
    /// <para>
    /// The numbers are WinUI's own: <c language="text">FromBottom</c> keeps 0 even though it is
    /// not declared here, so the two ported values sit on the numbers WinUI gives them. Code or
    /// XAML ported from WinUI that touches the numeric value maps across unchanged, and
    /// <c language="text">FromBottom</c> can be added later without moving anything.
    /// </para>
    /// <para>
    /// The cost of that numbering is that <c language="csharp">default</c> is not a declared
    /// member: it is the 0 held for <c language="text">FromBottom</c>. The dependency property
    /// defaults to <see cref="FromRight"/> explicitly, and
    /// <see cref="Controls.SlideNavigationPresenter.TransitionEffect"/> rejects 0, or any other
    /// undeclared value, with an <see cref="System.ArgumentException"/> rather than playing a
    /// horizontal effect the caller did not ask for.
    /// </para>
    /// </remarks>
    public enum SlideNavigationTransitionEffect
    {
        /// <summary>
        /// The incoming content enters from the left and the outgoing content leaves to the
        /// right, the effect WinUI uses when moving backward through a set of peers.
        /// </summary>
        FromLeft = 1,

        /// <summary>
        /// The incoming content enters from the right and the outgoing content leaves to the
        /// left, the effect WinUI uses when moving forward through a set of peers.
        /// </summary>
        FromRight = 2,
    }
}
