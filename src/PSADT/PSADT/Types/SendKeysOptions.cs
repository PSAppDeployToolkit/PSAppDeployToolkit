using System.Runtime.Serialization;
using PSADT.Serialization;

namespace PSADT.Types
{
    /// <summary>
    /// Represents options for sending key sequences to a specific window.
    /// </summary>
    /// <remarks>The <see cref="SendKeysOptions"/> class encapsulates the target window handle and the key
    /// sequence to be sent. Use this class to configure and manage the parameters required for sending keys to a
    /// window.</remarks>
    [DataContract]
    public sealed record SendKeysOptions
    {
        /// <summary>
        /// Initializes the <see cref="SendKeysOptions"/> class and registers it as a serializable type.
        /// </summary>
        /// <remarks>This static constructor ensures that the <see cref="SendKeysOptions"/> type is added
        /// to the list of serializable types for data contract serialization. This allows instances of <see
        /// cref="ClientException"/> to be serialized and deserialized using data contract serializers.</remarks>
        static SendKeysOptions()
        {
            DataContractSerialization.AddSerializableType(typeof(SendKeysOptions));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SendKeysOptions"/> class with the specified window handle and
        /// keys.
        /// </summary>
        /// <remarks>Use this constructor to specify the target window and the keys to send. Ensure that
        /// the <paramref name="windowHandle"/> corresponds to a valid window and that <paramref name="keys"/> contains
        /// the desired key sequence.</remarks>
        /// <param name="windowHandle">The handle of the window to which the keys will be sent. This must be a valid window handle.</param>
        /// <param name="keys">The string representing the keys to be sent. Cannot be null or empty.</param>
        public SendKeysOptions(nint windowHandle, string keys)
        {
            WindowHandle = windowHandle;
            Keys = keys;
        }

        /// <summary>
        /// Gets the native handle of the window.
        /// </summary>
        [DataMember]
        public readonly nint WindowHandle;

        /// <summary>
        /// Represents the keys associated with the current object.
        /// </summary>
        [DataMember]
        public readonly string Keys;
    }
}
