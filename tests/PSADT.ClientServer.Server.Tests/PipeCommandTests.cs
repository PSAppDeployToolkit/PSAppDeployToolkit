using System;
using System.Linq;
using PSADT.ClientServer.Server.Tests.TestHelpers;
using Xunit;

namespace PSADT.ClientServer.Server.Tests
{
    /// <summary>
    /// Tests the enumeration naming the commands a server sends its client.
    /// </summary>
    /// <remarks>
    /// A command is not written to the pipe as an integer. <c language="csharp">ServerInstance</c> casts it to a byte and
    /// makes that the first byte of the request, and the client reads it back the same way. Everything
    /// asserted here is about that: the cast has to be lossless, and two commands must not share a value,
    /// because either fault would have the client obey a different command from the one it was sent
    /// rather than fail.
    /// </remarks>
    public sealed class PipeCommandTests
    {
        /// <summary>
        /// Verifies that every command survives being narrowed to the single byte the wire carries.
        /// </summary>
        /// <remarks>
        /// The narrowing is unchecked, so a command numbered above 255 would not fail - it would arrive
        /// as whatever the low byte happens to be, which is another command. There is room for well over
        /// a hundred more before that becomes possible, so this is a guard against a future addition
        /// rather than a report about the present.
        /// </remarks>
        [Fact]
        public void PipeCommand_FitsInASingleByte()
        {
            foreach (PipeCommand command in EnumValues.Declared<PipeCommand>())
            {
                Assert.InRange((int)command, byte.MinValue, byte.MaxValue);
                Assert.Equal(command, (PipeCommand)(byte)command);
            }
        }

        /// <summary>
        /// Verifies that no two commands share a value, since the wire carries the value and not the name.
        /// </summary>
        [Fact]
        public void PipeCommand_HasNoDuplicateValues()
        {
            PipeCommand[] declared = EnumValues.Declared<PipeCommand>();
            Assert.Equal(declared.Length, declared.Distinct().Count());
        }

        /// <summary>
        /// Verifies that the enumeration is stored as an <see cref="int"/>, which is what the cast on the
        /// sending side and the client's own parse both assume.
        /// </summary>
        [Fact]
        public void PipeCommand_IsStoredAsAnInteger()
        {
            Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(PipeCommand)));
        }
    }
}
