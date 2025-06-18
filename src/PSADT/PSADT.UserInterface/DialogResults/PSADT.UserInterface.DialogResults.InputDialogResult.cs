using System.Runtime.Serialization;
using PSADT.Serialization;

namespace PSADT.UserInterface.DialogResults
{
    /// <summary>
    /// Represents the result of an input dialog.
    /// </summary>
    [DataContract]
    public sealed record InputDialogResult
    {
        /// <summary>
        /// Initializes the <see cref="InputDialogResult"/> class and registers it as a serializable type.
        /// </summary>
        /// <remarks>This static constructor ensures that the <see cref="InputDialogResult"/> type is added
        /// to the list of serializable types for data contract serialization. This allows instances of <see
        /// cref="ClientException"/> to be serialized and deserialized using data contract serializers.</remarks>
        static InputDialogResult()
        {
            DataContractSerialization.AddSerializableType(typeof(InputDialogResult));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InputDialogResult"/> class.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="text"></param>
        internal InputDialogResult(string result, string? text = null)
        {
            Result = result;
            Text = !string.IsNullOrWhiteSpace(text) ? text : null;
        }

        /// <summary>
        /// Gets the result of the dialog.
        /// </summary>
        [DataMember]
        public readonly string Result;

        /// <summary>
        /// Gets the text entered by the user.
        /// </summary>
        [DataMember]
        public readonly string? Text;
    }
}
