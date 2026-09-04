using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PSADT.ClientServer.Client.Tests.TestHelpers;
using Xunit;

namespace PSADT.ClientServer.Client.Tests
{
    /// <summary>
    /// Tests for <c language="csharp">ClientExecutable</c>, the client's single type.
    /// </summary>
    /// <remarks>
    /// Split by how a member has to be reached rather than by what it does. Everything except one
    /// method is private on a static class, so the helpers are called through reflection; the entry
    /// point and the standalone dispatch are reached by running the executable, because <c language="csharp">Main</c>
    /// answers an empty argument list with a modal dialog and answers a failure in a launcher with
    /// <c language="csharp">Environment.FailFast</c>.
    /// <para>
    /// Nothing here changes machine state. Where a switch would, only the guards that reject bad
    /// arguments before it acts are exercised, which for <c language="text">/SilentRestart</c> means the delay is never
    /// given a value that parses.
    /// </para>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "RCS1046:Add suffix 'Async' to asynchronous method name", Justification = "Test names describe the scenario under test; the async suffix would obscure them.")]
    public sealed class ClientExecutableTests
    {
        /// <summary>
        /// The type under test. <c>InternalsVisibleTo</c> makes it nameable; reflection reaches its
        /// members from there.
        /// </summary>
        private static readonly Type Subject = typeof(ClientExecutable);

        /// <summary>
        /// The reason a test is skipped when the client executables are not beside the assembly.
        /// </summary>
        private const string ClientRequired = "Requires the client executables alongside the test assembly.";

        #region ArgvToDictionary

        /// <summary>
        /// Confirms each switch takes the argument after it as its value.
        /// </summary>
        [Fact]
        public void ArgvToDictionary_PairsEachSwitchWithTheValueThatFollowsIt()
        {
            ReadOnlyDictionary<string, string> arguments = ArgvToDictionary(["-Alpha", "one", "-Beta", "two"]);
            Assert.Equal(2, arguments.Count);
            Assert.Equal("one", arguments["Alpha"]);
            Assert.Equal("two", arguments["Beta"]);
        }

        /// <summary>
        /// Confirms keys are matched without regard to case.
        /// </summary>
        [Fact]
        public void ArgvToDictionary_MatchesKeysWithoutRegardToCase()
        {
            Assert.Equal("value", ArgvToDictionary(["-Options", "value"])["oPtIoNs"]);
        }

        /// <summary>
        /// Confirms an operation switch is passed over rather than treated as a key.
        /// </summary>
        [Fact]
        public void ArgvToDictionary_IgnoresArgumentsThatAreNotSwitches()
        {
            ReadOnlyDictionary<string, string> arguments = ArgvToDictionary(["/GetLastInputTime", "-Alpha", "one"]);
            Assert.Equal("one", Assert.Single(arguments).Value);
        }

        /// <summary>
        /// Confirms surrounding whitespace is trimmed from both halves of a pair.
        /// </summary>
        [Fact]
        public void ArgvToDictionary_TrimsSurroundingWhitespaceFromBothHalves()
        {
            Assert.Equal("one", ArgvToDictionary(["-Alpha ", " one "])["Alpha"]);
        }

        /// <summary>
        /// Confirms an empty argument list parses to an empty dictionary rather than failing.
        /// </summary>
        [Fact]
        public void ArgvToDictionary_ReturnsNothingForAnEmptyArgumentList()
        {
            Assert.Empty(ArgvToDictionary([]));
        }

        /// <summary>
        /// Confirms a trailing switch with nothing after it is refused.
        /// </summary>
        [Fact]
        public void ArgvToDictionary_RefusesASwitchWithNoValueAfterIt()
        {
            AssertRefused(ClientExitCode.InvalidArguments, static () => ArgvToDictionary(["-Alpha"]));
        }

        /// <summary>
        /// Confirms a value that is blank or reads as another switch is refused.
        /// </summary>
        /// <param name="value">The value to offer.</param>
        [Theory]
        [InlineData("-Beta")]
        [InlineData("/GetLastInputTime")]
        [InlineData("   ")]
        public void ArgvToDictionary_RefusesAValueThatIsBlankOrAnotherSwitch(string value)
        {
            AssertRefused(ClientExitCode.InvalidArguments, () => ArgvToDictionary(["-Alpha", value]));
        }

