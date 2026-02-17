// Suppress RS0030 (banned API) diagnostics for CsWin32 source-generated code.
// All CsWin32 output lives under the Windows.Win32 namespace.
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0030:Do not use banned APIs", Justification = "CsWin32 source-generated code.", Scope = "namespaceanddescendants", Target = "~N:Windows.Win32")]
