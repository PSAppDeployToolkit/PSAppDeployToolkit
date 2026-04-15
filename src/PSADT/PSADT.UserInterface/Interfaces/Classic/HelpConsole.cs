using System;
using System.ComponentModel;
using PSADT.Foundation;
using PSADT.UserInterface.DialogOptions;

namespace PSADT.UserInterface.Interfaces.Classic
{
    /// <summary>
    /// Represents a console-based help dialog that provides assistance or guidance to users.
    /// </summary>
    /// <remarks>The <see cref="HelpConsole"/> class is intended for use in design-time scenarios and should
    /// not be instantiated directly in runtime mode. Attempting to use the parameterless constructor in runtime mode
    /// will result in an <see cref="NotSupportedException"/>.</remarks>
    internal partial class HelpConsole : ClassicBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HelpConsole"/> class.
        /// </summary>
        internal HelpConsole() : this(null!)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                throw new NotSupportedException("This constructor cannot be used in runtime mode.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the HelpConsole class using the specified options for configuring help content
        /// display.
        /// </summary>
        /// <remarks>If the options parameter is provided, the ComboBox is populated with available
        /// modules, and event handlers are set up to update the list of help topics and their content based on user
        /// selection. The initial selected index of the ComboBox is set to the first item if available.</remarks>
        /// <param name="options">The options used to configure the HelpConsole, including a mapping of modules to their help content. This
        /// parameter cannot be null.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Smell", "S3220:Method calls should not resolve ambiguously to overloads with 'params'", Justification = "WinForms ObjectCollection.AddRange intentionally uses the params object[] overload with collection expressions here; adding LINQ casts only to satisfy the analyzer would add unnecessary noise.")]
        internal HelpConsole(HelpConsoleOptions options)
        {
            // Initialise the underlying form as set up by the designer.
            InitializeComponent();

            // Apply options to the form if we have any (i.e. not in the designer).
            if (options is not null)
            {
                // Populate the ComboBox with our modules and set up required event handlers.
                comboBox.Items.Clear(); comboBox.Items.AddRange([.. options.ModuleHelpMap.Keys]);
                comboBox.SelectedIndexChanged += (sender, e) =>
                {
                    listBox.Items.Clear(); listBox.Items.AddRange([.. options.ModuleHelpMap[(string)(comboBox.SelectedItem ?? throw new InvalidOperationException("The selected combobox item was null."))].Keys]);
                };
                listBox.SelectedIndexChanged += (sender, e) =>
                {
                    richTextBox.Clear(); richTextBox.Text = options.ModuleHelpMap[(string)(comboBox.SelectedItem ?? throw new InvalidOperationException("The selected combobox item was null."))][(string)(listBox.SelectedItem ?? throw new InvalidOperationException("The selected listbox item was null."))];
                };

                // Lastly, set the initial selected index of the ComboBox to the first item, if available.
                if (comboBox.Items.Count > 0)
                {
                    comboBox.SelectedIndex = 0;
                }
            }

            // Set the NoWait success flag as the caller may be waiting for it.
            Load += (sender, e) => ClientServerUtilities.SetOperationSuccessFlag();
        }
    }
}
