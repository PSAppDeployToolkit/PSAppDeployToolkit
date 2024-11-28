using System;
using System.Text;

namespace PSADT.SecureIPC
{
    /// <summary>
    /// Represents the arguments for events triggered during named pipe communication.
    /// Holds the message data, including both the byte array and the string version.
    /// </summary>
    public class PipeEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the message as a byte array.
        /// </summary>
        public byte[] MessageBytes { get; protected set; }

        /// <summary>
        /// Gets the length of the message in bytes.
        /// </summary>
        public int MessageLength { get; protected set; }

        /// <summary>
        /// Gets the message as a string, based on the specified encoding.
        /// </summary>
        public string MessageText { get; protected set; }

        /// <summary>
        /// Gets the encoding used to decode the byte array message.
        /// </summary>
        public Encoding Encoding { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PipeEventArgs"/> class with a string message.
        /// </summary>
        /// <param name="message">The message as a string.</param>
        /// <param name="encoding">The encoding used for the string message.</param>
        public PipeEventArgs(string message, Encoding? encoding)
        {
            Encoding = encoding ?? Encoding.UTF8;
            MessageText = message;
            MessageBytes = Array.Empty<byte>();
            MessageLength = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PipeEventArgs"/> class with a byte array message.
        /// </summary>
        /// <param name="messageBytes">The message as a byte array.</param>
        /// <param name="messageLength">The length of the message in bytes.</param>
        /// <param name="encoding">The encoding used to decode the byte array into a string.</param>
        public PipeEventArgs(byte[] messageBytes, int messageLength, Encoding? encoding)
        {
            Encoding = encoding ?? Encoding.UTF8;
            MessageText = String.Empty;
            MessageBytes = messageBytes;
            MessageLength = messageLength;
        }
    }
}
