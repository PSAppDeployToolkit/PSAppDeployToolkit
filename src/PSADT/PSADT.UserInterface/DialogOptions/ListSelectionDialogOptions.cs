using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        /// Initializes a new instance of the <see cref="ListSelectionDialogOptions"/> class.
        /// </summary>
        /// <param name="options"></param>
        public ListSelectionDialogOptions(Hashtable options) : this(
            (options ?? throw new ArgumentNullException(nameof(options)))["AppTitle"] as string ?? null!,
            options["Subtitle"] as string ?? null!,
            options["AppIconImage"] as string ?? null!,
            options["AppIconDarkImage"] as string ?? null!,
            options["AppBannerImage"] as string ?? null!,
            options["AppTaskbarIconImage"] as string,
            options["DialogTopMost"] as bool? ?? false,
            options["Language"] as CultureInfo ?? null!,
            options["FluentAccentColor"] as int?,
            options["DialogPosition"] as DialogPosition?,
            options["DialogAllowMove"] as bool?,
            options["DialogExpiryDuration"] as TimeSpan?,
            options["DialogPersistInterval"] as TimeSpan?,
            options["MessageText"] as string ?? null!,
            options["MessageAlignment"] as DialogMessageAlignment?,
            options["ButtonLeftText"] as string,
            options["ButtonMiddleText"] as string,
            options["ButtonRightText"] as string,
            options["Icon"] as DialogSystemIcon?,
            options["MinimizeWindows"] as bool? ?? false,
            options["ListItems"] as string[] ?? null!,
            options["InitialSelectedItem"] as string ?? null!,
            options["Strings"] as Hashtable is { Count: > 0 } strings ? new ListSelectionDialogStrings(strings) : null)
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
        /// <param name="dialogPosition">The position of the dialog on the screen. If <see langword="null"/>, the default position is used.</param>
        /// <param name="dialogAllowMove">Indicates whether the dialog can be moved by the user. If <see langword="null"/>, the default behavior is
        /// used.</param>
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
        /// <param name="icon">The system icon displayed in the dialog. If <see langword="null"/>, no icon is displayed.</param>
        /// <param name="minimizeWindows">A value indicating whether all other windows should be minimized when the dialog is displayed.</param>
        /// <param name="listItems">The list of items to display for user selection. Cannot be <see langword="null"/>.</param>
        /// <param name="initialSelectedItem">The item that should be selected by default. Must exist in <paramref name="listItems"/>.</param>
        /// <param name="strings">The localized strings for the dialog. If <see langword="null"/>, the dialog falls back to XAML defaults.</param>
        private ListSelectionDialogOptions(string appTitle, string subtitle, string appIconImage, string appIconDarkImage, string appBannerImage, string? appTaskbarIconImage, bool dialogTopMost, CultureInfo language, int? fluentAccentColor, DialogPosition? dialogPosition, bool? dialogAllowMove, TimeSpan? dialogExpiryDuration, TimeSpan? dialogPersistInterval, string messageText, DialogMessageAlignment? messageAlignment, string? buttonLeftText, string? buttonMiddleText, string? buttonRightText, DialogSystemIcon? icon, bool minimizeWindows, IReadOnlyList<string> listItems, string initialSelectedItem, ListSelectionDialogStrings? strings) : base(appTitle, subtitle, appIconImage, appIconDarkImage, appBannerImage, appTaskbarIconImage, dialogTopMost, language, fluentAccentColor, dialogPosition, dialogAllowMove, dialogExpiryDuration, dialogPersistInterval, messageText, messageAlignment, buttonLeftText, buttonMiddleText, buttonRightText, icon, minimizeWindows)
        {
            ListItems = listItems ?? throw new ArgumentNullException(nameof(listItems), "ListItems cannot be null for a ListSelectionDialog.");
            InitialSelectedItem = !string.IsNullOrWhiteSpace(initialSelectedItem) ? initialSelectedItem : throw new ArgumentNullException(nameof(initialSelectedItem), "InitialSelectedItem cannot be null or empty for a ListSelectionDialog.");
            _ = Enumerable.Contains(ListItems, InitialSelectedItem, StringComparer.CurrentCultureIgnoreCase) ? true : throw new ArgumentException("InitialSelectedItem must exist in ListItems.", nameof(initialSelectedItem));
            Strings = strings;
        }

        /// <summary>
        /// The list of items to display for user selection.
        /// </summary>
        [DataMember]
        public IReadOnlyList<string> ListItems { get; private set; }

        /// <summary>
        /// The item that should be selected by default.
        /// </summary>
        [DataMember]
        public string InitialSelectedItem { get; private set; }

        /// <summary>
        /// The localized strings for the ListSelectionDialog.
        /// </summary>
        [DataMember]
        public ListSelectionDialogStrings? Strings { get; private set; }

        /// <summary>
        /// Localized strings for the ListSelectionDialog.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "The nesting in this case is alright.")]
        [DataContract]
        public sealed record ListSelectionDialogStrings
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ListSelectionDialogStrings"/> class.
            /// </summary>
            /// <param name="strings"></param>
            internal ListSelectionDialogStrings(Hashtable strings) : this(strings["ListSelectionMessage"] as string ?? null!)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ListSelectionDialogStrings"/> class with the specified strings.
            /// </summary>
            /// <param name="listSelectionMessage">The heading text displayed next to the list selection dropdown.</param>
            private ListSelectionDialogStrings(string listSelectionMessage)
            {
                ListSelectionMessage = listSelectionMessage;
            }

            /// <summary>
            /// The heading text displayed next to the list selection dropdown.
            /// </summary>
            [DataMember]
            public string ListSelectionMessage { get; private set; }
        }
    }
}
