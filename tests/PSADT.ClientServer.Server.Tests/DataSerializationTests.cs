using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using PSADT.ProcessManagement;
using Xunit;

namespace PSADT.ClientServer.Server.Tests
{
    /// <summary>
    /// Tests the serialization that turns everything crossing the pipe into bytes and back.
    /// </summary>
    /// <remarks>
    /// Every command, every result and every failure goes through this, so the round trip is the least of
    /// what has to hold. What gets the attention here is the two things it does that a plain data contract
    /// serializer does not.
    /// <para>
    /// The first is reading from an offset. A response is a marker byte followed by the serialized value,
    /// and the server hands the whole buffer over with an offset of one rather than copying the tail out of
    /// it, so the bounds on that offset are what stands between a malformed response and a read past the
    /// end of a buffer.
    /// </para>
    /// <para>
    /// The second is exceptions. A failure on the client is serialized, written to its standard error and
    /// rebuilt on the server, which is the only way a caller ever learns why something failed. That path
    /// has its own rules - the root element is not verified, because an exception's contract name is its
    /// concrete type rather than the one asked for - and a resolver exists solely so that the dictionary
    /// behind <see cref="Exception.Data"/> survives, since the type it actually is cannot be named in a
    /// list of known types alongside the one it shares a contract with.
    /// </para>
    /// </remarks>
    public sealed class DataSerializationTests
    {
        /// <summary>
        /// Verifies that a value survives being turned into bytes and read back.
        /// </summary>
        [Fact]
        public void Serialization_RoundTripsThroughBytes()
        {
            // Arrange
            ProcessDefinition original = new("notepad", "Notepad");

            // Act
            byte[] serialized = DataSerialization.SerializeToBytes(original);

            // Assert
            Assert.NotEmpty(serialized);
            Assert.Equal(original, DataSerialization.DeserializeFromBytes<ProcessDefinition>(serialized));
        }

        /// <summary>
        /// Verifies that a value survives the same trip through text, which is the form the client's
        /// standard error carries.
        /// </summary>
        [Fact]
        public void Serialization_RoundTripsThroughText()
        {
            // Arrange
            ProcessDefinition original = new("notepad", "Notepad");

            // Act
            string serialized = DataSerialization.SerializeToString(original);

            // Assert
            Assert.Equal(serialized, Convert.ToBase64String(Convert.FromBase64String(serialized)));
            Assert.Equal(original, DataSerialization.DeserializeFromString<ProcessDefinition>(serialized));
        }

        /// <summary>
        /// Verifies that the overloads taking a type rather than a type argument agree with the ones that
        /// take a type argument, since the client reaches for those when the type is only known at runtime.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "Calling the overload that takes a type is the point of this test; the generic one is covered above.")]
        [Fact]
        public void Serialization_RoundTripsThroughANamedType()
        {
            // Arrange
            ProcessDefinition original = new("notepad", "Notepad");
            byte[] serialized = DataSerialization.SerializeToBytes(original);

            // Assert
            Assert.Equal(original, DataSerialization.DeserializeFromBytes(serialized, typeof(ProcessDefinition)));
            Assert.Equal(original, DataSerialization.DeserializeFromString(DataSerialization.SerializeToString(original), typeof(ProcessDefinition)));
        }

        /// <summary>
        /// Verifies that nothing at all is refused rather than serialized into something a reader would
        /// have to interpret.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void SerializeToBytes_RefusesNothingAtAll()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => DataSerialization.SerializeToBytes<string>(null!));
        }

        /// <summary>
        /// Verifies that a string of nothing but space is refused.
        /// </summary>
        /// <remarks>
        /// A special case in the code, and worth keeping: several commands answer with a string, and a
        /// caller cannot tell an answer of whitespace from one that went missing. Refusing it at the point
        /// it is written means the question never arises on the far side.
        /// </remarks>
        /// <param name="value">The string to refuse.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\r\n")]
        public void SerializeToBytes_RefusesAStringOfNothing(string value)
        {
            _ = Assert.Throws<ArgumentException>(() => DataSerialization.SerializeToBytes(value));
        }

        /// <summary>
        /// Verifies that a string with something in it is serialized and read back, since the refusal above
        /// applies to the empty ones alone.
        /// </summary>
        [Fact]
        public void SerializeToBytes_AcceptsAStringWithSomethingInIt()
        {
            Assert.Equal("a value", DataSerialization.DeserializeFromBytes<string>(DataSerialization.SerializeToBytes("a value")));
        }

