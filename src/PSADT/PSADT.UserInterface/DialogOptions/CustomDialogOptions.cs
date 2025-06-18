using System;
using System.Collections;
using System.Runtime.Serialization;
using PSADT.Serialization;
using PSADT.UserInterface.Dialogs;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for the CustomDialog.
    /// </summary>
    [DataContract]
    [KnownType(typeof(InputDialogOptions))]
    public record CustomDialogOptions : BaseOptions
    {
        /// <summary>
        /// Initializes the <see cref="CustomDialogOptions"/> class and registers it as a serializable type.
        /// </summary>
        /// <remarks>This static constructor ensures that the <see cref="CustomDialogOptions"/> type is added
        /// to the list of serializable types for data contract serialization. This allows instances of <see
        /// cref="ClientException"/> to be serialized and deserialized using data contract serializers.</remarks>
        static CustomDialogOptions()
        {
            DataContractSerialization.AddSerializableType(typeof(CustomDialogOptions));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomDialogOptions"/> class.
        /// </summary>
        /// <param name="options"></param>
        public CustomDialogOptions(Hashtable options) : base(options)
        {
            // Nothing here is allowed to be null.
            if (options["MessageText"] is not string messageText || string.IsNullOrWhiteSpace(messageText))
            {
                throw new ArgumentNullException("MessageText value is null or invalid.", (Exception?)null);
            }
            if (options["MinimizeWindows"] is not bool minimizeWindows)
            {
                throw new ArgumentNullException("MinimizeWindows value is null or invalid.", (Exception?)null);
            }

            // Test and set optional values.
            if (options.ContainsKey("MessageAlignment"))
            {
                if (options["MessageAlignment"] is not DialogMessageAlignment messageAlignment)
                {
                    throw new ArgumentOutOfRangeException("MessageAlignment value is not valid.", (Exception?)null);
                }
                MessageAlignment = messageAlignment;
            }
            if (options.ContainsKey("ButtonLeftText"))
            {
                if (options["ButtonLeftText"] is not string buttonLeftText || string.IsNullOrWhiteSpace(buttonLeftText))
                {
                    throw new ArgumentOutOfRangeException("ButtonLeftText value is not valid.", (Exception?)null);
                }
                ButtonLeftText = buttonLeftText;
            }
            if (options.ContainsKey("ButtonMiddleText"))
            {
                if (options["ButtonMiddleText"] is not string buttonMiddleText || string.IsNullOrWhiteSpace(buttonMiddleText))
                {
                    throw new ArgumentOutOfRangeException("ButtonMiddleText value is not valid.", (Exception?)null);
                }
                ButtonMiddleText = buttonMiddleText;
            }
            if (options.ContainsKey("ButtonRightText"))
            {
                if (options["ButtonRightText"] is not string buttonRightText || string.IsNullOrWhiteSpace(buttonRightText))
                {
                    throw new ArgumentOutOfRangeException("ButtonRightText value is not valid.", (Exception?)null);
                }
                ButtonRightText = buttonRightText;
            }
            if (options.ContainsKey("Icon"))
            {
                if (options["Icon"] is not DialogSystemIcon icon)
                {
                    throw new ArgumentOutOfRangeException("Icon value is not valid.", (Exception?)null);
                }
                Icon = icon;
            }

            // The hashtable was correctly defined, assign the remaining values.
            MessageText = messageText;
            MinimizeWindows = minimizeWindows;

            // At least one button must be defined before we finish.
            if (string.IsNullOrWhiteSpace(ButtonLeftText) && string.IsNullOrWhiteSpace(ButtonMiddleText) && string.IsNullOrWhiteSpace(ButtonRightText))
            {
                throw new ArgumentNullException("At least one button must be defined.", (Exception?)null);
            }
        }

        /// <summary>
        /// The custom message to be displayed in the dialog.
        /// </summary>
        [DataMember]
        public readonly string MessageText;

        /// <summary>
        /// The alignment of the message text in the dialog.
        /// </summary>
        [DataMember]
        public readonly DialogMessageAlignment? MessageAlignment;

        /// <summary>
        /// The text for the left button in the dialog.
        /// </summary>
        [DataMember]
        public readonly string? ButtonLeftText;

        /// <summary>
        /// The text for the middle button in the dialog.
        /// </summary>
        [DataMember]
        public readonly string? ButtonMiddleText;

        /// <summary>
        /// The text for the right button in the dialog.
        /// </summary>
        [DataMember]
        public readonly string? ButtonRightText;

        /// <summary>
        /// The icon to be displayed in the dialog.
        /// </summary>
        [DataMember]
        public readonly DialogSystemIcon? Icon;

        /// <summary>
        /// Gets a value indicating whether windows should be minimized.
        /// </summary>
        [DataMember]
        public readonly bool MinimizeWindows;
    }
}
