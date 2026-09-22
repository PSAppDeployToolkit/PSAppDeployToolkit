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
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace Fluence.Wpf.Demo
{
    /// <summary>
    /// Copies text to the Windows clipboard without letting a transient failure reach the user as
    /// an unhandled exception.
    /// </summary>
    /// <remarks>
    /// The clipboard is a single system wide resource that one process owns at a time. When another
    /// application is holding it open, <c language="csharp">OpenClipboard</c> fails and
    /// <see cref="Clipboard.SetText(string)"/> throws
    /// <see cref="ExternalException"/> with <c language="text">CLIPBRD_E_CANT_OPEN</c> (0x800401D0);
    /// clipboard managers, remote desktop sessions and synchronisation tools all cause this
    /// routinely. The owner usually releases within a few milliseconds, so the accepted remedy is a
    /// short bounded retry rather than a single attempt. A copy that still fails after that reports
    /// <see langword="false"/> through the completion callback instead of throwing, because a
    /// gallery copy button is not worth terminating the application over. The retry runs on a
    /// dispatcher timer, so the outcome is not known by the time <see cref="SetText"/> returns and
    /// a caller that shows a success cue has to wait for the callback rather than assume success.
    /// </remarks>
    internal static class DemoClipboard
    {
        /// <summary>
        /// How many times to attempt the copy before giving up.
        /// </summary>
        private const int MaxAttempts = 5;

        /// <summary>
        /// How long to wait between attempts. The retry runs on a dispatcher timer rather than a
        /// blocking sleep, so the window keeps painting and responding while it waits.
        /// </summary>
        private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(40);

        /// <summary>
        /// Places <paramref name="text"/> on the clipboard, retrying a few times if another process
        /// currently owns it. Never throws.
        /// </summary>
        /// <param name="text">The text to copy. Null, empty, and whitespace are ignored.</param>
        /// <param name="onCompleted">
        /// Raised on the UI thread once the copy has succeeded or the retries have run out, with
        /// <see langword="true"/> only when the text actually reached the clipboard. Nothing is
        /// raised for text that was ignored.
        /// </param>
        internal static void SetText(string? text, Action<bool>? onCompleted = null)
        {
            // The null test is explicit because net472 does not carry the NotNullWhen annotation on
            // string.IsNullOrWhiteSpace, so the checked-null flow does not reach Attempt without it.
            if (text is null || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            Attempt(text, MaxAttempts, onCompleted);
        }

        /// <summary>
        /// Makes one copy attempt and schedules the next one if it failed and attempts remain.
        /// </summary>
        /// <param name="text">The text to copy.</param>
        /// <param name="attemptsRemaining">How many attempts are left, including this one.</param>
        /// <param name="onCompleted">Raised once the outcome is settled. See <see cref="SetText"/>.</param>
        private static void Attempt(string text, int attemptsRemaining, Action<bool>? onCompleted)
        {
            bool copied;

            try
            {
                // SetDataObject with copy:true is what Clipboard.SetText calls internally, and the
                // flush it performs is the step that throws while the clipboard is held elsewhere.
                Clipboard.SetDataObject(text, copy: true);
                copied = true;
            }
            catch (ExternalException)
            {
                copied = false;
            }

            if (copied)
            {
                onCompleted?.Invoke(true);
                return;
            }

            if (attemptsRemaining > 1)
            {
                ScheduleRetry(text, attemptsRemaining - 1, onCompleted);
                return;
            }

            onCompleted?.Invoke(false);
        }

        /// <summary>
        /// Queues another attempt after <see cref="RetryDelay"/> without blocking the UI thread.
        /// </summary>
        /// <param name="text">The text to copy.</param>
        /// <param name="attemptsRemaining">How many attempts are left after this one is queued.</param>
        /// <param name="onCompleted">Raised once the outcome is settled. See <see cref="SetText"/>.</param>
        private static void ScheduleRetry(string text, int attemptsRemaining, Action<bool>? onCompleted)
        {
            Dispatcher? dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is null)
            {
                // With no dispatcher there is nowhere to run the retry, so the copy is over.
                onCompleted?.Invoke(false);
                return;
            }

            DispatcherTimer timer = new(DispatcherPriority.Background, dispatcher)
            {
                Interval = RetryDelay,
            };

            timer.Tick += (_, _) =>
            {
                timer.Stop();
                Attempt(text, attemptsRemaining, onCompleted);
            };

            timer.Start();
        }
    }
}
