using System.Collections.Generic;
using System.Collections.ObjectModel;
using PSADT.ClientServer.Payloads;
using PSADT.ClientServer.Server.Tests.TestHelpers;
using PSADT.UserInterface;
using PSADT.UserInterface.DialogOptions;
using Xunit;

namespace PSADT.ClientServer.Server.Tests.Payloads
{
    /// <summary>
    /// Tests the payload asking a client to show one of the modal dialogs.
    /// </summary>
    /// <remarks>
    /// The only payload whose contents are not of a type known when it is written. Every modal dialog goes
    /// through it, and the options it carries are typed as the interface they all implement, so what
    /// actually crosses the wire depends on which dialog was asked for. That makes two things worth
    /// asserting that the other payloads do not need: the concrete type of the options has to survive the
    /// trip, which it only does because the payload names each of them as a known type; and the comparison
    /// has to reach through the interface to the concrete record behind it.
    /// </remarks>
    public sealed class ShowModalDialogPayloadTests
    {
        /// <summary>
        /// Verifies that all three parts it was built with are carried.
        /// </summary>
        [Fact]
        public void ShowModalDialogPayload_CarriesItsTypeStyleAndOptions()
        {
            // Arrange
            ShowModalDialogPayload payload = new(DialogType.DialogBox, DialogStyle.Classic, SampleOptions.DialogBox());

            // Assert
            Assert.Equal(DialogType.DialogBox, payload.DialogType);
            Assert.Equal(DialogStyle.Classic, payload.DialogStyle);
            Assert.Equal<IDialogOptions>(SampleOptions.DialogBox(), payload.Options);
        }

        /// <summary>
        /// Verifies that all three parts count towards the comparison.
        /// </summary>
        /// <remarks>
        /// The options are compared through the interface, so this only holds because the concrete type
        /// behind it is a record whose own comparison is by value. A dialog options type that was not
        /// would make two identical requests unequal, and nothing about this payload would show it.
        /// </remarks>
        [Fact]
        public void ShowModalDialogPayload_ComparesByAllThree()
        {
            Assert.Equal(
                new ShowModalDialogPayload(DialogType.DialogBox, DialogStyle.Classic, SampleOptions.DialogBox()),
                new ShowModalDialogPayload(DialogType.DialogBox, DialogStyle.Classic, SampleOptions.DialogBox()));
            Assert.NotEqual(
                new ShowModalDialogPayload(DialogType.DialogBox, DialogStyle.Classic, SampleOptions.DialogBox()),
                new ShowModalDialogPayload(DialogType.CustomDialog, DialogStyle.Classic, SampleOptions.DialogBox()));
            Assert.NotEqual(
                new ShowModalDialogPayload(DialogType.DialogBox, DialogStyle.Classic, SampleOptions.DialogBox()),
                new ShowModalDialogPayload(DialogType.DialogBox, DialogStyle.Fluent, SampleOptions.DialogBox()));
            Assert.NotEqual(
                new ShowModalDialogPayload(DialogType.DialogBox, DialogStyle.Classic, SampleOptions.DialogBox()),
                new ShowModalDialogPayload(DialogType.DialogBox, DialogStyle.Classic, SampleOptions.DialogBox("something else")));
        }

        /// <summary>
        /// Verifies that the concrete kind of options survives the trip, rather than arriving as the
        /// interface it was declared as.
        /// </summary>
        /// <remarks>
        /// This is what the payload's list of known dialog types buys. Without the concrete type named
        /// there, the serializer has nothing to write the options as and the client has nothing to rebuild
        /// them into.
        /// </remarks>
        [Fact]
        public void ShowModalDialogPayload_SurvivesTheTripAsTheDialogItAsksFor()
        {
            // Arrange
            ShowModalDialogPayload original = new(DialogType.DialogBox, DialogStyle.Classic, SampleOptions.DialogBox());

            // Act
            ShowModalDialogPayload restored = DataSerialization.DeserializeFromBytes<ShowModalDialogPayload>(DataSerialization.SerializeToBytes(original));

            // Assert
            Assert.Equal(original, restored);
            DialogBoxOptions options = Assert.IsType<DialogBoxOptions>(restored.Options);
            Assert.Equal("the message", options.MessageText);
        }