        /// <summary>
        /// Verifies that nothing at all, and nothing to read, are both refused.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void DeserializeFromBytes_RefusesNothingAtAll()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => DataSerialization.DeserializeFromBytes<string>(null!));
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => DataSerialization.DeserializeFromBytes<string>([]));
        }

        /// <summary>
        /// Verifies that a value can be read from part way into a buffer, which is how a response is read:
        /// one byte of marker, then the value.
        /// </summary>
        [Fact]
        public void DeserializeFromBytes_ReadsFromAnOffset()
        {
            // Arrange: a response as the server builds one, marker byte and all
            ProcessDefinition original = new("notepad", "Notepad");
            byte[] serialized = DataSerialization.SerializeToBytes(original);
            byte[] response = new byte[serialized.Length + 1];
            response[0] = (byte)ResponseMarker.Success;
            serialized.CopyTo(response, 1);

            // Assert
            Assert.Equal(original, DataSerialization.DeserializeFromBytes<ProcessDefinition>(response, 1));
        }

        /// <summary>
        /// Verifies that an offset which does not point at anything is refused.
        /// </summary>
        /// <remarks>
        /// The offset comes from a response that came off the wire, so it has to be bounded rather than
        /// trusted. An offset at the very end is refused as well as one past it, since there would be
        /// nothing there to read. A negative one is refused by the same test, which compares the offset
        /// unsigned so that anything below zero lands above the length.
        /// </remarks>
        /// <param name="offset">The offset to refuse.</param>
        [Theory]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void DeserializeFromBytes_RefusesAnOffsetPointingAtNothing(int offset)
        {
            // Arrange
            byte[] serialized = DataSerialization.SerializeToBytes("a value");

            // Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => DataSerialization.DeserializeFromBytes<string>(serialized, offset));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => DataSerialization.DeserializeFromBytes<string>(serialized, serialized.Length));
        }

        /// <summary>
        /// Verifies that reading a value as the wrong type fails rather than producing something.
        /// </summary>
        [Fact]
        public void DeserializeFromBytes_RefusesTheWrongType()
        {
            _ = Assert.Throws<SerializationException>(static () => DataSerialization.DeserializeFromBytes<int>(DataSerialization.SerializeToBytes("a value")));
        }

        /// <summary>
        /// Verifies that an exception survives the trip with the parts a caller acts on intact.
        /// </summary>
        /// <remarks>
        /// The result code matters as much as the message. A client failure carries its exit code there, and
        /// it is what the deployment session reports when the client could not do what it was asked.
        /// </remarks>
        [Fact]
        public void Exception_RoundTripsWhatACallerReads()
        {
            // Arrange
            InvalidOperationException original = new("the outer failure", new IOException("the inner failure"));

            // Act
            Exception restored = DataSerialization.DeserializeFromBytes<Exception>(DataSerialization.SerializeToBytes<Exception>(original));

            // Assert
            _ = Assert.IsType<InvalidOperationException>(restored);
            Assert.Equal(original.Message, restored.Message);
            Assert.Equal(original.HResult, restored.HResult);
            Assert.NotNull(restored.InnerException);
            Assert.Equal("the inner failure", restored.InnerException.Message);
        }

        /// <summary>
        /// Verifies that the exception this serializer raises for unreadable input can itself be
        /// serialized.
        /// </summary>
        /// <remarks>
        /// Its callers report a failure by serializing the exception, so one that cannot be serialized
        /// leaves them nothing to report. The client is the case that matters: its error handler calls
        /// <c>Environment.FailFast</c> when serializing the exception throws, so the process aborted with
        /// an empty standard error rather than an exit code the server could read.
        /// <para>
        /// The obstacle was the inner <c>XmlException</c>, which carries the arguments of its own message
        /// as a <c>string[]</c>. That type had to be a known type before an exception holding one could
        /// be written.
        /// </para>
        /// </remarks>
        [Fact]
        public void Exception_FromUnreadableInputIsItselfSerializable()
        {
            // Arrange
            SerializationException thrown = Assert.Throws<SerializationException>(
                static () => DataSerialization.DeserializeFromString<ProcessDefinition>("notserializedcontent"));

            // Act
            Exception restored = DataSerialization.DeserializeFromBytes<Exception>(DataSerialization.SerializeToBytes<Exception>(thrown));

            // Assert
            _ = Assert.IsType<SerializationException>(restored);
            Assert.Equal(thrown.Message, restored.Message);
        }

        /// <summary>
        /// Verifies that whatever an exception was carrying in its data survives with it.
        /// </summary>
        /// <remarks>
        /// The reason the resolver exists. That dictionary is an internal type of the framework, and it
        /// serializes under the same contract name as <see cref="Hashtable"/>, so the two cannot both sit in
        /// a list of known types. The resolver keeps the name on the way out and hands back the public one
        /// on the way in; without it, an exception carrying anything in its data cannot be rebuilt at all.
        /// </remarks>
        [Fact]
        public void Exception_RoundTripsWhatItCarriesInItsData()
        {
            // Arrange
            InvalidOperationException original = new("a failure");
            original.Data["a key"] = "a value";

            // Act
            Exception restored = DataSerialization.DeserializeFromBytes<Exception>(DataSerialization.SerializeToBytes<Exception>(original));

            // Assert
            Assert.Equal("a value", restored.Data["a key"]);
        }

        /// <summary>
        /// Verifies that a client failure survives with the exit code it was given, which the server reads
        /// back off the client's standard error during shutdown.
        /// </summary>
        [Fact]
        public void Exception_RoundTripsAClientFailure()
        {
            // Arrange
            ClientException original = new("the client gave up", ClientExitCode.InvalidRequest);

            // Act
            Exception restored = DataSerialization.DeserializeFromBytes<Exception>(DataSerialization.SerializeToBytes<Exception>(original));

            // Assert
            _ = Assert.IsType<ClientException>(restored);
            Assert.Equal("the client gave up", restored.Message);
            Assert.Equal((int)ClientExitCode.InvalidRequest, restored.HResult);
        }

        /// <summary>
        /// Verifies that something which is not an exception is refused when read as one.
        /// </summary>
        /// <remarks>
        /// Reading an exception does not verify the root element, because the name on it is the concrete
        /// type rather than the one that was asked for. That leaves nothing to stop some other value being
        /// read in its place, so what comes back is checked instead - which is the branch asserted here.
        /// </remarks>
        [Fact]
        public void DeserializeFromBytes_RefusesSomethingThatIsNotAnException()
        {
            // Arrange: written as an object, so the dictionary's own contract name goes on the wire
            byte[] serialized = DataSerialization.SerializeToBytes<object>(new Hashtable { ["a key"] = "a value" });

            // Act
            SerializationException failure = Assert.Throws<SerializationException>(() => DataSerialization.DeserializeFromBytes<Exception>(serialized));

            // Assert
            Assert.Contains("expected an Exception type", failure.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that the last exception written to the client's standard error is the one read back.
        /// </summary>
        /// <remarks>
        /// The search runs backwards for a reason: a client that failed more than once has written more than
        /// one, and the last is the one that ended it.
        /// </remarks>
        [Fact]
        public void DeserializeExceptionFromStdErr_ReadsTheLastOneWritten()
        {
            // Arrange
            using ProcessResult result = new(1, stdOut: null, stdErr: [
                DataSerialization.SerializeToString<Exception>(new InvalidOperationException("the first failure")),
                DataSerialization.SerializeToString<Exception>(new IOException("the last failure")),
            ], interleaved: null);

            // Act
            Exception? restored = DataSerialization.DeserializeExceptionFromStdErr(result);

            // Assert
            Assert.NotNull(restored);
            Assert.Equal("the last failure", restored.Message);
        }

        /// <summary>
        /// Verifies that ordinary error output is stepped over rather than taken for an exception.
        /// </summary>
        /// <remarks>
        /// A client writes plain text to its standard error as well, and anything it did not put there
        /// itself - a loader failure, a runtime message - lands in the same place. All of it has to be read
        /// past without the search giving up.
        /// </remarks>
        [Fact]
        public void DeserializeExceptionFromStdErr_StepsOverEverythingElse()
        {
            // Arrange
            using ProcessResult result = new(1, stdOut: null, stdErr: [
                DataSerialization.SerializeToString<Exception>(new IOException("the failure")),
                "a line of plain text",
                "!!! not base64 !!!",
                Convert.ToBase64String([1, 2, 3, 4]),
            ], interleaved: null);

            // Act
            Exception? restored = DataSerialization.DeserializeExceptionFromStdErr(result);

            // Assert
            Assert.NotNull(restored);
            Assert.Equal("the failure", restored.Message);
        }

        /// <summary>
        /// Verifies that error output holding no exception is reported as holding none, since a client that
        /// failed without saying why is the ordinary case rather than an error in itself.
        /// </summary>
        [Fact]
        public void DeserializeExceptionFromStdErr_ReportsWhenThereIsNone()
        {
            // Arrange
            using ProcessResult empty = new(1, stdOut: null, stdErr: null, interleaved: null);
            using ProcessResult noise = new(1, stdOut: null, stdErr: ["a line of plain text"], interleaved: null);

            // Assert
            Assert.Null(DataSerialization.DeserializeExceptionFromStdErr(empty));
            Assert.Null(DataSerialization.DeserializeExceptionFromStdErr(noise));
        }

        /// <summary>
        /// Verifies that a collection survives the trip with its contents, which is the case a serializer
        /// is most likely to get half right.
        /// </summary>
        /// <remarks>
        /// A read-only collection rather than any other shape, because that is the one the protocol
        /// actually carries and the one named in the serializer's list of known types. A collection type
        /// that is not on that list - a compiler-generated one behind an interface, say - is refused
        /// outright rather than serialized, so the shape here is part of what is being asserted.
        /// </remarks>
        [Fact]
        public void Serialization_RoundTripsACollection()
        {
            // Arrange
            ReadOnlyCollection<ProcessDefinition> original = new([new("notepad", "Notepad"), new("calc", "Calculator")]);

            // Act
            byte[] serialized = DataSerialization.SerializeToBytes(original);

            // Assert
            Assert.Equal(original, DataSerialization.DeserializeFromBytes<ReadOnlyCollection<ProcessDefinition>>(serialized));
        }
    }
}