        /// <summary>
        /// Confirms the same switch given twice is refused, rather than throwing something the caller
        /// cannot map to an exit code.
        /// </summary>
        [Fact]
        public void ArgvToDictionary_RefusesTheSameSwitchTwice()
        {
            AssertRefused(ClientExitCode.InvalidArguments, static () => ArgvToDictionary(["-Alpha", "one", "-Alpha", "two"]));
        }

        /// <summary>
        /// Confirms a bare hyphen, which names no argument, is refused.
        /// </summary>
        [Fact]
        public void ArgvToDictionary_RefusesASwitchWithNoName()
        {
            AssertRefused(ClientExitCode.InvalidArguments, static () => ArgvToDictionary(["-", "one"]));
        }

        /// <summary>
        /// Confirms an arguments dictionary supplied inline is read, under either switch name.
        /// </summary>
        /// <param name="switchName">The switch naming the dictionary.</param>
        [Theory]
        [InlineData("ArgumentsDictionary")]
        [InlineData("ArgV")]
        public void ArgvToDictionary_ReadsAnArgumentsDictionaryGivenAsALiteral(string switchName)
        {
            ReadOnlyDictionary<string, string> arguments = ArgvToDictionary([$"-{switchName}", SerializedDictionary()]);
            Assert.Equal("PATH", Assert.Single(arguments).Value);
        }

        /// <summary>
        /// Confirms an arguments dictionary given as a file path is read from that file.
        /// </summary>
        [Fact]
        public void ArgvToDictionary_ReadsAnArgumentsDictionaryFromAFile()
        {
            using TempDirectory directory = new();
            string path = directory.WriteFile("arguments.txt", SerializedDictionary());
            Assert.Equal("PATH", Assert.Single(ArgvToDictionary(["-ArgumentsDictionary", path])).Value);
        }

        /// <summary>
        /// Confirms an arguments dictionary replaces the switches parsed alongside it.
        /// </summary>
        [Fact]
        public void ArgvToDictionary_DiscardsTheSwitchesAlongsideAnArgumentsDictionary()
        {
            ReadOnlyDictionary<string, string> arguments = ArgvToDictionary(["-Alpha", "one", "-ArgumentsDictionary", SerializedDictionary()]);
            Assert.Equal("PATH", Assert.Single(arguments).Value);
        }

        /// <summary>
        /// Confirms an arguments dictionary naming a registry value that is not there is refused.
        /// </summary>
        [Fact]
        public void ArgvToDictionary_RefusesAnArgumentsDictionaryRegistryValueThatDoesNotExist()
        {
            string path = string.Create(CultureInfo.InvariantCulture, $@"HKEY_CURRENT_USER\Software\PSADT.ClientServer.Client.Tests\{Guid.NewGuid():N}\Value");
            AssertRefused(ClientExitCode.InvalidArguments, () => ArgvToDictionary(["-ArgumentsDictionary", path]));
        }

        /// <summary>
        /// Confirms an arguments dictionary that is not serialized content is refused.
        /// </summary>
        [Fact]
        public void ArgvToDictionary_RefusesAnArgumentsDictionaryThatIsNotSerializedContent()
        {
            AssertRefused(ClientExitCode.InvalidOptions, static () => ArgvToDictionary(["-ArgumentsDictionary", "not serialized content"]));
        }

        #endregion ArgvToDictionary

        #region GetOptionsFromArguments

        /// <summary>
        /// Confirms the options are handed back as given.
        /// </summary>
        [Fact]
        public void GetOptionsFromArguments_ReturnsWhatWasGiven()
        {
            Assert.Equal("payload", GetOptionsFromArguments("Options", "payload"));
        }

        /// <summary>
        /// Confirms absent options are reported separately from invalid ones.
        /// </summary>
        [Fact]
        public void GetOptionsFromArguments_ReportsNoOptionsWhenTheKeyIsAbsent()
        {
            AssertRefused(ClientExitCode.NoOptions, static () => GetOptionsFromArguments("Something", "else"));
        }

