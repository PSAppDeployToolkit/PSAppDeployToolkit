using System;
using System.ComponentModel;
using PSADT.ClientServer;
using PSADT.UserInterface.DialogOptions;

namespace PSADT.UserInterface.Interfaces.Classic
{
    /// <summary>
    /// Represents a console-based help dialog that provides assistance or guidance to users.
    /// </summary>
    /// <remarks>The <see cref="HelpConsole"/> class is intended for use in design-time scenarios and should
    /// not be instantiated directly in runtime mode. Attempting to use the parameterless constructor in runtime mode
    /// will result in an <see cref="InvalidOperationException"/>.</remarks>
    internal partial class HelpConsole : ClassicBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HelpConsole"/> class.
        /// </summary>
        internal HelpConsole() : this(null!)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                throw new InvalidOperationException("This constructor cannot be used in runtime mode.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassicDialog"/> class with the specified options.
        /// </summary>
        /// <param name="options"></param>
        internal HelpConsole(HelpConsoleOptions options) : base()
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
                    listBox.Items.Clear(); listBox.Items.AddRange([.. options.ModuleHelpMap[(string)comboBox.SelectedItem!].Keys]);
                };
                listBox.SelectedIndexChanged += (sender, e) =>
                {
                    richTextBox.Clear(); richTextBox.Text = options.ModuleHelpMap[(string)comboBox.SelectedItem!][(string)listBox.SelectedItem!];
                };

                // Lastly, set the initial selected index of the ComboBox to the first item, if available.
                if (comboBox.Items.Count > 0)
                {
                    comboBox.SelectedIndex = 0;
                }
            }

            // Set the NoWait success flag as the caller may be waiting for it.
            Load += (sender, e) => ClientServerUtilities.SetClientServerOperationSuccess();
        }
    }
}
