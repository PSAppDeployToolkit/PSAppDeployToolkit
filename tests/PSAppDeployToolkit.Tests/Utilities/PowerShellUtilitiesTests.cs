using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using PSADT.PowerShellTestFixture;
using PSAppDeployToolkit.Tests.TestHelpers;
using PSAppDeployToolkit.Utilities;
using Xunit;

namespace PSAppDeployToolkit.Tests.Utilities
{
    /// <summary>
    /// Tests the helpers that sit between PowerShell's object model and the rest of the toolkit.
    /// </summary>
    /// <remarks>
    /// Split by what each member needs. Unwrapping and the null tests are pure and run without an engine; the three
    /// that ask PowerShell to render something need a runspace, because they reach <c language="powershell">Out-String</c> to decide whether
    /// a value has anything worth showing.
    /// </remarks>
    /// <param name="powerShell">The hosted engine, shared across the collection.</param>
    [Collection(PowerShellCollection.Name)]
    public sealed class PowerShellUtilitiesTests(PowerShellFixture powerShell)
    {
        /// <summary>
        /// Verifies that a value that was never wrapped is handed straight back.
        /// </summary>
        [Fact]
        public void GetBaseObject_ReturnsAnUnwrappedValueAsItIs()
        {
            Assert.Equal("text", PowerShellUtilities.GetBaseObject<string>("text"));
            Assert.Equal(42, PowerShellUtilities.GetBaseObject<int>(42));
        }

        /// <summary>
        /// Verifies that wrapping is undone however deep it goes.
        /// </summary>
        /// <remarks>
        /// The loop rather than a single unwrap is the point. A value that has crossed the engine boundary more than
        /// once - returned from a script that was itself handed a wrapped value - arrives wrapped more than once.
        /// </remarks>
        [Fact]
        public void GetBaseObject_UnwrapsHoweverDeepTheWrappingGoes()
        {
            Assert.Equal("text", PowerShellUtilities.GetBaseObject<string>(PSObject.AsPSObject("text")));
            Assert.Equal("text", PowerShellUtilities.GetBaseObject<string>(PSObject.AsPSObject(PSObject.AsPSObject("text"))));
            Assert.Equal(42, PowerShellUtilities.GetBaseObject<int>(PSObject.AsPSObject(PSObject.AsPSObject(PSObject.AsPSObject(42)))));
        }

        /// <summary>
        /// Verifies that asking for the wrong type fails rather than returning nothing.
        /// </summary>
        [Fact]
        public void GetBaseObject_RefusesTheWrongType()
        {
            _ = Assert.Throws<InvalidCastException>(static () => PowerShellUtilities.GetBaseObject<int>("text"));
            _ = Assert.Throws<InvalidCastException>(static () => PowerShellUtilities.GetBaseObject<int>(PSObject.AsPSObject("text")));
        }

        /// <summary>
        /// Verifies that this overload has no notion of PowerShell's several kinds of nothing.
        /// </summary>
        /// <remarks>
        /// Worth recording because it differs from <see cref="PowerShellUtilities.TryGetBaseObject"/>, which treats all
        /// four as null. Here they are unwrapped and cast like any other value, so <c language="csharp">AutomationNull</c> - itself a
        /// wrapper - comes out as the object it wraps. Harmless in current use, since the only callers ask for
        /// <see cref="object"/> or hand it a value they already know the shape of, but the two are not
        /// interchangeable.
        /// </remarks>
        [Fact]
        public void GetBaseObject_DoesNotRecognisePowerShellsKindsOfNothing()
        {
            Assert.Null(PowerShellUtilities.GetBaseObject<string>(obj: null!));
            Assert.NotNull(PowerShellUtilities.GetBaseObject<object>(System.Management.Automation.Internal.AutomationNull.Value));
            _ = Assert.Throws<InvalidCastException>(static () => PowerShellUtilities.GetBaseObject<string>(System.Management.Automation.Internal.AutomationNull.Value));
        }

        /// <summary>
        /// Verifies that a wrapped value is unwrapped and reported as found.
        /// </summary>
        [Fact]
        public void TryGetBaseObject_UnwrapsAndReportsWhatItFound()
        {
            Assert.True(PowerShellUtilities.TryGetBaseObject(PSObject.AsPSObject("text"), out string? text));
            Assert.Equal("text", text);
            Assert.True(PowerShellUtilities.TryGetBaseObject(PSObject.AsPSObject(PSObject.AsPSObject(42)), out int number));
            Assert.Equal(42, number);
        }