        /// <summary>
        /// Confirms blank options are reported as invalid rather than absent.
        /// </summary>
        [Fact]
        public void GetOptionsFromArguments_ReportsInvalidOptionsWhenTheValueIsBlank()
        {
            AssertRefused(ClientExitCode.InvalidOptions, static () => GetOptionsFromArguments("Options", "   "));
        }

        #endregion GetOptionsFromArguments

        #region Serialization wrappers

        /// <summary>
        /// Confirms a value survives the byte round trip.
        /// </summary>
        [Fact]
        public void SerializeToBytes_RoundTripsThroughDeserializeBytes()
        {
            Assert.Equal("a value", DeserializeBytes<string>(SerializeToBytes("a value"), 0));
        }

        /// <summary>
        /// Confirms the offset is honoured, which is how the command byte is skipped in the real loop.
        /// </summary>
        [Fact]
        public void DeserializeBytes_ReadsFromTheGivenOffset()
        {
            byte[] serialized = SerializeToBytes("a value");
            byte[] prefixed = new byte[serialized.Length + 1];
            prefixed[0] = 0xFF;
            serialized.CopyTo(prefixed, 1);
            Assert.Equal("a value", DeserializeBytes<string>(prefixed, 1));
        }

        /// <summary>
        /// Confirms a value survives the string round trip.
        /// </summary>
        [Fact]
        public void SerializeToString_RoundTripsThroughDeserializeString()
        {
            Assert.Equal("a value", DeserializeString<string>(SerializeToString("a value")));
        }

        /// <summary>
        /// Confirms unreadable bytes are reported as invalid options.
        /// </summary>
        [Fact]
        public void DeserializeBytes_ReportsInvalidOptionsForContentItCannotRead()
        {
            AssertRefused(ClientExitCode.InvalidOptions, static () => DeserializeBytes<string>([1, 2, 3, 4], 0));
        }

        /// <summary>
        /// Confirms an unreadable string is reported as invalid options.
        /// </summary>
        [Fact]
        public void DeserializeString_ReportsInvalidOptionsForContentItCannotRead()
        {
            AssertRefused(ClientExitCode.InvalidOptions, static () => DeserializeString<string>("not serialized content"));
        }

        /// <summary>
        /// Confirms a value that cannot be written is reported as an invalid result rather than invalid
        /// input, because at that point it is the client's own output that is wrong.
        /// </summary>
        [Fact]
        public void SerializeToBytes_ReportsInvalidResultForSomethingItCannotWrite()
        {
            AssertRefused(ClientExitCode.InvalidResult, static () => SerializeToBytes("   "));
        }

        /// <summary>
        /// Confirms the string serializer reports the same failure as the byte serializer.
        /// </summary>
        [Fact]
        public void SerializeToString_ReportsInvalidResultForSomethingItCannotWrite()
        {
            AssertRefused(ClientExitCode.InvalidResult, static () => SerializeToString("   "));
        }

        #endregion Serialization wrappers

        #region Read-only queries of machine state

        /// <summary>
        /// Confirms focus mode is reported as active, inactive, or unavailable and nothing else.
        /// </summary>
        [Fact]
        public void GetUserFocusModeState_ReportsActiveInactiveOrUnavailable()
        {
            Assert.Contains(NonPublic.CallStatic<int>(Subject, "GetUserFocusModeState"), new[] { -1, 0, 1 });
        }

        /// <summary>
        /// Confirms the focus mode answer does not change between two consecutive reads.
        /// </summary>
        [Fact]
        public void GetUserFocusModeState_AnswersTheSameWayTwiceRunning()
        {
            Assert.Equal(NonPublic.CallStatic<int>(Subject, "GetUserFocusModeState"), NonPublic.CallStatic<int>(Subject, "GetUserFocusModeState"));
        }

