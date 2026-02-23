using System;
using Windows.Win32.Foundation;

namespace PSADT.Interop
{
    /// <summary>
    /// Represents a persistence mode for opening or creating Windows Installer (MSI) databases, encapsulating the mode
    /// value used by native MSI APIs.
    /// </summary>
    /// <remarks>This class provides type safety and convenience when specifying database persistence
    /// modes in interop scenarios with Windows Installer APIs. It includes predefined values for common modes such as
    /// direct, transactional, and read-only access, as well as support for patch files. Use the provided static fields
    /// to select the appropriate mode when working with MSI database operations. This type is intended for internal use
    /// and is not intended to be used directly in application code.</remarks>
    internal sealed class MSI_PERSISTENCE_MODE : TypedConstant<MSI_PERSISTENCE_MODE>
    {
        /// <summary>
        /// Open a database read-only, no persistent changes.
        /// </summary>
        internal static readonly MSI_PERSISTENCE_MODE MSIDBOPEN_READONLY = new(Windows.Win32.PInvoke.MSIDBOPEN_READONLY);

        /// <summary>
        /// Open a database read/write in transaction mode.
        /// </summary>
        internal static readonly MSI_PERSISTENCE_MODE MSIDBOPEN_TRANSACT = new(Windows.Win32.PInvoke.MSIDBOPEN_TRANSACT);

        /// <summary>
        /// Open a database direct read/write without transaction.
        /// </summary>
        internal static readonly MSI_PERSISTENCE_MODE MSIDBOPEN_DIRECT = new(Windows.Win32.PInvoke.MSIDBOPEN_DIRECT);

        /// <summary>
        /// Create a new database, transact mode read/write.
        /// </summary>
        internal static readonly MSI_PERSISTENCE_MODE MSIDBOPEN_CREATE = new(Windows.Win32.PInvoke.MSIDBOPEN_CREATE);

        /// <summary>
        /// Create a new database, direct mode read/write.
        /// </summary>
        internal static readonly MSI_PERSISTENCE_MODE MSIDBOPEN_CREATEDIRECT = new(Windows.Win32.PInvoke.MSIDBOPEN_CREATEDIRECT);

        /// <summary>
        /// Add this flag to indicate a patch file. Combine with another mode using the + operator.
        /// </summary>
        /// <remarks>
        /// The Win32 API defines MSIDBOPEN_PATCHFILE as the integer 16. When combined with a PCWSTR mode
        /// (e.g., MSIDBOPEN_READONLY + MSIDBOPEN_PATCHFILE), char* pointer arithmetic is used to
        /// produce the final mode value: (char*)0 + 16 = 32 (since sizeof(char) = 2 in UTF-16).
        /// </remarks>
        internal static readonly unsafe MSI_PERSISTENCE_MODE MSIDBOPEN_PATCHFILE = new((char*)Windows.Win32.PInvoke.MSIDBOPEN_PATCHFILE);

        /// <summary>
        /// Initializes a new instance of the <see cref="MSI_PERSISTENCE_MODE"/> class with the specified value.
        /// </summary>
        /// <param name="value">A PCWSTR representing the persistence mode value to assign.</param>
        /// <param name="name">The name of the constant, automatically captured from the calling member.</param>
        private MSI_PERSISTENCE_MODE(PCWSTR value, [System.Runtime.CompilerServices.CallerMemberName] string name = null!) : base(value, name)
        {
        }

        /// <summary>
        /// Combines two persistence modes using char* pointer arithmetic.
        /// </summary>
        /// <remarks>
        /// This operator enables Win32-style mode combination such as MSIDBOPEN_READONLY + MSIDBOPEN_PATCHFILE.
        /// One operand must be MSIDBOPEN_PATCHFILE (integer-based), and the other must be a PCWSTR-based mode.
        /// Pointer + pointer arithmetic is not permitted; one side must be MSIDBOPEN_PATCHFILE.
        /// </remarks>
        /// <param name="left">The first persistence mode.</param>
        /// <param name="right">The second persistence mode.</param>
        /// <returns>A new <see cref="MSI_PERSISTENCE_MODE"/> representing the combined mode.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="left"/> or <paramref name="right"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when neither operand is MSIDBOPEN_PATCHFILE.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Converting this to conditional expression just makes a mess.")]
        public static MSI_PERSISTENCE_MODE operator +(MSI_PERSISTENCE_MODE left, MSI_PERSISTENCE_MODE right)
        {
            // Validate that neither operand is null.
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }
            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            // Determine which operand is MSIDBOPEN_PATCHFILE (the integer offset) and which is the base pointer
            bool leftIsPatchFile = ReferenceEquals(left, MSIDBOPEN_PATCHFILE);
            bool rightIsPatchFile = ReferenceEquals(right, MSIDBOPEN_PATCHFILE);
            if (!leftIsPatchFile && !rightIsPatchFile)
            {
                throw new InvalidOperationException("Pointer + pointer arithmetic is not permitted. One operand must be MSIDBOPEN_PATCHFILE.");
            }

            // Use the PATCHFILE value as the offset and the other as the base pointer,
            // then use char* pointer arithmetic: (char*)base + offset.
            MSI_PERSISTENCE_MODE baseMode, offsetMode;
            if (leftIsPatchFile)
            {
                baseMode = right;
                offsetMode = left;
            }
            else
            {
                baseMode = left;
                offsetMode = right;
            }
            unsafe
            {
                return new((char*)baseMode.ToIntPtr() + (int)offsetMode, $"{baseMode}, {offsetMode}");
            }
        }
    }
}
