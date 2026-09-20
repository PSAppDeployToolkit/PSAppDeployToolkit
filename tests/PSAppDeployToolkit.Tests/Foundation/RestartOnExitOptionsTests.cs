using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using PSAppDeployToolkit.Foundation;
using Xunit;

namespace PSAppDeployToolkit.Tests.Foundation
{
    /// <summary>
    /// Tests the restart a deployment arms for the moment it exits.
    /// </summary>
    /// <remarks>
    /// This is not read in-process. It is handed to the client executable as serialized options, which then waits
    /// out the countdown and restarts the device, so the journey matters as much as the constructor: a member the
    /// serializer was not told to carry is dropped in silence and the client restarts on a CLR default instead of
    /// on what the deployment asked for.
    /// <para>
    /// Nothing else covers that here. The sweep in PSADT.ClientServer.Server.Tests holds every payload crossing
    /// the pipe to this same rule, but this type is not one of them - it travels as an argument to a standalone
    /// client invocation - so its contract is pinned below instead.
    /// </para>
    /// </remarks>
    public sealed class RestartOnExitOptionsTests
    {
        /// <summary>
        /// Verifies that the values handed in are the ones read back.
        /// </summary>
        [Fact]
        public void Constructor_KeepsWhatItIsGiven()
        {
            // Act
            RestartOnExitOptions data = new(TimeSpan.FromMinutes(5), "a maintenance window", noForceCloseApps: true);

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(5), data.Countdown);
            Assert.Equal("a maintenance window", data.Reason);
            Assert.True(data.NoForceCloseApps);
        }

        /// <summary>
        /// Verifies that no reason at all is accepted, since the parameter that supplies one is optional.
        /// </summary>
        [Fact]
        public void Constructor_AcceptsNoReasonAtAll()
        {
            // Act
            RestartOnExitOptions data = new(TimeSpan.FromSeconds(5), reason: null, noForceCloseApps: false);

            // Assert
            Assert.Null(data.Reason);
            Assert.False(data.NoForceCloseApps);
        }