        /// <summary>
        /// Confirms the toast notification mode is a mode the platform defines, or unavailable.
        /// </summary>
        [Fact]
        public void GetUserToastNotificationMode_ReportsADefinedModeOrUnavailable()
        {
            int mode = NonPublic.CallStatic<int>(Subject, "GetUserToastNotificationMode");
            Assert.True(
                mode == -1 || Enum.IsDefined(typeof(Windows.UI.Notifications.ToastNotificationMode), mode),
                string.Create(CultureInfo.InvariantCulture, $"[{mode}] is neither -1 nor a defined ToastNotificationMode."));
        }

        #endregion Read-only queries of machine state

        #region InvokeMainErrorHandler

        /// <summary>
        /// Confirms an explicit exit code wins over the exception's own.
        /// </summary>
        [Fact]
        public void InvokeMainErrorHandler_ReturnsTheExitCodeItWasGiven()
        {
            Assert.Equal((int)ClientExitCode.InvalidDialog, InvokeMainErrorHandler(new InvalidOperationException("boom"), ClientExitCode.InvalidDialog));
        }

        /// <summary>
        /// Confirms the exception's own result code is used when none is given, which is how a client
        /// exception carries its exit code out of the process.
        /// </summary>
        [Fact]
        public void InvokeMainErrorHandler_FallsBackToTheExceptionsResultCode()
        {
            Assert.Equal((int)ClientExitCode.PromptToSaveFailure, InvokeMainErrorHandler(new ClientException("boom", ClientExitCode.PromptToSaveFailure), exitCode: null));
        }

        /// <summary>
        /// Confirms the failure reaches standard error in the form the server reads it back from.
        /// </summary>
        [Fact]
        public void InvokeMainErrorHandler_WritesTheSerializedExceptionToStandardError()
        {
            TextWriter original = Console.Error;
            using StringWriter captured = new(CultureInfo.InvariantCulture);
            try
            {
                Console.SetError(captured);
                _ = InvokeMainErrorHandler(new InvalidOperationException("boom"), ClientExitCode.Unknown);
            }
            finally
            {
                Console.SetError(original);
            }
            Assert.Equal("boom", DataSerialization.DeserializeFromString<Exception>(captured.ToString().Trim()).Message);
        }

        #endregion InvokeMainErrorHandler

        #region Type initialisation

        /// <summary>
        /// Confirms the dialog thread's unhandled exception handler is published where the dialog
        /// manager looks for it. The handler is never invoked here: it calls <c language="csharp">FailFast</c>.
        /// </summary>
        [Fact]
        public void Init_PublishesAnUnhandledExceptionHandlerForTheDialogThread()
        {
            ClientExecutable.Init();
            _ = Assert.IsType<Action<Exception>>(AppDomain.CurrentDomain.GetData("PSADT.UserInterface.DialogManager.UnhandledExceptionHandler"));
        }

