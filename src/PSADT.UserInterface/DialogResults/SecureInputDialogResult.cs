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
        /// <remarks>The getter unprotects for the duration of the write and keeps hold of what it handed out so
        /// that <see cref="OnSerialized"/> can overwrite it afterwards; it unprotects afresh each time, so a result
        /// may be sent more than once. The setter stages the bytes for <see cref="OnDeserialized"/> to consume and
        /// discard.</remarks>
        [DataMember(Name = nameof(Text))]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Read and written by the serializer rather than by any caller.")]
        [SuppressMessage("Redundancy", "RCS1213:Remove unused member declaration", Justification = "Read and written by the serializer rather than by any caller.")]
        private byte[]? TextBytes
        {
            get => _sentText = Text is not null ? SecureStringUtilities.ToUtf16Bytes(Text) : null;
            set => _stagedText = value;
        }

        /// <summary>
        /// Discards the transported bytes once the serializer has written them.
        /// </summary>
        /// <param name="context">The streaming context supplied by the serializer.</param>
        [OnSerialized]
        private void OnSerialized(StreamingContext context)
        {
            CryptographicUtilities.SecureZeroMemory(_sentText);
            _sentText = null;
        }

        /// <summary>
        /// Rebuilds the masked value on the receiving side and discards the transported bytes.
        /// </summary>
        /// <param name="context">The streaming context supplied by the serializer.</param>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (_stagedText is null)
            {
                return;
            }
            try
            {
                // Rebuilding refuses a buffer holding a partial character, so the discard cannot wait on it.
                Text = SecureStringUtilities.FromUtf16Bytes(_stagedText);
            }
            finally
            {
                CryptographicUtilities.SecureZeroMemory(_stagedText);
                _stagedText = null;
            }
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current instance.
        /// </summary>
        /// <remarks>Compares the Result, and whether there is an answer at all. What the answer holds is not
        /// compared, because reading two masked values to tell them apart would mean unprotecting both; two
        /// results differing only in what was typed are equal.
        /// <para>
        /// Whether an answer is present has to count, because the module tests a result against
        /// <see cref="DefaultResult"/> to decide a dialog timed out and may end the deployment on the strength of
        /// it. Button captions are free text, so a deployment may label one "Timeout" - and on Result alone, a
        /// user who typed a password and pressed it would be read as having answered nothing at all.
        /// </para></remarks>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>true if the specified object is a SecureInputDialogResult with an equal Result value and the same presence or absence of an answer; otherwise, false.</returns>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is SecureInputDialogResult other && Result.Equals(other.Result, StringComparison.Ordinal) && (Text is null) == (other.Text is null);
        }

        /// <summary>
        /// Returns a hash code for the current instance.
        /// </summary>
        /// <remarks>Combines the same two values <see cref="Equals(object?)"/> compares. What the answer holds is
        /// excluded for the reason given there, and because feeding a secret to a non-cryptographic hash puts it
        /// somewhere it does not belong; whether there is one is already plain from the property.</remarks>
        /// <returns>A hash code combining Result and whether there is an answer.</returns>
        public override int GetHashCode()
        {
            return CryptographicUtilities.GenerateHashCode(Result, Text is not null);
        }

        /// <summary>
        /// Holds the transported bytes between deserialization and the callback that consumes them.
        /// </summary>
        private byte[]? _stagedText;

        /// <summary>
        /// Holds the transported bytes between serialization and the callback that overwrites them.
        /// </summary>
        private byte[]? _sentText;
    }
}
