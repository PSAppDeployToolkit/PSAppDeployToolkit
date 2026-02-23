namespace PSADT.Interop
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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1712:Do not prefix enum values with type name", Justification = "These values are precisely as they're defined in the Win32 API.")]
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
}