        /// <summary>
        /// Confirms the static constructor loads the assemblies sitting beside the client, which is
        /// what lets it run from a directory the loader would not otherwise probe.
        /// </summary>
        [Fact]
        public void ClientExecutable_EagerlyLoadsTheAssembliesBesideIt()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(Subject.TypeHandle);
            List<string> loaded = [.. AppDomain.CurrentDomain.GetAssemblies().Select(static a => a.GetName().Name)];
            Assert.Contains("PSADT", loaded, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("PSADT.ClientServer.Server", loaded, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("PSADT.UserInterface.Interfaces", loaded, StringComparer.OrdinalIgnoreCase);
        }

        #endregion Type initialisation

        #region Main and the standalone dispatch, reached by running the executable

        /// <summary>
        /// Confirms arguments that name no operation are reported as an invalid mode.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact(Skip = ClientRequired, SkipUnless = nameof(TestEnvironment.CanRunClient), SkipType = typeof(TestEnvironment))]
        public async Task Client_ReportsInvalidModeForArgumentsThatNameNoOperation()
        {
            ClientResult result = await ClientProcess.RunAsync("-Alpha", "one").ConfigureAwait(true);
            Assert.Equal(ClientExitCode.InvalidMode, result.ExitCodeAsEnum);
        }

        /// <summary>
        /// Confirms a failure leaves standard error carrying the exception the server deserializes.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact(Skip = ClientRequired, SkipUnless = nameof(TestEnvironment.CanRunClient), SkipType = typeof(TestEnvironment))]
        public async Task Client_WritesTheFailureToStandardErrorAsASerializedException()
        {
            ClientResult result = await ClientProcess.RunAsync("-Alpha", "one").ConfigureAwait(true);
            Assert.NotEqual(0, result.ExitCode);
            Assert.Equal((int)ClientExitCode.InvalidMode, DataSerialization.DeserializeFromString<Exception>(result.StandardError.Trim()).HResult);
        }

        /// <summary>
        /// Confirms client/server mode names which pipe handle it is missing.
        /// </summary>
        /// <param name="expected">The exit code the missing handle should produce.</param>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Theory]
        [InlineData(ClientExitCode.NoOutputPipe)]
        [InlineData(ClientExitCode.NoInputPipe)]
        [InlineData(ClientExitCode.NoLogPipe)]
        public async Task Client_ReportsWhichPipeHandleIsMissingInClientServerMode(ClientExitCode expected)
        {
            Assert.SkipUnless(TestEnvironment.CanRunClient, ClientRequired);
            List<string> arguments = ["/ClientServer"];
            if (expected is not ClientExitCode.NoOutputPipe)
            {
                arguments.AddRange(["-OutputPipe", "1234"]);
            }
            if (expected is ClientExitCode.NoLogPipe)
            {
                arguments.AddRange(["-InputPipe", "5678"]);
            }
            ClientResult result = await ClientProcess.RunAsync([.. arguments]).ConfigureAwait(true);
            Assert.Equal(expected, result.ExitCodeAsEnum);
        }

        /// <summary>
        /// Confirms a handle that is present but unusable is reported separately from an absent one.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact(Skip = ClientRequired, SkipUnless = nameof(TestEnvironment.CanRunClient), SkipType = typeof(TestEnvironment))]
        public async Task Client_ReportsAnInvalidOutputPipeForAHandleItCannotOpen()
        {
            ClientResult result = await ClientProcess.RunAsync("/ClientServer", "-OutputPipe", "notahandle", "-InputPipe", "notahandle", "-LogPipe", "notahandle").ConfigureAwait(true);
            Assert.Equal(ClientExitCode.InvalidOutputPipe, result.ExitCodeAsEnum);
        }

        /// <summary>
        /// Confirms the abbreviated form of a switch reaches the same operation as the long one.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact(Skip = ClientRequired, SkipUnless = nameof(TestEnvironment.CanRunClient), SkipType = typeof(TestEnvironment))]
        public async Task Client_AcceptsTheShortFormOfTheClientServerSwitch()
        {
            ClientResult result = await ClientProcess.RunAsync("/cs").ConfigureAwait(true);
            Assert.Equal(ClientExitCode.NoOutputPipe, result.ExitCodeAsEnum);
        }

        /// <summary>
        /// Confirms a modal dialog request missing or misnaming its type or style is refused before any
        /// dialog is built.
        /// </summary>
        /// <param name="expected">The exit code the malformed request should produce.</param>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Theory]
        [InlineData(ClientExitCode.NoDialogType)]
        [InlineData(ClientExitCode.InvalidDialog)]
        [InlineData(ClientExitCode.NoDialogStyle)]
        public async Task Client_RefusesAModalDialogItCannotIdentify(ClientExitCode expected)
        {
            Assert.SkipUnless(TestEnvironment.CanRunClient, ClientRequired);
            string[] arguments = expected is ClientExitCode.NoDialogType
                ? ["/ShowModalDialog"]
                : expected is ClientExitCode.InvalidDialog
                    ? ["/ShowModalDialog", "-DialogType", "NotADialog", "-DialogStyle", "Fluent"]
                    : ["/ShowModalDialog", "-DialogType", "InputDialog"];
            ClientResult result = await ClientProcess.RunAsync(arguments).ConfigureAwait(true);
            Assert.Equal(expected, result.ExitCodeAsEnum);
        }

        /// <summary>
        /// Confirms a dialog style the client cannot parse is refused.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact(Skip = ClientRequired, SkipUnless = nameof(TestEnvironment.CanRunClient), SkipType = typeof(TestEnvironment))]
        public async Task Client_RefusesAModalDialogWithAStyleItCannotIdentify()
        {
            ClientResult result = await ClientProcess.RunAsync("/ShowModalDialog", "-DialogType", "InputDialog", "-DialogStyle", "NotAStyle").ConfigureAwait(true);
            Assert.Equal(ClientExitCode.NoDialogStyle, result.ExitCodeAsEnum);
        }

        /// <summary>
        /// Confirms a modal dialog whose options cannot be read reports that, rather than escaping as
        /// an unclassified failure. Deserialization happens before the dialog is built, so nothing is
        /// shown.
        /// </summary>
        /// <remarks>
        /// Reaching the exit code at all depends on the exception being serializable: the error handler
        /// reports by serializing, and aborts on <c language="csharp">FailFast</c> if that throws. Failures out of
        /// <c language="csharp">ReadObject</c> carry an <c language="csharp">XmlException</c>, whose own message arguments are a
        /// <c language="csharp">string[]</c>, so this test also covers that type staying in the serializer's known types.
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact(Skip = ClientRequired, SkipUnless = nameof(TestEnvironment.CanRunClient), SkipType = typeof(TestEnvironment))]
        public async Task Client_RefusesAModalDialogWhoseOptionsCannotBeRead()
        {
            ClientResult result = await ClientProcess.RunAsync("/ShowModalDialog", "-DialogType", "InputDialog", "-DialogStyle", "Classic", "-Options", "notserializedcontent").ConfigureAwait(true);
            Assert.Equal(ClientExitCode.InvalidOptions, result.ExitCodeAsEnum);
            Assert.NotEmpty(result.StandardError.Trim());
        }

