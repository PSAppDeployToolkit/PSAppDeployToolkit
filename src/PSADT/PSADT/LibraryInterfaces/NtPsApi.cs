using System;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Specifies flags that control the behavior of thread creation in native interop scenarios.
    /// </summary>
    /// <remarks>Use the THREAD_CREATE_FLAGS enumeration to customize thread creation when invoking native
    /// APIs that accept thread creation flags. These flags allow you to control aspects such as whether the thread
    /// starts suspended, receives DLL thread notifications, is visible to debuggers, or participates in loader and
    /// process freeze operations. These options are primarily intended for advanced or low-level scenarios, such as
    /// interop with unmanaged code, debugging, or process injection. Most applications do not need to set these flags
    /// explicitly.</remarks>
    internal enum THREAD_CREATE_FLAGS : uint
    {
        /// <summary>
        /// Specifies that a newly created thread is initialized in a suspended state and does not run until it is
        /// explicitly resumed.
        /// </summary>
        /// <remarks>Use this flag when creating a thread to prevent it from executing immediately. The
        /// thread will remain suspended until a resume operation is performed, allowing for additional setup or
        /// synchronization before execution begins.</remarks>
        THREAD_CREATE_FLAGS_CREATE_SUSPENDED = 0x00000001,

        /// <summary>
        /// Indicates that the system should not call the DLL_THREAD_ATTACH and DLL_THREAD_DETACH notifications for this
        /// thread when a DLL is loaded or unloaded.
        /// </summary>
        /// <remarks>This flag is typically used when creating threads in native interop scenarios to
        /// improve performance by avoiding unnecessary thread attach and detach notifications for DLLs. Use this flag
        /// only if you are certain that your application does not require these notifications for proper
        /// operation.</remarks>
        THREAD_CREATE_FLAGS_SKIP_THREAD_ATTACH = 0x00000002,

        /// <summary>
        /// Specifies that the thread is created with the attribute to hide it from the debugger.
        /// </summary>
        /// <remarks>When this flag is set, the thread will not be visible to debuggers using standard
        /// debugging APIs. This can be used to prevent the thread from being detected or manipulated during debugging
        /// sessions. Use with caution, as this may interfere with debugging and diagnostic tools.</remarks>
        THREAD_CREATE_FLAGS_HIDE_FROM_DEBUGGER = 0x00000004,

        /// <summary>
        /// Indicates that the thread is created as a loader worker thread.
        /// </summary>
        /// <remarks>Loader worker threads are used internally by the system to perform background loading
        /// operations. This flag is typically used in low-level thread creation scenarios and is not commonly required
        /// for general application development.</remarks>
        THREAD_CREATE_FLAGS_LOADER_WORKER = 0x00000010,

        /// <summary>
        /// Specifies that the loader initialization code should be skipped when creating a new thread.
        /// </summary>
        /// <remarks>This flag is typically used in advanced scenarios where the thread does not require
        /// standard loader initialization, such as when creating threads in a suspended state for debugging or
        /// injection purposes. Skipping loader initialization may affect the thread's ability to use certain runtime
        /// features.</remarks>
        THREAD_CREATE_FLAGS_SKIP_LOADER_INIT = 0x00000020,

        /// <summary>
        /// Specifies that the thread should not be suspended when the process is frozen.
        /// </summary>
        /// <remarks>This flag can be used when creating a thread to ensure it continues running even if
        /// the process enters a frozen state, such as during debugging or process snapshot operations. Use with
        /// caution, as bypassing process freeze may have implications for process consistency and debugging
        /// scenarios.</remarks>
        THREAD_CREATE_FLAGS_BYPASS_PROCESS_FREEZE = 0x00000040,
    }

    /// <summary>
    /// Specifies extended flags for process creation, allowing customization of process behavior.
    /// </summary>
    /// <remarks>Sourced from https://github.com/winsiderss/phnt/blob/fc1f96ee976635f51faa89896d1d805eb0586350/ntpsapi.h#L2781-L2783</remarks>
    [Flags]
    internal enum EXTENDED_PROCESS_CREATION_FLAG : uint
    {
        /// <summary>
        /// Indicates that process elevation has been handled during extended process creation.
        /// </summary>
        /// <remarks>This flag is used to specify that elevation requirements have already been addressed
        /// and that no further elevation prompts should occur during the process creation. It is typically used in
        /// scenarios where the caller has explicitly managed elevation before invoking the process creation
        /// API.</remarks>
        EXTENDED_PROCESS_CREATION_FLAG_ELEVATION_HANDLED = 0x1,

        /// <summary>
        /// Specifies that the process should be created with forced User Account Control (UAC) virtualization enabled.
        /// </summary>
        /// <remarks>This flag is typically used when creating processes that require UAC virtualization,
        /// ensuring that file and registry operations are redirected for compatibility with legacy applications. Use
        /// this flag only when necessary, as it may affect how the process interacts with system resources.</remarks>
        EXTENDED_PROCESS_CREATION_FLAG_FORCELUA = 0x2,

        /// <summary>
        /// Specifies that the process should be created with a force breakaway flag, allowing it to break away from any
        /// job object it would otherwise inherit.
        /// </summary>
        /// <remarks>Use this flag when creating a process that must not be associated with the job object
        /// of its parent, even if the parent is running within a job. This is typically relevant in advanced process
        /// management scenarios on Windows platforms.</remarks>
        EXTENDED_PROCESS_CREATION_FLAG_FORCE_BREAKAWAY = 0x4,
    }
}
