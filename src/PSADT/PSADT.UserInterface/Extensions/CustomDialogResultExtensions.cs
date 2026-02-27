using System;
using System.Runtime.CompilerServices;
using PSADT.UserInterface.DialogResults;

namespace PSADT.UserInterface.Extensions
{
    internal static class CustomDialogResultExtensions
    {
        /// <summary>
        /// Ensures that the specified <see cref="CustomDialogResult"/> value is not null, and throws an exception if it
        /// is.
        /// </summary>
        /// <remarks>Use this method to enforce non-nullability of <see cref="CustomDialogResult"/> values
        /// before performing further operations. This can help prevent null reference errors and improve code
        /// reliability.</remarks>
        /// <param name="value">The <see cref="CustomDialogResult"/> instance to validate for null.</param>
        /// <param name="name">The name of the calling member. This value is automatically supplied by the compiler.</param>
        /// <returns>The original <see cref="CustomDialogResult"/> value if it is not null.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        internal static CustomDialogResult ThrowIfNull(this CustomDialogResult value, [CallerMemberName] string name = null!)
        {
            return value ?? throw new ArgumentNullException(name);
        }
    }
}
