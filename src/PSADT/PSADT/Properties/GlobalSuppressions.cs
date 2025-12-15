// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The object disposal is correct here, the compiler just can't tell.", Scope = "member", Target = "~M:PSADT.AccountManagement.AccountUtilities.IsSidMemberOfWellKnownGroup(System.Security.Principal.SecurityIdentifier,System.Security.Principal.WellKnownSidType)~System.Boolean")]
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The object disposal is correct here, the compiler just can't tell.", Scope = "member", Target = "~M:PSADT.FileSystem.FileHandleManager.CloseHandles(PSADT.LibraryInterfaces.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX[])")]
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The object disposal is correct here, the compiler just can't tell.", Scope = "member", Target = "~M:PSADT.ProcessManagement.ProcessManager.LaunchAsync(PSADT.ProcessManagement.ProcessLaunchInfo)~PSADT.ProcessManagement.ProcessHandle")]
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The object disposal is correct here, the compiler just can't tell.", Scope = "member", Target = "~M:PSADT.ProcessManagement.ProcessToken.GetUserPrimaryToken(PSADT.Core.RunAsActiveUser,System.Boolean,System.Boolean)~Microsoft.Win32.SafeHandles.SafeFileHandle")]
[assembly: SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "The text that we're lowering in case is always in English.", Scope = "member", Target = "~M:PSADT.Core.DeploymentSession.#ctor(System.Collections.Generic.IReadOnlyDictionary{System.String,System.Object},System.Nullable{System.Boolean},System.Management.Automation.SessionState)")]
[assembly: SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "The text that we're lowering in case is always in English.", Scope = "member", Target = "~M:PSADT.Core.DeploymentSession.Close~System.Int32")]
