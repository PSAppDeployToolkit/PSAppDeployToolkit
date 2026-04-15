using System;

namespace PSADT.Interop
{
    /// <summary>
    /// Defines flags that specify how a command should be invoked on a URL.
    /// </summary>
    /// <remarks>These flags are used with the IURL_INVOKECOMMAND method to control the behavior of the
    /// command invocation, such as allowing user interface interaction, using the default verb, waiting for DDE
    /// conversations, enabling asynchronous execution, and logging usage for telemetry purposes.</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "This is how they're named in the Win32 API.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S2344:Enumeration type names should not have \"Flags\" or \"Enum\" suffixes", Justification = "This is appropriately named.")]
    [Flags]
    public enum IURL_INVOKECOMMAND_FLAGS
    {
        /// <summary>
        /// Specifies that the command can display a user interface when invoked.
        /// </summary>
        /// <remarks>This flag is used in conjunction with the IURL_INVOKECOMMAND method to indicate that
        /// the command may require user interaction.</remarks>
        IURL_INVOKECOMMAND_FL_ALLOW_UI = Windows.Win32.UI.Shell.IURL_INVOKECOMMAND_FLAGS.IURL_INVOKECOMMAND_FL_ALLOW_UI,

        /// <summary>
        /// Specifies that the default verb should be used when invoking a command on a URL.
        /// </summary>
        /// <remarks>This value is part of the IURL_INVOKECOMMAND_FLAGS enumeration and indicates that the
        /// default action associated with the URL will be executed. Use this flag when you want to perform the standard
        /// operation defined for the URL, such as opening it in a browser or launching the associated
        /// application.</remarks>
        IURL_INVOKECOMMAND_FL_USE_DEFAULT_VERB = Windows.Win32.UI.Shell.IURL_INVOKECOMMAND_FLAGS.IURL_INVOKECOMMAND_FL_USE_DEFAULT_VERB,

        /// <summary>
        /// Specifies that the command should wait for a Dynamic Data Exchange (DDE) conversation to complete before
        /// returning control to the caller.
        /// </summary>
        /// <remarks>Use this flag with the IURL_INVOKECOMMAND method to ensure synchronous execution.
        /// When set, the caller will not proceed until the DDE conversation has finished, which may be necessary when
        /// interacting with applications that require DDE communication.</remarks>
        IURL_INVOKECOMMAND_FL_DDEWAIT = Windows.Win32.UI.Shell.IURL_INVOKECOMMAND_FLAGS.IURL_INVOKECOMMAND_FL_DDEWAIT,

        /// <summary>
        /// Specifies that the command can be invoked asynchronously without blocking the calling thread.
        /// </summary>
        /// <remarks>Use this flag to indicate that the operation may be performed in a non-blocking
        /// manner, allowing for asynchronous execution. This is useful when the command might take a significant amount
        /// of time to complete and you want to maintain responsiveness in the calling application.</remarks>
        IURL_INVOKECOMMAND_FL_ASYNCOK = Windows.Win32.UI.Shell.IURL_INVOKECOMMAND_FLAGS.IURL_INVOKECOMMAND_FL_ASYNCOK,

        /// <summary>
        /// Records the usage of the command for telemetry purposes, allowing for analysis of how often and in what contexts the command is invoked.
        /// </summary>
        IURL_INVOKECOMMAND_FL_LOG_USAGE = Windows.Win32.UI.Shell.IURL_INVOKECOMMAND_FLAGS.IURL_INVOKECOMMAND_FL_LOG_USAGE,
    }
}
