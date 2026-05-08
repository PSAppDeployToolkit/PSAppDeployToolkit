using System.Security.Principal;

namespace PSADT.Extensions
{
    /// <summary>
    /// Provides extension methods for the SecurityIdentifier class to facilitate working with its binary
    /// representation.
    /// </summary>
    internal static class SecurityIdentifierExtensions
    {
        /// <summary>
        /// Returns the binary representation of the specified SecurityIdentifier as a byte array.
        /// </summary>
        /// <param name="sid">The SecurityIdentifier instance to convert to its binary form. Cannot be null.</param>
        /// <returns>A byte array containing the binary form of the specified SecurityIdentifier.</returns>
        internal static byte[] GetBinaryForm(this SecurityIdentifier sid)
        {
            byte[] binaryForm = new byte[sid.BinaryLength];
            sid.GetBinaryForm(binaryForm, 0);
            return binaryForm;
        }
    }
}
