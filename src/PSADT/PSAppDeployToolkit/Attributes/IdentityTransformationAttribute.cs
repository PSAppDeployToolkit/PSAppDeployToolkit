using System;
using System.Management.Automation;
using System.Reflection;
using System.Security.Principal;

namespace PSAppDeployToolkit.Attributes
{
    /// <summary>
    /// Transforms objects into a <see cref="IdentityReference"/>.
    /// </summary>
    public sealed class IdentityTransformationAttribute : ArgumentTransformationAttribute
    {
        /// <summary>
        /// Transforms input object into base <see cref="IdentityReference"/> objects for consumption in downstream PowerShell functions.
        /// Supported types:
        ///  * <see cref="IdentityReference"/>
        ///  * <see cref="WindowsIdentity"/>
        ///  * <see cref="WellKnownSidType"/>
        ///  * LocalUser
        ///  * <see cref="string"/> representing the above
        /// </summary>
        /// <param name="engineIntrinsics">The PowerShell engine intrinsics.</param>
        /// <param name="inputData">The input value to transform.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if the input object did not contain the required properties.</exception>
        /// <exception cref="ArgumentException">Thrown if the input object is not supported for transformation.</exception>
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {

            if (inputData is WellKnownSidType wellKnownSidType)
            {
                return new SecurityIdentifier(wellKnownSidType, null);
            }
            else if (inputData is IdentityReference identity)
            {
                return identity;
            }
            else if (inputData is WindowsIdentity windowsIdentity)
            {
                return windowsIdentity.User
                    ?? throw new InvalidOperationException("The given WindowsIdentity did not provide the required user identity.");
            }
            else if (inputData.GetType().FullName == "Microsoft.PowerShell.Commands.LocalUser"
                && inputData.GetType().GetProperty("SID", typeof(SecurityIdentifier)) is PropertyInfo localUserSidProperty)
            {
                return localUserSidProperty.GetValue(inputData)
                    ?? throw new InvalidOperationException("The given LocalUser did not provide the required SID property.");
            }
            else if (inputData is string str && TryParseIdentityReference(str, out IdentityReference strIdentity))
            {
                return strIdentity;
            }
            else
            {
                throw new ArgumentException("Input data must be of type string, IdentityReference, WindowsIdentity or a string representing any of those types, or a WellKnownSidType value.", nameof(inputData));
            }
        }

        /// <summary>
        /// Tries to parse a string as <see cref="IdentityReference"/>, returning the corresponding object.
        /// </summary>
        /// <param name="identityString">The string to parse.</param>
        /// <param name="identity">The <see cref="IdentityReference"/> if parsing succeeds; otherwise NullSid</param>
        /// <returns>True if the input string was successfully parsed  name; otherwise, false.</returns>
        private static bool TryParseIdentityReference(string identityString, out IdentityReference identity)
        {
            identity = new SecurityIdentifier(WellKnownSidType.NullSid, null);
            if (string.IsNullOrWhiteSpace(identityString))
            {
                return false;
            }

            // Try Enum
            if (Enum.TryParse(identityString, true, out WellKnownSidType wellKnownSidType))
            {
                identity = new SecurityIdentifier(wellKnownSidType, null);
                return true;
            }

            // Try SID
            if (identityString.StartsWith("S-", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    identity = new SecurityIdentifier(identityString);
                    return true;
                }
                catch (ArgumentException)
                {
                }
            }

            // Try NTAccount
            try
            {
                identity = new NTAccount(identityString);
                return true;
            }
            catch (IdentityNotMappedException)
            {
            }

            return false;
        }
    }
}
