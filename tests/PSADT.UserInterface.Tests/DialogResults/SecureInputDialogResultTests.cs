using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using PSADT.UserInterface.DialogResults;
using Xunit;

namespace PSADT.UserInterface.Tests.DialogResults
{
    /// <summary>
    /// Tests the result of an input dialog whose typing was masked.
    /// </summary>
    /// <remarks>
    /// The sibling of <see cref="InputDialogResult"/>, differing only in how it carries the answer. A
    /// <see cref="SecureString"/> cannot be serialised, so the value crosses the client/server boundary as
    /// unprotected UTF-16 and is rebuilt on arrival - which makes the round trip, rather than the constructor,
    /// the case worth testing.
    /// </remarks>
    public sealed class SecureInputDialogResultTests
    {
        /// <summary>
        /// Verifies that the outcome and the masked value are both kept.
        /// </summary>
        [Fact]
        public void Constructor_KeepsWhatItIsGiven()
        {
            // Arrange & Act
            using SecureString secret = Protect("a secret");
            SecureInputDialogResult result = new("Continue", secret);

            // Assert
            Assert.Equal("Continue", result.Result);
            Assert.NotNull(result.Text);
            Assert.Equal("a secret", Unprotect(result.Text));
        }

        /// <summary>
        /// Verifies that the shared default names a timeout with nothing typed.
        /// </summary>
        [Fact]
        public void DefaultResult_IsATimeoutWithNoText()
        {
            Assert.Equal("Timeout", SecureInputDialogResult.DefaultResult.Result);
            Assert.Null(SecureInputDialogResult.DefaultResult.Text);
        }

        /// <summary>
        /// Verifies that a masked answer survives the trip between the two processes.
        /// </summary>
        /// <remarks>
        /// This is the case the type exists for. A contract that dropped the value, or one that carried it
        /// but never rebuilt it, would leave the caller holding a result with nothing in it.
        /// </remarks>
        [Fact]
        public void Serialization_CarriesTheMaskedAnswerAcrossTheBoundary()
        {
            // Arrange
            using SecureString secret = Protect("a secret");
            SecureInputDialogResult sent = new("Continue", secret);

            // Act
            SecureInputDialogResult received = RoundTrip(sent);

            // Assert
            Assert.Equal("Continue", received.Result);
            Assert.NotNull(received.Text);
            Assert.Equal("a secret", Unprotect(received.Text));
        }

        /// <summary>
        /// Verifies that a result carrying nothing survives the trip as well.
        /// </summary>
        /// <remarks>
        /// The timeout and cancellation cases both arrive with no value, so the callback that rebuilds the
        /// answer has to tolerate there being nothing to rebuild.
        /// </remarks>
        [Fact]
        public void Serialization_CarriesAResultWithNoAnswer()
        {
            // Act
            SecureInputDialogResult received = RoundTrip(new SecureInputDialogResult("Cancel", text: null));

            // Assert
            Assert.Equal("Cancel", received.Result);
            Assert.Null(received.Text);
        }

        /// <summary>
        /// Records that two results differing only in what was typed compare as equal.
        /// </summary>
        /// <remarks>
        /// Comparing masked values would mean unprotecting both, and feeding one to a non-cryptographic hash
        /// would put it somewhere it does not belong, so the value is left out of both
        /// <see cref="SecureInputDialogResult.Equals(object?)"/> and
        /// <see cref="SecureInputDialogResult.GetHashCode"/>. <see cref="InputDialogResult"/> keeps comparing
        /// its own text, because a plain answer costs nothing to read. Nothing in the module compares two
        /// masked results, so this is recorded rather than corrected.
        /// </remarks>
        [Fact]
        public void Equality_IgnoresTheMaskedValue()
        {
            // Arrange
            using SecureString left = Protect("one secret");
            using SecureString right = Protect("another secret");

            // Assert
            Assert.Equal(new SecureInputDialogResult("Continue", left), new SecureInputDialogResult("Continue", right));
        }

        /// <summary>
        /// Verifies that a difference in the outcome still makes two results unequal.
        /// </summary>
        [Fact]
        public void Equality_DistinguishesTheOutcome()
        {
            Assert.NotEqual(new SecureInputDialogResult("Continue", text: null), new SecureInputDialogResult("Cancel", text: null));
        }

        /// <summary>
        /// Verifies that a masked result is not equal to the plain sibling reporting the same button.
        /// </summary>
        /// <remarks>
        /// They are different answers to different questions. A caller that had been handed the wrong one
        /// would be reading a string where it expected something it could not accidentally log.
        /// </remarks>
        [Fact]
        public void Equality_IsRefusedAgainstThePlainSibling()
        {
            Assert.NotEqual<object>(new SecureInputDialogResult("Continue", text: null), new InputDialogResult("Continue", text: null));
            Assert.NotEqual<object>(new InputDialogResult("Continue", text: null), new SecureInputDialogResult("Continue", text: null));
        }

        /// <summary>
        /// Sends a result through the serializer the way the client/server boundary does.
        /// </summary>
        /// <param name="value">The result to send.</param>
        /// <returns>The result as it arrives on the far side.</returns>
        private static SecureInputDialogResult RoundTrip(SecureInputDialogResult value)
        {
            DataContractSerializer serializer = new(typeof(SecureInputDialogResult), new DataContractSerializerSettings { SerializeReadOnlyTypes = true });
            using MemoryStream stream = new();
            serializer.WriteObject(stream, value);
            stream.Position = 0;
            return Assert.IsType<SecureInputDialogResult>(serializer.ReadObject(stream));
        }

        /// <summary>
        /// Protects a value so that a test can hand it to a result.
        /// </summary>
        /// <param name="value">The text to protect.</param>
        /// <returns>A read-only value holding the supplied text.</returns>
        private static SecureString Protect(string value)
        {
            SecureString result = new();
            foreach (char character in value)
            {
                result.AppendChar(character);
            }
            result.MakeReadOnly();
            return result;
        }

        /// <summary>
        /// Unprotects a masked value so that a test can assert on what it holds.
        /// </summary>
        /// <param name="value">The value to read.</param>
        /// <returns>The text the value holds.</returns>
        private static string Unprotect(SecureString value)
        {
            IntPtr ptr = Marshal.SecureStringToBSTR(value);
            try
            {
                return Marshal.PtrToStringBSTR(ptr);
            }
            finally
            {
                Marshal.ZeroFreeBSTR(ptr);
            }
        }
    }
}
