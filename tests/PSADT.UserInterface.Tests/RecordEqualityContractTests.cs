using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PSADT.UserInterface.DialogOptions;
using Xunit;

namespace PSADT.UserInterface.Tests
{
    /// <summary>
    /// Holds every record in this assembly to the guarantee a record makes.
    /// </summary>
    /// <remarks>
    /// Not a test of one class. Declaring a type a record is a promise that two of them describing the
    /// same thing compare equal, and the compiler generates that comparison over the type's instance
    /// fields - so a single field whose own type compares by reference silently reduces the whole record
    /// to reference equality.
    /// <para>
    /// It matters more here than almost anywhere else in the codebase. These options are built in one
    /// process and compared in another after a round trip, so a record that has quietly fallen back to
    /// reference equality does not merely compare oddly - it never compares equal again, and nothing
    /// about adding a field to a record prompts anyone to think about it.
    /// </para>
    /// <para>
    /// The same sweep exists in <c>PSAppDeployToolkit.Tests</c>, where it was written after
    /// <c>LogEntry</c> turned out to have one <see cref="System.IO.FileInfo"/> among nine values. This
    /// assembly's records were built with that already known, so the three reference-typed surfaces here
    /// - a culture, a list and a nested map - are already stored as a name, a <c>ValueList</c> and a
    /// <c>ValueDictionary</c>. This is what keeps the next one from being different.
    /// </para>
    /// </remarks>
    public sealed class RecordEqualityContractTests
    {
        /// <summary>
        /// Verifies that no record in this assembly holds a field that compares by reference.
        /// </summary>
        /// <remarks>
        /// Reported as one failure listing everything found rather than as a theory per field. A change
        /// that breaks this usually breaks it in several places at once - a new collection property tends
        /// to arrive alongside its siblings - and one message naming all of them is more use than the
        /// first of six.
        /// </remarks>
        [Fact]
        public void Records_DoNotHoldFieldsThatCompareByReference()
        {
            // Act
            List<string> offenders = [];
            foreach (Type record in RecordTypes())
            {
                foreach (FieldInfo instanceField in record.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (ComparesByReference(instanceField.FieldType) && !Allowed.Contains($"{record.FullName}.{instanceField.Name}"))
                    {
                        offenders.Add($"{record.Name}.{FieldDescription(instanceField)} is a {instanceField.FieldType.Name}, which compares by reference");
                    }
                }
            }

            // Assert
            Assert.True(offenders.Count is 0, $"These record members do not compare by value:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", offenders)}");
        }

        /// <summary>
        /// Verifies that this test is actually looking at something, so that a change to how records are
        /// detected cannot quietly turn it into a test of nothing.
        /// </summary>
        /// <remarks>
        /// Records are found by the <c>PrintMembers</c> method the compiler generates for them, which is
        /// an implementation detail rather than a documented one. If a future compiler stops emitting it,
        /// the sweep above would find no records, pass, and go on passing for ever. Naming the records
        /// that are known to be there is what stops that.
        /// </remarks>
        [Fact]
        public void Records_AreFoundAtAll()
        {
            // Act
            string[] found = [.. RecordTypes().Select(static t => t.Name)];
            Array.Sort(found, StringComparer.Ordinal);

            // Assert
            Assert.Equal(
                [
                    "BalloonTipOptions",
                    "BaseDialogOptions",
                    "CloseAppsDialogClassicStrings",
                    "CloseAppsDialogFluentStrings",
                    "CloseAppsDialogOptions",
                    "CloseAppsDialogStrings",
                    "CustomDialogOptions",
                    "DialogBoxOptions",
                    "HelpConsoleOptions",
                    "InputDialogOptions",
                    "ListSelectionDialogOptions",
                    "ListSelectionDialogStrings",
                    "NotifyIconOptions",
                    "ProgressDialogOptions",
                    "RestartDialogOptions",
                    "RestartDialogStrings",
                ],
                found);
        }

        /// <summary>
        /// Verifies that the types which are not records are the ones deliberately left out.
        /// </summary>
        /// <remarks>
        /// The other half of the review this sweep came out of: a type that should be a record and is not
        /// gets no equality at all, which is the same failure arrived at from the other direction. Each
        /// of these is a considered exception rather than an oversight, so the reasons are written down
        /// here instead of being absent.
        /// </remarks>
        [Fact]
        public void NonRecords_AreTheOnesDeliberatelyLeftOut()
        {
            // Arrange - every non-record type in the assembly that holds instance state, and why.
            Dictionary<string, string> expected = new(StringComparer.Ordinal)
            {
                ["CustomDialogResult"] = "hand-written equality, so that PowerShell can compare it to a bare string",
                ["CustomDialogDerivative"] = "shares its base's hand-written equality",
                ["InputDialogResult"] = "hand-written equality, matching its base",
                ["ListSelectionDialogResult"] = "hand-written equality, matching its base",
                ["CloseAppsDialogResult"] = "a TypedConstant, whose members are shared singletons",
                ["DialogBoxResult"] = "a TypedConstant, whose members are shared singletons",
                ["BaseDialogState"] = "state rather than a value; it has identity and a lifetime",
                ["CloseAppsDialogState"] = "state rather than a value; owns a service and is disposable",
            };

            // Act
            string[] unexplained =
            [
                .. typeof(BaseDialogOptions).Assembly.GetTypes()
                    .Where(static type => type.IsClass
                        && !IsCompilerGenerated(type)
                        && !IsRecord(type)
                        && type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Length > 0)
                    .Select(static type => type.Name)
                    .Where(name => !expected.ContainsKey(name)),
            ];

            // Assert
            Assert.True(unexplained.Length is 0, $"These types hold state, are not records, and have no reason recorded: {string.Join(", ", unexplained)}");
        }

