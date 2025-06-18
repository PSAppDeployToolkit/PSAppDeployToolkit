using System;
using System.Collections;
using System.Runtime.Serialization;
using PSADT.Serialization;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for the InputDialog.
    /// </summary>
    [DataContract]
    public sealed record InputDialogOptions : CustomDialogOptions
    {
        /// <summary>
        /// Initializes the <see cref="InputDialogOptions"/> class and registers it as a serializable type.
        /// </summary>
        /// <remarks>This static constructor ensures that the <see cref="InputDialogOptions"/> type is added
        /// to the list of serializable types for data contract serialization. This allows instances of <see
        /// cref="ClientException"/> to be serialized and deserialized using data contract serializers.</remarks>
        static InputDialogOptions()
        {
            DataContractSerialization.AddSerializableType(typeof(InputDialogOptions));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InputDialogOptions"/> class.
        /// </summary>
        /// <param name="options"></param>
        public InputDialogOptions(Hashtable options) : base(options)
        {
            // Just set our one and only field.
            if (options.ContainsKey("InitialInputText"))
            {
                if (options["InitialInputText"] is not string initialInputText || string.IsNullOrWhiteSpace(initialInputText))
                {
                    throw new ArgumentOutOfRangeException("InitialInputText value is not valid.", (Exception?)null);
                }
                InitialInputText = initialInputText;
            }
        }

        /// <summary>
        /// The initial text to be displayed in the input field.
        /// </summary>
        [DataMember]
        public readonly string? InitialInputText;
    }
}
