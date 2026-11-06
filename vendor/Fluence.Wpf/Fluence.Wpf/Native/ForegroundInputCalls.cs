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
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Fluence.Wpf.Native
{
    /// <summary>
    /// The user32 calls behind <see cref="NativeMethods.TryTakeForeground(IntPtr)"/>, grouped so
    /// tests can replace them with a recording fake. Every member forwards to the real function.
    /// </summary>
    internal class ForegroundInputCalls
    {
        /// <summary>
        /// Gets the instance that calls the real user32 functions.
        /// </summary>
        public static ForegroundInputCalls Default { get; } = new();

        /// <summary>
        /// Gets the native identifier of the calling thread.
        /// </summary>
        public virtual uint CurrentThreadId => PInvoke.GetCurrentThreadId();

        /// <summary>
        /// Returns the window that holds the foreground, or <see cref="IntPtr.Zero"/> when none does.
        /// </summary>
        /// <returns>The foreground window handle.</returns>
        public virtual IntPtr GetForegroundWindow()
        {
            return PInvoke.GetForegroundWindow();
        }

        /// <summary>
        /// Returns the native identifier of the thread that created <paramref name="hwnd"/>.
        /// </summary>
        /// <param name="hwnd">The window to query.</param>
        /// <returns>The owning thread identifier, or 0 when the handle is invalid.</returns>
        public virtual uint GetWindowThreadId(IntPtr hwnd)
        {
            return PInvoke.GetWindowThreadProcessId((HWND)hwnd);
        }

        /// <summary>
        /// Reports whether Windows considers <paramref name="hwnd"/> not responding.
        /// </summary>
        /// <param name="hwnd">The window to query.</param>
        /// <returns><see langword="true"/> when the window is hung.</returns>
        public virtual bool IsHungAppWindow(IntPtr hwnd)
        {
            return PInvoke.IsHungAppWindow((HWND)hwnd);
        }

        /// <summary>
        /// Creates the calling thread's message queue, which <c language="csharp">AttachThreadInput</c>
        /// requires on both threads.
        /// </summary>
        public virtual void EnsureMessageQueue()
        {
            _ = PInvoke.PeekMessage(out MSG _, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_NOREMOVE);
        }

        /// <summary>
        /// Attaches or detaches the input queues of two threads.
        /// </summary>
        /// <param name="threadId">The thread to attach.</param>
        /// <param name="targetThreadId">The thread to attach to.</param>
        /// <param name="attach"><see langword="true"/> to attach; <see langword="false"/> to detach.</param>
        /// <returns><see langword="true"/> when the call succeeded.</returns>
        public virtual bool AttachThreadInput(uint threadId, uint targetThreadId, bool attach)
        {
            return PInvoke.AttachThreadInput(threadId, targetThreadId, attach);
        }

        /// <summary>
        /// Moves <paramref name="hwnd"/> to the top of the Z order.
        /// </summary>
        /// <param name="hwnd">The window to raise.</param>
        /// <returns><see langword="true"/> when the call succeeded.</returns>
        public virtual bool BringWindowToTop(IntPtr hwnd)
        {
            return PInvoke.BringWindowToTop((HWND)hwnd);
        }

        /// <summary>
        /// Asks Windows to make <paramref name="hwnd"/> the foreground window.
        /// </summary>
        /// <param name="hwnd">The window to activate.</param>
        /// <returns><see langword="true"/> when Windows accepted the request.</returns>
        public virtual bool SetForegroundWindow(IntPtr hwnd)
        {
            return PInvoke.SetForegroundWindow((HWND)hwnd);
        }
    }
}
