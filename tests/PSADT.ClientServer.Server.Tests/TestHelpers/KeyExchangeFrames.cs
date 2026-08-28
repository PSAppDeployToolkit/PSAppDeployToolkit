using System.IO;
using System.Threading.Tasks;

namespace PSADT.ClientServer.Server.Tests.TestHelpers
{
    /// <summary>
    /// Captures the public key each half of the key exchange puts on the wire.
    /// </summary>
    /// <remarks>
    /// Neither half offers its public key as a return value - it is written straight to the output stream
    /// - so the only way to see one is to run the exchange far enough for it to be written and then stop.
    /// Running it against a stream with nothing in it does exactly that: the half writes what it has to
    /// write, then reads, then fails at the end of the empty stream, leaving the frame behind in the
    /// output.
    /// <para>
    /// A frame captured this way is also what a test needs in order to get the other half past its own key
    /// derivation, which is what several of the refusals sit behind.
    /// </para>
    /// </remarks>
    internal static class KeyExchangeFrames
    {
        /// <summary>
        /// Runs a server far enough to write its public key.
        /// </summary>
        /// <returns>The length-prefixed public key frame the server sent.</returns>
        internal static async Task<byte[]> ServerPublicKeyAsync()
        {
            using ServerPipeEncryption server = new();
            using MemoryStream output = new();
            using MemoryStream input = new();
            try
            {
                await server.PerformKeyExchangeAsync(output, input).ConfigureAwait(true);
            }
            catch (EndOfStreamException)
            {
                // Expected: the server has written its key and there is no reply for it to read.
            }
            return output.ToArray();
        }

        /// <summary>
        /// Runs a client far enough to write its public key, which means first handing it one to read.
        /// </summary>
        /// <returns>The length-prefixed public key frame the client sent.</returns>
        internal static async Task<byte[]> ClientPublicKeyAsync()
        {
            using ClientPipeEncryption client = new();
            using MemoryStream output = new();
            using MemoryStream input = new(await ServerPublicKeyAsync().ConfigureAwait(true));
            try
            {
                await client.PerformKeyExchangeAsync(output, input).ConfigureAwait(true);
            }
            catch (EndOfStreamException)
            {
                // Expected: the client has written its key and there is no challenge for it to read.
            }
            return output.ToArray();
        }
    }
}
