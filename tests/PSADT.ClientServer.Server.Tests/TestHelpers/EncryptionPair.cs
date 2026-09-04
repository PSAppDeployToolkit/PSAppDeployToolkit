using System;
using System.Threading.Tasks;

namespace PSADT.ClientServer.Server.Tests.TestHelpers
{
    /// <summary>
    /// A server and a client that have completed a key exchange with each other.
    /// </summary>
    /// <remarks>
    /// Most of what there is to test about the encryption is what happens after the key exchange, and
    /// getting to that point takes both halves running at once over a pair of pipes. This does that once
    /// so that a test wanting two parties holding the same key can say so in a line.
    /// <para>
    /// The pipes are kept and released with the pair even though nothing needs them after the exchange.
    /// A test that wants to keep using them can, and one that does not is not left leaking handles.
    /// </para>
    /// </remarks>
    internal sealed class EncryptionPair : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EncryptionPair"/> class.
        /// </summary>
        /// <remarks>Private because the key exchange has to be awaited, which a constructor cannot do.</remarks>
        private EncryptionPair()
        {
            Pipes = new();
            Server = new();
            Client = new();
        }

        /// <summary>
        /// Creates a pair and runs the key exchange between them.
        /// </summary>
        /// <returns>A pair whose two halves hold the same derived key.</returns>
        internal static async Task<EncryptionPair> CreateAsync()
        {
            EncryptionPair pair = new();
            try
            {
                await PipePair.RunBothAsync(
                    async () => await pair.Server.PerformKeyExchangeAsync(pair.Pipes.ServerOutput, pair.Pipes.ServerInput).ConfigureAwait(false),
                    async () => await pair.Client.PerformKeyExchangeAsync(pair.Pipes.ClientOutput, pair.Pipes.ClientInput).ConfigureAwait(false)).ConfigureAwait(true);
                return pair;
            }
            catch
            {
                pair.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Releases both halves and the pipes between them.
        /// </summary>
        public void Dispose()
        {
            Server.Dispose();
            Client.Dispose();
            Pipes.Dispose();
        }

        /// <summary>
        /// The server's half.
        /// </summary>
        internal ServerPipeEncryption Server { get; }

        /// <summary>
        /// The client's half.
        /// </summary>
        internal ClientPipeEncryption Client { get; }

        /// <summary>
        /// The pipes the exchange was run over.
        /// </summary>
        internal PipePair Pipes { get; }
    }
}
