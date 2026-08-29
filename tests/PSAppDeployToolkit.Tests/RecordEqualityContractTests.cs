using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PSAppDeployToolkit.Foundation;
using Xunit;

namespace PSAppDeployToolkit.Tests
{
    /// <summary>
    /// Holds every record in this assembly to the guarantee a record makes.
    /// </summary>
    /// <remarks>
    /// Not a test of one class. Declaring a type a record is a promise that two of them describing the same thing
    /// compare equal, and the compiler generates that comparison over the type's instance fields - so a single
    /// field whose own type compares by reference silently reduces the whole record to reference equality. That is
    /// how <c language="csharp">LogEntry</c> came to have a broken comparison: one <see cref="System.IO.FileInfo"/> among nine
    /// values.
    /// <para>
    /// Reading the source found that one. This finds the next one, which is the point: the failure mode is
    /// invisible at the declaration and nothing about adding a field to a record prompts anyone to think about
    /// it. Anything genuinely intended goes in <see cref="Allowed"/> with its reason, so the exceptions are
    /// written down rather than merely absent.
    /// </para>
    /// </remarks>
    public sealed class RecordEqualityContractTests
    {
        /// <summary>
        /// Verifies that no record in this assembly holds a field that compares by reference.
        /// </summary>
        /// <remarks>
        /// Reported as one failure listing everything found rather than as a theory per field. A change that
        /// breaks this usually breaks it in several places at once - a new collection property is typically added
        /// alongside its siblings - and one message naming all of them is more use than the first of six.
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
        /// Verifies that this test is actually looking at something, so that a change to how records are detected
        /// cannot quietly turn it into a test of nothing.
        /// </summary>
        /// <remarks>
        /// Records are found by the <c language="csharp">PrintMembers</c> method the compiler generates for them, which is an
        /// implementation detail rather than a documented one. If a future compiler stops emitting it, the sweep
        /// above would find no records, pass, and go on passing for ever. Naming the records that are known to be
        /// there is what stops that.
        /// </remarks>
        [Fact]
        public void Records_AreFoundAtAll()
        {
            // Act
            string[] found = [.. RecordTypes().Select(static t => t.Name)];
            Array.Sort(found, StringComparer.Ordinal);

            // Assert
            Assert.Equal(["DeferHistory", "LogEntry"], found);
        }

        /// <summary>
        /// The records declared in the assembly under test.
        /// </summary>
        /// <remarks>
        /// A record is identified by <c language="csharp">PrintMembers</c>, which the compiler generates for every record and for
        /// nothing else. There is no attribute or reflection flag that says "record", so this is the usual way of
        /// asking; <see cref="Records_AreFoundAtAll"/> guards it.
        /// </remarks>
        /// <returns>Every record type in the assembly, nested ones included.</returns>
        private static IEnumerable<Type> RecordTypes()
        {
            return typeof(EnvironmentTable).Assembly.GetTypes()
                .Where(static t => t.GetMethod("PrintMembers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is not null);
        }

        /// <summary>
        /// Determines whether values of a type compare by reference rather than by what they hold.
        /// </summary>
        /// <remarks>
        /// The question reduces to one thing: whose <c language="csharp">Equals(object)</c> runs. A reference type that does not
        /// override it inherits <see cref="object"/>'s, which is reference comparison - that catches arrays, every
        /// collection the framework offers, <see cref="System.IO.FileInfo"/>, <see cref="System.IO.DirectoryInfo"/>,
        /// <see cref="System.Text.RegularExpressions.Regex"/> and the rest. A value type that does not override it
        /// inherits <see cref="ValueType"/>'s, which compares field by field, so that is fine and enums come out
        /// fine with it.
        /// <para>
        /// An interface-typed field is reported as well, since what it holds at runtime decides the answer and the
        /// declaration cannot promise anything. That is deliberate rather than conservative: a record field typed
        /// <c language="csharp">IReadOnlyList&lt;T&gt;</c> is the exact shape of the bug this exists to find.
        /// </para>
        /// <para>
        /// Generic arguments are not examined. A type that overrides equality is taken at its word about how it
        /// compares what it contains - <c language="csharp">ValueList&lt;T&gt;</c> compares array elements by their contents, for
        /// instance - and second-guessing that would report it wrongly.
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
        /// An auto-property's backing field is named <c language="csharp">&lt;Name&gt;k__BackingField</c>, which is not what anyone
        /// reading a failure is looking for.
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
        /// Empty, and worth keeping that way. Every reference-comparing member found in this assembly was either
        /// fixed - <c>LogEntry</c> now records its caller as a path and rebuilds the file on each
        /// read - or belonged to a type that turned out not to be a value at all, which is why
        /// <see cref="EnvironmentTable"/> is no longer a record. An entry here should be argued for in review
        /// rather than added to make this pass.
        /// </remarks>
        private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal);
    }
}
