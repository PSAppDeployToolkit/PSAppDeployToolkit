using System;
using System.ComponentModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using PSADT.UserInterface.DialogOptions;

namespace PSADT.UserInterface.Dialogs.Classic
{
    /// <summary>
    /// Represents a console-based help dialog that provides assistance or guidance to users.
    /// </summary>
    /// <remarks>The <see cref="HelpConsole"/> class is intended for use in design-time scenarios and should
    /// not be instantiated directly in runtime mode. Attempting to use the parameterless constructor in runtime mode
    /// will result in an <see cref="InvalidOperationException"/>.</remarks>
    internal partial class HelpConsole : BaseDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HelpConsole"/> class.
        /// </summary>
        internal HelpConsole() : this(default!)
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
            if (null != options)
            {
                // Null out PSModulePath to prevent any module conflicts.
                // https://github.com/PowerShell/PowerShell/issues/18530#issuecomment-1325691850
                Environment.SetEnvironmentVariable("PSModulePath", null);

                // Set up a PowerShell initial session state.
                var iss = InitialSessionState.CreateDefault2();
                iss.ExecutionPolicy = options.ExecutionPolicy;
                iss.ImportPSModule(options.ModulePaths.ToArray());

                // Set up a runspace and open it for usage.
                this.runspace = RunspaceFactory.CreateRunspace(iss);
                this.runspace.Open();

                // Populate the ComboBox with our modules.
                using (var ps = PowerShell.Create())
                {
                    ps.Runspace = this.runspace;
                    this.comboBox.Items.Clear();
                    this.comboBox.Items.AddRange(ps.AddCommand("Get-Module").Invoke<PSModuleInfo>().Where(m => options.ModulePaths.Contains(m.ModuleBase)).ToArray());
                }

                // Set up the ComboBox event handler.
                this.comboBox.SelectedIndexChanged += (sender, e) =>
                {
                    // Update the listbox with the commands from the selected module.
                    this.listBox.Items.Clear(); this.listBox.Items.AddRange(((PSModuleInfo)this.comboBox.SelectedItem!).ExportedCommands.Keys.ToArray());
                };

                // Set up the ListBox event handler.
                this.listBox.SelectedIndexChanged += (sender, e) =>
                {
                    // Clear the text box.
                    using (var ps = PowerShell.Create())
                    {
                        ps.Runspace = this.runspace;
                        this.richTextBox.Clear();
                        this.richTextBox.Text = string.Join("\n", ps.AddCommand("Get-Help").AddParameter("Name", (string)this.listBox.SelectedItem!).AddParameter("Full", true).AddCommand("Out-String").AddParameter("Width", int.MaxValue).AddParameter("Stream", true).Invoke<string>().Select(static s => !string.IsNullOrWhiteSpace(s) ? s.TrimEnd() : null)).Trim().Replace("<br />", null) + "\n";
                    }
                };

                // Ensure the runspace is closed when the form is closed.
                this.FormClosed += (sender, e) =>
                {
                    // Ensure the runspace is closed when the form is closed.
                    if (null != this.runspace && this.runspace.RunspaceStateInfo.State == RunspaceState.Opened)
                    {
                        this.runspace.Close();
                        this.runspace.Dispose();
                        this.runspace = null!;
                    }
                };

                // Lastly, set the initial selected index of the ComboBox to the first item, if available.
                if (this.comboBox.Items.Count > 0)
                {
                    this.comboBox.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        /// Represents the PowerShell runspace used to execute commands and scripts.
        /// </summary>
        /// <remarks>A runspace is a container for the execution environment of PowerShell commands. This
        /// field is initialized before use and should not be null during runtime.</remarks>
        private Runspace runspace = null!;
    }
}
