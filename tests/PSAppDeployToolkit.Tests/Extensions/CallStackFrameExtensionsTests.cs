using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation;
using PSADT.PowerShellTestFixture;
using PSAppDeployToolkit.Tests.TestHelpers;
using Xunit;

namespace PSAppDeployToolkit.Tests.Extensions
{
    /// <summary>
    /// Tests the helper that decides what to call the command a call stack frame belongs to.
    /// </summary>
    /// <remarks>
    /// A transcription of the <c language="text">Command</c> script property PowerShell attaches to <see cref="CallStackFrame"/> in
    /// its type data, so the contract under test is agreement with that property rather than any behaviour of its
    /// own. That property is live in the fixture's runspace, which makes it usable as an oracle: every frame goes
    /// through both and the two answers have to match, save for the one difference the toolkit takes deliberately.
    /// <para>
    /// Driven with real frames throughout, since the type is sealed with no constructor a caller can reach. Each
    /// shape is here for the branch it reaches, not for variety.
    /// </para>
    /// </remarks>
    /// <param name="powerShell">The hosted engine, shared across the collection.</param>
    [Collection(PowerShellCollection.Name)]
    public sealed class CallStackFrameExtensionsTests(PowerShellFixture powerShell)
    {
        /// <summary>
        /// Verifies that every frame is named whatever PowerShell's own property names it.
        /// </summary>
        /// <remarks>
        /// The whole point of the type, and it covers the reachable branches without asserting a value by hand.
        /// </remarks>
        [Fact]
        public void GetCommand_MatchesThePowerShellCommandProperty()
        {
            // Arrange
            IReadOnlyList<FrameCase> cases = FrameCases(powerShell, FrameShapesScript);

            // Assert: compared as labelled sequences, so a mismatch names the shape that produced it.
            string[] expected = [.. cases.Select(static frameCase => $"{frameCase.Shape} => [{frameCase.Oracle}]")];
            string[] actual = [.. cases.Select(static frameCase => $"{frameCase.Shape} => [{frameCase.Frame.GetCommand()}]")];
            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Verifies that a named command is called by its command name rather than by its frame's function name.
        /// </summary>
        /// <remarks>
        /// A trap's own frame is the one shape where the two candidates differ: the engine decorates the function
        /// name and leaves the command name alone. Every other named shape has them identical, so this is what
        /// separates the implementation from one that simply returned the function name.
        /// </remarks>
        [Fact]
        public void GetCommand_PrefersTheCommandNameOverADecoratedFunctionName()
        {
            // Arrange
            CallStackFrame trap = InnermostFrame(FrameCases(powerShell, FrameShapesScript), TrapShape);

            // Assert
            Assert.Equal("Invoke-Trap<trap>", trap.FunctionName);
            Assert.Equal("Invoke-Trap", trap.GetCommand());
        }

        /// <summary>
        /// Verifies that a script block, which has no name, falls back to the frame's function name.
        /// </summary>
        /// <remarks>
        /// Worth pinning for how it gets there: the frame does have an <c language="powershell">$InvocationInfo.MyCommand</c>, so it is the
        /// empty name rather than a missing command that sends it to the last fallback.
        /// <para>
        /// The value matters downstream. The log writer's caller filter recognises <c language="text">&lt;ScriptBlock&gt;</c> by
        /// name and looks past it for something more useful, so this literal is a dependency rather than an
        /// incidental detail.
        /// </para>
        /// </remarks>
        [Fact]
        public void GetCommand_FallsBackToTheFunctionNameForAScriptBlock()
        {
            // Arrange
            CallStackFrame scriptBlock = InnermostFrame(FrameCases(powerShell, FrameShapesScript), ScriptBlockShape);

            // Assert: the branch above this one did not fire, since MyCommand is present.
            Assert.NotNull(scriptBlock.InvocationInfo);
            Assert.NotNull(scriptBlock.InvocationInfo.MyCommand);
            Assert.Equal(string.Empty, scriptBlock.InvocationInfo.MyCommand.Name);

            // Assert
            Assert.Equal(scriptBlock.FunctionName, scriptBlock.GetCommand());
            Assert.Equal("<ScriptBlock>", scriptBlock.GetCommand());
        }

        /// <summary>
        /// Verifies that a frame carrying no command information at all is named nothing.
        /// </summary>
        /// <remarks>
        /// Two ordinary constructs reach this branch - invoking a script block through <c language="powershell">Invoke()</c> and calling a
        /// PowerShell class method - and for both the engine answers with an invocation name that is itself empty.
        /// So the helper can and does return an empty string, which is why the log writer's search for a caller
        /// rejects a blank answer and keeps walking outwards. Without that guard, an entry logged from a class
        /// method would be attributed to nothing at all.
        /// </remarks>
        /// <param name="shape">The shape to take the frame from.</param>
        [Theory]
        [InlineData(ScriptBlockInvokeShape)]
        [InlineData(ClassMethodShape)]
        public void GetCommand_IsEmptyWhenAFrameCarriesNoCommandInfo(string shape)
        {
            // Arrange
            CallStackFrame frame = InnermostFrame(FrameCases(powerShell, FrameShapesScript), shape);

            // Assert
            Assert.Null(frame.InvocationInfo?.MyCommand);
            Assert.Equal(string.Empty, frame.GetCommand());
        }

        /// <summary>
        /// Verifies that a command named only with whitespace is treated as having no name at all.
        /// </summary>
        /// <remarks>
        /// The one deliberate departure from the script property, and the reason the check is against whitespace
        /// rather than against the empty string the engine compares to. PowerShell hands back the space; the toolkit
        /// declines to call anything by it and falls through to the function name.
        /// <para>
        /// Pinned because the difference looks like a mistranscription and is not one. Making the comparison literal
        /// would also change which frame a log entry is attributed to: an answer of whitespace is discarded by the
        /// log writer's blank check and the search moves outwards, where the current answer of
        /// <c language="text">&lt;ScriptBlock&gt;</c> survives it.
        /// </para>
        /// </remarks>
        [Fact]
        public void GetCommand_TreatsAWhitespaceOnlyCommandNameAsNoNameAtAll()
        {
            // Arrange
            FrameCase whitespace = FrameCases(powerShell, WhitespaceNamedFunctionScript)[0];

            // Assert: the engine's answer, so the shape is confirmed to be the one intended before the departure
            // from it is asserted.
            Assert.Equal(" ", whitespace.Oracle);

            // Assert
            Assert.Equal(whitespace.Frame.FunctionName, whitespace.Frame.GetCommand());
            Assert.NotEqual(whitespace.Oracle, whitespace.Frame.GetCommand(), StringComparer.Ordinal);
        }

        /// <summary>
        /// Runs a harness in the fixture's runspace and pairs each frame it captured with PowerShell's own answer.
        /// </summary>
        /// <param name="powerShell">The hosted engine to take call stacks from.</param>
        /// <param name="script">The harness to run.</param>
        /// <returns>The captured frames, innermost first within each shape.</returns>
        private static IReadOnlyList<FrameCase> FrameCases(PowerShellFixture powerShell, string script)
        {
            IReadOnlyList<FrameCase> cases =
            [
                .. powerShell.InvokeInRunspace(script).Select(static written => new FrameCase(
                    Text(Property(written, "Shape")) ?? throw new InvalidOperationException("The harness captured a frame without labelling its shape."),
                    Unwrap(Property(written, "Frame")),
                    Text(Property(written, "Command")))),
            ];
            Assert.NotEmpty(cases);
            return cases;
        }

        /// <summary>
        /// Reads a property from an object the harness wrote out.
        /// </summary>
        /// <param name="written">The object the harness wrote.</param>
        /// <param name="name">The property to read.</param>
        /// <returns>The value, or <see langword="null"/> where there is no such property.</returns>
        private static object? Property(PSObject written, string name)
        {
            return written.Properties[name]?.Value;
        }

        /// <summary>
        /// Reads a value the engine may have handed back inside one or more <see cref="PSObject"/> shells as text.
        /// </summary>
        /// <remarks>
        /// The unwrapping is the point. A script property's result arrives wrapped, so casting the value straight to
        /// a string yields null and an oracle that agrees with anything.
        /// </remarks>
        /// <param name="value">The value to read.</param>
        /// <returns>The text, or <see langword="null"/> where the value is not text.</returns>
        private static string? Text(object? value)
        {
            while (value is PSObject psObject)
            {
                value = psObject.BaseObject;
            }
            return value as string;
        }

        /// <summary>
        /// Unwraps a frame the engine may have handed back inside one or more <see cref="PSObject"/> shells.
        /// </summary>
        /// <param name="value">The value to unwrap.</param>
        /// <returns>The frame itself.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the value is not a frame.</exception>
        private static CallStackFrame Unwrap(object? value)
        {
            while (value is PSObject psObject)
            {
                value = psObject.BaseObject;
            }
            return value as CallStackFrame
                ?? throw new InvalidOperationException($"The harness wrote a [{value?.GetType().Name ?? "null"}] where a call stack frame was expected.");
        }

        /// <summary>
        /// Finds the innermost frame captured for one shape.
        /// </summary>
        /// <param name="cases">Everything the harness captured.</param>
        /// <param name="shape">The shape to look for.</param>
        /// <returns>The first frame the harness captured for that shape.</returns>
        private static CallStackFrame InnermostFrame(IReadOnlyList<FrameCase> cases, string shape)
        {
            // Asserted rather than assumed: a shape the engine stopped producing would otherwise quietly stop being
            // covered.
            FrameCase? match = cases.FirstOrDefault(frameCase => string.Equals(frameCase.Shape, shape, StringComparison.Ordinal));
            Assert.NotNull(match);
            return match.Frame;
        }

        /// <summary>
        /// Builds a call stack from each shape that reaches a different branch.
        /// </summary>
        /// <remarks>
        /// A command named only with whitespace is left to its own script, since it is the one shape where the
        /// toolkit and the engine are meant to disagree.
        /// <para>
        /// A script file frame is left out on purpose. It reaches the same branch as a function by the same route,
        /// differing only in which <c>CommandInfo</c> subclass carries the name, so it would restate the named
        /// function case rather than add to it.
        /// </para>
        /// </remarks>
        [SuppressMessage("Style", "MA0136:Raw String contains an implicit end of line character", Justification = "The literal is PowerShell source, which parses either line ending, so the source file's choice cannot change what this does.")]
        private const string FrameShapesScript = """
            $cases = [System.Collections.Generic.List[object]]::new()
            function Add-FrameCase
            {
                param([string]$Shape, $Frames)
                foreach ($frame in $Frames)
                {
                    $cases.Add([pscustomobject]@{ Shape = $Shape; Frame = $frame; Command = $frame.Command })
                }
            }

            function Invoke-NamedFunction { Get-PSCallStack }
            Add-FrameCase 'named function' (Invoke-NamedFunction)

            Add-FrameCase 'script block' (& { Get-PSCallStack })

            Add-FrameCase 'scriptblock.Invoke' ({ Get-PSCallStack }.Invoke())

            function Invoke-Trap { trap { Get-PSCallStack; continue } throw 'trap probe' }
            Add-FrameCase 'trap' (Invoke-Trap)

            class FrameCaseProbe { static [object] Probe() { return (Get-PSCallStack) } }
            Add-FrameCase 'class method' ([FrameCaseProbe]::Probe())

            $cases
            """;

        /// <summary>
        /// Builds a call stack from a function whose name is a single space, and removes it again.
        /// </summary>
        /// <remarks>
        /// Created with <c>Set-Item</c> rather than <c>New-Item</c>, which rejects the name outright on PowerShell
        /// 7, and removed within the one script so the shared runspace is left as it was found.
        /// </remarks>
        [SuppressMessage("Style", "MA0136:Raw String contains an implicit end of line character", Justification = "The literal is PowerShell source, which parses either line ending, so the source file's choice cannot change what this does.")]
        private const string WhitespaceNamedFunctionScript = """
            Set-Item -Path 'Function:\ ' -Value { Get-PSCallStack }
            try
            {
                $frames = & ' '
            }
            finally
            {
                Remove-Item -Path 'Function:\ ' -Force
            }
            [pscustomobject]@{ Shape = 'whitespace-named function'; Frame = $frames[0]; Command = $frames[0].Command }
            """;

        /// <summary>
        /// The shape whose innermost frame is an anonymous script block.
        /// </summary>
        private const string ScriptBlockShape = "script block";

        /// <summary>
        /// The shape whose innermost frame is a script block invoked through <c>Invoke()</c>.
        /// </summary>
        private const string ScriptBlockInvokeShape = "scriptblock.Invoke";

        /// <summary>
        /// The shape whose innermost frame is a trap.
        /// </summary>
        private const string TrapShape = "trap";

        /// <summary>
        /// The shape whose innermost frame is a PowerShell class method.
        /// </summary>
        private const string ClassMethodShape = "class method";

        /// <summary>
        /// One captured frame, with the shape that produced it and the answer PowerShell's own property gives.
        /// </summary>
        /// <param name="shape">The shape that produced the frame.</param>
        /// <param name="frame">The frame itself.</param>
        /// <param name="oracle">What PowerShell's <c language="text">Command</c> property calls it.</param>
        private sealed class FrameCase(string shape, CallStackFrame frame, string? oracle)
        {
            /// <summary>
            /// The shape that produced this frame.
            /// </summary>
            public string Shape { get; } = shape;

            /// <summary>
            /// The frame itself.
            /// </summary>
            public CallStackFrame Frame { get; } = frame;

            /// <summary>
            /// What PowerShell's own <c language="text">Command</c> property calls it.
            /// </summary>
            public string? Oracle { get; } = oracle;
        }
    }
}
