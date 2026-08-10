using System;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Reflection;
using System.Security.Principal;
using PSAppDeployToolkit.Utilities;

namespace PSAppDeployToolkit.Attributes
{
    /// <summary>
    /// Transforms objects into a <see cref="IdentityReference"/>.
    /// </summary>
    public sealed class IdentityReferenceTransformationAttribute : ArgumentTransformationAttribute
    {
        /// <summary>
        /// Transforms input object into base <see cref="IdentityReference"/> objects for consumption in downstream PowerShell functions.
        /// Supported types:
        ///  * <see cref="IdentityReference"/>
        ///  * <see cref="WindowsIdentity"/>
        ///  * <see cref="WellKnownSidType"/>
        ///  * LocalPrincipal
        ///  * <see cref="string"/> representing the above
        /// </summary>
        /// <param name="engineIntrinsics">The PowerShell engine intrinsics.</param>
        /// <param name="inputData">The input value to transform.</param>
        /// <returns>The identity reference the input object represents.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the input object was valid but did not contain the required data.</exception>
        /// <exception cref="ArgumentException">Thrown if the input object is not supported for transformation.</exception>
        /// <exception cref="ArgumentNullException">Thrown if the input data is null.</exception>
        [SuppressMessage("Style", "IDE0046:Use conditional expression for return", Justification = "Conditional expression would be unreadable.")]
        public override object Transform(EngineIntrinsics engineIntrinsics, object? inputData)
        {
            if (!PowerShellUtilities.TryGetBaseObject(inputData, out inputData))
            {
                throw new ArgumentNullException(paramName: nameof(inputData), "Cannot transform null to IdentityReference.");
            }
            if (inputData is IdentityReference identity)
            {
                return identity;
            }
            if (inputData is WellKnownSidType wellKnownSidType)
            {
                return new SecurityIdentifier(wellKnownSidType, domainSid: null);
            }
            if (inputData is WindowsIdentity windowsIdentity)
            {
                return windowsIdentity.User ?? throw new InvalidOperationException("The given WindowsIdentity did not provide the required user identity.");
            }
            if (inputData is string str && TryParseIdentityReference(str, out IdentityReference? strIdentity))
            {
                return strIdentity;
            }
            if (TryConvertPowerShellCommandObject(inputData, out SecurityIdentifier? pwshCommandSid))
            {
                return pwshCommandSid;
            }
            throw new ArgumentException("Input data must be of type IdentityReference, WindowsIdentity, LocalPrincipal, WellKnownSidType or a string representing any of those types.", nameof(inputData));
        }

        /// <summary>
        /// Try to parse string into an <see cref="IdentityReference"/>.
        /// </summary>
        /// <param name="identityString">The string representing the IdentityReference.</param>
        /// <param name="identity">The parsed IdentityReference.</param>
        /// <returns>True if parsing was successful, otherwise false.</returns>
        private static bool TryParseIdentityReference(string identityString, [NotNullWhen(true)] out IdentityReference? identity)
        {
            if (string.IsNullOrWhiteSpace(identityString))
            {
                identity = null;
                return false;
            }
            if (Enum.TryParse(identityString, ignoreCase: true, out WellKnownSidType wellKnownSidType))
            {
                identity = new SecurityIdentifier(wellKnownSidType, domainSid: null);
                return true;
            }
            if (TryParseSid(identityString, out SecurityIdentifier? sid))
            {
                identity = sid;
                return true;
            }
            if (TryParseNTAccount(identityString, out NTAccount? ntAccount))
            {
                identity = ntAccount;
                return true;
            }
            identity = null;
            return false;
        }

        /// <summary>
        /// Try to parse string into a <see cref="SecurityIdentifier"/>.
        /// </summary>
        /// <param name="identityString">The string representing the SecrutiyIdentifier.</param>
        /// <param name="identity">The parsed SecrutiyIdentifier.</param>
        /// <returns>True if parsing was successful, otherwise false.</returns>
        private static bool TryParseSid(string identityString, [NotNullWhen(true)] out SecurityIdentifier? identity)
        {
            try
            {
                identity = new SecurityIdentifier(identityString);
                return true;
            }
            catch (ArgumentException)
            {
                identity = null;
                return false;
            }
        }

        /// <summary>
        /// Try to parse string into a <see cref="NTAccount"/>.
        /// </summary>
        /// <param name="identityString">The string representing the NTAccount.</param>
        /// <param name="identity">The parsed NTAccount.</param>
        /// <returns>True if parsing was successful, otherwise false.</returns>
        private static bool TryParseNTAccount(string identityString, [NotNullWhen(true)] out NTAccount? identity)
        {
            try
            {
                identity = new NTAccount(identityString);
                return true;
            }
            catch (IdentityNotMappedException)
            {
                identity = null;
                return false;
            }
        }


        /// <summary>
        /// Try to extract the SID from a LocalPrincipal type using reflection. The LocalPrincipal type is used within the Microsoft.PowerShell.LocalAccounts module.
        /// </summary>
        /// <param name="identityObject">The object to analyse.</param>
        /// <param name="identity">The parsed SecrutiyIdentifier.</param>
        /// <returns>True if extraction was successful, otherwise false.</returns>
        private static bool TryConvertPowerShellCommandObject(object identityObject, [NotNullWhen(true)] out SecurityIdentifier? identity)
        {
            Type objectType = identityObject.GetType();
            if (!string.Equals(objectType.Namespace, "Microsoft.PowerShell.Commands", StringComparison.Ordinal))
            {
                identity = null;
                return false;
            }
            if (string.Equals(objectType.Name, "LocalPrincipal", StringComparison.Ordinal)
                && objectType.GetProperty("SID", typeof(SecurityIdentifier)) is PropertyInfo directProperty
                && directProperty.GetValue(identityObject) is SecurityIdentifier directSid)
            {
                identity = directSid;
                return true;
            }
            if (objectType.BaseType is Type baseType
                && string.Equals(baseType.Name, "LocalPrincipal", StringComparison.Ordinal)
                && objectType.GetProperty("SID", typeof(SecurityIdentifier)) is PropertyInfo baseProperty
                && baseProperty.GetValue(identityObject) is SecurityIdentifier baseSid)
            {
                identity = baseSid;
                return true;
            }
            identity = null;
            return false;
        }
    }
}
