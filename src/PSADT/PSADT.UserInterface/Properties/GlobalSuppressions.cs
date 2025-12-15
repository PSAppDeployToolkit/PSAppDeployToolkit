// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "We lower the case here for aesthetic in our dialogs. As process names are 99.99% English, this is OK.", Scope = "member", Target = "~M:PSADT.UserInterface.Dialogs.Fluent.CloseAppsDialog.AppToClose.#ctor(PSADT.ProcessManagement.ProcessToClose)")]
