using System;
using System.Collections;
using System.Runtime.Serialization;
using PSADT.Serialization;
using PSADT.UserInterface.Dialogs;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for the ProgressDialog.
    /// </summary>
    [DataContract]
    public sealed record ProgressDialogOptions : BaseOptions
    {
        /// <summary>
        /// Initializes the <see cref="ProgressDialogOptions"/> class and registers it as a serializable type.
        /// </summary>
        /// <remarks>This static constructor ensures that the <see cref="ProgressDialogOptions"/> type is added
        /// to the list of serializable types for data contract serialization. This allows instances of <see
        /// cref="ClientException"/> to be serialized and deserialized using data contract serializers.</remarks>
        static ProgressDialogOptions()
        {
            DataContractSerialization.AddSerializableType(typeof(ProgressDialogOptions));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressDialogOptions"/> class.
        /// </summary>
        /// <param name="options"></param>
        public ProgressDialogOptions(Hashtable options) : base(options)
        {
            // Nothing here is allowed to be null.
            if (options["ProgressMessageText"] is not string progressMessageText || string.IsNullOrWhiteSpace(progressMessageText))
            {
                throw new ArgumentNullException("ProgressMessageText value is null or invalid.", (Exception?)null);
            }
            if (options["ProgressDetailMessageText"] is not string progressDetailMessageText || string.IsNullOrWhiteSpace(progressDetailMessageText))
            {
                throw new ArgumentNullException("ProgressDetailMessageText value is null or invalid.", (Exception?)null);
            }

            // Test and set optional values.
            if (options.ContainsKey("ProgressPercentage"))
            {
                if (options["ProgressPercentage"] is not double progressPercentage)
                {
                    throw new ArgumentOutOfRangeException("ProgressPercentage value is not valid.", (Exception?)null);
                }
                ProgressPercentage = progressPercentage;
            }
            if (options.ContainsKey("MessageAlignment"))
            {
                if (options["MessageAlignment"] is not DialogMessageAlignment messageAlignment)
                {
                    throw new ArgumentOutOfRangeException("MessageAlignment value is not valid.", (Exception?)null);
                }
                MessageAlignment = messageAlignment;
            }

            // The hashtable was correctly defined, assign the remaining values.
            ProgressMessageText = progressMessageText;
            ProgressDetailMessageText = progressDetailMessageText;
        }

        /// <summary>
        /// The message to be displayed in the progress dialog, indicating the current status or action being performed.
        /// </summary>
        [DataMember]
        public readonly string ProgressMessageText;

        /// <summary>
        /// The detailed message to be displayed in the progress dialog, providing more context or information about the current action.
        /// </summary>
        [DataMember]
        public readonly string ProgressDetailMessageText;

        /// <summary>
        /// The percentage value to be displayed on the status bar, if available.
        /// </summary>
        [DataMember]
        public readonly double? ProgressPercentage;

        /// <summary>
        /// The alignment of the message text in the dialog.
        /// </summary>
        [DataMember]
        public readonly DialogMessageAlignment? MessageAlignment;
    }
}
