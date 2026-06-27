using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.Serialization;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for the ListSelectionDialog.
    /// </summary>
    [DataContract]
    public sealed record ListSelectionDialogOptions : CustomDialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the ListSelectionDialogOptions class using the specified configuration
        /// options.
        /// </summary>
        /// <remarks>The options dictionary must not be null and should contain keys corresponding to the
        /// dialog's configurable properties. If a key is missing, a default value may be applied for that option. Refer
        /// to the documentation for the expected keys and value types.</remarks>
        /// <param name="options">A dictionary containing key-value pairs that define the dialog's configuration, such as application title,
        /// subtitle, images, dialog behavior, and other settings. Keys should match the expected option names; missing
        /// keys may result in default values being used.</param>
        /// <exception cref="ArgumentNullException">Thrown if the options dictionary is null.</exception>
        public ListSelectionDialogOptions(IDictionary options) : this(
            (string?)(options ?? throw new ArgumentNullException(nameof(options)))["AppTitle"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'AppTitle' is missing."),
            (string?)options["Subtitle"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'Subtitle' is missing."),
            (string?)options["AppIconImage"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'AppIconImage' is missing."),
            (string?)options["AppIconDarkImage"],
            (string?)options["AppBannerImage"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'AppBannerImage' is missing."),
            (string?)options["AppTaskbarIconImage"],
            (bool?)options["DialogTopMost"] ?? false,
            (CultureInfo?)options["Language"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'Language' is missing."),
            (int?)options["FluentAccentColor"],
            (int?)options["FluentAccentColorDark"],
            (DialogPosition?)options["DialogPosition"],
            (bool?)options["DialogAllowMove"],
            (bool?)options["DialogAllowMinimize"],
            (TimeSpan?)options["DialogExpiryDuration"],
            (TimeSpan?)options["DialogPersistInterval"],
            (string?)options["MessageText"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'MessageText' is missing."),
            (DialogMessageAlignment?)options["MessageAlignment"],
            (string?)options["ButtonLeftText"],
            (string?)options["ButtonMiddleText"],
            (string?)options["ButtonRightText"],
            (DialogDefaultButton?)options["DefaultButton"],
            (DialogSystemIcon?)options["Icon"],
            (bool?)options["MinimizeWindows"] ?? false,
            (IReadOnlyList<string>?)options["ListItems"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'ListItems' is missing."),
            (int?)options["SelectedIndex"],
            new((IDictionary?)options["Strings"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'Strings' is missing.")))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ListSelectionDialogOptions"/> class with the specified dialog
        /// configuration options.
        /// </summary>
        /// <param name="appTitle">The title of the application displayed in the dialog.</param>
        /// <param name="subtitle">The subtitle text displayed in the dialog.</param>
        /// <param name="appIconImage">The path to the application's icon image used in the dialog.</param>
        /// <param name="appIconDarkImage">The path to the application's dark mode icon image used in the dialog.</param>
        /// <param name="appBannerImage">The path to the banner image displayed in the dialog.</param>
        /// <param name="appTaskbarIconImage">The path to the application's tray icon image used in the dialog. If <see langword="null"/>,
        /// the default tray icon is used.</param>
        /// <param name="dialogTopMost">A value indicating whether the dialog should always appear on top of other windows.</param>
        /// <param name="language">The culture information used for localizing the dialog.</param>
        /// <param name="fluentAccentColor">The accent color used for Fluent design elements in the dialog. If <see langword="null"/>, the default
        /// accent color is used.</param>
        /// <param name="fluentAccentColorDark">The accent color used for Fluent design elements in the dialog when in dark mode. If <see langword="null"/>, the default dark accent color is used.</param>
        /// <param name="dialogPosition">The position of the dialog on the screen. If <see langword="null"/>, the default position is used.</param>
        /// <param name="dialogAllowMove">Indicates whether the dialog can be moved by the user. If <see langword="null"/>, the default behavior is
        /// used.</param>
        /// <param name="dialogAllowMinimize">Indicates whether the dialog exposes a minimize button in its caption area. If <see langword="null"/> or
        /// <see langword="false"/>, the minimize button remains hidden.</param>
        /// <param name="dialogExpiryDuration">The duration after which the dialog expires and closes automatically. If <see langword="null"/>, the dialog
        /// does not expire.</param>
        /// <param name="dialogPersistInterval">The interval at which the dialog persists its state. If <see langword="null"/>, persistence is disabled.</param>
        /// <param name="messageText">The main message text displayed in the dialog.</param>
        /// <param name="messageAlignment">The alignment of the message text within the dialog. If <see langword="null"/>, the default alignment is
        /// used.</param>
        /// <param name="buttonLeftText">The text displayed on the left button in the dialog. If <see langword="null"/>, the button is not displayed.</param>
        /// <param name="buttonMiddleText">The text displayed on the middle button in the dialog. If <see langword="null"/>, the button is not
        /// displayed.</param>
        /// <param name="buttonRightText">The text displayed on the right button in the dialog. If <see langword="null"/>, the button is not
        /// displayed.</param>
        /// <param name="defaultButton">Indicates which button is the default button in the dialog. If <see langword="null"/>, no default button is set.</param>
        /// <param name="icon">The system icon displayed in the dialog. If <see langword="null"/>, no icon is displayed.</param>
        /// <param name="minimizeWindows">A value indicating whether all other windows should be minimized when the dialog is displayed.</param>
        /// <param name="listItems">The list of items to display for user selection. Cannot be <see langword="null"/>.</param>
        /// <param name="selectedIndex">The index for the default item to be displayed for user selection.</param>
        /// <param name="strings">The localized strings for the dialog. If <see langword="null"/>, the dialog falls back to XAML defaults.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3236:Caller information arguments should not be provided explicitly", Justification = "This is intentional as we're testing a parameter member.")]
        private ListSelectionDialogOptions(string appTitle, string subtitle, string appIconImage, string? appIconDarkImage, string appBannerImage, string? appTaskbarIconImage, bool dialogTopMost, CultureInfo language, int? fluentAccentColor, int? fluentAccentColorDark, DialogPosition? dialogPosition, bool? dialogAllowMove, bool? dialogAllowMinimize, TimeSpan? dialogExpiryDuration, TimeSpan? dialogPersistInterval, string messageText, DialogMessageAlignment? messageAlignment, string? buttonLeftText, string? buttonMiddleText, string? buttonRightText, DialogDefaultButton? defaultButton, DialogSystemIcon? icon, bool minimizeWindows, IReadOnlyList<string> listItems, int? selectedIndex, ListSelectionDialogStrings strings) : base(appTitle, subtitle, appIconImage, appIconDarkImage, appBannerImage, appTaskbarIconImage, dialogTopMost, language, fluentAccentColor, fluentAccentColorDark, dialogPosition, dialogAllowMove, dialogAllowMinimize, dialogExpiryDuration, dialogPersistInterval, messageText, messageAlignment, buttonLeftText, buttonMiddleText, buttonRightText, defaultButton, icon, minimizeWindows)
        {
            ArgumentNullException.ThrowIfNull(strings);
            ArgumentNullException.ThrowIfNull(listItems);
            ArgumentOutOfRangeException.ThrowIfZero(listItems.Count, nameof(listItems));
            if (selectedIndex is not null && (selectedIndex.Value < 0 || selectedIndex.Value >= listItems.Count))
            {
                throw new ArgumentOutOfRangeException(nameof(selectedIndex), selectedIndex, "SelectedIndex must be a valid index within ListItems.");
            }
            ListItems = new ReadOnlyCollection<string>([.. listItems]);
            SelectedIndex = selectedIndex;
            Strings = strings;
        }

        /// <summary>
        /// The list of items to display for user selection.
        /// </summary>
        [DataMember]
        public readonly IReadOnlyList<string> ListItems;

        /// <summary>
        /// The item that should be selected by default.
        /// </summary>
        [DataMember]
        public readonly int? SelectedIndex;

        /// <summary>
        /// The localized strings for the ListSelectionDialog.
        /// </summary>
        [DataMember]
        public readonly ListSelectionDialogStrings Strings;

        /// <summary>
        /// Localized strings for the ListSelectionDialog.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "The nesting in this case is alright.")]
        [DataContract]
        public sealed record ListSelectionDialogStrings
        {
            /// <summary>
            /// Initializes a new instance of the ListSelectionDialogStrings class using the specified collection of
            /// string resources.
            /// </summary>
            /// <remarks>If the 'ListSelectionMessage' key is not present in the dictionary, the
            /// message will be set to null.</remarks>
            /// <param name="strings">An IDictionary containing string resources. The entry with the key 'ListSelectionMessage' is used to
            /// provide the message for the dialog.</param>
            internal ListSelectionDialogStrings(IDictionary strings) : this((string?)(strings ?? throw new ArgumentNullException(nameof(strings)))["ListSelectionMessage"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'ListSelectionMessage' is missing."))
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ListSelectionDialogStrings"/> class with the specified strings.
            /// </summary>
            /// <param name="listSelectionMessage">The heading text displayed next to the list selection dropdown.</param>
            private ListSelectionDialogStrings(string listSelectionMessage)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(listSelectionMessage);
                ListSelectionMessage = listSelectionMessage;
            }

            /// <summary>
            /// The heading text displayed next to the list selection dropdown.
            /// </summary>
            [DataMember]
            public readonly string ListSelectionMessage;
        }
    }
}
