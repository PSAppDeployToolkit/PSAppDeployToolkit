using System;
using System.IO;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// A read-only view of a stream that stops reading the stream once it has reported its end.
    /// </summary>
    /// <remarks>An anonymous pipe on .NET Framework throws on a read made after its end, which
    /// <see cref="StreamReader"/> makes whenever the last line has no line break.</remarks>
    /// <param name="stream">The stream to read, which is disposed along with this one.</param>
    internal sealed class EndOfStreamLatchingStream(Stream stream) : Stream
    {
        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (EndReached)
            {
                return 0;
            }
            int read = SourceStream.Read(buffer, offset, count);
            EndReached = read is 0 && count > 0;
            return read;
        }

        /// <inheritdoc/>
        public override void Flush()
        {
            // Nothing is written through this stream, so there is nothing to flush.
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SourceStream.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <inheritdoc/>
        public override bool CanRead => SourceStream.CanRead;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc/>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// The stream being read.
        /// </summary>
        private readonly Stream SourceStream = stream;

        /// <summary>
        /// Whether the stream being read has reported its end.
        /// </summary>
        private bool EndReached;
    }
}
