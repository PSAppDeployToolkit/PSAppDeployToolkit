using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace PSADT.PInvoke
{
    /// <summary>
    /// Represents a safe handle for Windows access tokens.
    /// </summary>
    /// <remarks>
    /// This class ensures that access token handles are properly released when no longer needed, 
    /// preventing resource leaks. It is derived from <see cref="SafeHandle"/> 
    /// to manage the lifetime of a Windows access token handle, which is considered invalid if it 
    /// is set to 0 or -1.
    /// </remarks>
    public sealed class SafeAccessToken : SafeHandle
    {
        /// <summary> Constant for invalid handle value </summary>
        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        /// <summary>
        /// An invalid handle that may be used in place of <see cref="INVALID_HANDLE_VALUE"/>.
        /// </summary>
        public static readonly SafeAccessToken Invalid = new SafeAccessToken();

        /// <summary>
        /// A handle that may be used in place of <see cref="IntPtr.Zero"/>.
        /// </summary>
        public static readonly SafeAccessToken Null = new SafeAccessToken(IntPtr.Zero, false);

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeAccessToken"/> class with an invalid handle value.
        /// </summary>
        /// <remarks>
        /// This constructor is primarily used by the .NET runtime or when you need to instantiate 
        /// an invalid handle that will be set later.
        /// </remarks>
        public SafeAccessToken()
            : base(INVALID_HANDLE_VALUE, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeAccessToken"/> class.
        /// </summary>
        /// <param name="preexistingHandle">An object that represents the pre-existing handle to use.</param>
        /// <param name="ownsHandle">
        ///     <see langword="true" /> to have the native handle released when this safe handle is disposed or finalized;
        ///     <see langword="false" /> otherwise.
        /// </param>
        public SafeAccessToken(IntPtr preexistingHandle, bool ownsHandle = true)
            : base(INVALID_HANDLE_VALUE, ownsHandle)
        {
            SetHandle(preexistingHandle);
        }

        /// <summary>
        /// Gets a value indicating whether the handle value is invalid.
        /// </summary>
        public override bool IsInvalid => handle == INVALID_HANDLE_VALUE || handle == IntPtr.Zero;

        /// <summary>
        /// Releases the handle associated with this <see cref="SafeAccessToken"/> instance.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the handle was released successfully; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method is called by the .NET runtime to release the handle. It calls the 
        /// <see cref="NativeMethods.CloseHandle"/> method to close the handle.
        /// </remarks>
        protected override bool ReleaseHandle() => NativeMethods.CloseHandle(handle);
    }

    /// <summary>
    /// Represents a safe handle for an environment block.
    /// </summary>
    public sealed class SafeEnvironmentBlock : SafeHandle
    {
        /// <summary> Constant for invalid handle value </summary>
        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        /// <summary>
        /// An invalid handle that may be used in place of <see cref="INVALID_HANDLE_VALUE"/>.
        /// </summary>
        public static readonly SafeEnvironmentBlock Invalid = new SafeEnvironmentBlock();

        /// <summary>
        /// A handle that may be used in place of <see cref="IntPtr.Zero"/>.
        /// </summary>
        public static readonly SafeEnvironmentBlock Null = new SafeEnvironmentBlock(IntPtr.Zero, false);

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeEnvironmentBlock"/> class.
        /// </summary>
        public SafeEnvironmentBlock()
            : base(INVALID_HANDLE_VALUE, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeEnvironmentBlock"/> class with an existing handle.
        /// </summary>
        /// <param name="preexistingHandle">The environment block handle.</param>
        /// <param name="ownsHandle">
        ///     <see langword="true" /> to have the native handle released when this safe handle is disposed or finalized;
        ///     <see langword="false" /> otherwise.
        /// </param>
        public SafeEnvironmentBlock(IntPtr preexistingHandle, bool ownsHandle = true)
            : base(INVALID_HANDLE_VALUE, ownsHandle)
        {
            SetHandle(preexistingHandle);
        }

        /// <summary>
        /// Gets a value indicating whether the handle value is invalid.
        /// </summary>
        public override bool IsInvalid => handle == INVALID_HANDLE_VALUE || handle == IntPtr.Zero;

        /// <summary>
        /// Releases the handle associated with the environment block.
        /// </summary>
        /// <returns><c>true</c> if the handle is released successfully; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method is called when the <see cref="SafeEnvironmentBlock"/> object is disposed or finalized. 
        /// It ensures that the environment block is destroyed using the <see cref="NativeMethods.DestroyEnvironmentBlock"/> function to avoid memory leaks.
        /// </remarks>
        protected override bool ReleaseHandle()
        {
            return NativeMethods.DestroyEnvironmentBlock(handle);
        }
    }

    /// <summary>
    /// Represents a Wts server handle that can be closed with <see cref="WTSCloseServer(IntPtr)"/>.
    /// </summary>
    public sealed class SafeWTSServer : SafeHandle
    {
        /// <summary>A handle that may be used in place of <see cref="IntPtr.Zero"/>.</summary>
        public static readonly SafeWTSServer Null = new SafeWTSServer(IntPtr.Zero, false);

        /// <summary>A constant representing the handle of the current WTS server.</summary>
        public static readonly SafeWTSServer WTS_CURRENT_SERVER_HANDLE = Null;

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeWTSServer"/> class.
        /// </summary>
        public SafeWTSServer()
            : base(IntPtr.Zero, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeWTSServer"/> class.
        /// </summary>
        /// <param name="preexistingHandle">An object that represents the pre-existing handle to use.</param>
        /// <param name="ownsHandle">
        ///     <see langword="true" /> to have the native handle released when this safe handle is disposed or finalized;
        ///     <see langword="false" /> otherwise.
        /// </param>
        public SafeWTSServer(IntPtr preexistingHandle, bool ownsHandle = true)
            : base(IntPtr.Zero, ownsHandle)
        {
            SetHandle(preexistingHandle);
        }

        /// <summary>
        /// Releases the handle associated with the WTS (Windows Terminal Services) memory.
        /// </summary>
        protected override bool ReleaseHandle()
        {
            if (handle == IntPtr.Zero)
            {
                return true;
            }

            NativeMethods.WTSCloseServer(handle);
            return true;
        }

        /// <summary>
        /// Determines if the handle represens the local WTS server
        /// </summary>
        public bool IsLocalServer => handle == IntPtr.Zero;

        /// <summary>
        /// Gets a value indicating whether the handle value is invalid.
        /// </summary>
        public override bool IsInvalid => handle == IntPtr.Zero;
    }

    /// <summary>Provides a <see cref="SafeHandle"/> for WTS memory that is disposed using <see cref="NativeMethods.WTSFreeMemory"/>.</summary>
    public sealed class SafeWtsMemory : SafeHandle
    {
        /// <summary> Constant for invalid handle value </summary>
        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        /// <summary>
        /// An invalid handle that may be used in place of <see cref="INVALID_HANDLE_VALUE"/>.
        /// </summary>
        public static readonly SafeWtsMemory Invalid = new SafeWtsMemory();

        /// <summary>
        /// A handle that may be used in place of <see cref="IntPtr.Zero"/>.
        /// </summary>
        public static readonly SafeWTSServer Null = new SafeWTSServer(IntPtr.Zero, false);

        /// <summary>Initializes a new instance of the <see cref="SafeWtsMemory"/> class.</summary>
        public SafeWtsMemory()
            : base(INVALID_HANDLE_VALUE, true)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="SafeWtsMemory"/> class and assigns an existing handle.</summary>
        /// <param name="preexistingHandle">An <see cref="IntPtr"/> object that represents the pre-existing handle to use.</param>
        /// <param name="ownsHandle">
        /// <see langword="true"/> to reliably release the handle during the finalization phase; otherwise, <see langword="false"/> (not recommended).
        /// </param>
        public SafeWtsMemory(IntPtr preexistingHandle, bool ownsHandle = true)
            : base(preexistingHandle, ownsHandle)
        {
            SetHandle(preexistingHandle);
        }

        /// <summary>
        /// Gets a value indicating whether the handle value is invalid.
        /// </summary>
        public override bool IsInvalid => handle == INVALID_HANDLE_VALUE || handle == IntPtr.Zero;

        // Override the ReleaseHandle method to free the WTS memory
        protected override bool ReleaseHandle()
        {
            if (!IsInvalid)
            {
                NativeMethods.WTSFreeMemory(handle);
            }
            return true;
        }

        /// <summary>
        /// Converts the memory pointed to by the handle into a managed structure of type T.
        /// </summary>
        /// <typeparam name="T">The type of the structure to convert to.</typeparam>
        /// <param name="allocatedBytes">The number of bytes allocated for the structure.</param>
        /// <returns>The managed structure of type T.</returns>
        public T ToStructure<T>(uint allocatedBytes)
        {
            if (IsInvalid || IsClosed)
            {
                throw new InvalidOperationException("Cannot convert from an invalid or closed handle.");
            }

            if (allocatedBytes < Marshal.SizeOf<T>())
            {
                throw new ArgumentException("Allocated bytes are less than the size of the structure.", nameof(allocatedBytes));
            }

            var result = Marshal.PtrToStructure<T>(handle);

            return result ?? Activator.CreateInstance<T>();
        }

        /// <summary>
        /// Converts the memory pointed to by the handle into a managed string.
        /// </summary>
        /// <param name="allocatedBytes">The number of bytes allocated for the string.</param>
        /// <returns>The managed string.</returns>
        public string ToString(uint allocatedBytes)
        {
            if (IsInvalid || IsClosed)
            {
                throw new InvalidOperationException("Cannot convert from an invalid or closed handle.");
            }

            return Marshal.PtrToStringUni(handle, (int)(allocatedBytes / 2)) ?? string.Empty;
        }

        /// <summary>
        /// Converts the memory pointed to by the handle into an array of managed structures of type T.
        /// </summary>
        /// <typeparam name="T">The type of the structures to convert to.</typeparam>
        /// <param name="count">The number of structures to convert.</param>
        /// <returns>An array of managed structures of type T.</returns>
        public T[] ToArray<T>(int count)
        {
            if (IsInvalid || IsClosed)
            {
                throw new InvalidOperationException("Cannot convert from an invalid or closed handle.");
            }

            var sizeOfT = Marshal.SizeOf<T>();
            var array = new T[count];

            for (int i = 0; i < count; i++)
            {
                var ptr = IntPtr.Add(handle, i * sizeOfT);
                var item = Marshal.PtrToStructure<T>(ptr);
                array[i] = item ?? Activator.CreateInstance<T>();
            }

            return array;
        }
    }
}