        /// <summary>
        /// Verifies that each of PowerShell's kinds of nothing is reported as absent.
        /// </summary>
        /// <param name="shape">What the absence is called, for the failure message.</param>
        /// <param name="nothing">The value standing for absence.</param>
        [Theory]
        [MemberData(nameof(Nothings))]
        public void TryGetBaseObject_ReportsEveryKindOfNothingAsAbsent(string shape, object? nothing)
        {
            Assert.NotNull(shape);
            Assert.False(PowerShellUtilities.TryGetBaseObject(nothing, out object? value));
            Assert.Null(value);
        }

        /// <summary>
        /// Verifies that a value of the wrong type is reported as absent rather than throwing.
        /// </summary>
        /// <remarks>
        /// The whole reason this overload exists alongside the other: a caller testing what it was handed should not
        /// have to catch a cast failure to find out.
        /// </remarks>
        [Fact]
        public void TryGetBaseObject_ReportsTheWrongTypeAsAbsent()
        {
            Assert.False(PowerShellUtilities.TryGetBaseObject("text", out int number));
            Assert.Equal(0, number);
        }

        /// <summary>
        /// Verifies which values count as nothing and which do not.
        /// </summary>
        /// <remarks>
        /// An empty string is the case worth stating: it is not null, so it passes here and is caught elsewhere by
        /// whatever judges content.
        /// </remarks>
        [Fact]
        public void ObjectIsNull_RecognisesEveryKindOfNothingAndNothingElse()
        {
            Assert.True(PowerShellUtilities.ObjectIsNull(obj: null));
            Assert.True(PowerShellUtilities.ObjectIsNull(DBNull.Value));
            Assert.True(PowerShellUtilities.ObjectIsNull(System.Management.Automation.Internal.AutomationNull.Value));
            Assert.True(PowerShellUtilities.ObjectIsNull(NullString.Value));
            Assert.False(PowerShellUtilities.ObjectIsNull(string.Empty));
            Assert.False(PowerShellUtilities.ObjectIsNull("   "));
            Assert.False(PowerShellUtilities.ObjectIsNull(0));
            Assert.False(PowerShellUtilities.ObjectIsNull(false));
            Assert.False(PowerShellUtilities.ObjectIsNull(Array.Empty<string>()));
        }

        /// <summary>
        /// Verifies that a value with nothing to show is recognised as such.
        /// </summary>
        /// <remarks>
        /// Judged by asking PowerShell to render the value rather than by inspecting it, which is why an engine is
        /// needed. That is also what makes it useful: an object PowerShell renders as nothing - an empty collection, a
        /// custom object with no properties - is caught without this having to know the shapes.
        /// </remarks>
        [Fact]
        public void ObjectRendersAsEmpty_RecognisesAValueWithNothingToShow()
        {
            using IDisposable scope = powerShell.Enter();

            Assert.True(PowerShellUtilities.ObjectRendersAsEmpty(obj: null));
            Assert.True(PowerShellUtilities.ObjectRendersAsEmpty(string.Empty));
            Assert.True(PowerShellUtilities.ObjectRendersAsEmpty("   "));
            Assert.True(PowerShellUtilities.ObjectRendersAsEmpty(Array.Empty<string>()));
            Assert.True(PowerShellUtilities.ObjectRendersAsEmpty(new PSObject()));
        }

        /// <summary>
        /// Verifies that a value with something to show is not mistaken for nothing.
        /// </summary>
        /// <remarks>
        /// Zero and false are the cases that matter: both are falsy to PowerShell but both render as text, so neither
        /// is empty. A parameter set to <c language="text">0</c> has been set.
        /// </remarks>
        [Fact]
        public void ObjectRendersAsEmpty_DoesNotMistakeAValueForNothing()
        {
            using IDisposable scope = powerShell.Enter();

            Assert.False(PowerShellUtilities.ObjectRendersAsEmpty("text"));
            Assert.False(PowerShellUtilities.ObjectRendersAsEmpty(0));
            Assert.False(PowerShellUtilities.ObjectRendersAsEmpty(false));
            Assert.False(PowerShellUtilities.ObjectRendersAsEmpty(testArray));
            Assert.False(PowerShellUtilities.ObjectRendersAsEmpty(new SwitchParameter(isPresent: false)));
        }

