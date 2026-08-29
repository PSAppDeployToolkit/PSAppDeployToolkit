using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PSADT.UserInterface.DialogOptions;
using PSAppDeployToolkit.Foundation;
using Xunit;

namespace PSADT.UserInterface.Tests
{
    /// <summary>
    /// Holds every options type to the rules they all share.
    /// </summary>
    /// <remarks>
    /// Not a test of one class. All eleven options types are built the same way - from an
    /// <see cref="IDictionary"/> the PowerShell module assembles - and the same two things can go wrong
    /// with any of them regardless of what keys they read. Checking those two things once per type here
    /// keeps the eleven per-type files about what each type actually adds, and means a twelfth options
    /// type is covered the moment it is written.
    /// <para>
    /// That is not hypothetical: <c>NotifyIconOptions</c> was the one type that indexed its dictionary
    /// before checking it for null, which is precisely the kind of single-type omission a per-type file
    /// is unlikely to catch, because whoever writes it copies the type's own source to know what to test.
    /// </para>
    /// </remarks>
    public sealed class DialogOptionsContractTests
    {
        /// <summary>
        /// Verifies that every options type refuses a null dictionary as a null argument.
        /// </summary>
        /// <remarks>
        /// Reported as one failure naming every offender rather than as a theory per type, because a
        /// change to how the dictionary is read tends to affect several at once and one message listing
        /// all of them is more use than the first.
        /// </remarks>
        [Fact]
        public void EveryOptionsType_RefusesANullDictionary()
        {
            // Act
            List<string> offenders = [];
            foreach (Type type in OptionsTypes())
            {
                if (ThrownBy(type, dictionary: null) is Exception thrown and not ArgumentNullException)
                {
                    offenders.Add($"{type.Name} threw {thrown.GetType().Name}");
                }
            }

            // Assert
            Assert.True(offenders.Count is 0, $"These types do not report a null dictionary as ArgumentNullException:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", offenders)}");
        }

        /// <summary>
        /// Verifies that every options type refuses an empty dictionary by naming a missing key.
        /// </summary>
        /// <remarks>
        /// An empty dictionary is what a caller gets when the module's configuration has not been loaded,
        /// and every one of these types requires at least one key. What is being checked is that the
        /// failure arrives as <see cref="ArgumentNullException"/> naming the key rather than as an
        /// <see cref="InvalidCastException"/> from casting a null to a value type, or a
        /// <see cref="NullReferenceException"/> from indexing into a nested table that was not there.
        /// </remarks>
        [Fact]
        public void EveryOptionsType_RefusesAnEmptyDictionaryByNamingAKey()
        {
            // Act
            List<string> offenders = [];
            foreach (Type type in OptionsTypes())
            {
                if (ThrownBy(type, new Hashtable()) is not ArgumentNullException thrown)
                {
                    offenders.Add($"{type.Name} threw {ThrownBy(type, new Hashtable())?.GetType().Name ?? "nothing"}");
                }
                else
                {
                    Assert.NotEmpty(thrown.Message);
                }
            }

            // Assert
            Assert.True(offenders.Count is 0, $"These types do not report an empty dictionary as ArgumentNullException:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", offenders)}");
        }

        /// <summary>
        /// Verifies that this sweep is actually looking at every options type there is.
        /// </summary>
        /// <remarks>
        /// The types are found by asking the assembly, so an options type added later is swept without
        /// anyone remembering to add it. The cost of that is a sweep that would quietly cover nothing if
        /// the search were ever broken, which is what naming them here prevents.
        /// </remarks>
        [Fact]
        public void OptionsTypes_AreFoundAtAll()
        {
            // Act
            string[] found = [.. OptionsTypes().Select(static type => type.Name)];
            Array.Sort(found, StringComparer.Ordinal);

            // Assert
            Assert.Equal(
                [
                    "BalloonTipOptions",
                    "CloseAppsDialogOptions",
                    "CustomDialogOptions",
                    "DialogBoxOptions",
                    "HelpConsoleOptions",
                    "InputDialogOptions",
                    "ListSelectionDialogOptions",
                    "NotifyIconOptions",
                    "ProgressDialogOptions",
                    "RestartDialogOptions",
                ],
                found);
        }

        /// <summary>
        /// Every concrete options type in the assembly under test.
        /// </summary>
        /// <remarks>
        /// Identified by implementing the marker interface all of them carry, minus the abstract ones
        /// that exist only to be inherited.
        /// </remarks>
        /// <returns>The types.</returns>
        private static Type[] OptionsTypes()
        {
            return
            [
                .. typeof(BaseDialogOptions).Assembly.GetTypes()
                    .Where(static type => !type.IsAbstract && type.GetInterface("IDialogOptions") is not null),
            ];
        }

        /// <summary>
        /// Builds one options type from the given dictionary and returns whatever came out.
        /// </summary>
        /// <remarks>
        /// The two types taking a <see cref="DeploymentType"/> alongside the dictionary are handled by
        /// picking whichever public constructor ends in an <see cref="IDictionary"/> and filling any
        /// earlier parameter with its default, rather than by naming those two types here - so a third
        /// one gaining a leading argument does not silently drop out of the sweep.
        /// </remarks>
        /// <param name="type">The options type to build.</param>
        /// <param name="dictionary">The dictionary to build it from, or null.</param>
        /// <returns>The exception it raised, or null if it somehow built one.</returns>
        private static Exception? ThrownBy(Type type, IDictionary? dictionary)
        {
            ConstructorInfo constructor = type.GetConstructors()
                .Single(static c => c.GetParameters() is ParameterInfo[] parameters
                    && parameters.Length > 0
                    && parameters[^1].ParameterType == typeof(IDictionary));
            object?[] arguments = [.. constructor.GetParameters().Select(static p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null)];
            arguments[^1] = dictionary;
            try
            {
                _ = constructor.Invoke(arguments);
                return null;
            }
            catch (TargetInvocationException ex)
            {
                return ex.InnerException;
            }
        }
    }
}
