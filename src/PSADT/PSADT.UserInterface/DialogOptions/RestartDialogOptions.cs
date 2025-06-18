using System;
using System.Collections;
using System.Runtime.Serialization;
using PSADT.Module;
using PSADT.Serialization;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for the RestartDialog.
    /// </summary>
    [DataContract]
    public sealed record RestartDialogOptions : BaseOptions
    {
        /// <summary>
        /// Initializes the <see cref="RestartDialogOptions"/> class and registers it as a serializable type.
        /// </summary>
        /// <remarks>This static constructor ensures that the <see cref="RestartDialogOptions"/> type is added
        /// to the list of serializable types for data contract serialization. This allows instances of <see
        /// cref="ClientException"/> to be serialized and deserialized using data contract serializers.</remarks>
        static RestartDialogOptions()
        {
            DataContractSerialization.AddSerializableType(typeof(RestartDialogOptions));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestartDialogOptions"/> class.
        /// </summary>
        /// <param name="options"></param>
        public RestartDialogOptions(DeploymentType deploymentType, Hashtable options) : base(options)
        {
            // Nothing here is allowed to be null.
            if (options["Strings"] is not Hashtable strings || strings.Count == 0)
            {
                throw new ArgumentNullException("Strings value is null or invalid.", (Exception?)null);
            }

            // Test and set optional values.
            if (options.ContainsKey("CountdownDuration"))
            {
                if (options["CountdownDuration"] is not TimeSpan countdownDuration)
                {
                    throw new ArgumentOutOfRangeException("CountdownDuration value is not valid.", (Exception?)null);
                }
                CountdownDuration = countdownDuration;
            }
            if (options.ContainsKey("CountdownNoMinimizeDuration"))
            {
                if (options["CountdownNoMinimizeDuration"] is not TimeSpan countdownNoMinimizeDuration)
                {
                    throw new ArgumentOutOfRangeException("CountdownNoMinimizeDuration value is not valid.", (Exception?)null);
                }
                CountdownNoMinimizeDuration = countdownNoMinimizeDuration;
            }
            if (options.ContainsKey("CustomMessageText"))
            {
                if (options["CustomMessageText"] is not string customMessageText || string.IsNullOrWhiteSpace(customMessageText))
                {
                    throw new ArgumentOutOfRangeException("CustomMessageText value is not valid.", (Exception?)null);
                }
                CustomMessageText = customMessageText;
            }

            // The hashtable was correctly defined, assign the remaining values.
            Strings = new RestartDialogStrings(strings, deploymentType);
        }

        /// <summary>
        /// The strings used for the RestartDialog.
        /// </summary>
        [DataMember]
        public readonly RestartDialogStrings Strings;

        /// <summary>
        /// The duration for which the countdown will be displayed.
        /// </summary>
        [DataMember]
        public readonly TimeSpan? CountdownDuration;

        /// <summary>
        /// The duration for which the countdown will be displayed without minimizing the dialog.
        /// </summary>
        [DataMember]
        public readonly TimeSpan? CountdownNoMinimizeDuration;

        /// <summary>
        /// Represents a custom message text that can be optionally provided.
        /// </summary>
        [DataMember]
        public readonly string? CustomMessageText;

        /// <summary>
        /// The strings used for the RestartDialog.
        /// </summary>
        [DataContract]
        public sealed record RestartDialogStrings
        {
            /// <summary>
            /// Initializes the <see cref="RestartDialogStrings"/> class and registers it as a serializable type.
            /// </summary>
            /// <remarks>This static constructor ensures that the <see cref="RestartDialogStrings"/> type is added
            /// to the list of serializable types for data contract serialization. This allows instances of <see
            /// cref="ClientException"/> to be serialized and deserialized using data contract serializers.</remarks>
            static RestartDialogStrings()
            {
                DataContractSerialization.AddSerializableType(typeof(RestartDialogStrings));
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="RestartDialogStrings"/> class with the specified strings.
            /// </summary>
            /// <param name="strings"></param>
            /// <param name="deploymentType"></param>
            /// <exception cref="ArgumentNullException"></exception>
            internal RestartDialogStrings(Hashtable strings, DeploymentType deploymentType)
            {
                // Nothing here is allowed to be null.
                if (strings["Title"] is not string title || string.IsNullOrWhiteSpace(title))
                {
                    throw new ArgumentNullException("Title value is null or invalid.", (Exception?)null);
                }
                if (strings["Message"] is not Hashtable messageTable || messageTable[deploymentType.ToString()] is not string message || string.IsNullOrWhiteSpace(message))
                {
                    throw new ArgumentNullException("Message value is null or invalid.", (Exception?)null);
                }
                if (strings["MessageTime"] is not string messageTime || string.IsNullOrWhiteSpace(messageTime))
                {
                    throw new ArgumentNullException("MessageTime value is null or invalid.", (Exception?)null);
                }
                if (strings["MessageRestart"] is not string messageRestart || string.IsNullOrWhiteSpace(messageRestart))
                {
                    throw new ArgumentNullException("MessageRestart value is null or invalid.", (Exception?)null);
                }
                if (strings["TimeRemaining"] is not string timeRemaining || string.IsNullOrWhiteSpace(timeRemaining))
                {
                    throw new ArgumentNullException("TimeRemaining value is null or invalid.", (Exception?)null);
                }
                if (strings["ButtonRestartNow"] is not string buttonRestartNow || string.IsNullOrWhiteSpace(buttonRestartNow))
                {
                    throw new ArgumentNullException("ButtonRestartNow value is null or invalid.", (Exception?)null);
                }
                if (strings["ButtonRestartLater"] is not string buttonRestartLater || string.IsNullOrWhiteSpace(buttonRestartLater))
                {
                    throw new ArgumentNullException("ButtonRestartLater value is null or invalid.", (Exception?)null);
                }

                // The hashtable was correctly defined, assign the remaining values.
                Title = title;
                Message = message;
                MessageTime = messageTime;
                MessageRestart = messageRestart;
                TimeRemaining = timeRemaining;
                ButtonRestartNow = buttonRestartNow;
                ButtonRestartLater = buttonRestartLater;
            }

            /// <summary>
            /// Text displayed in the title of the restart prompt which helps the script identify whether there is already a restart prompt being displayed and not to duplicate it.
            /// </summary>
            [DataMember]
            public readonly string Title;

            /// <summary>
            /// Text displayed when the device requires a restart.
            /// </summary>
            [DataMember]
            public readonly string Message;

            /// <summary>
            /// Text displayed as a prefix to the time remaining, indicating that users should save their work, etc.
            /// </summary>
            [DataMember]
            public readonly string MessageTime;

            /// <summary>
            /// Text displayed when indicating when the device will be restarted.
            /// </summary>
            [DataMember]
            public readonly string MessageRestart;

            /// <summary>
            /// Text displayed to indicate the amount of time remaining until a restart will occur.
            /// </summary>
            [DataMember]
            public readonly string TimeRemaining;

            /// <summary>
            /// Button text for when wanting to restart the device now.
            /// </summary>
            [DataMember]
            public readonly string ButtonRestartNow;

            /// <summary>
            /// Button text for allowing the user to restart later.
            /// </summary>
            [DataMember]
            public readonly string ButtonRestartLater;
        }
    }
}
