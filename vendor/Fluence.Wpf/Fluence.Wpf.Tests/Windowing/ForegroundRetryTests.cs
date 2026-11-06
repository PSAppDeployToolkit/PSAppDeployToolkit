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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Fluence.Wpf.Native;
using Xunit;

namespace Fluence.Wpf.Tests.Windowing
{
    /// <summary>
    /// Pins the refused-activation retry in <see cref="NativeMethods.TryTakeForeground(IntPtr, ForegroundInputCalls, TimeSpan)"/>
    /// through a recording fake: the call order, that a successful attach is always detached, that a
    /// failed attach is never detached, and that the retry runs off the calling thread and gives up at
    /// the timeout instead of blocking it. The real foreground-lock behavior is a Windows integration
    /// check and is not covered here.
    /// </summary>
    public sealed class ForegroundRetryTests
    {
        private static readonly IntPtr Target = new(0x10);
        private static readonly IntPtr Other = new(0x20);
        private static readonly TimeSpan RetryTimeout = TimeSpan.FromSeconds(5);

        private sealed class FakeCalls : ForegroundInputCalls
        {
            private readonly List<string> _calls = [];

            public IntPtr Foreground { get; set; } = Other;

            public uint ForegroundThread { get; set; } = 7;

            public uint ThreadId { get; set; } = 5;

            public bool AttachResult { get; set; } = true;

            public bool ActivateResult { get; set; } = true;

            public bool Hung { get; set; }

            public ManualResetEventSlim? BlockActivation { get; set; }

            public int ActivatingManagedThreadId { get; private set; } = -1;

            public IReadOnlyList<string> Calls => _calls;

            public override uint CurrentThreadId => ThreadId;

            public override IntPtr GetForegroundWindow()
            {
                _calls.Add("GetForegroundWindow");
                return Foreground;
            }

            public override uint GetWindowThreadId(IntPtr hwnd)
            {
                return ForegroundThread;
            }

            public override bool IsHungAppWindow(IntPtr hwnd)
            {
                return Hung;
            }

            public override void EnsureMessageQueue()
            {
                _calls.Add("EnsureMessageQueue");
            }

            public override bool AttachThreadInput(uint threadId, uint targetThreadId, bool attach)
            {
                _calls.Add((attach ? "Attach:" : "Detach:") + threadId + "->" + targetThreadId);
                return AttachResult;
            }

            public override bool BringWindowToTop(IntPtr hwnd)
            {
                _calls.Add("BringWindowToTop");
                return true;
            }

            public override bool SetForegroundWindow(IntPtr hwnd)
            {
                ActivatingManagedThreadId = Environment.CurrentManagedThreadId;
                _calls.Add("SetForegroundWindow");
                BlockActivation?.Wait(TestContext.Current.CancellationToken);
                if (ActivateResult)
                {
                    Foreground = hwnd;
                }

                return ActivateResult;
            }
        }

        [Fact]
        public void ZeroHandle_ReturnsFalseWithoutCalls()
        {
            FakeCalls calls = new();

            Assert.False(NativeMethods.TryTakeForeground(IntPtr.Zero, calls, RetryTimeout));
            Assert.Empty(calls.Calls);
        }

        [Fact]
        public void AlreadyForeground_ReturnsTrueWithoutAttaching()
        {
            FakeCalls calls = new() { Foreground = Target };

            Assert.True(NativeMethods.TryTakeForeground(Target, calls, RetryTimeout));
            string[] expected = ["GetForegroundWindow"];
            Assert.Equal(expected, calls.Calls);
        }

        [Fact]
        public void AnotherThreadHoldsForeground_AttachesActivatesThenDetaches()
        {
            FakeCalls calls = new();

            Assert.True(NativeMethods.TryTakeForeground(Target, calls, RetryTimeout));
            string[] expected = ["GetForegroundWindow", "GetForegroundWindow", "EnsureMessageQueue", "Attach:5->7", "BringWindowToTop", "SetForegroundWindow", "GetForegroundWindow", "Detach:5->7"];
            Assert.Equal(expected, calls.Calls);
        }

        [Fact]
        public void ActivationRefused_ReturnsFalseAndStillDetaches()
        {
            FakeCalls calls = new() { ActivateResult = false };

            Assert.False(NativeMethods.TryTakeForeground(Target, calls, RetryTimeout));
            Assert.Contains("Attach:5->7", calls.Calls, StringComparer.Ordinal);
            Assert.Equal("Detach:5->7", calls.Calls[calls.Calls.Count - 1]);
        }

        [Fact]
        public void AttachRefused_RetriesWithoutDetaching()
        {
            FakeCalls calls = new() { AttachResult = false };

            Assert.True(NativeMethods.TryTakeForeground(Target, calls, RetryTimeout));
            Assert.Contains("Attach:5->7", calls.Calls, StringComparer.Ordinal);
            Assert.Contains("SetForegroundWindow", calls.Calls, StringComparer.Ordinal);
            Assert.DoesNotContain("Detach:5->7", calls.Calls, StringComparer.Ordinal);
        }

        [Fact]
        public void OwnThreadHoldsForeground_SkipsAttach()
        {
            FakeCalls calls = new() { ForegroundThread = 5 };

            Assert.True(NativeMethods.TryTakeForeground(Target, calls, RetryTimeout));
            Assert.DoesNotContain("EnsureMessageQueue", calls.Calls, StringComparer.Ordinal);
            Assert.DoesNotContain(calls.Calls, static c => c.StartsWith("Attach:", StringComparison.Ordinal));
            Assert.Contains("SetForegroundWindow", calls.Calls, StringComparer.Ordinal);
        }

        [Fact]
        public void HungForeground_SkipsAttach()
        {
            FakeCalls calls = new() { Hung = true };

            Assert.True(NativeMethods.TryTakeForeground(Target, calls, RetryTimeout));
            Assert.DoesNotContain(calls.Calls, static c => c.StartsWith("Attach:", StringComparison.Ordinal));
            Assert.Contains("SetForegroundWindow", calls.Calls, StringComparer.Ordinal);
        }

        [Fact]
        public void Retry_RunsOffTheCallingThread()
        {
            FakeCalls calls = new();

            Assert.True(NativeMethods.TryTakeForeground(Target, calls, RetryTimeout));
            Assert.NotEqual(-1, calls.ActivatingManagedThreadId);
            Assert.NotEqual(Environment.CurrentManagedThreadId, calls.ActivatingManagedThreadId);
        }

        [Fact]
        public void UnresponsiveForeground_GivesUpAtTheTimeoutWithoutBlockingTheCaller()
        {
            using ManualResetEventSlim block = new(initialState: false);
            FakeCalls calls = new() { BlockActivation = block };
            Stopwatch elapsed = Stopwatch.StartNew();

            bool result = NativeMethods.TryTakeForeground(Target, calls, TimeSpan.FromMilliseconds(200));
            elapsed.Stop();
            block.Set();

            Assert.False(result);
            Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(3), "The caller waited past the retry timeout: " + elapsed.Elapsed.ToString("c", System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
