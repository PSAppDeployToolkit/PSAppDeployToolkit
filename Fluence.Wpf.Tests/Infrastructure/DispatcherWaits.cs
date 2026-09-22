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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Xunit;

namespace Fluence.Wpf.Tests.Infrastructure
{
    /// <summary>
    /// The one set of dispatcher pumps for the suite. Every test class brings these into scope
    /// with a using static Fluence.Wpf.Tests.Infrastructure.DispatcherWaits directive so the call
    /// sites read exactly as they did when each partial or class carried its own private copy.
    /// </summary>
    internal static class DispatcherWaits
    {
        // Pump the dispatcher for `milliseconds` so any in-flight storyboard
        // (e.g. the LeftCompact pane's 167 ms Width animation) reaches its
        // HoldEnd state before the test samples layout values.
        internal static async Task WaitForAnimationAndDrainAsync(Dispatcher dispatcher, int milliseconds)
        {
            DispatcherFrame frame = new();
            DispatcherTimer timer = new(
                TimeSpan.FromMilliseconds(milliseconds),
                DispatcherPriority.Normal,
                delegate { frame.Continue = false; },
                dispatcher);
            timer.Start();
            Dispatcher.PushFrame(frame);
            timer.Stop();
            await dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.ApplicationIdle, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);
        }

        internal static async Task<bool> WaitUntilAsync(Dispatcher dispatcher, int milliseconds, Func<bool> condition)
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(milliseconds);
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            do
            {
                await dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.ApplicationIdle, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);
                if (condition())
                {
                    return true;
                }

                DispatcherFrame frame = new();
                DispatcherTimer timer = new(
                    TimeSpan.FromMilliseconds(16),
                    DispatcherPriority.Normal,
                    delegate { frame.Continue = false; },
                    dispatcher);
                timer.Start();
                Dispatcher.PushFrame(frame);
                timer.Stop();
            }
            while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested);

            await dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.ApplicationIdle, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);
            return condition();
        }
    }
}