        /// <summary>
        /// Verifies that a parameter with no value becomes a switch.
        /// </summary>
        /// <remarks>
        /// This is how remaining arguments arrive when a caller splats or forwards them: a flat list of tokens with no
        /// binding, which has to be turned back into something shaped like <c language="powershell">$PSBoundParameters</c>.
        /// </remarks>
        [Fact]
        public void ConvertValuesFromRemainingArguments_TurnsAValuelessParameterIntoASwitch()
        {
            using IDisposable scope = powerShell.Enter();

            // Act
            IDictionary<string, object> values = PowerShellUtilities.ConvertValuesFromRemainingArguments(["-Force"]);

            // Assert
            Assert.True(((SwitchParameter)values["Force"]).IsPresent);
        }

        /// <summary>
        /// Verifies that a parameter followed by a value keeps that value.
        /// </summary>
        [Fact]
        public void ConvertValuesFromRemainingArguments_KeepsAValueAgainstItsParameter()
        {
            using IDisposable scope = powerShell.Enter();

            // Act
            IDictionary<string, object> values = PowerShellUtilities.ConvertValuesFromRemainingArguments(["-Name", "Notepad", "-Count", 3]);

            // Assert
            Assert.Equal("Notepad", values["Name"]);
            Assert.Equal(3, values["Count"]);
        }

        /// <summary>
        /// Verifies that a parameter whose value renders as nothing is dropped altogether.
        /// </summary>
        /// <remarks>
        /// Deliberate: forwarding <c language="powershell">-Name ''</c> to a command that would reject a blank name is worse than not
        /// forwarding it at all, so the key is removed rather than passed on empty.
        /// </remarks>
        [Fact]
        public void ConvertValuesFromRemainingArguments_DropsAParameterWhoseValueRendersAsNothing()
        {
            using IDisposable scope = powerShell.Enter();

            // Act
            IDictionary<string, object> values = PowerShellUtilities.ConvertValuesFromRemainingArguments(["-Name", string.Empty, "-Other", "kept"]);

            // Assert
            Assert.False(values.ContainsKey("Name"));
            Assert.Equal("kept", values["Other"]);
        }

        /// <summary>
        /// Verifies that a trailing colon is not taken as part of the name.
        /// </summary>
        /// <remarks>
        /// <c language="powershell">-Force:</c> is how a caller writes a switch whose value follows, so the colon belongs to the syntax
        /// rather than the name.
        /// </remarks>
        [Fact]
        public void ConvertValuesFromRemainingArguments_StripsATrailingColonFromTheName()
        {
            using IDisposable scope = powerShell.Enter();

            // Act
            IDictionary<string, object> values = PowerShellUtilities.ConvertValuesFromRemainingArguments(["-Force:", true]);

            // Assert
            Assert.True(values.ContainsKey("Force"));
            Assert.True((bool)values["Force"]);
        }

        /// <summary>
        /// Verifies that names are matched without regard to case.
        /// </summary>
        /// <remarks>
        /// PowerShell binds parameters case-insensitively, so a dictionary standing in for
        /// <c language="powershell">$PSBoundParameters</c> has to as well.
        /// </remarks>
        [Fact]
        public void ConvertValuesFromRemainingArguments_MatchesNamesWithoutRegardToCase()
        {
            using IDisposable scope = powerShell.Enter();

            // Act
            IDictionary<string, object> values = PowerShellUtilities.ConvertValuesFromRemainingArguments(["-Name", "Notepad"]);

            // Assert
            Assert.Equal("Notepad", values["NAME"]);
            Assert.Equal("Notepad", values["name"]);
        }

        /// <summary>
        /// Verifies that the result can still be added to.
        /// </summary>
        /// <remarks>
        /// The source says in capitals that this must not be read-only, because it stands in for
        /// <c language="powershell">$PSBoundParameters</c> and callers go on to adjust it. Nothing else would catch a change to that.
        /// </remarks>
        [Fact]
        public void ConvertValuesFromRemainingArguments_ReturnsSomethingStillMutable()
        {
            using IDisposable scope = powerShell.Enter();

            // Act
            IDictionary<string, object> values = PowerShellUtilities.ConvertValuesFromRemainingArguments(["-Name", "Notepad"]);
            values["Added"] = "later";

            // Assert
            Assert.False(values.IsReadOnly);
            Assert.Equal("later", values["Added"]);
        }