        /// <summary>
        /// Confirms an operation needing options refuses to run without them.
        /// </summary>
        /// <param name="operation">The operation switch to run.</param>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Theory]
        [InlineData("/GetProcessWindowInfo")]
        [InlineData("/SendKeys")]
        [InlineData("/ShellExecuteProcess")]
        public async Task Client_ReportsNoOptionsWhenAnOperationNeedingThemHasNone(string operation)
        {
            Assert.SkipUnless(TestEnvironment.CanRunClient, ClientRequired);
            ClientResult result = await ClientProcess.RunAsync(operation).ConfigureAwait(true);
            Assert.Equal(ClientExitCode.NoOptions, result.ExitCodeAsEnum);
        }

        /// <summary>
        /// Confirms each read-only query succeeds and writes something back.
        /// </summary>
        /// <param name="operation">The operation switch to run.</param>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Theory]
        [InlineData("/GetUserNotificationState")]
        [InlineData("/GetForegroundWindowProcessId")]
        [InlineData("/GetUserFocusModeState")]
        [InlineData("/GetUserToastNotificationMode")]
        public async Task Client_AnswersAReadOnlyQueryWithSerializedOutput(string operation)
        {
            Assert.SkipUnless(TestEnvironment.CanRunClient, ClientRequired);
            ClientResult result = await ClientProcess.RunAsync(operation).ConfigureAwait(true);
            Assert.Equal(ClientExitCode.Success, result.ExitCodeAsEnum);
            Assert.NotEmpty(result.StandardOutput.Trim());
        }

        /// <summary>
        /// Confirms the last input time is written as raw ticks, which is the one operation whose
        /// output is not serialized because <c language="csharp">SessionInfo</c> parses it as a number.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact(Skip = ClientRequired, SkipUnless = nameof(TestEnvironment.CanRunClient), SkipType = typeof(TestEnvironment))]
        public async Task Client_AnswersGetLastInputTimeWithRawTicks()
        {
            ClientResult result = await ClientProcess.RunAsync("/GetLastInputTime").ConfigureAwait(true);
            Assert.Equal(ClientExitCode.Success, result.ExitCodeAsEnum);
            Assert.True(long.TryParse(result.StandardOutput.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks), result.Describe());
            Assert.InRange(ticks, 0, TimeSpan.MaxValue.Ticks);
        }

