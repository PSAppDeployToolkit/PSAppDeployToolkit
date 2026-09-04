using System;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace PSADT.UserInterface.Interfaces.Tests.TestHelpers
{
    /// <summary>
    /// Reaches the few members that neither <c language="xml">InternalsVisibleTo</c> nor a derived test dialog reaches.
    /// </summary>
    /// <remarks>
    /// Most of this project is internal, which the test assembly sees directly. Most of the rest is
    /// private-protected, which a test type deriving from the dialog also sees, because friend access
    /// satisfies the internal half of that accessibility - so the probe dialogs in these tests reach the
    /// tag stripper, the countdown formatter and the window procedure without any reflection at all.
    /// <para>
    /// What is left is genuinely out of reach: fields the Windows Forms designer emits as private,
    /// static helpers that are private rather than private-protected, and members of dialogs that
    /// cannot be derived from. Those are reached here. Reflection couples a test to a member's name, so
    /// it is used only where the alternative is no coverage at all.
    /// </para>
    /// <para>
    /// The Fluent dialogs are the ones that cannot be derived from, and not because they are sealed -
    /// most are, but the base is abstract and its immediate subclass is not. The obstacle is XAML:
    /// <c language="csharp">InitializeComponent</c> asks <c language="csharp">Application.LoadComponent</c> for a resource addressed by a
    /// URI naming the assembly the markup was compiled into, and that call refuses when the object it
    /// is loading into comes from a different assembly. A test type deriving from a Fluent dialog
    /// therefore throws before its constructor finishes, however the accessibility works out. The
    /// Classic dialogs have no such constraint, which is why they are tested through a derived probe
    /// and these are not.
    /// </para>
    /// </remarks>
    internal static class NonPublic
    {
        /// <summary>
        /// The flags naming a member regardless of which flavour of non-public it is.
        /// </summary>
        private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        /// <summary>
        /// Calls a static method and returns its result.
        /// </summary>
        /// <remarks>
        /// The exception a member throws is unwrapped from the <see cref="TargetInvocationException"/>
        /// reflection wraps it in and rethrown with its original stack trace, so a test can assert on
        /// what the code under test threw rather than on how it was called.
        /// </remarks>
        /// <typeparam name="TDeclaring">The type declaring the method.</typeparam>
        /// <typeparam name="TResult">The type the method returns.</typeparam>
        /// <param name="name">The method's name.</param>
        /// <param name="arguments">The arguments to pass.</param>
        /// <returns>Whatever the method returned.</returns>
        /// <exception cref="MissingMethodException">Thrown if the type declares no such method.</exception>
        public static TResult CallStatic<TDeclaring, TResult>(string name, params object?[] arguments)
        {
            MethodInfo method = typeof(TDeclaring).GetMethod(name, Any)
                ?? throw new MissingMethodException(typeof(TDeclaring).FullName, name);
            return (TResult)Unwrap(() => method.Invoke(obj: null, arguments))!;
        }

        /// <summary>
        /// Calls a static method that returns nothing.
        /// </summary>
        /// <typeparam name="TDeclaring">The type declaring the method.</typeparam>
        /// <param name="name">The method's name.</param>
        /// <param name="arguments">The arguments to pass.</param>
        /// <exception cref="MissingMethodException">Thrown if the type declares no such method.</exception>
        public static void CallStatic<TDeclaring>(string name, params object?[] arguments)
        {
            MethodInfo method = typeof(TDeclaring).GetMethod(name, Any)
                ?? throw new MissingMethodException(typeof(TDeclaring).FullName, name);
            _ = Unwrap(() => method.Invoke(obj: null, arguments));
        }

        /// <summary>
        /// Calls an instance method that returns nothing.
        /// </summary>
        /// <param name="instance">The object to call on.</param>
        /// <param name="name">The method's name.</param>
        /// <param name="arguments">The arguments to pass.</param>
        /// <exception cref="MissingMethodException">Thrown if neither the type nor its bases declare such a method.</exception>
        public static void Call(object instance, string name, params object?[] arguments)
        {
            ArgumentNullException.ThrowIfNull(instance);
            for (Type? type = instance.GetType(); type is not null; type = type.BaseType)
            {
                if (type.GetMethod(name, Any) is MethodInfo method)
                {
                    _ = Unwrap(() => method.Invoke(instance, arguments));
                    return;
                }
            }
            throw new MissingMethodException(instance.GetType().FullName, name);
        }

        /// <summary>
        /// Reads a field, including one declared by a base type.
        /// </summary>
        /// <typeparam name="TValue">The field's type.</typeparam>
        /// <param name="instance">The object to read from.</param>
        /// <param name="name">The field's name.</param>
        /// <returns>The field's value.</returns>
        /// <exception cref="MissingFieldException">Thrown if neither the type nor its bases declare such a field.</exception>
        public static TValue Field<TValue>(object instance, string name)
        {
            ArgumentNullException.ThrowIfNull(instance);
            for (Type? type = instance.GetType(); type is not null; type = type.BaseType)
            {
                if (type.GetField(name, Any) is FieldInfo field)
                {
                    return (TValue)field.GetValue(instance);
                }
            }
            throw new MissingFieldException(instance.GetType().FullName, name);
        }

        /// <summary>
        /// Reads a property, including one declared by a base type.
        /// </summary>
        /// <typeparam name="TValue">The property's type.</typeparam>
        /// <param name="instance">The object to read from.</param>
        /// <param name="name">The property's name.</param>
        /// <returns>The property's value.</returns>
        /// <exception cref="MissingMemberException">Thrown if neither the type nor its bases declare such a property.</exception>
        public static TValue Property<TValue>(object instance, string name)
        {
            ArgumentNullException.ThrowIfNull(instance);
            for (Type? type = instance.GetType(); type is not null; type = type.BaseType)
            {
                if (type.GetProperty(name, Any) is PropertyInfo property)
                {
                    return (TValue)Unwrap(() => property.GetValue(instance))!;
                }
            }
            throw new MissingMemberException(instance.GetType().FullName, name);
        }

        /// <summary>
        /// Runs a reflection call and rethrows whatever it wrapped.
        /// </summary>
        /// <param name="call">The reflection call to run.</param>
        /// <returns>Whatever the call returned.</returns>
        private static object? Unwrap(Func<object?> call)
        {
            try
            {
                return call();
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }
    }
}