        /// <summary>
        /// Verifies that a countdown of nothing or less is refused.
        /// </summary>
        /// <remarks>
        /// Zero counts as nothing here rather than as "restart now". The parameter feeding this refuses it, and a
        /// countdown that arrived as zero is far more likely to be one that was never set.
        /// </remarks>
        /// <param name="ticks">The countdown to refuse, in ticks.</param>
        [Theory]
        [InlineData(0L)]
        [InlineData(-1L)]
        [InlineData(-TimeSpan.TicksPerSecond)]
        public void Constructor_RefusesACountdownOfNothingOrLess(long ticks)
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new RestartOnExitOptions(TimeSpan.FromTicks(ticks), reason: null, noForceCloseApps: false));
        }

        /// <summary>
        /// Verifies that a reason of nothing but space is refused, which no reason at all is not.
        /// </summary>
        /// <param name="reason">The reason to refuse.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_RefusesAReasonOfNothingButSpace(string reason)
        {
            _ = Assert.Throws<ArgumentException>(() => new RestartOnExitOptions(TimeSpan.FromSeconds(5), reason, noForceCloseApps: false));
        }

        /// <summary>
        /// Verifies that the reason is held to the length the restart can actually carry.
        /// </summary>
        /// <remarks>
        /// The cap is shutdown.exe's: its '/c' switch takes 512 characters and no more. Anything longer accepted
        /// here would be refused by the restart itself, in the client, after the deployment had already exited.
        /// </remarks>
        [Fact]
        public void Constructor_RefusesAReasonLongerThanTheRestartCanCarry()
        {
            // Assert
            Assert.Equal(512, new RestartOnExitOptions(TimeSpan.FromSeconds(5), new string('a', 512), noForceCloseApps: false).Reason?.Length);
            _ = Assert.Throws<ArgumentException>(static () => new RestartOnExitOptions(TimeSpan.FromSeconds(5), new string('a', 513), noForceCloseApps: false));
        }

        /// <summary>
        /// Verifies that every part of it counts towards the comparison, since each changes the restart performed.
        /// </summary>
        [Fact]
        public void RestartOnExitOptions_ComparesByEverythingItCarries()
        {
            // Arrange
            RestartOnExitOptions data = new(TimeSpan.FromMinutes(5), "a reason", noForceCloseApps: true);

            // Assert
            Assert.Equal(data, new RestartOnExitOptions(TimeSpan.FromMinutes(5), "a reason", noForceCloseApps: true));
            Assert.NotEqual(data, new RestartOnExitOptions(TimeSpan.FromMinutes(6), "a reason", noForceCloseApps: true));
            Assert.NotEqual(data, new RestartOnExitOptions(TimeSpan.FromMinutes(5), "another reason", noForceCloseApps: true));
            Assert.NotEqual(data, new RestartOnExitOptions(TimeSpan.FromMinutes(5), reason: null, noForceCloseApps: true));
            Assert.NotEqual(data, new RestartOnExitOptions(TimeSpan.FromMinutes(5), "a reason", noForceCloseApps: false));
        }

        /// <summary>
        /// Verifies that it survives the trip to the client with every member intact.
        /// </summary>
        /// <remarks>
        /// The guard against the failure this type exists to avoid. A type the serializer cannot carry fails here
        /// outright, and a member it was not told about comes back as a default that the assertions below name.
        /// </remarks>
        [Fact]
        public void DataContract_RoundTripsEveryMember()
        {
            // Arrange
            RestartOnExitOptions original = new(TimeSpan.FromMinutes(5), "a maintenance window", noForceCloseApps: true);

            // Act
            RestartOnExitOptions restored = RoundTrip(original);

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(5), restored.Countdown);
            Assert.Equal("a maintenance window", restored.Reason);
            Assert.True(restored.NoForceCloseApps);
            Assert.Equal(original, restored);
        }

        /// <summary>
        /// Verifies that a restart armed without a reason arrives without one, rather than being refused on the
        /// way in for carrying nothing where a reason would go.
        /// </summary>
        [Fact]
        public void DataContract_RoundTripsARestartWithNoReason()
        {
            // Act
            RestartOnExitOptions restored = RoundTrip(new RestartOnExitOptions(TimeSpan.FromSeconds(30), reason: null, noForceCloseApps: false));

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(30), restored.Countdown);
            Assert.Null(restored.Reason);
            Assert.False(restored.NoForceCloseApps);
        }

        /// <summary>
        /// Verifies that data arriving with a member it requires left out is refused rather than acted on.
        /// </summary>
        /// <remarks>
        /// The refusals above are the constructor's, and nothing off the wire runs one: DataContractSerializer
        /// allocates the object and assigns the members it finds, so one the sender left out keeps its CLR
        /// default. Blanking the field before it is written is what a sender omitting it puts on the wire.
        /// </remarks>
        /// <param name="countdown">The countdown to blank to, in ticks.</param>
        /// <param name="reason">The reason to blank to.</param>
        [Theory]
        [InlineData(0L, "a reason")]
        [InlineData(-TimeSpan.TicksPerSecond, "a reason")]
        [InlineData(TimeSpan.TicksPerMinute, "   ")]
        public void DataContract_RefusesDataArrivingWithoutWhatItNeeds(long countdown, string reason)
        {
            // Arrange
            RestartOnExitOptions data = new(TimeSpan.FromMinutes(5), "a reason", noForceCloseApps: true);
            Blank(data, nameof(RestartOnExitOptions.Countdown), TimeSpan.FromTicks(countdown));
            Blank(data, nameof(RestartOnExitOptions.Reason), reason);

            // Assert
            _ = Assert.ThrowsAny<SerializationException>(() => RoundTrip(data));
        }

        /// <summary>
        /// Verifies that a reason too long for the restart is refused on arrival as well as on the way in.
        /// </summary>
        [Fact]
        public void DataContract_RefusesAReasonLongerThanTheRestartCanCarry()
        {
            // Arrange
            RestartOnExitOptions data = new(TimeSpan.FromMinutes(5), "a reason", noForceCloseApps: true);
            Blank(data, nameof(RestartOnExitOptions.Reason), new string('a', 513));

            // Assert
            _ = Assert.ThrowsAny<SerializationException>(() => RoundTrip(data));
        }

        /// <summary>
        /// Sends the data through the serializer and reads it back, as the client does.
        /// </summary>
        /// <remarks>
        /// Named as PSADT.ClientServer.Server names it, mirroring production. That assembly is not referenced
        /// here, and the contract under test is this type's rather than the serializer's wrapper around it.
        /// </remarks>
        /// <param name="data">The data to send.</param>
        /// <returns>The data as the client reads it.</returns>
        private static RestartOnExitOptions RoundTrip(RestartOnExitOptions data)
        {
            DataContractSerializer serializer = new(typeof(RestartOnExitOptions));
            using MemoryStream stream = new();
            serializer.WriteObject(stream, data);
            stream.Position = 0;
            object? deserialized = serializer.ReadObject(stream);
            Assert.NotNull(deserialized);
            return (RestartOnExitOptions)deserialized;
        }

        /// <summary>
        /// Sets one of the data's members to what a sender leaving it out would put on the wire.
        /// </summary>
        /// <param name="data">The data to alter.</param>
        /// <param name="memberName">The member to alter.</param>
        /// <param name="value">The value to give it.</param>
        private static void Blank(RestartOnExitOptions data, string memberName, object value)
        {
            FieldInfo? member = typeof(RestartOnExitOptions).GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(member);
            member.SetValue(data, value);
        }
    }
}