        /// <summary>
        /// Confirms an environment variable can be read back.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact(Skip = ClientRequired, SkipUnless = nameof(TestEnvironment.CanRunClient), SkipType = typeof(TestEnvironment))]
        public async Task Client_ReadsAnEnvironmentVariableWithoutChangingIt()
        {
            ClientResult result = await ClientProcess.RunAsync("/GetEnvironmentVariable", "-Variable", "PATH").ConfigureAwait(true);
            Assert.Equal(ClientExitCode.Success, result.ExitCodeAsEnum);
            Assert.NotEmpty(result.StandardOutput.Trim());
        }

        /// <summary>
        /// Confirms the arguments dictionary indirection works through the real executable, which is
        /// how arguments too long for a command line reach the client.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact(Skip = ClientRequired, SkipUnless = nameof(TestEnvironment.CanRunClient), SkipType = typeof(TestEnvironment))]
        public async Task Client_TakesItsArgumentsFromAnArgumentsDictionary()
        {
            ClientResult result = await ClientProcess.RunAsync("/GetEnvironmentVariable", "-ArgumentsDictionary", SerializedDictionary()).ConfigureAwait(true);
            Assert.Equal(ClientExitCode.Success, result.ExitCodeAsEnum);
        }

        /// <summary>
        /// Confirms an environment operation with no variable named is refused before it acts.
        /// </summary>
        /// <param name="operation">The operation switch to run.</param>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Theory]
        [InlineData("/GetEnvironmentVariable")]
        [InlineData("/SetEnvironmentVariable")]
        [InlineData("/RemoveEnvironmentVariable")]
        public async Task Client_RefusesAnEnvironmentOperationWithNoVariableNamed(string operation)
        {
            Assert.SkipUnless(TestEnvironment.CanRunClient, ClientRequired);
            ClientResult result = await ClientProcess.RunAsync(operation).ConfigureAwait(true);
            Assert.Equal(ClientExitCode.InvalidArguments, result.ExitCodeAsEnum);
        }

        /// <summary>
        /// Confirms setting a variable is refused when no value accompanies it, so nothing is written.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact(Skip = ClientRequired, SkipUnless = nameof(TestEnvironment.CanRunClient), SkipType = typeof(TestEnvironment))]
        public async Task Client_RefusesToSetAnEnvironmentVariableWithNoValue()
        {
            ClientResult result = await ClientProcess.RunAsync("/SetEnvironmentVariable", "-Variable", "PSADTClientTestsNeverSet").ConfigureAwait(true);
            Assert.Equal(ClientExitCode.InvalidArguments, result.ExitCodeAsEnum);
        }

        /// <summary>
        /// Confirms a silent restart is refused when its delay cannot be parsed. The delay is
        /// deliberately unparseable: a value that parses reaches the restart.
        /// </summary>
        /// <param name="operation">The long or short form of the switch.</param>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Theory]
        [InlineData("/SilentRestart")]
        [InlineData("/sr")]
        public async Task Client_RefusesASilentRestartWithNoUsableDelay(string operation)
        {
            Assert.SkipUnless(TestEnvironment.CanRunClient, ClientRequired);
            ClientResult result = await ClientProcess.RunAsync(operation, "-Delay", "notatimespan").ConfigureAwait(true);
            Assert.Equal(ClientExitCode.InvalidArguments, result.ExitCodeAsEnum);
        }

        /// <summary>
        /// Confirms token brokering refuses a caller that is not the local system account, which is the
        /// guard standing in front of a primary token being handed to another process.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact(Skip = "Requires a caller that is not the local system account.", SkipUnless = nameof(TestEnvironment.IsNotLocalSystem), SkipType = typeof(TestEnvironment))]
        public async Task Client_RefusesToBrokerATokenForACallerThatIsNotLocalSystem()
        {
            Assert.SkipUnless(TestEnvironment.CanRunClient, ClientRequired);
            ClientResult result = await ClientProcess.RunAsync("/TokenBroker", "-PipeName", "PSADTClientTestsNeverConnected").ConfigureAwait(true);
            Assert.Equal(ClientExitCode.InvalidCaller, result.ExitCodeAsEnum);
        }

        #endregion Main and the standalone dispatch, reached by running the executable

        #region Helpers

