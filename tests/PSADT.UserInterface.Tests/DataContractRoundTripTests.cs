using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;
using PSADT.UserInterface.Tests.TestHelpers;
using PSAppDeployToolkit.Foundation;
using Xunit;

namespace PSADT.UserInterface.Tests
{
    /// <summary>
    /// Holds every serializable type in this assembly to surviving the journey it is built for.
    /// </summary>
    /// <remarks>
    /// Nothing in this project displays a dialog. The types here are built in the deployment process and
    /// read in a client running in the user's session, with a <see cref="DataContractSerializer"/> and a
    /// named pipe in between, so a type that does not round trip is a type that does not work - and the
    /// failure appears at the far end of a process boundary rather than where the mistake was made.
    /// <para>
    /// Equality after the trip is the assertion rather than a field-by-field comparison, which is what
    /// makes this worth running against every type at once: these types already have to compare by value
    /// for other reasons, so equality is the strongest single statement available about whether
    /// everything came back.
    /// </para>
    /// <para>
    /// Written as facts that collect every failure rather than as theories, because a change to how these
    /// are serialized tends to break several at once and one message naming all of them is more use than
    /// the first of ten.
    /// </para>
    /// </remarks>
    public sealed class DataContractRoundTripTests
    {
        /// <summary>
        /// Verifies that every options type comes back equal to what was sent.
        /// </summary>
        [Fact]
        public void Options_ComeBackEqualToWhatWasSent()
        {
            // Act
            List<string> offenders = [];
            foreach (object original in EveryOptions())
            {
                object restored = RoundTrip(original, original.GetType());
                if (!original.Equals(restored))
                {
                    offenders.Add(original.GetType().Name);
                }
            }

            // Assert
            Assert.True(offenders.Count is 0, $"These options did not come back equal to what was sent: {string.Join(", ", offenders)}");
        }

        /// <summary>
        /// Verifies that every dialog result comes back equal to what was sent.
        /// </summary>
        /// <remarks>
        /// The results travel the other way, from the client back to the deployment. Each is a singleton
        /// in practice, so what is really being checked is that a rebuilt instance compares equal to the
        /// shared one the module will test it against - a result that came back only reference-equal
        /// would fail every comparison the module makes.
        /// </remarks>
        [Fact]
        public void Results_ComeBackEqualToWhatWasSent()
        {
            // Act
            List<string> offenders = [];
            foreach (object original in EveryResult())
            {
                object restored = RoundTrip(original, original.GetType());
                if (!original.Equals(restored) || ReferenceEquals(original, restored))
                {
                    offenders.Add(original.GetType().Name);
                }
            }

            // Assert
            Assert.True(offenders.Count is 0, $"These results did not come back as an equal but separate instance: {string.Join(", ", offenders)}");
        }

        /// <summary>
        /// Verifies that a derived options type survives being sent as its base type.
        /// </summary>
        /// <remarks>
        /// This is how they actually travel: the payload carrying a dialog request declares the base type,
        /// so the serializer has to resolve the concrete one from the <see cref="KnownTypeAttribute"/>
        /// list. <c language="csharp">BaseDialogOptions</c> names eight known types and neither
        /// <see cref="InputDialogOptions"/> nor <see cref="ListSelectionDialogOptions"/> is among them -
        /// they are reachable only through <see cref="CustomDialogOptions"/>'s own list. That the chain is
        /// followed rather than only its first level is the thing worth proving.
        /// </remarks>
        [Fact]
        public void Options_SurviveBeingSentAsTheirBaseType()
        {
            foreach (object original in EveryOptions().Where(static o => o is BaseDialogOptions))
            {
                // Act
                object restored = RoundTrip(original, typeof(BaseDialogOptions));

                // Assert
                Assert.IsType(original.GetType(), restored);
                Assert.Equal(original, restored);
            }
        }

        /// <summary>
        /// Verifies that a derived result survives being sent as its base type.
        /// </summary>
        /// <remarks>
        /// The same question on the return path, where the module declares the result as
        /// <see cref="CustomDialogResult"/> and gets back whichever derivative the dialog produced.
        /// </remarks>
        [Fact]
        public void Results_SurviveBeingSentAsTheirBaseType()
        {
            foreach (object original in EveryResult().Where(static r => r is CustomDialogResult))
            {
                // Act
                object restored = RoundTrip(original, typeof(CustomDialogResult));

                // Assert
                Assert.IsType(original.GetType(), restored);
                Assert.Equal(original, restored);
            }
        }

        /// <summary>
        /// Verifies that a derived result carries its result value exactly once.
        /// </summary>
        /// <remarks>
        /// <c language="csharp">CustomDialogDerivative</c> re-exposes the base type's non-public field as a property rather
        /// than declaring a second field of its own. A field would be a second
        /// <see cref="DataMemberAttribute"/> of the same name, and the value would then be written twice
        /// into every input and list-selection result that crosses the pipe. It round trips either way,
        /// which is exactly why this needs asserting rather than assuming - the difference is visible only
        /// in the XML.
        /// </remarks>
        /// <param name="typeName">The derived result type to serialize.</param>
        [Theory]
        [InlineData(nameof(InputDialogResult))]
        [InlineData(nameof(ListSelectionDialogResult))]
        public void Results_CarryTheirResultValueOnlyOnce(string typeName)
        {
            // Arrange
            object original = EveryResult().Single(o => string.Equals(o.GetType().Name, typeName, StringComparison.Ordinal));

            // Act
            string xml = Serialize(original, original.GetType());

            // Assert
            Assert.Equal(1, Occurrences(xml, "<Result>"));
        }

