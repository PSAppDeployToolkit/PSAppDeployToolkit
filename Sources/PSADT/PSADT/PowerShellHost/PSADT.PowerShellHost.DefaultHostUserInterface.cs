using System;
using System.Security;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Host;

namespace PSADT.PowerShellHost
{
    /// <summary>
    /// Provides a default implementation of PSHostUserInterface.
    /// </summary>
    internal class DefaultHostUserInterface : PSHostUserInterface
    {
        public override PSHostRawUserInterface RawUI => new DefaultHostRawUserInterface();
        public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions) => new Dictionary<string, PSObject>();
        public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice) => defaultChoice;
        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName) => PSCredential.Empty;
        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options) => PSCredential.Empty;
        public override string ReadLine() => string.Empty;
        public override SecureString ReadLineAsSecureString() => new SecureString();
        public override void Write(string value) { }
        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value) { }
        public override void WriteLine(string value) { }
        public override void WriteErrorLine(string value) { }
        public override void WriteDebugLine(string value) { }
        public override void WriteProgress(long sourceId, ProgressRecord record) { }
        public override void WriteVerboseLine(string value) { }
        public override void WriteWarningLine(string value) { }
    }
}