        /// <summary>
        /// Asserts that an operation is refused with a particular exit code.
        /// </summary>
        /// <param name="expected">The exit code the client exception should carry.</param>
        /// <param name="operation">The operation to run.</param>
        private static void AssertRefused(ClientExitCode expected, Action operation)
        {
            Assert.Equal(expected, (ClientExitCode)Assert.Throws<ClientException>(operation).HResult);
        }

        /// <summary>
        /// Calls the private argument parser.
        /// </summary>
        /// <remarks>
        /// The argument list is wrapped rather than passed straight through. A <c language="csharp">string[]</c> converts
        /// to the <c language="csharp">object[]</c> the caller takes as its parameter array, so handing it over bare
        /// spreads each argument into a parameter of its own instead of arriving as the one array the
        /// method expects.
        /// </remarks>
        /// <param name="argv">The arguments to parse.</param>
        /// <returns>The parsed arguments.</returns>
        private static ReadOnlyDictionary<string, string> ArgvToDictionary(string[] argv)
        {
            return NonPublic.CallStatic<ReadOnlyDictionary<string, string>>(Subject, "ArgvToDictionary", [argv]);
        }

        /// <summary>
        /// Calls the private options reader with a dictionary holding one entry.
        /// </summary>
        /// <param name="key">The entry's key.</param>
        /// <param name="value">The entry's value.</param>
        /// <returns>The options.</returns>
        private static string GetOptionsFromArguments(string key, string value)
        {
            Dictionary<string, string> arguments = new(StringComparer.OrdinalIgnoreCase) { [key] = value };
            return NonPublic.CallStatic<string>(Subject, "GetOptionsFromArguments", new ReadOnlyDictionary<string, string>(arguments));
        }

        /// <summary>
        /// Calls the private byte serializer.
        /// </summary>
        /// <typeparam name="T">The type to serialize.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <returns>The serialized bytes.</returns>
        private static byte[] SerializeToBytes<T>(T value)
        {
            return NonPublic.CallStaticGeneric<byte[]>(Subject, "SerializeToBytes", typeof(T), value);
        }

        /// <summary>
        /// Calls the private string serializer.
        /// </summary>
        /// <typeparam name="T">The type to serialize.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <returns>The serialized string.</returns>
        private static string SerializeToString<T>(T value)
        {
            return NonPublic.CallStaticGeneric<string>(Subject, "SerializeToString", typeof(T), value);
        }

        /// <summary>
        /// Calls the private byte deserializer.
        /// </summary>
        /// <typeparam name="T">The type to deserialize.</typeparam>
        /// <param name="input">The bytes to read.</param>
        /// <param name="offset">Where in the bytes to start.</param>
        /// <returns>The deserialized value.</returns>
        private static T DeserializeBytes<T>(byte[] input, int offset)
        {
            return NonPublic.CallStaticGeneric<T>(Subject, "DeserializeBytes", typeof(T), input, offset);
        }

        /// <summary>
        /// Calls the private string deserializer.
        /// </summary>
        /// <typeparam name="T">The type to deserialize.</typeparam>
        /// <param name="input">The string to read.</param>
        /// <returns>The deserialized value.</returns>
        private static T DeserializeString<T>(string input)
        {
            return NonPublic.CallStaticGeneric<T>(Subject, "DeserializeString", typeof(T), input);
        }

        /// <summary>
        /// Calls the private error handler.
        /// </summary>
        /// <param name="exception">The exception to report.</param>
        /// <param name="exitCode">The exit code to report, if any.</param>
        /// <returns>The exit code the handler settled on.</returns>
        private static int InvokeMainErrorHandler(Exception exception, ClientExitCode? exitCode)
        {
            return NonPublic.CallStatic<int>(Subject, "InvokeMainErrorHandler", exception, "message", exitCode);
        }

        /// <summary>
        /// Serializes a one-entry dictionary the way the client expects an arguments dictionary to
        /// arrive, naming a variable every machine has.
        /// </summary>
        /// <returns>The serialized dictionary.</returns>
        private static string SerializedDictionary()
        {
            Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase) { ["Variable"] = "PATH" };
            return DataSerialization.SerializeToString(new ReadOnlyDictionary<string, string>(values));
        }

        #endregion Helpers
    }
}
