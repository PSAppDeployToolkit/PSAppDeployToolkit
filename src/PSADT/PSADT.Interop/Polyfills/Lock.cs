#if !NET9_0_OR_GREATER
using System.Runtime.CompilerServices;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Threading
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// Polyfill for <see cref="Lock"/> on runtimes prior to .NET 9.
    /// Provides a lightweight mutual exclusion lock primitive compatible with the <c>lock</c> statement.
    /// </summary>
    internal sealed class Lock
    {
        /// <summary>
        /// Enters the lock, blocking the current thread until it can do so.
        /// </summary>
        /// <returns>A <see cref="Scope"/> that can be disposed to exit the lock.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Scope EnterScope()
        {
            Monitor.Enter(_syncRoot);
            return new(_syncRoot);
        }

        /// <summary>
        /// Represents a scope during which the lock is held. Disposing the scope exits the lock.
        /// </summary>
        public readonly ref struct Scope
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Scope"/> struct.
            /// </summary>
            /// <param name="syncRoot">The synchronization object that is held.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Scope(object syncRoot)
            {
                _syncRoot = syncRoot;
            }

            /// <summary>
            /// Exits the lock.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                Monitor.Exit(_syncRoot);
            }

            /// <summary>
            /// An object used to synchronize access to the containing instance.
            /// </summary>
            /// <remarks>This object can be used as a lock to ensure thread-safe operations on shared
            /// resources within the class.</remarks>
            private readonly object _syncRoot;
        }

        /// <summary>
        /// An object used to synchronize access to the containing instance.
        /// </summary>
        /// <remarks>Use this object as a lock to ensure thread-safe operations when accessing shared
        /// resources within the class.</remarks>
        private readonly object _syncRoot = new();
    }
}
#endif
