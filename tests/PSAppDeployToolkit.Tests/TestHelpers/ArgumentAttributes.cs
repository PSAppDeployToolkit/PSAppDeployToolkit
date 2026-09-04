using System;
using System.Management.Automation;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace PSAppDeployToolkit.Tests.TestHelpers
{
    /// <summary>
    /// Invokes the validation methods PowerShell declares as protected.
    /// </summary>
    /// <remarks>
    /// PowerShell calls these itself during parameter binding and exposes no public entry point, and the attributes
    /// are sealed so they cannot be subclassed to widen one. Reflection is the only way in.
    /// <para>
    /// The engine intrinsics are always passed as null. Nothing in these attributes reads them, and there is no way to
    /// construct one outside the engine.
    /// </para>
    /// </remarks>
    internal static class ArgumentAttributes
    {
        /// <summary>
        /// Runs an argument validator against a whole argument.
        /// </summary>
        /// <param name="attribute">The validator to run.</param>
        /// <param name="arguments">The argument to validate.</param>
        internal static void Validate(ValidateArgumentsAttribute attribute, object? arguments)
        {
            Invoke(attribute, nameof(Validate), [typeof(object), typeof(EngineIntrinsics)], [arguments, null]);
        }

        /// <summary>
        /// Runs an enumerated validator against one element.
        /// </summary>
        /// <param name="attribute">The validator to run.</param>
        /// <param name="element">The element to validate.</param>
        internal static void ValidateElement(ValidateEnumeratedArgumentsAttribute attribute, object? element)
        {
            Invoke(attribute, nameof(ValidateElement), [typeof(object)], [element]);
        }

        /// <summary>
        /// Calls a protected method and rethrows whatever it threw.
        /// </summary>
        /// <remarks>
        /// The rethrow matters. Reflection wraps anything the target throws in a
        /// <see cref="TargetInvocationException"/>, so a test asserting on the exception type would be asserting on
        /// reflection rather than on the validator. <see cref="ExceptionDispatchInfo"/> puts the original back with its
        /// stack intact.
        /// </remarks>
        /// <param name="attribute">The attribute to call into.</param>
        /// <param name="methodName">The method to call.</param>
        /// <param name="parameterTypes">Its parameter types, so the right overload is found.</param>
        /// <param name="arguments">The arguments to pass.</param>
        /// <exception cref="InvalidOperationException">Thrown when the method is not where it was expected to be,
        /// which means PowerShell changed its shape rather than that the code under test is wrong.</exception>
        private static void Invoke(Attribute attribute, string methodName, Type[] parameterTypes, object?[] arguments)
        {
            MethodInfo method = attribute.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic, binder: null, parameterTypes, modifiers: null)
                ?? throw new InvalidOperationException($"{attribute.GetType().Name} has no protected {methodName} taking {parameterTypes.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)} argument(s).");
            try
            {
                _ = method.Invoke(attribute, arguments);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }
    }
}
