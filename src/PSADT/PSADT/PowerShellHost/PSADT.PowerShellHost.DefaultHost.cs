using System;
using System.Globalization;
using System.Management.Automation.Host;

namespace PSADT.PowerShellHost
{
    /// <summary>
    /// Provides a default implementation of PSHost.
    /// </summary>
    internal class DefaultHost : PSHost
    {
        public override CultureInfo CurrentCulture => CultureInfo.CurrentCulture;
        public override CultureInfo CurrentUICulture => CultureInfo.CurrentUICulture;
        public override Guid InstanceId { get; } = Guid.NewGuid();
        public override string Name => "PowerShell App Deployment Toolkit";
        public override PSHostUserInterface UI => new DefaultHostUserInterface();
        public override Version Version => new Version(1, 0);
        public override void EnterNestedPrompt() { }
        public override void ExitNestedPrompt() { }
        public override void NotifyBeginApplication() { }
        public override void NotifyEndApplication() { }
        public override void SetShouldExit(int exitCode) { }
    }
}