        /// <summary>
        /// Verifies that this sweep covers every serializable type the assembly declares.
        /// </summary>
        /// <remarks>
        /// The instances below are built by hand, because each type needs a valid set of values that
        /// cannot be conjured generically. That makes forgetting one easy, so the hand-written list is
        /// compared against what the assembly actually declares. The dialog state types are excluded by
        /// not carrying a data contract at all: state stays in the process that owns the dialog and is
        /// never sent.
        /// </remarks>
        [Fact]
        public void SerializableTypes_AreAllCovered()
        {
            // Arrange - the nested strings records travel inside their parent rather than on their own,
            // and the two abstract bases are covered through their derivatives.
            HashSet<string> coveredWithinAParent = new(StringComparer.Ordinal)
            {
                "CloseAppsDialogStrings",
                "CloseAppsDialogClassicStrings",
                "CloseAppsDialogFluentStrings",
                "ListSelectionDialogStrings",
                "RestartDialogStrings",
            };

            // Act
            HashSet<string> covered = new(EveryOptions().Concat(EveryResult()).Select(static value => value.GetType().Name), StringComparer.Ordinal);
            string[] missing =
            [
                .. typeof(BaseDialogOptions).Assembly.GetTypes()
                    .Where(static type => !type.IsAbstract && Attribute.IsDefined(type, typeof(DataContractAttribute)))
                    .Select(static type => type.Name)
                    .Where(name => !covered.Contains(name) && !coveredWithinAParent.Contains(name)),
            ];

            // Assert
            Assert.True(missing.Length is 0, $"These serializable types have no round trip case: {string.Join(", ", missing)}");
        }

        /// <summary>
        /// One instance of every options type, each built from the smallest valid dictionary.
        /// </summary>
        /// <returns>The instances.</returns>
        private static object[] EveryOptions()
        {
            return
            [
                new BalloonTipOptions(SampleOptions.BalloonTip()),
                new NotifyIconOptions(SampleOptions.NotifyIcon()),
                new DialogBoxOptions(SampleOptions.DialogBox()),
                new HelpConsoleOptions(SampleOptions.HelpConsole()),
                new ProgressDialogOptions(SampleOptions.ProgressDialog()),
                new CustomDialogOptions(SampleOptions.CustomDialog()),
                new InputDialogOptions(SampleOptions.InputDialog()),
                new ListSelectionDialogOptions(SampleOptions.ListSelectionDialog()),
                new CloseAppsDialogOptions(DeploymentType.Install, SampleOptions.CloseAppsDialog()),
                new RestartDialogOptions(DeploymentType.Install, SampleOptions.RestartDialog()),
            ];
        }

        /// <summary>
        /// One instance of every dialog result type.
        /// </summary>
        /// <returns>The instances.</returns>
        private static object[] EveryResult()
        {
            return
            [
                CustomDialogResult.DefaultResult,
                InputDialogResult.DefaultResult,
                ListSelectionDialogResult.DefaultResult,
                CloseAppsDialogResult.Defer,
                DialogBoxResult.Yes,
            ];
        }

        /// <summary>
        /// Sends an object through a serializer and back.
        /// </summary>
        /// <param name="value">The object to send.</param>
        /// <param name="declaredType">The type to declare it as, which decides whether known types have to
        /// be resolved.</param>
        /// <returns>What came back.</returns>
        private static object RoundTrip(object value, Type declaredType)
        {
            DataContractSerializer serializer = new(declaredType);
            using MemoryStream stream = new();
            serializer.WriteObject(stream, value);
            stream.Position = 0;

            // Assigned through a local rather than cast inline: the two target frameworks disagree on
            // whether ReadObject's return is nullable, so a null-forgiving operator is necessary on one
            // and flagged as redundant on the other.
            object? restored = serializer.ReadObject(stream);
            Assert.NotNull(restored);
            return restored;
        }

        /// <summary>
        /// Serializes an object and returns the XML, for the cases about the shape on the wire rather than
        /// about what comes back.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="declaredType">The type to declare it as.</param>
        /// <returns>The XML.</returns>
        private static string Serialize(object value, Type declaredType)
        {
            DataContractSerializer serializer = new(declaredType);
            using MemoryStream stream = new();
            serializer.WriteObject(stream, value);
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }

        /// <summary>
        /// Counts non-overlapping occurrences of one string within another.
        /// </summary>
        /// <remarks>
        /// Hand-rolled because the string overload of <c language="csharp">Split</c> that would express this does not exist
        /// on .NET Framework.
        /// </remarks>
        /// <param name="haystack">The text to search.</param>
        /// <param name="needle">The text to count.</param>
        /// <returns>The number of occurrences.</returns>
        private static int Occurrences(string haystack, string needle)
        {
            int count = 0;
            for (int index = haystack.IndexOf(needle, StringComparison.Ordinal); index >= 0; index = haystack.IndexOf(needle, index + needle.Length, StringComparison.Ordinal))
            {
                count++;
            }
            return count;
        }
    }
}
