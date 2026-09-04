using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Interfaces.Classic;
using PSADT.UserInterface.Interfaces.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests.Classic
{
    /// <summary>
    /// Tests the console that browses the toolkit's own command help.
    /// </summary>
    /// <remarks>
    /// An administrator's tool rather than an end user's, and the only dialog here with no options
    /// beyond the help itself. Its whole behaviour is two linked selections: choosing a module fills the
    /// command list, and choosing a command fills the help pane. Both are wired as event handlers in the
    /// constructor, so they are reached by changing the selection rather than by calling anything.
    /// </remarks>
    public sealed class HelpConsoleTests
    {
        /// <summary>
        /// Verifies that the modules are offered for selection.
        /// </summary>
        [Fact]
        public void Constructor_OffersEveryModule()
        {
            // Act
            using HelpConsole console = Build(Help(("Alpha", "One", "what one does"), ("Bravo", "Two", "what two does")));

            // Assert
            ComboBox modules = FormControls.Find<ComboBox>(console, "comboBox");
            Assert.Equal(["Alpha", "Bravo"], modules.Items.Cast<string>(), StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that the first module is selected to start with.
        /// </summary>
        /// <remarks>
        /// Opening on an empty console would leave an administrator to work out that the dropdown needs
        /// touching before anything appears. Selecting the first module also runs the handler that fills
        /// the command list, so the console opens with something in it.
        /// </remarks>
        [Fact]
        public void Constructor_SelectsTheFirstModule()
        {
            // Act
            using HelpConsole console = Build(Help(("Alpha", "One", "what one does"), ("Bravo", "Two", "what two does")));

            // Assert
            Assert.Equal(0, FormControls.Find<ComboBox>(console, "comboBox").SelectedIndex);
            Assert.Equal(["One"], FormControls.Find<ListBox>(console, "listBox").Items.Cast<string>(), StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that choosing a different module replaces the list of commands.
        /// </summary>
        /// <remarks>
        /// Replaces rather than appends. A handler that forgot to clear first would leave the previous
        /// module's commands in the list, and choosing one of them would then fail to find its help.
        /// </remarks>
        [Fact]
        public void ModuleSelection_ReplacesTheCommandList()
        {
            // Arrange
            using HelpConsole console = Build(Help(("Alpha", "One", "what one does"), ("Bravo", "Two", "what two does")));

            // Act
            DialogHost.Run(() => FormControls.Select(FormControls.Find<ComboBox>(console, "comboBox"), 1));

            // Assert
            Assert.Equal(["Two"], FormControls.Find<ListBox>(console, "listBox").Items.Cast<string>(), StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that choosing a command shows its help.
        /// </summary>
        [Fact]
        public void CommandSelection_ShowsThatCommandsHelp()
        {
            // Arrange
            using HelpConsole console = Build(Help(("Alpha", "One", "what one does")));

            // Act
            DialogHost.Run(() => FormControls.Select(FormControls.Find<ListBox>(console, "listBox"), 0));

            // Assert
            Assert.Equal("what one does", FormControls.Find<RichTextBox>(console, "richTextBox").Text);
        }

        /// <summary>
        /// Verifies that a console with no modules to show opens without complaint.
        /// </summary>
        /// <remarks>
        /// The initial selection is only made when there is something to select. Without that guard an
        /// empty help map - which is what a module that failed to load its help would produce - would
        /// throw while the console was being built rather than opening empty.
        /// </remarks>
        [Fact]
        public void Constructor_OpensEmptyWhenThereIsNoHelp()
        {
            // Act
            using HelpConsole console = Build(Help());

            // Assert
            Assert.Empty(FormControls.Find<ComboBox>(console, "comboBox").Items);
            Assert.Equal(-1, FormControls.Find<ComboBox>(console, "comboBox").SelectedIndex);
        }

        /// <summary>
        /// Verifies that the designer's parameterless constructor is refused at runtime.
        /// </summary>
        /// <remarks>
        /// It exists for the Windows Forms designer, which builds a form with no arguments in order to
        /// draw it. A test run is runtime by definition, so the guard fires.
        /// </remarks>
        [Fact]
        public void DesignerConstructor_RefusesToBuildAtRuntime()
        {
            // Act & Assert
            _ = DialogHost.Run(static () => Assert.Throws<NotSupportedException>(static () => new HelpConsole()));
        }

        /// <summary>
        /// Builds a help console on the shared apartment over the given help map.
        /// </summary>
        /// <param name="table">The options to build it from.</param>
        /// <returns>The console, which the caller owns.</returns>
        private static HelpConsole Build(Hashtable table)
        {
            return DialogHost.Run(() => new HelpConsole(new HelpConsoleOptions(table)));
        }

        /// <summary>
        /// Assembles a help map from module, command and help text triples.
        /// </summary>
        /// <param name="entries">One triple per command, grouped by the module named first.</param>
        /// <returns>The options table carrying the map.</returns>
        private static Hashtable Help(params (string Module, string Command, string Text)[] entries)
        {
            Dictionary<string, IReadOnlyDictionary<string, string>> map = new(StringComparer.Ordinal);
            foreach ((string module, string command, string text) in entries)
            {
                if (!map.TryGetValue(module, out IReadOnlyDictionary<string, string>? commands))
                {
                    map[module] = commands = new Dictionary<string, string>(StringComparer.Ordinal);
                }
                ((Dictionary<string, string>)commands)[command] = text;
            }
            return new Hashtable { ["ModuleHelpMap"] = map };
        }
    }
}
