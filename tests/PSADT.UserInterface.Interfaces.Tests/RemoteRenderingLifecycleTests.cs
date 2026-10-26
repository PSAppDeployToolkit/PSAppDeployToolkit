using System;
using System.Collections.Generic;
using System.Windows.Interop;
using Microsoft.Win32;
using PSADT.UserInterface.Interfaces.Rendering;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests
{
    /// <summary>
    /// Exercises the rendering lifecycle with fake evidence and session events, without creating windows.
    /// </summary>
    public sealed class RemoteRenderingLifecycleTests
    {
        /// <summary>
        /// Checks the startup preference for override, positive, local, and unknown evidence.
        /// </summary>
        /// <param name="force">Whether the operator override is enabled.</param>
        /// <param name="windows">Windows remote evidence.</param>
        /// <param name="citrix">Citrix remote evidence.</param>
        /// <param name="expected">The expected WPF preference.</param>
        [Theory]
        [InlineData(true, null, null, RenderMode.SoftwareOnly)]
        [InlineData(false, true, null, RenderMode.SoftwareOnly)]
        [InlineData(false, false, true, RenderMode.SoftwareOnly)]
        [InlineData(false, false, false, RenderMode.Default)]
        [InlineData(false, null, null, RenderMode.Default)]
        [InlineData(false, null, false, RenderMode.Default)]
        public void Startup_AppliesExpectedPreference(bool force, bool? windows, bool? citrix, RenderMode expected)
        {
            FakeEnvironment environment = new()
            {
                Evidence = (windows, citrix),
                Preference = expected is RenderMode.SoftwareOnly ? RenderMode.Default : RenderMode.SoftwareOnly,
            };
            using RemoteRenderingLifecycle lifecycle = environment.Create(force);

            lifecycle.ApplyInitial();

            Assert.Equal(expected, environment.Preference);
            Assert.Equal(force ? 0 : 1, environment.QueryCount);
            Assert.Equal(force ? 0 : 1, environment.SessionIdReads);
            Assert.Equal(1, environment.PreferenceWrites);
            Assert.Contains(expected.ToString(), Assert.Single(environment.Choices), StringComparison.Ordinal);
        }

        /// <summary>
        /// Checks that the explicit override avoids both the native provider and event registration.
        /// </summary>
        [Fact]
        public void ForcedMode_NeverSubscribesOrQueries()
        {
            FakeEnvironment environment = new();
            using RemoteRenderingLifecycle lifecycle = environment.Create(force: true);
            lifecycle.ApplyInitial();
            lifecycle.Start(environment.Dispatch);

            Assert.Equal(RenderMode.SoftwareOnly, environment.Preference);
            Assert.Equal(0, environment.SubscriptionCount);
            Assert.Equal(0, environment.QueryCount);
        }

        /// <summary>
        /// Checks event filtering, one subscription, dispatcher re-query, and preference transitions.
        /// </summary>
        [Fact]
        public void AutomaticMode_RequeriesOwnSessionForRelevantEventsOnly()
        {
            FakeEnvironment environment = new() { SessionId = 37, Evidence = (false, false), };
            using RemoteRenderingLifecycle lifecycle = environment.Create(force: false);
            lifecycle.ApplyInitial();
            lifecycle.Start(environment.Dispatch);
            lifecycle.Start(environment.Dispatch);
            Assert.Equal(1, environment.SubscriptionCount);
            Assert.Equal(1, environment.QueryCount);

            environment.Evidence = (true, null);
            environment.Raise(SessionSwitchReason.SessionLock);
            Assert.Empty(environment.Pending);
            Assert.Equal(1, environment.QueryCount);

            environment.Raise(SessionSwitchReason.RemoteConnect);
            Assert.Equal(1, environment.QueryCount);
            environment.Drain();
            Assert.Equal(2, environment.QueryCount);
            Assert.Equal(37u, environment.LastQueriedSessionId);
            Assert.Equal(RenderMode.SoftwareOnly, environment.Preference);

            environment.Raise(SessionSwitchReason.RemoteDisconnect);
            environment.Drain();
            Assert.Equal(3, environment.QueryCount);
            Assert.Equal(1, environment.PreferenceWrites);

            environment.Evidence = (false, false);
            environment.Raise(SessionSwitchReason.ConsoleConnect);
            environment.Drain();
            Assert.Equal(4, environment.QueryCount);
            Assert.Equal(RenderMode.Default, environment.Preference);
            Assert.Equal(2, environment.PreferenceWrites);
        }

        /// <summary>
        /// Checks that an unsupported event source retains the startup preference.
        /// </summary>
        [Fact]
        public void UnsupportedSubscription_RetainsStartupChoice()
        {
            FakeEnvironment environment = new() { Evidence = (true, null), SubscriptionError = new NotSupportedException("unsupported"), };
            using RemoteRenderingLifecycle lifecycle = environment.Create(force: false);
            lifecycle.ApplyInitial();
            lifecycle.Start(environment.Dispatch);

            Assert.Equal(RenderMode.SoftwareOnly, environment.Preference);
            Assert.Equal(1, environment.QueryCount);
            Assert.Contains("unsupported", Assert.Single(environment.Warnings), StringComparison.Ordinal);
        }

        /// <summary>
        /// Checks that shutdown detaches the handler and ignores queued events.
        /// </summary>
        [Fact]
        public void Shutdown_DetachesAndDiscardsQueuedRequery()
        {
            FakeEnvironment environment = new() { Evidence = (false, false), };
            RemoteRenderingLifecycle lifecycle = environment.Create(force: false);
            lifecycle.ApplyInitial();
            lifecycle.Start(environment.Dispatch);
            environment.Raise(SessionSwitchReason.ConsoleDisconnect);

            lifecycle.Dispose();
            lifecycle.Dispose();
            environment.Drain();
            environment.Raise(SessionSwitchReason.ConsoleConnect);

            Assert.Equal(1, environment.UnsubscriptionCount);
            Assert.Equal(1, environment.QueryCount);
            Assert.Equal(RenderMode.Default, environment.Preference);
        }

        private sealed class FakeEnvironment
        {
            private SessionSwitchEventHandler? handler;

            internal (bool? WindowsRemote, bool? CitrixRemote) Evidence { get; set; }
            internal RenderMode Preference { get; set; } = RenderMode.Default;
            internal uint SessionId { get; set; } = 11;
            internal uint LastQueriedSessionId { get; private set; }
            internal int SessionIdReads { get; private set; }
            internal int QueryCount { get; private set; }
            internal int PreferenceWrites { get; private set; }
            internal int SubscriptionCount { get; private set; }
            internal int UnsubscriptionCount { get; private set; }
            internal Exception? SubscriptionError { get; set; }
            internal List<Action> Pending { get; } = [];
            internal List<string> Choices { get; } = [];
            internal List<string> Warnings { get; } = [];

            internal RemoteRenderingLifecycle Create(bool force)
            {
                return new RemoteRenderingLifecycle(force,
                    () => { SessionIdReads++; return SessionId; },
                    sessionId => { QueryCount++; LastQueriedSessionId = sessionId; return Evidence; },
                    () => Preference,
                    mode => { PreferenceWrites++; Preference = mode; },
                    subscription => { SubscriptionCount++; if (SubscriptionError is not null) { throw SubscriptionError; } handler += subscription; },
                    subscription => { UnsubscriptionCount++; handler -= subscription; },
                    Choices.Add,
                    Warnings.Add);
            }

            internal void Dispatch(Action action)
            {
                Pending.Add(action);
            }

            internal void Drain()
            {
                foreach (Action action in Pending)
                {
                    action();
                }
                Pending.Clear();
            }

            internal void Raise(SessionSwitchReason reason)
            {
                handler?.Invoke(this, new SessionSwitchEventArgs(reason));
            }
        }
    }
}
