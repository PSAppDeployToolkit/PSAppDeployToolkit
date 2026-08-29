using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace PSADT.ClientServer.Client.Tests.TestHelpers
{
    /// <summary>
    /// Reaches the members of <c>ClientExecutable</c> that <c>InternalsVisibleTo</c> does not.
    /// </summary>
    /// <remarks>
    /// The type itself is internal, so the friend grant reaches it and its one internal method. Every
    /// other member is private, and a static class cannot be derived from to reach them the way the
    /// dialog tests derive a probe. Reflection is the only route, and it is used here for that reason
    /// rather than by preference: it couples each test to a member's name.
    /// <para>
    /// Two of the members under test are generic, which the ordinary lookup cannot call - a
    /// <c>MethodInfo</c> for an open generic has to be closed over the type argument first. Those go
    /// through <see cref="CallStaticGeneric{TResult}"/>.
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
        /// <typeparam name="TResult">The type the method returns.</typeparam>
        /// <param name="declaring">The type declaring the method.</param>
        /// <param name="name">The method's name.</param>
        /// <param name="arguments">The arguments to pass.</param>
        /// <returns>Whatever the method returned.</returns>
        /// <exception cref="MissingMethodException">Thrown if the type declares no such method.</exception>
        public static TResult CallStatic<TResult>(Type declaring, string name, params object?[] arguments)
        {
            ArgumentNullException.ThrowIfNull(declaring);
            MethodInfo method = declaring.GetMethod(name, Any)
                ?? throw new MissingMethodException(declaring.FullName, name);
            return (TResult)Unwrap(() => method.Invoke(obj: null, arguments))!;
        }

        /// <summary>
        /// Calls a static method that returns nothing.
        /// </summary>
        /// <param name="declaring">The type declaring the method.</param>
        /// <param name="name">The method's name.</param>
        /// <param name="arguments">The arguments to pass.</param>
        /// <exception cref="MissingMethodException">Thrown if the type declares no such method.</exception>
        public static void CallStatic(Type declaring, string name, params object?[] arguments)
        {
            ArgumentNullException.ThrowIfNull(declaring);
            MethodInfo method = declaring.GetMethod(name, Any)
                ?? throw new MissingMethodException(declaring.FullName, name);
            _ = Unwrap(() => method.Invoke(obj: null, arguments));
        }

        /// <summary>
        /// Calls a static generic method closed over one type argument and returns its result.
        /// </summary>
        /// <typeparam name="TResult">The type the method returns once closed.</typeparam>
        /// <param name="declaring">The type declaring the method.</param>
        /// <param name="name">The method's name.</param>
        /// <param name="typeArgument">The type to close the method over.</param>
        /// <param name="arguments">The arguments to pass.</param>
        /// <returns>Whatever the method returned.</returns>
        /// <exception cref="MissingMethodException">Thrown if the type declares no such method.</exception>
        public static TResult CallStaticGeneric<TResult>(Type declaring, string name, Type typeArgument, params object?[] arguments)
        {
            ArgumentNullException.ThrowIfNull(declaring);
            MethodInfo method = declaring.GetMethod(name, Any)
                ?? throw new MissingMethodException(declaring.FullName, name);
            MethodInfo closed = method.MakeGenericMethod(typeArgument);
            return (TResult)Unwrap(() => closed.Invoke(obj: null, arguments))!;
        }

        /// <summary>
        /// Creates an instance of a non-public type.
        /// </summary>
        /// <param name="type">The type to create.</param>
        /// <returns>The new instance.</returns>
        public static object Create(Type type)
        {
            return Activator.CreateInstance(type, nonPublic: true);
        }

        /// <summary>
        /// Finds a type nested inside another.
        /// </summary>
        /// <param name="declaring">The type declaring the nested type.</param>
        /// <param name="name">The nested type's name.</param>
        /// <returns>The nested type.</returns>
        /// <exception cref="TypeLoadException">Thrown if the type declares no such nested type.</exception>
        public static Type Nested(Type declaring, string name)
        {
            ArgumentNullException.ThrowIfNull(declaring);
            return declaring.GetNestedType(name, BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new TypeLoadException($"[{declaring.FullName}] declares no nested type named [{name}].");
        }

        /// <summary>
        /// Calls an instance method and awaits it if it returned an awaitable.
        /// </summary>
        /// <remarks>
        /// The members this is used for return <see cref="ValueTask"/>, which is not an
        /// <see cref="IAsyncResult"/> and cannot be cast to <see cref="Task"/>. Rather than close over
        /// its type here, the returned object is asked for its <c>AsTask</c> and that is awaited, which
        /// works for both flavours of awaitable this project has.
        /// </remarks>
        /// <param name="instance">The object to call on.</param>
        /// <param name="name">The method's name.</param>
        /// <param name="arguments">The arguments to pass.</param>
        /// <exception cref="MissingMethodException">Thrown if neither the type nor its bases declare such a method.</exception>
        public static async Task CallAsync(object instance, string name, params object?[] arguments)
        {
            ArgumentNullException.ThrowIfNull(instance);
            for (Type? type = instance.GetType(); type is not null; type = type.BaseType)
            {
                if (type.GetMethod(name, Any) is MethodInfo method)
                {
                    object? returned = Unwrap(() => method.Invoke(instance, arguments));
                    if (returned is Task task)
                    {
                        await task.ConfigureAwait(false);
                    }
                    else if (returned?.GetType().GetMethod("AsTask", Type.EmptyTypes)?.Invoke(returned, parameters: null) is Task valueTask)
                    {
                        await valueTask.ConfigureAwait(false);
                    }
                    return;
                }
            }
            throw new MissingMethodException(instance.GetType().FullName, name);
        }

        /// <summary>
        /// Reads a property, including one declared by a base type.
        /// </summary>
        /// <typeparam name="TValue">The property's type.</typeparam>
        /// <param name="instance">The object to read from.</param>
        /// <param name="name">The property's name.</param>
        /// <returns>The property's value.</returns>
        /// <exception cref="MissingMemberException">Thrown if neither the type nor its bases declare such a property.</exception>
        public static TValue? Property<TValue>(object instance, string name)
        {
            ArgumentNullException.ThrowIfNull(instance);
            for (Type? type = instance.GetType(); type is not null; type = type.BaseType)
            {
                if (type.GetProperty(name, Any) is PropertyInfo property)
                {
                    return (TValue?)Unwrap(() => property.GetValue(instance));
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