        /// <summary>
        /// Verifies that a null list yields an empty dictionary rather than throwing.
        /// </summary>
        /// <remarks>
        /// A <c language="powershell">ValueFromRemainingArguments</c> parameter that bound nothing arrives here as null, and the
        /// PowerShell wrapper declares <c language="powershell">[AllowNull()]</c>. Throwing instead broke every caller that
        /// forwarded no arguments of its own.
        /// </remarks>
        [Fact]
        public void ConvertValuesFromRemainingArguments_TreatsNullAsNoArguments()
        {
            using IDisposable scope = powerShell.Enter();

            // Act
            IDictionary<string, object> values = PowerShellUtilities.ConvertValuesFromRemainingArguments(remainingArguments: null);

            // Assert
            Assert.Empty(values);
            Assert.False(values.IsReadOnly);
        }

        /// <summary>
        /// Verifies that an empty list yields an empty dictionary, the same as null.
        /// </summary>
        [Fact]
        public void ConvertValuesFromRemainingArguments_TreatsAnEmptyListAsNoArguments()
        {
            using IDisposable scope = powerShell.Enter();

            // Act
            IDictionary<string, object> values = PowerShellUtilities.ConvertValuesFromRemainingArguments([]);

            // Assert
            Assert.Empty(values);
        }

        /// <summary>
        /// Verifies that a value with no parameter before it is discarded.
        /// </summary>
        [Fact]
        public void ConvertValuesFromRemainingArguments_DiscardsAValueWithNoParameter()
        {
            using IDisposable scope = powerShell.Enter();

            Assert.Empty(PowerShellUtilities.ConvertValuesFromRemainingArguments(["orphan"]));
            Assert.Empty(PowerShellUtilities.ConvertValuesFromRemainingArguments([]));
        }

        /// <summary>
        /// Verifies that a single-character parameter name is not recognised.
        /// </summary>
        /// <remarks>
        /// The pattern requires at least two characters after the dash, so <c language="powershell">-f</c> is read as a value rather than a
        /// parameter and is discarded for having no parameter before it. PowerShell itself accepts a one-character
        /// name, so this is a real difference; pinned as the current behaviour rather than endorsed.
        /// </remarks>
        [Fact]
        public void ConvertValuesFromRemainingArguments_DoesNotRecogniseASingleCharacterName()
        {
            using IDisposable scope = powerShell.Enter();

            Assert.Empty(PowerShellUtilities.ConvertValuesFromRemainingArguments(["-f"]));
            Assert.True(PowerShellUtilities.ConvertValuesFromRemainingArguments(["-fo"]).ContainsKey("fo"));
        }

        /// <summary>
        /// Verifies that a name and value joined by a colon in one token is not recognised.
        /// </summary>
        /// <remarks>
        /// <c language="powershell">-Name:Notepad</c> is valid PowerShell, but the pattern only allows a colon at the very end, so the whole
        /// token is read as a value. Pinned for the same reason as the single-character case.
        /// </remarks>
        [Fact]
        public void ConvertValuesFromRemainingArguments_DoesNotRecogniseANameJoinedToItsValue()
        {
            using IDisposable scope = powerShell.Enter();

            Assert.Empty(PowerShellUtilities.ConvertValuesFromRemainingArguments(["-Name:Notepad"]));
        }

        /// <summary>
        /// Verifies that a switch becomes a bare parameter, and an absent one nothing at all.
        /// </summary>
        [Fact]
        public void ConvertBoundParametersToArgumentList_RendersASwitchAsABareParameter()
        {
            using IDisposable scope = powerShell.Enter();

            Assert.Equal(["-Force"], PowerShellUtilities.ConvertBoundParametersToArgumentList([new("Force", new SwitchParameter(isPresent: true))]));
            Assert.Empty(PowerShellUtilities.ConvertBoundParametersToArgumentList([new("Force", new SwitchParameter(isPresent: false))]));
        }

