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
using System.Windows.Threading;
using Xunit;

namespace Fluence.Wpf.Tests.Infrastructure
{
    /// <summary>
    /// A second, deliberately distinct animation wait, kept apart from
    /// <see cref="DispatcherWaits.WaitForAnimationAndDrainAsync"/> because the two pump the
    /// dispatcher differently and are not interchangeable. That member starts a
    /// <see cref="DispatcherTimer"/> and pushes a nested frame so the wait ends only once the
    /// dispatcher itself fires the timer tick. This one instead awaits a plain
    /// <see cref="Task.Delay(int, System.Threading.CancellationToken)"/>, which resumes on the
    /// dispatcher's own synchronization context, so the dispatcher keeps pumping (and any
    /// in-flight storyboard keeps animating) for exactly the delay's duration. A caller that
    /// samples a value mid-animation, rather than only after the animation settles, needs this
    /// timing and not the pump-based one.
    /// </summary>
    internal static class DispatcherDelayWaits
    {
        internal static async Task WaitForAnimationAndDrainByDelayAsync(Dispatcher dispatcher, int milliseconds)
        {
            // Awaiting resumes on the dispatcher via its synchronization context, so the
            // dispatcher keeps pumping (animations advance) while the delay elapses.
            await Task.Delay(milliseconds, TestContext.Current.CancellationToken).ConfigureAwait(true);
            await dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.ApplicationIdle, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);
        }
    }
}
