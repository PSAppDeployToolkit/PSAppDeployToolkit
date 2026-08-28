using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace PSADT.ClientServer.Server.Tests.TestHelpers
{
    /// <summary>
    /// Two anonymous pipes wired up the way a server and its client are wired up, both ends inside the
    /// test process.
    /// </summary>
    /// <remarks>
    /// The key exchange is a lock-step conversation - one side writes and the other reads, turn about -
    /// so it cannot be driven from a single thread against a buffer. Real pipes are used rather than
    /// memory streams for the same reason the code under test uses them: a read on an empty pipe blocks
    /// until the other side writes, which is what makes running the two halves concurrently a faithful
    /// rehearsal of the real thing rather than an arrangement that only works because everything was
    /// written down first.
    /// <para>
    /// Nothing here reaches outside the process. An anonymous pipe has no name, so there is nothing for
    /// anything else on the machine to connect to and nothing left behind afterwards.
    /// </para>
    /// <para>
    /// The client ends are built from the server ends' own client handles rather than from the handle
    /// strings the server would hand to a child process. Both would work between processes; only this one
    /// works within one, because the local copy a server disposes after launching a child is the very
    /// handle a client in the same process would still be holding.
    /// </para>
    /// </remarks>
    public sealed class PipePair : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PipePair"/> class.
        /// </summary>
        public PipePair()
        {
            // Server writes, client reads. Mirrors ServerInstance's own output pipe.
            _serverToClient = new(PipeDirection.Out, HandleInheritability.None);
            ClientInput = new(PipeDirection.In, _serverToClient.ClientSafePipeHandle);

            // Client writes, server reads. Mirrors ServerInstance's own input pipe.
            _clientToServer = new(PipeDirection.In, HandleInheritability.None);
            ClientOutput = new(PipeDirection.Out, _clientToServer.ClientSafePipeHandle);
        }

        /// <summary>
        /// Runs both halves of a conversation at once and waits for them both.
        /// </summary>
        /// <remarks>
        /// Both have to be in flight together, since each spends most of its time waiting on the other.
        /// The wait is bounded so that a protocol that has stopped making progress fails the test rather
        /// than hanging the run, which is the difference between a bug being reported and a build being
        /// killed by its own timeout with nothing to show.
        /// </remarks>
        /// <param name="server">The server's half of the conversation.</param>
        /// <param name="client">The client's half of the conversation.</param>
        /// <returns>A task that completes when both halves have.</returns>
        public static async Task RunBothAsync(Func<Task> server, Func<Task> client)
        {
            using CancellationTokenSource timeout = new();
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            await Task.WhenAll(Task.Run(server, timeout.Token), Task.Run(client, timeout.Token)).WaitAsync(timeout.Token).ConfigureAwait(true);
        }

        /// <summary>
        /// Releases the pipes.
        /// </summary>
        public void Dispose()
        {
            _serverToClient.Dispose();
            _clientToServer.Dispose();
            ClientInput.Dispose();
            ClientOutput.Dispose();
        }

        /// <summary>
        /// The stream the server writes to.
        /// </summary>
        public Stream ServerOutput => _serverToClient;

        /// <summary>
        /// The stream the server reads from.
        /// </summary>
        public Stream ServerInput => _clientToServer;

        /// <summary>
        /// The stream the client reads from.
        /// </summary>
        public AnonymousPipeClientStream ClientInput { get; }

        /// <summary>
        /// The stream the client writes to.
        /// </summary>
        public AnonymousPipeClientStream ClientOutput { get; }

        /// <summary>
        /// The server's end of the pipe carrying data towards the client.
        /// </summary>
        private readonly AnonymousPipeServerStream _serverToClient;

        /// <summary>
        /// The server's end of the pipe carrying data back from the client.
        /// </summary>
        private readonly AnonymousPipeServerStream _clientToServer;
    }
}
