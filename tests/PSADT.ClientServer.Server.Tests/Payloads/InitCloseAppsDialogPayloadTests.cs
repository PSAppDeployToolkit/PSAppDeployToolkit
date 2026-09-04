using System.Collections.ObjectModel;
using PSADT.ClientServer.Payloads;
using PSADT.ProcessManagement;
using Xunit;

namespace PSADT.ClientServer.Server.Tests.Payloads
{
    /// <summary>
    /// Tests the payload telling a client which applications a close-apps dialog is about.
    /// </summary>
    /// <remarks>
    /// The only payload carrying a collection, and so the only one whose comparison depends on that
    /// collection comparing by its contents. A record compares each of its fields, and every collection the
    /// framework offers compares by reference, so a payload holding one directly never equals another
    /// listing the same applications however alike the two are.
    /// <para>
    /// It is also the only payload that can carry nothing at all, which the client treats as a distinct
    /// state rather than as an empty list: a later prompt is refused outright when no definitions were
    /// given, where an empty list is a dialog about no applications. So nothing and an empty list have to
    /// compare as different things.
    /// </para>
    /// </remarks>
    public sealed class InitCloseAppsDialogPayloadTests
    {
        /// <summary>
        /// Verifies that the definitions it was built with are the definitions it carries.
        /// </summary>
        [Fact]
        public void InitCloseAppsDialogPayload_CarriesItsProcessDefinitions()
        {
            // Arrange
            InitCloseAppsDialogPayload payload = new(Definitions());

            // Assert
            Assert.NotNull(payload.ProcessDefinitions);
            Assert.Equal(2, payload.ProcessDefinitions.Count);
            Assert.Equal(new ProcessDefinition("notepad", "Notepad"), payload.ProcessDefinitions[0]);
        }

        /// <summary>
        /// Verifies that two payloads listing the same applications are the same, and two listing different
        /// ones are not.
        /// </summary>
        [Fact]
        public void InitCloseAppsDialogPayload_ComparesByItsProcessDefinitions()
        {
            Assert.Equal(new InitCloseAppsDialogPayload(Definitions()), new InitCloseAppsDialogPayload(Definitions()));
            Assert.Equal(new InitCloseAppsDialogPayload(Definitions()).GetHashCode(), new InitCloseAppsDialogPayload(Definitions()).GetHashCode());
            Assert.NotEqual(new InitCloseAppsDialogPayload(Definitions()), new InitCloseAppsDialogPayload(new ReadOnlyCollection<ProcessDefinition>([new("notepad", "Notepad")])));
        }

        /// <summary>
        /// Verifies that carrying nothing is not the same as carrying an empty list, since the client acts
        /// differently on each.
        /// </summary>
        [Fact]
        public void InitCloseAppsDialogPayload_TellsNothingApartFromAnEmptyList()
        {
            Assert.Equal(new InitCloseAppsDialogPayload(processDefinitions: null), new InitCloseAppsDialogPayload(processDefinitions: null));
            Assert.NotEqual(new InitCloseAppsDialogPayload(processDefinitions: null), new InitCloseAppsDialogPayload(new ReadOnlyCollection<ProcessDefinition>([])));
            Assert.Equal(new InitCloseAppsDialogPayload(new ReadOnlyCollection<ProcessDefinition>([])), new InitCloseAppsDialogPayload(new ReadOnlyCollection<ProcessDefinition>([])));
        }

        /// <summary>
        /// Verifies that it survives the trip to the client with its definitions intact.
        /// </summary>
        [Fact]
        public void InitCloseAppsDialogPayload_SurvivesTheTripToTheClient()
        {
            // Arrange
            InitCloseAppsDialogPayload original = new(Definitions());

            // Act
            InitCloseAppsDialogPayload restored = DataSerialization.DeserializeFromBytes<InitCloseAppsDialogPayload>(DataSerialization.SerializeToBytes(original));

            // Assert
            Assert.Equal(original, restored);
            Assert.NotNull(restored.ProcessDefinitions);
            Assert.Equal(2, restored.ProcessDefinitions.Count);
        }

        /// <summary>
        /// Verifies that a payload carrying nothing survives the trip still carrying nothing.
        /// </summary>
        [Fact]
        public void InitCloseAppsDialogPayload_SurvivesTheTripCarryingNothing()
        {
            // Arrange
            InitCloseAppsDialogPayload original = new(processDefinitions: null);

            // Act
            InitCloseAppsDialogPayload restored = DataSerialization.DeserializeFromBytes<InitCloseAppsDialogPayload>(DataSerialization.SerializeToBytes(original));

            // Assert
            Assert.Equal(original, restored);
            Assert.Null(restored.ProcessDefinitions);
        }

        /// <summary>
        /// Builds a fresh list of definitions, so that two payloads compared against each other are holding
        /// different collections carrying the same things.
        /// </summary>
        /// <returns>The definitions.</returns>
        private static ReadOnlyCollection<ProcessDefinition> Definitions()
        {
            return new([new("notepad", "Notepad"), new("calc", "Calculator")]);
        }
    }
}
