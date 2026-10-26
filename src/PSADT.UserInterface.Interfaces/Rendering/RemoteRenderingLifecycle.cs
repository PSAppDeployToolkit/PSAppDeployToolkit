using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Microsoft.Win32;

namespace PSADT.UserInterface.Interfaces.Rendering
{
    /// <summary>
    /// Owns the process rendering preference and one optional Windows session-switch subscription.
    /// </summary>
    internal sealed class RemoteRenderingLifecycle : IDisposable
    {
        private readonly bool forceSoftwareRendering;
        private readonly Func<uint> currentSessionId;
        private readonly Func<uint, (bool? WindowsRemote, bool? CitrixRemote)> query;
        private readonly Func<RenderMode> readPreference;
        private readonly Action<RenderMode> writePreference;
        private readonly Action<SessionSwitchEventHandler> subscribe;
        private readonly Action<SessionSwitchEventHandler> unsubscribe;
        private readonly Action<string> logChoice;
        private readonly Action<string> logWarning;
        private Action<Action>? dispatch;
        private bool subscribed;
        private volatile bool disposed;

        internal RemoteRenderingLifecycle(
            bool forceSoftwareRendering,
            Func<uint> currentSessionId,
            Func<uint, (bool? WindowsRemote, bool? CitrixRemote)> query,
            Func<RenderMode> readPreference,
            Action<RenderMode> writePreference,
            Action<SessionSwitchEventHandler> subscribe,
            Action<SessionSwitchEventHandler> unsubscribe,
            Action<string> logChoice,
            Action<string> logWarning)
        {
            this.forceSoftwareRendering = forceSoftwareRendering;
            this.currentSessionId = currentSessionId;
            this.query = query;
            this.readPreference = readPreference;
            this.writePreference = writePreference;
            this.subscribe = subscribe;
            this.unsubscribe = unsubscribe;
            this.logChoice = logChoice;
            this.logWarning = logWarning;
        }

        internal void ApplyInitial()
        {
            ApplyPreference();
        }

        internal void Start(Action<Action> dispatch)
        {
            if (disposed || subscribed || forceSoftwareRendering)
            {
                return;
            }

            this.dispatch = dispatch;
            try
            {
                subscribe(OnSessionSwitch);
                subscribed = true;
            }
            catch (InvalidOperationException exception)
            {
                this.dispatch = null;
                logWarning($"Remote rendering session-switch registration unavailable; retaining the startup preference. {exception}");
            }
            catch (ExternalException exception)
            {
                this.dispatch = null;
                logWarning($"Remote rendering session-switch registration unavailable; retaining the startup preference. {exception}");
            }
            catch (NotSupportedException exception)
            {
                this.dispatch = null;
                logWarning($"Remote rendering session-switch registration unavailable; retaining the startup preference. {exception}");
            }
        }

        private void OnSessionSwitch(object? sender, SessionSwitchEventArgs args)
        {
            if (disposed || !IsConnectionChange(args.Reason))
            {
                return;
            }

            // SystemEvents supplies a reason, not an affected session ID. Re-query this client on its dispatcher.
            dispatch?.Invoke(() =>
            {
                if (!disposed)
                {
                    ApplyPreference();
                }
            });
        }

        private static bool IsConnectionChange(SessionSwitchReason reason)
        {
            return reason is SessionSwitchReason.ConsoleConnect or SessionSwitchReason.ConsoleDisconnect
                or SessionSwitchReason.RemoteConnect or SessionSwitchReason.RemoteDisconnect;
        }

        private void ApplyPreference()
        {
            (bool? windowsRemote, bool? citrixRemote) = forceSoftwareRendering
                ? (null, null)
                : query(currentSessionId());
            RenderMode choice = RemoteRenderingPolicy.Choose(forceSoftwareRendering, windowsRemote, citrixRemote);
            if (readPreference() != choice)
            {
                writePreference(choice);
            }
            logChoice($"WPF process rendering preference: {choice}; override={forceSoftwareRendering}; WindowsRemote={windowsRemote?.ToString() ?? "unknown"}; CitrixRemote={citrixRemote?.ToString() ?? "unknown"}.");
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            if (subscribed)
            {
                unsubscribe(OnSessionSwitch);
                subscribed = false;
            }
            dispatch = null;
        }
    }
}