        /// <summary>
        /// Verifies that a payload carrying options which themselves hold a list compares by that list's
        /// contents.
        /// </summary>
        /// <remarks>
        /// The list selection dialog is the only one holding a collection, and holding one is exactly what
        /// breaks a record's comparison: two payloads offering the same choices used to come out unequal
        /// because the lists behind them were different objects. Asserted here rather than beside the
        /// options themselves because this is where it matters - a request to show a dialog is the thing
        /// callers compare.
        /// </remarks>
        [Fact]
        public void ShowModalDialogPayload_ComparesOptionsHoldingAListByItsContents()
        {
            // Arrange: two payloads offering the same choices, through different lists
            ShowModalDialogPayload first = new(DialogType.ListSelectionDialog, DialogStyle.Fluent, SampleOptions.ListSelectionDialog("alpha", "bravo"));
            ShowModalDialogPayload second = new(DialogType.ListSelectionDialog, DialogStyle.Fluent, SampleOptions.ListSelectionDialog("alpha", "bravo"));

            // Assert
            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
            Assert.NotEqual(first, new ShowModalDialogPayload(DialogType.ListSelectionDialog, DialogStyle.Fluent, SampleOptions.ListSelectionDialog("alpha", "charlie")));
            Assert.NotEqual(first, new ShowModalDialogPayload(DialogType.ListSelectionDialog, DialogStyle.Fluent, SampleOptions.ListSelectionDialog("bravo", "alpha")));
        }

        /// <summary>
        /// Verifies that a payload carrying options which hold a dictionary of dictionaries compares by
        /// those entries, all the way down.
        /// </summary>
        /// <remarks>
        /// The help console is the only options type holding a mapping, and the only one whose comparison
        /// needed a dictionary that compares by value to exist at all. Both levels count: an outer mapping
        /// comparing by its entries is no use if the inner ones fall back to comparing by reference.
        /// </remarks>
        [Fact]
        public void ShowModalDialogPayload_ComparesOptionsHoldingAMappingByItsEntries()
        {
            // Arrange: two payloads offering the same help, through different dictionaries
            ShowModalDialogPayload first = new(DialogType.HelpConsole, DialogStyle.Classic, SampleOptions.HelpConsole());
            ShowModalDialogPayload second = new(DialogType.HelpConsole, DialogStyle.Classic, SampleOptions.HelpConsole());

            // Assert
            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
            Assert.NotEqual(first, new ShowModalDialogPayload(DialogType.HelpConsole, DialogStyle.Classic, SampleOptions.HelpConsole("something else")));
        }

        /// <summary>
        /// Verifies that options holding a list and a mapping survive the trip with their contents, and are
        /// handed back as concrete collections rather than as the interfaces they are declared through.
        /// </summary>
        /// <remarks>
        /// The second half matters because of where these end up. PowerShell cannot work with an interface,
        /// so what a caller reads back has to be a dictionary it can index rather than something only the
        /// type system understands - at both levels of the help map.
        /// </remarks>
        [Fact]
        public void ShowModalDialogPayload_SurvivesTheTripWithCollectionsIntact()
        {
            // Arrange
            ShowModalDialogPayload list = new(DialogType.ListSelectionDialog, DialogStyle.Fluent, SampleOptions.ListSelectionDialog("alpha", "bravo"));
            ShowModalDialogPayload help = new(DialogType.HelpConsole, DialogStyle.Classic, SampleOptions.HelpConsole());

            // Act
            ShowModalDialogPayload restoredList = DataSerialization.DeserializeFromBytes<ShowModalDialogPayload>(DataSerialization.SerializeToBytes(list));
            ShowModalDialogPayload restoredHelp = DataSerialization.DeserializeFromBytes<ShowModalDialogPayload>(DataSerialization.SerializeToBytes(help));

            // Assert
            Assert.Equal(list, restoredList);
            Assert.Equal(["alpha", "bravo"], Assert.IsType<ListSelectionDialogOptions>(restoredList.Options).ListItems);
            Assert.Equal(help, restoredHelp);
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> moduleHelpMap = Assert.IsType<HelpConsoleOptions>(restoredHelp.Options).ModuleHelpMap;
            _ = Assert.IsType<ReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>>(moduleHelpMap);
            _ = Assert.IsType<ReadOnlyDictionary<string, string>>(moduleHelpMap["a module"]);
            Assert.Equal("what it does", moduleHelpMap["a module"]["a function"]);
        }

        /// <summary>
        /// Verifies that a progress dialog's options survive the same trip, since the payload names more
        /// than one kind and a list that only worked for the first would go unnoticed.
        /// </summary>
        [Fact]
        public void ShowModalDialogPayload_SurvivesTheTripForMoreThanOneKindOfDialog()
        {
            // Arrange
            ShowModalDialogPayload original = new(DialogType.ProgressDialog, DialogStyle.Fluent, SampleOptions.ProgressDialog());

            // Act
            ShowModalDialogPayload restored = DataSerialization.DeserializeFromBytes<ShowModalDialogPayload>(DataSerialization.SerializeToBytes(original));

            // Assert
            Assert.Equal(original, restored);
            _ = Assert.IsType<ProgressDialogOptions>(restored.Options);
        }
    }
}