        /// <summary>
        /// Verifies that a value is rendered against its parameter.
        /// </summary>
        [Fact]
        public void ConvertBoundParametersToArgumentList_RendersAValueAgainstItsParameter()
        {
            using IDisposable scope = powerShell.Enter();

            Assert.Equal(
                ["-Name:Notepad", "-Count:3"],
                PowerShellUtilities.ConvertBoundParametersToArgumentList([new("Name", "Notepad"), new("Count", 3)]));
        }

        /// <summary>
        /// Verifies that a parameter whose value renders as nothing is left out.
        /// </summary>
        [Fact]
        public void ConvertBoundParametersToArgumentList_LeavesOutAValueThatRendersAsNothing()
        {
            using IDisposable scope = powerShell.Enter();

            Assert.Equal(
                ["-Other:kept"],
                PowerShellUtilities.ConvertBoundParametersToArgumentList([new("Name", string.Empty), new("Other", "kept")]));
        }

        /// <summary>
        /// Verifies that a nested list of remaining arguments is flattened into the result.
        /// </summary>
        /// <remarks>
        /// This is how a forwarded <c language="powershell">$args</c> arrives: one bound parameter whose value is the whole unbound list.
        /// Recognised by the list carrying something that looks like a parameter, and turned back into arguments by
        /// the same conversion that reads remaining arguments in the first place.
        /// </remarks>
        [Fact]
        public void ConvertBoundParametersToArgumentList_FlattensNestedRemainingArguments()
        {
            using IDisposable scope = powerShell.Enter();

            // Act
            IReadOnlyList<string> arguments = PowerShellUtilities.ConvertBoundParametersToArgumentList(
                [new("Name", "Notepad"), new("Rest", new object[] { "-Extra", "value", "-Flag" })]);

            // Assert
            Assert.Equal(["-Name:Notepad", "-Extra:value", "-Flag"], arguments);
        }

        /// <summary>
        /// Verifies that a list carrying no parameter-looking token is rendered as a value rather than flattened.
        /// </summary>
        [Fact]
        public void ConvertBoundParametersToArgumentList_TreatsAPlainListAsAValue()
        {
            using IDisposable scope = powerShell.Enter();

            // Act
            IReadOnlyList<string> arguments = PowerShellUtilities.ConvertBoundParametersToArgumentList(
                [new("Items", new object[] { "one", "two" })]);

            // Assert
            _ = Assert.Single(arguments);
            Assert.StartsWith("-Items:", arguments[0], StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a missing name is refused.
        /// </summary>
        /// <remarks>
        /// Not reachable through a dictionary, which cannot hold a null key, so the pairs are built by hand - which is
        /// the only way the guard can be exercised at all.
        /// </remarks>
        [Fact]
        public void ConvertBoundParametersToArgumentList_RefusesAMissingName()
        {
            using IDisposable scope = powerShell.Enter();

            _ = Assert.Throws<InvalidOperationException>(static () =>
                PowerShellUtilities.ConvertBoundParametersToArgumentList([new(null!, "value")]));
        }

        /// <summary>
        /// Verifies that the rendered arguments cannot be added to.
        /// </summary>
        /// <remarks>
        /// The opposite of the other conversion, and deliberately so: a rendered command line is finished, whereas a
        /// stand-in for <c language="powershell">$PSBoundParameters</c> is not.
        /// </remarks>
        [Fact]
        public void ConvertBoundParametersToArgumentList_ReturnsSomethingFinished()
        {
            using IDisposable scope = powerShell.Enter();

            // Act
            IReadOnlyList<string> arguments = PowerShellUtilities.ConvertBoundParametersToArgumentList([new("Name", "Notepad")]);

            // Assert
            Assert.True(((IList<string>)arguments).IsReadOnly);
        }

        /// <summary>
        /// The shapes PowerShell uses to mean nothing.
        /// </summary>
        public static TheoryData<string, object?> Nothings =>
            new()
            {
                { "null", null },
                { "AutomationNull", System.Management.Automation.Internal.AutomationNull.Value },
                { "NullString", NullString.Value },
                { "DBNull", DBNull.Value },
            };

        /// <summary>
        /// An array to use in tests that need a value which renders as something rather than nothing.
        /// </summary>
        private static readonly string[] testArray = ["one"];
    }
}
