using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace PSADT.PInvokes
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

        /// <summary>
        /// Tries to create a <see cref="SafeAccessToken"/> from an <see cref="IntPtr"/> handle.
        /// </summary>
        /// <param name="ptr">The pointer to the token handle.</param>
        /// <param name="handle">The resulting <see cref="SafeAccessToken"/> if successful.</param>
        /// <returns><c>true</c> if the handle was valid and successfully wrapped; otherwise, <c>false</c>.</returns>
        public static bool TryCreate(IntPtr ptr, out SafeAccessToken handle)
        {
            try
            {
                handle = new SafeAccessToken(ptr);
                return true;
            }
            catch
            {
                handle = Null; // Return a null SafeAccessToken in case of failure
                return false;
            }
        }

        /// <summary>
        /// Implicit conversion to <see cref="SafeAccessTokenHandle"/>.
        /// </summary>
        /// <param name="safeAccessToken">The <see cref="SafeAccessToken"/> instance to convert.</param>
        public static implicit operator SafeAccessTokenHandle(SafeAccessToken safeAccessToken)
        {
            return new SafeAccessTokenHandle(safeAccessToken.DangerousGetHandle());
        }

        /// <summary>
        /// Converts the <see cref="SafeAccessToken"/> to a <see cref="SafeAccessTokenHandle"/>.
        /// </summary>
        /// <returns>A new instance of <see cref="SafeAccessTokenHandle"/> that wraps the same handle.</returns>
        public SafeAccessTokenHandle ToSafeAccessTokenHandle()
        {
            return new SafeAccessTokenHandle(this.DangerousGetHandle());
        }
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
    /// Represents a Wts server handle that can be closed with <see cref="NativeMethods.WTSCloseServer(IntPtr)"/>.
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

    /// <summary>
    /// Represents a safe handle for a Catalog Admin context.
    /// </summary>
    public sealed class SafeCatAdminHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SafeCatAdminHandle"/> class.
        /// </summary>
        public SafeCatAdminHandle() : base(true) { }

        /// <summary>
        /// Releases the catalog admin context handle.
        /// </summary>
        /// <returns>True if the handle was released successfully; otherwise, false.</returns>
        protected override bool ReleaseHandle()
        {
            return NativeMethods.CryptCATAdminReleaseContext(handle, 0);
        }
    }

    /// <summary>
    /// SafeHandle implementation for handling unmanaged memory allocations.
    /// </summary>
    public sealed class SafeHGlobalHandle : SafeHandle
    {
        public SafeHGlobalHandle(int size)
            : base(IntPtr.Zero, true)
        {
            if (size < 0)
                throw new ArgumentOutOfRangeException(nameof(size), "Size must be non-negative.");

            SetHandle(Marshal.AllocHGlobal(size));
        }

        public SafeHGlobalHandle(IntPtr existingHandle)
            : base(IntPtr.Zero, true)
        {
            if (existingHandle == IntPtr.Zero)
                throw new ArgumentNullException(nameof(existingHandle));

            SetHandle(existingHandle);
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            if (!IsInvalid)
            {
                Marshal.FreeHGlobal(handle);
                SetHandle(IntPtr.Zero);
            }
            return true;
        }

        public int Size { get; private set; }

        public void Allocate(int size)
        {
            if (!IsInvalid)
                throw new InvalidOperationException("Memory is already allocated.");

            SetHandle(Marshal.AllocHGlobal(size));
            Size = size;
        }
    }

    public sealed class SafeLibraryHandle : SafeHandle
    {
        /// <summary> Constant for invalid handle value </summary>
        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        /// <summary>
        /// An invalid handle that may be used in place of <see cref="INVALID_HANDLE_VALUE"/>.
        /// </summary>
        public static readonly SafeLibraryHandle Invalid = new SafeLibraryHandle();

        /// <summary>
        /// A handle that may be used in place of <see cref="IntPtr.Zero"/>.
        /// </summary>
        public static readonly SafeLibraryHandle Null = new SafeLibraryHandle(IntPtr.Zero, false);

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeLibraryHandle"/> class.
        /// </summary>
        public SafeLibraryHandle()
            : base(INVALID_HANDLE_VALUE, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeLibraryHandle"/> class with an existing handle.
        /// </summary>
        /// <param name="preexistingHandle">The environment block handle.</param>
        /// <param name="ownsHandle">
        ///     <see langword="true" /> to have the native handle released when this safe handle is disposed or finalized;
        ///     <see langword="false" /> otherwise.
        /// </param>
        public SafeLibraryHandle(IntPtr preexistingHandle, bool ownsHandle = true)
            : base(INVALID_HANDLE_VALUE, ownsHandle)
        {
            SetHandle(preexistingHandle);
        }

        /// <summary>
        /// Gets a value indicating whether the handle value is invalid.
        /// </summary>
        public override bool IsInvalid => handle == INVALID_HANDLE_VALUE || handle == IntPtr.Zero;

        /// <summary>
        /// Frees the library handle when the object is disposed or finalized.
        /// </summary>
        /// <returns>
        /// Returns <see langword="true"/> if the handle was released successfully; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// This method calls <see cref="NativeMethods.FreeLibrary"/> to release the library handle.
        /// </remarks>
        protected override bool ReleaseHandle()
        {
            return NativeMethods.FreeLibrary(handle);
        }
    }

    /// <summary>
    /// Represents a safe handle for COM error information (IErrorInfo interface).
    /// </summary>
    /// <remarks>
    /// This class ensures that the COM error information handle is released correctly using <see cref="Marshal.Release"/> when the handle is no longer needed.
    /// </remarks>
    public sealed class SafeErrorInfoHandle : SafeHandle
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SafeErrorInfoHandle"/> class with a handle set to <see cref="IntPtr.Zero"/> and specifies ownership for handle release.
        /// </summary>
        public SafeErrorInfoHandle() : base(IntPtr.Zero, true) { }

        /// <summary>
        /// Gets a value indicating whether the handle is invalid.
        /// </summary>
        /// <value>
        /// Returns <see langword="true"/> if the handle is <see cref="IntPtr.Zero"/>; otherwise, <see langword="false"/>.
        /// </value>
        public override bool IsInvalid => handle == IntPtr.Zero;

        /// <summary>
        /// Releases the COM error information handle when the object is disposed or finalized.
        /// </summary>
        /// <returns>
        /// Returns <see langword="true"/> if the handle was released successfully; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// This method calls <see cref="Marshal.Release"/> to release the COM error information handle if it is valid.
        /// </remarks>
        protected override bool ReleaseHandle()
        {
            if (!IsInvalid)
            {
                Marshal.Release(handle);
                return true;
            }
            return false;
        }
    }
}