        /// <summary>
        /// The records declared in the assembly under test.
        /// </summary>
        /// <returns>Every record type in the assembly, nested ones included.</returns>
        private static IEnumerable<Type> RecordTypes()
        {
            return typeof(BaseDialogOptions).Assembly.GetTypes().Where(IsRecord);
        }

        /// <summary>
        /// Determines whether a type is a record.
        /// </summary>
        /// <remarks>
        /// A record is identified by <c>PrintMembers</c>, which the compiler generates for every record
        /// and for nothing else. There is no attribute or reflection flag that says "record", so this is
        /// the usual way of asking; <see cref="Records_AreFoundAtAll"/> guards it.
        /// </remarks>
        /// <param name="type">The type to judge.</param>
        /// <returns><see langword="true"/> if it is a record; otherwise, <see langword="false"/>.</returns>
        private static bool IsRecord(Type type)
        {
            return type.GetMethod("PrintMembers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is not null;
        }

        /// <summary>
        /// Determines whether values of a type compare by reference rather than by what they hold.
        /// </summary>
        /// <remarks>
        /// The question reduces to one thing: whose <c>Equals(object)</c> runs. A reference type that does
        /// not override it inherits <see cref="object"/>'s, which is reference comparison - that catches
        /// arrays, every collection the framework offers, <see cref="System.IO.FileInfo"/>,
        /// <see cref="System.Globalization.CultureInfo"/> and the rest. A value type that does not
        /// override it inherits <see cref="ValueType"/>'s, which compares field by field, so that is fine
        /// and enums come out fine with it.
        /// <para>
        /// An interface-typed field is reported as well, since what it holds at runtime decides the answer
        /// and the declaration cannot promise anything. That is deliberate rather than conservative: a
        /// record field typed <c>IReadOnlyList&lt;T&gt;</c> is the exact shape of the bug this exists to
        /// find.
        /// </para>
        /// <para>
        /// Generic arguments are not examined. A type that overrides equality is taken at its word about
        /// how it compares what it contains - <c>ValueList&lt;T&gt;</c> compares its elements by their
        /// contents, for instance - and second-guessing that would report it wrongly.
        /// </para>
        /// </remarks>
        /// <param name="type">The field type to judge.</param>
        /// <returns><see langword="true"/> if it compares by reference; otherwise, <see langword="false"/>.</returns>
        private static bool ComparesByReference(Type type)
        {
            return type.IsInterface
                || type.GetMethod("Equals", BindingFlags.Instance | BindingFlags.Public, binder: null, [typeof(object)], modifiers: null)?.DeclaringType == typeof(object);
        }

        /// <summary>
        /// Describes a field by the property it backs, where it backs one.
        /// </summary>
        /// <remarks>
        /// An auto-property's backing field is named <c>&lt;Name&gt;k__BackingField</c>, which is not what
        /// anyone reading a failure is looking for.
        /// </remarks>
        /// <param name="field">The field to describe.</param>
        /// <returns>The property name where the field backs one, or the field's own name.</returns>
        private static string FieldDescription(FieldInfo field)
        {
            return field.Name.StartsWith('<') && field.Name.IndexOf('>', StringComparison.Ordinal) is int end && end > 1
                ? field.Name[1..end]
                : field.Name;
        }

        /// <summary>
        /// The members allowed to compare by reference, each with the reason it is allowed.
        /// </summary>
        /// <remarks>
        /// Empty, and worth keeping that way. An entry here should be argued for in review rather than
        /// added to make this pass.
        /// </remarks>
        private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal);

        /// <summary>
        /// Determines whether a type was emitted by the compiler rather than written.
        /// </summary>
        /// <remarks>
        /// Iterator state machines, closures and the like hold fields and are not records, so they would
        /// otherwise have to be listed as deliberate exceptions.
        /// </remarks>
        /// <param name="type">The type to judge.</param>
        /// <returns><see langword="true"/> if the compiler generated it; otherwise, <see langword="false"/>.</returns>
        private static bool IsCompilerGenerated(Type type)
        {
            return Attribute.IsDefined(type, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute))
                || type.Name.Contains('<', StringComparison.Ordinal);
        }
    }
}
