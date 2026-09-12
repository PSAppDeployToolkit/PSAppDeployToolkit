using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Security;
using PSADT.Utilities;

namespace PSADT.UserInterface.DialogResults
{
    /// <summary>
    /// Represents the result of an input dialog whose typing was masked.
    /// </summary>
    /// <remarks>
    /// The transported form of the answer is unprotected UTF-16, because a <see cref="SecureString"/> cannot cross
    /// a process boundary: its memory protection is scoped to the process that created it. Confidentiality on the
    /// wire comes from the encrypted client/server channel rather than from this type.
    /// </remarks>
    [DataContract]
    [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "A result is handed to PowerShell callers who cannot be relied upon to dispose it, and making it disposable pushes that contract onto every call site. SecureString's finalizer zeroes the buffer instead, which is the same trade PSCredential makes.")]
    public sealed class SecureInputDialogResult : CustomDialogDerivative
    {
        /// <summary>
        /// Represents the default dialog result used when a dialog times out.
        /// </summary>
        public static new readonly SecureInputDialogResult DefaultResult = new("Timeout", text: null);

        /// <summary>
        /// Initializes a new instance of the SecureInputDialogResult class with the specified result and masked value.
        /// </summary>
        /// <remarks>The instance takes ownership of the supplied value.</remarks>
        /// <param name="result">The result of the input dialog. Cannot be null or empty.</param>
        /// <param name="text">An optional masked value associated with the result.</param>
        internal SecureInputDialogResult(string result, SecureString? text) : base(result)
        {
            Text = text;
        }

        /// <summary>
        /// Gets the masked value entered by the user.
        /// </summary>
        public SecureString? Text { get; private set; }

        /// <summary>
        /// Gets or sets the transported form of <see cref="Text"/>.
        /// </summary>
        /// <remarks>The getter unprotects only for the duration of the write, so a sender never retains the
        /// bytes. The setter stages them for <see cref="OnDeserialized"/> to consume and discard.</remarks>
        [DataMember(Name = nameof(Text))]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Read and written by the serializer rather than by any caller.")]
        [SuppressMessage("Redundancy", "RCS1213:Remove unused member declaration", Justification = "Read and written by the serializer rather than by any caller.")]
        private byte[]? TextBytes
        {
            get => Text is not null ? SecureStringUtilities.ToUtf16Bytes(Text) : null;
            set => _stagedText = value;
        }

        /// <summary>
        /// Rebuilds the masked value on the receiving side and discards the transported bytes.
        /// </summary>
        /// <param name="context">The streaming context supplied by the serializer.</param>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (_stagedText is not null)
            {
                Text = SecureStringUtilities.FromUtf16Bytes(_stagedText);
                SecureStringUtilities.SecureZeroMemory(_stagedText);
                _stagedText = null;
            }
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current instance.
        /// </summary>
        /// <remarks>Compares the Result only. <see cref="Text"/> is deliberately excluded: comparing two masked
        /// values would mean unprotecting both, so two results differing only in what was typed compare as equal.
        /// <see cref="InputDialogResult"/> does compare its text, because a plain answer costs nothing to read.</remarks>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>true if the specified object is a SecureInputDialogResult with an equal Result value; otherwise, false.</returns>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is SecureInputDialogResult other && Result.Equals(other.Result, StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns a hash code for the current instance.
        /// </summary>
        /// <remarks><see cref="Text"/> is excluded for the reason given on <see cref="Equals(object?)"/>, and
        /// because feeding a secret to a non-cryptographic hash puts it somewhere it does not belong.</remarks>
        /// <returns>A hash code derived from Result.</returns>
        public override int GetHashCode()
        {
            return Result.GetHashCode(StringComparison.Ordinal);
        }

        /// <summary>
        /// Holds the transported bytes between deserialization and the callback that consumes them.
        /// </summary>
        private byte[]? _stagedText;
    }
}
