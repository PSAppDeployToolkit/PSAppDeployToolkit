using System;
using System.Collections.Generic;
using System.Linq;
using PSADT.ProcessManagement;

namespace PSADT.Tests.ProcessManagement
{
    /// <summary>
    /// Comprehensive unit tests for the CommandLineUtilities class that verify strict adherence
    /// to Microsoft's CommandLineToArgv(), msvcrt pre-2008, msvcrt post-2008, and other Windows
    /// command line parsing standards.
    /// </summary>
    public class CommandLineUtilitiesTests
    {
        /// <summary>
        /// Tests basic argument parsing with simple cases.
        /// </summary>
        [Theory]
        [InlineData("program", new[] { "program" })]
        [InlineData("program arg1 arg2", new[] { "program", "arg1", "arg2" })]
        [InlineData("program  arg1   arg2  ", new[] { "program", "arg1", "arg2" })]
        public void CommandLineToArgumentList_BasicCases_ReturnsCorrectArguments(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests argument parsing with quoted strings.
        /// </summary>
        [Theory]
        [InlineData("\"program\"", new[] { "program" })]
        [InlineData("\"program with spaces\"", new[] { "program with spaces" })]
        [InlineData("program \"arg with spaces\"", new[] { "program", "arg with spaces" })]
        [InlineData("\"program\" \"arg1\" \"arg2\"", new[] { "program", "arg1", "arg2" })]
        [InlineData("\"program\"  \"arg1\"   \"arg2\"  ", new[] { "program", "arg1", "arg2" })]
        public void CommandLineToArgumentList_QuotedArguments_ReturnsCorrectArguments(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests argument parsing with backslash escaping according to Windows rules.
        /// </summary>
        [Theory]
        [InlineData("program \\arg", new[] { "program", "\\arg" })]
        [InlineData("program \\\\arg", new[] { "program", "\\\\arg" })]
        [InlineData("program \"\\arg\"", new[] { "program", "\\arg" })]
        [InlineData("program \"\\\\arg\"", new[] { "program", "\\\\arg" })]
        [InlineData("program \\\"arg", new[] { "program", "\"arg" })]
        [InlineData("program \\\\\\\"arg", new[] { "program", "\\\"arg" })]
        [InlineData("program \"\\\"arg\"", new[] { "program", "\"arg" })]
        [InlineData("program \"\\\\\\\"arg\"", new[] { "program", "\\\"arg" })]
        public void CommandLineToArgumentList_BackslashEscaping_ReturnsCorrectArguments(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests complex cases that test the interaction between quotes and backslashes
        /// according to CommandLineToArgv() and msvcrt rules.
        /// </summary>
        [Theory]
        [InlineData("program \"a\\\\b\"", new[] { "program", "a\\\\b" })]
        [InlineData("program \"a\\\\\\\"b\"", new[] { "program", "a\\\"b" })]
        [InlineData("program \"a\\\\\\\\\"", new[] { "program", "a\\\\" })] // 4 backslashes followed by a quote -> 2 backslashes, quote is a delimiter
        [InlineData("program a\\\\\\\"b", new[] { "program", "a\\\"b" })]
        [InlineData("program \"a b\\\\\"", new[] { "program", "a b\\" })] // 2 backslashes followed by a quote -> 1 backslash, quote is a delimiter
        [InlineData("program \"a b\\\\\\\"\"", new[] { "program", "a b\\\"" })]
        public void CommandLineToArgumentList_ComplexBackslashQuoteCombinations_ReturnsCorrectArguments(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests edge cases with empty arguments and special characters.
        /// </summary>
        [Theory]
        [InlineData("program \"\"", new[] { "program", "" })]
        [InlineData("program \"\" \"\"", new[] { "program", "", "" })]
        [InlineData("program \"\" arg \"\"", new[] { "program", "", "arg", "" })]
        [InlineData("program \t arg", new[] { "program", "arg" })]
        [InlineData("program\targ", new[] { "program", "arg" })]
        public void CommandLineToArgumentList_EdgeCases_ReturnsCorrectArguments(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests null input handling.
        /// </summary>
        [Fact]
        public void CommandLineToArgumentList_NullInput_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => CommandLineUtilities.CommandLineToArgumentList(null!));
        }

        /// <summary>
        /// Tests basic command line creation from arguments.
        /// </summary>
        [Theory]
        [InlineData(new[] { "program" }, "program")]
        [InlineData(new[] { "program", "arg1", "arg2" }, "program arg1 arg2")]
        public void ArgumentListToCommandLine_BasicCases_ReturnsCorrectCommandLine(string[] args, string expected)
        {
            // Act
            string result = CommandLineUtilities.ArgumentListToCommandLine(args)!;

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests command line creation with arguments that need quoting.
        /// </summary>
        [Theory]
        [InlineData(new[] { "program with spaces" }, "\"program with spaces\"")]
        [InlineData(new[] { "program", "arg with spaces" }, "program \"arg with spaces\"")]
        [InlineData(new[] { "program with spaces", "arg with spaces" }, "\"program with spaces\" \"arg with spaces\"")]
        [InlineData(new[] { "" }, "\"\"")]
        [InlineData(new[] { "program", "", "arg" }, "program \"\" arg")]
        public void ArgumentListToCommandLine_ArgumentsWithSpaces_ReturnsQuotedCommandLine(string[] args, string expected)
        {
            // Act
            string result = CommandLineUtilities.ArgumentListToCommandLine(args)!;

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests command line creation with arguments containing special characters.
        /// </summary>
        [Theory]
        [InlineData(new[] { "program", "arg\"with\"quotes" }, "program \"arg\\\"with\\\"quotes\"")]
        [InlineData(new[] { "program", "arg\\with\\backslashes" }, "program arg\\with\\backslashes")] // No quotes needed
        [InlineData(new[] { "program", "arg\\\"escaped" }, "program \"arg\\\\\\\"escaped\"")]
        [InlineData(new[] { "program", "path\\to\\file\\" }, "program path\\to\\file\\")]
        public void ArgumentListToCommandLine_ArgumentsWithSpecialCharacters_ReturnsEscapedCommandLine(string[] args, string expected)
        {
            // Act
            string result = CommandLineUtilities.ArgumentListToCommandLine(args)!;

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests that ArgumentListToCommandLine with List and Array return the same results.
        /// </summary>
        [Fact]
        public void ArgumentListToCommandLine_ListAndArray_ReturnSameResults()
        {
            // Arrange
            string[] args = { "program", "arg1", "arg2" };
            List<string> argsList = args.ToList();

            // Act
            string arrayResult = CommandLineUtilities.ArgumentListToCommandLine(args)!;
            string listResult = CommandLineUtilities.ArgumentListToCommandLine(argsList)!;

            // Assert
            Assert.Equal(arrayResult, listResult);
        }

        /// <summary>
        /// Tests that ArgumentListToCommandLine with List and Array return the same results for complex arguments.
        /// </summary>
        [Fact]
        public void ArgumentListToCommandLine_ComplexListAndArray_ReturnSameResults()
        {
            // Arrange
            string[] args = { "program with spaces", "arg with spaces" };
            List<string> argsList = args.ToList();

            // Act
            string arrayResult = CommandLineUtilities.ArgumentListToCommandLine(args)!;
            string listResult = CommandLineUtilities.ArgumentListToCommandLine(argsList)!;

            // Assert
            Assert.Equal(arrayResult, listResult);
        }

        /// <summary>
        /// Tests that ArgumentListToCommandLine with List and Array return the same results for arguments with quotes.
        /// </summary>
        [Fact]
        public void ArgumentListToCommandLine_QuotedListAndArray_ReturnSameResults()
        {
            // Arrange
            string[] args = { "program", "arg\"with\"quotes" };
            List<string> argsList = args.ToList();

            // Act
            string arrayResult = CommandLineUtilities.ArgumentListToCommandLine(args)!;
            string listResult = CommandLineUtilities.ArgumentListToCommandLine(argsList)!;

            // Assert
            Assert.Equal(arrayResult, listResult);
        }

        /// <summary>
        /// Tests null input handling for ArgumentListToCommandLine.
        /// </summary>
        [Fact]
        public void ArgumentListToCommandLine_NullArrayInput_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => CommandLineUtilities.ArgumentListToCommandLine((IReadOnlyList<string>)null!));
        }

        /// <summary>
        /// Tests null input handling for ArgumentListToCommandLine with List.
        /// </summary>
        [Fact]
        public void ArgumentListToCommandLine_NullListInput_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => CommandLineUtilities.ArgumentListToCommandLine((List<string>)null!));
        }

        /// <summary>
        /// Tests that a null argument within the array is treated as an empty, quoted string.
        /// </summary>
        [Fact]
        public void ArgumentListToCommandLine_NullArgument_IsTreatedAsEmptyString()
        {
            // Arrange
            string[] argsWithNull = { "program", null!, "arg" };
            const string expected = "program \"\" arg";

            // Act
            string result = CommandLineUtilities.ArgumentListToCommandLine(argsWithNull)!;

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests round-trip parsing: parsing a command line and then recreating it should yield equivalent results.
        /// </summary>
        [Theory]
        [InlineData("program arg1 arg2")]
        [InlineData("\"program with spaces\" \"arg with spaces\"")]
        [InlineData("program \"arg\\\"with\\\"quotes\"")]
        [InlineData("program \"arg\\\\with\\\\backslashes\"")]
        [InlineData("program \"\"")]
        [InlineData("program \"path\\\\to\\\\file\\\\\"")]
        public void RoundTripParsing_VariousInputs_PreservesArguments(string originalCommandLine)
        {
            // Act
            IReadOnlyList<string> parsed = CommandLineUtilities.CommandLineToArgumentList(originalCommandLine);
            string recreated = CommandLineUtilities.ArgumentListToCommandLine(parsed)!;
            IReadOnlyList<string> reparsed = CommandLineUtilities.CommandLineToArgumentList(recreated)!;

            // Assert
            Assert.Equal(parsed, reparsed);
        }

        /// <summary>
        /// Tests round-trip conversion: creating a command line from arguments and parsing it back.
        /// </summary>
        [Fact]
        public void RoundTrip_CreateAndParse_PreservesComplexArguments()
        {
            // Test case 1: Complex arguments with various special characters
            string[] originalArgs1 = { "a", "b c", "d\"e", "f\\g", "h\\\"i", "j\\\\k", "l\\\\" };
            string[] expectedArgs1 = originalArgs1.Select(a => a ?? string.Empty).ToArray();
            string commandLine1 = CommandLineUtilities.ArgumentListToCommandLine(originalArgs1)!;
            IReadOnlyList<string> parsedArgs1 = CommandLineUtilities.CommandLineToArgumentList(commandLine1)!;
            Assert.Equal(expectedArgs1, parsedArgs1);

            // Test case 2: Path arguments  
            string[] originalArgs2 = { "C:\\Program Files\\My App\\", "data.csv" };
            string[] expectedArgs2 = originalArgs2.Select(a => a ?? string.Empty).ToArray();
            string commandLine2 = CommandLineUtilities.ArgumentListToCommandLine(originalArgs2)!;
            IReadOnlyList<string> parsedArgs2 = CommandLineUtilities.CommandLineToArgumentList(commandLine2)!;
            Assert.Equal(expectedArgs2, parsedArgs2);

            // Test case 3: Empty string
            string[] originalArgs3 = { "" };
            string[] expectedArgs3 = originalArgs3.Select(a => a ?? string.Empty).ToArray();
            string commandLine3 = CommandLineUtilities.ArgumentListToCommandLine(originalArgs3)!;
            IReadOnlyList<string> parsedArgs3 = CommandLineUtilities.CommandLineToArgumentList(commandLine3)!;
            Assert.Equal(expectedArgs3, parsedArgs3);

            // Test case 4: Arguments with empty strings
            string[] originalArgs4 = { "a", "", "b" };
            string[] expectedArgs4 = originalArgs4.Select(a => a ?? string.Empty).ToArray();
            string commandLine4 = CommandLineUtilities.ArgumentListToCommandLine(originalArgs4)!;
            IReadOnlyList<string> parsedArgs4 = CommandLineUtilities.CommandLineToArgumentList(commandLine4)!;
            Assert.Equal(expectedArgs4, parsedArgs4);

            // Test case 5: Multiple simple arguments
            string[] originalArgs5 = { "a", "b c", "d", "e" };
            string[] expectedArgs5 = originalArgs5.Select(a => a ?? string.Empty).ToArray();
            string commandLine5 = CommandLineUtilities.ArgumentListToCommandLine(originalArgs5)!;
            IReadOnlyList<string> parsedArgs5 = CommandLineUtilities.CommandLineToArgumentList(commandLine5)!;
            Assert.Equal(expectedArgs5, parsedArgs5);
        }

        /// <summary>
        /// Tests known cases that should match CommandLineToArgv() behavior exactly.
        /// These test cases are based on documented Windows behavior.
        /// </summary>
        [Theory]
        [InlineData("program a\\\\\\\"b c", new[] { "program", "a\\\"b", "c" })]
        [InlineData("program \"a\\\\\\\"b c\"", new[] { "program", "a\\\"b c" })]
        [InlineData("program a\\\\\\\\\"b c\"", new[] { "program", "a\\\\b c" })]
        [InlineData("program \"a b\\\\\"", new[] { "program", "a b\\" })] // 2 backslashes followed by a quote -> 1 backslash, quote is a delimiter
        [InlineData("program \"a b\\\\\\\\\"", new[] { "program", "a b\\\\" })] // 4 backslashes followed by a quote -> 2 backslashes, quote is a delimiter
        public void CommandLineToArgumentList_CommandLineToArgvCompatibility_MatchesExpectedBehavior(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests the CommandLineToArgvW-specific behavior where characters immediately
        /// following a closing quote are appended to the argument.
        /// </summary>
        [Theory]
        [InlineData("\"ab\"c", new[] { "abc" })]
        [InlineData("\"a b\"c", new[] { "a bc" })]
        [InlineData("\"a b\" c", new[] { "a b", "c" })]
        [InlineData("x\"a b\"c", new[] { "xa bc" })]
        [InlineData("x\"a b\"c y", new[] { "xa bc", "y" })]
        [InlineData("\"\"c", new[] { "c" })]
        public void CommandLineToArgumentList_AppendedCharsAfterQuotes_ArePartOfArgument(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests that unquoted and quoted parts of a single argument are parsed as one.
        /// </summary>
        [Theory]
        [InlineData("arg1\" part2\"arg3", new[] { "arg1 part2arg3" })]
        //[InlineData("arg1\" part2 \"arg3", new[] { "arg1 part2 ", "arg3" })]
        [InlineData("arg1\\\"part2", new[] { "arg1\"part2" })]
        public void CommandLineToArgumentList_MixedQuotedAndUnquoted_ParsesAsSingleArgument(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests tab character handling (tabs should be treated as whitespace).
        /// </summary>
        [Theory]
        [InlineData("program\targ1\targ2", new[] { "program", "arg1", "arg2" })]
        [InlineData("program\t\targ1", new[] { "program", "arg1" })]
        [InlineData("\tprogram arg1\t", new[] { "program", "arg1" })]
        [InlineData("program \"arg\twith\ttabs\"", new[] { "program", "arg\twith\ttabs" })]
        public void CommandLineToArgumentList_TabCharacters_TreatedAsWhitespace(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests performance with large command lines to ensure the parser remains efficient.
        /// </summary>
        [Fact]
        public void CommandLineToArgumentList_LargeCommandLine_ParsesEfficiently()
        {
            // Arrange
            const int argCount = 1000;
            List<string> originalArgs = [];
            for (int i = 0; i < argCount; i++)
            {
                originalArgs.Add($"argument{i}");
            }
            string commandLine = CommandLineUtilities.ArgumentListToCommandLine(originalArgs)!;

            // Act
            DateTime start = DateTime.UtcNow;
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine)!;
            TimeSpan elapsed = DateTime.UtcNow - start;

            // Assert
            Assert.Equal(argCount, result.Count);
            Assert.True(elapsed.TotalMilliseconds < 100, $"Parsing took {elapsed.TotalMilliseconds}ms, expected < 100ms");
        }

        /// <summary>
        /// Tests that arguments with null values are handled properly in ArgumentListToCommandLine.
        /// </summary>
        [Fact]
        public void ArgumentListToCommandLine_ArgumentsWithNull_HandlesGracefully()
        {
            // Arrange
            string[] argsWithNull = { "program", null!, "arg" };
            string expected = "program \"\" arg";

            // Act
            string result = CommandLineUtilities.ArgumentListToCommandLine(argsWithNull)!;

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests special Windows-specific cases that might appear in real-world scenarios.
        /// </summary>
        [Theory]
        [InlineData("\"C:\\Program Files\\MyApp\\myapp.exe\" --config \"C:\\Users\\User\\Documents\\config.json\"",
                   new[] { "C:\\Program Files\\MyApp\\myapp.exe", "--config", "C:\\Users\\User\\Documents\\config.json" })]
        [InlineData("cmd.exe /c \"echo Hello World\"", new[] { "cmd.exe", "/c", "echo Hello World" })]
        [InlineData("powershell.exe -Command \"Get-Process | Where-Object { $_.Name -eq 'notepad' }\"",
                   new[] { "powershell.exe", "-Command", "Get-Process | Where-Object { $_.Name -eq 'notepad' }" })]
        public void CommandLineToArgumentList_RealWorldScenarios_ParsedCorrectly(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests handling of empty and whitespace-only command lines.
        /// </summary>
        [Theory]
        [InlineData("", new string[0])]
        [InlineData("   ", new string[0])]
        [InlineData("\t", new string[0])]
        [InlineData("  \t  ", new string[0])]
        public void CommandLineToArgumentList_EmptyAndWhitespace_ReturnsEmptyList(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests msvcrt-specific edge cases with backslashes at end of arguments.
        /// </summary>
        [Theory]
        [InlineData("program arg\\", new[] { "program", "arg\\" })]
        [InlineData("program arg\\\\", new[] { "program", "arg\\\\" })]
        [InlineData("program \"arg\\\"", new[] { "program", "arg\"" })]  // 1 backslash + quote -> literal quote
        [InlineData("program \"arg\\\\\"", new[] { "program", "arg\\" })]  // 2 backslashes + quote -> 1 backslash, quote is delimiter
        [InlineData("program \"arg\\\\\\\"", new[] { "program", "arg\\\"" })] // 3 backslashes + quote -> 1 backslash + literal quote
        public void CommandLineToArgumentList_TrailingBackslashes_ParsedCorrectly(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests advanced quote state transitions according to CommandLineToArgv behavior.
        /// </summary>
        [Theory]
        [InlineData("a\"b c\"d", new[] { "ab cd" })]
        [InlineData("a\"\"b", new[] { "ab" })]
        [InlineData("\"\"\"a", new[] { "\"a" })]
        [InlineData("\"\"\"\"", new[] { "\"" })]
        [InlineData("\"a\"\"b\"", new[] { "a\"b" })]
        public void CommandLineToArgumentList_ComplexQuoteStates_ParsedCorrectly(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests various msvcrt pre-2008 vs post-2008 differences in backslash handling.
        /// These tests ensure the unified parser handles both standards correctly.
        /// </summary>
        [Theory]
        [InlineData("\"a\\\\\\\\\\\"b\"", new[] { "a\\\\\"b" })] // 5 backslashes + quote -> 2 backslashes + literal quote
        [InlineData("\"a\\\\\\\\\\\\\"b\"", new[] { "a\\\\\\b" })] // 6 backslashes + quote -> 3 backslashes, quote is delimiter
        [InlineData("a\\\\\\\"b c", new[] { "a\\\"b", "c" })] // 3 backslashes + quote -> 1 backslash + literal quote
        [InlineData("a\\\\\\\\\"b c\"", new[] { "a\\\\b c" })] // 4 backslashes + quote -> 2 backslashes, quote starts string
        public void CommandLineToArgumentList_MsvcrtBackslashRules_ParsedCorrectly(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests Windows-specific filename and path parsing scenarios.
        /// </summary>
        [Theory]
        [InlineData("\"C:\\Program Files\\App\\app.exe\" /flag", new[] { "C:\\Program Files\\App\\app.exe", "/flag" })]
        [InlineData("C:\\path\\file.txt", new[] { "C:\\path\\file.txt" })]
        [InlineData("\"C:\\path with spaces\\file.txt\"", new[] { "C:\\path with spaces\\file.txt" })]
        [InlineData("\"\\\\server\\share\\file.txt\"", new[] { "\\\\server\\share\\file.txt" })]
        [InlineData("\"C:\\temp\\\\\"", new[] { "C:\\temp\\" })]
        public void CommandLineToArgumentList_WindowsPathScenarios_ParsedCorrectly(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests edge cases in ArgumentListToCommandLine escaping.
        /// </summary>
        [Theory]
        [InlineData(new[] { "arg ending with backslash\\" }, "\"arg ending with backslash\\\\\"")]
        [InlineData(new[] { "arg ending with quote\"" }, "\"arg ending with quote\\\"\"")]
        [InlineData(new[] { "arg ending with backslash and quote\\\"" }, "\"arg ending with backslash and quote\\\\\\\"\"")]
        public void ArgumentListToCommandLine_TrailingSpecialChars_EscapedCorrectly(string[] args, string expected)
        {
            // Act
            string result = CommandLineUtilities.ArgumentListToCommandLine(args)!;

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests handling of unterminated quotes - quotes that are opened but never closed.
        /// In this case, the parser should continue to the end of the command line.
        /// </summary>
        [Theory]
        [InlineData("\"a b c", new[] { "a b c" })]
        [InlineData("\"a b c ", new[] { "a b c " })]
        [InlineData("program \"unterminated arg", new[] { "program", "unterminated arg" })]
        [InlineData("\"program with spaces", new[] { "program with spaces" })]
        public void CommandLineToArgumentList_UnterminatedQuote_ParsesToEndOfLine(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests MSVCRT-specific quote escaping using the double quote ("") method within quoted strings.
        /// This is a specific behavior of some Microsoft C Runtime implementations.
        /// </summary>
        [Theory]
        [InlineData("\"a \"\" b\"", new[] { "a \" b" })]
        [InlineData("\"a \"\"\" b\"", new[] { "a \"", "b" })]  // Complex case: first "" becomes literal ", then third " ends quote
        [InlineData("\"text \"\"with\"\" quotes\"", new[] { "text \"with\" quotes" })]
        [InlineData("\"\"\"\"", new[] { "\"" })]
        [InlineData("\"a\"\"b\"", new[] { "a\"b" })]
        public void CommandLineToArgumentList_MsvcrtDoubleQuoteEscaping_HandledCorrectly(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests explicit even backslash count handling before quotes in quoted arguments.
        /// Even number of backslashes before a quote means the backslashes are literal and the quote toggles quote mode.
        /// </summary>
        [Theory]
        [InlineData("\"a\\\\\"b\"", new[] { "a\\b" })]
        [InlineData("\"a\\\\\\\\\"b\"", new[] { "a\\\\b" })]
        [InlineData("\"path\\\\\"end\"", new[] { "path\\end" })]
        public void CommandLineToArgumentList_EvenBackslashesBeforeQuote_EscapesBackslashesAndTogglesQuote(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests explicit odd backslash count handling before quotes in quoted arguments.
        /// Odd number of backslashes before a quote means the last backslash escapes the quote, making it literal.
        /// </summary>
        [Theory]
        [InlineData("\"a\\\"b\"", new[] { "a\"b" })]
        [InlineData("\"a\\\\\\\"b\"", new[] { "a\\\"b" })]
        [InlineData("\"path\\\\\\\"file\"", new[] { "path\\\"file" })]
        public void CommandLineToArgumentList_OddBackslashesBeforeQuote_EscapesBackslashesAndQuote(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests comprehensive ArgumentListToCommandLine scenarios with Windows path handling
        /// and trailing backslash edge cases that are common in real-world usage.
        /// </summary>
        [Theory]
        [InlineData(new[] { "C:\\Program Files\\App\\", "arg" }, "\"C:\\Program Files\\App\\\\\" arg")]
        [InlineData(new[] { "C:\\Path\\" }, "C:\\Path\\")]  // Fixed: No quotes needed for simple paths
        [InlineData(new[] { "C:\\Path\\\"" }, "\"C:\\Path\\\\\\\"\"")]
        [InlineData(new[] { "program", "arg\\\\escaped" }, "program arg\\\\escaped")]  // Fixed: No quotes needed
        [InlineData(new[] { "a\\", "b" }, "a\\ b")]
        [InlineData(new[] { "a\\\\", "b" }, "a\\\\ b")]
        [InlineData(new[] { "a\\", "" }, "a\\ \"\"")]
        public void ArgumentListToCommandLine_WindowsPathsAndTrailingBackslashes_EscapedCorrectly(string[] args, string expected)
        {
            // Act
            string result = CommandLineUtilities.ArgumentListToCommandLine(args)!;

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Provides comprehensive test data for systematic round-trip testing.
        /// This ensures that ArgumentListToCommandLine followed by CommandLineToArgumentList
        /// preserves the original arguments exactly.
        /// </summary>
        public static IEnumerable<object[]> SystematicRoundTripTestData()
        {
            yield return new object[] { new[] { "a", "b" } };
            yield return new object[] { new[] { "a b", "c" } };
            yield return new object[] { new[] { "a\tb", "c" } };
            yield return new object[] { new[] { "" } };
            yield return new object[] { new[] { "a", "" } };
            yield return new object[] { new[] { "a\"b" } };
            yield return new object[] { new[] { "a\\b" } };
            yield return new object[] { new[] { "a\\\\b" } };
            yield return new object[] { new[] { "a\\\"b" } };
            yield return new object[] { new[] { "a b", "c d" } };
            yield return new object[] { new[] { "a\\", "b" } };
            yield return new object[] { new[] { "a\\\\", "b" } };
            yield return new object[] { new[] { "a\\", "" } };
            yield return new object[] { new[] { "a b c" } };
            yield return new object[] { new[] { "a", "b c" } };
            yield return new object[] { new[] { "a", "b\tc" } };
            yield return new object[] { new[] { "a", "b\"c" } };
            yield return new object[] { new[] { "a", "b\\" } };
            yield return new object[] { new[] { "a", "b\\\\" } };
            yield return new object[] { new[] { "a", "b\\c" } };
            yield return new object[] { new[] { "a", "b\\\\c" } };
            yield return new object[] { new[] { "a", "b\\\"c" } };
            yield return new object[] { new[] { "C:\\Program Files\\App\\" } };
            yield return new object[] { new[] { "C:\\Path\\", "arg with space" } };
            yield return new object[] { new[] { "argument with \"quotes\"" } };
            yield return new object[] { new[] { "argument with \"\"escaped quotes\"\"" } };
            yield return new object[] { new[] { "c:\\Path with spaces\\trailing_backslash\\" } };
            yield return new object[] { new[] { "program", "arg1", "arg2" } };
            yield return new object[] { new[] { "complex\"arg", "with\\backslashes", "and spaces" } };
            yield return new object[] { new[] { "\t\t", "  ", "mixed whitespace" } };
        }

        /// <summary>
        /// Tests systematic round-trip conversion using MemberData for comprehensive coverage.
        /// This test ensures that arguments can be converted to command line and back without loss.
        /// </summary>
        [Theory]
        [MemberData(nameof(SystematicRoundTripTestData))]
        public void SystematicRoundTrip_ArgumentListToCommandLineAndBack_PreservesArguments(string[] originalArgv)
        {
            // Act
            string commandLine = CommandLineUtilities.ArgumentListToCommandLine(originalArgv)!;
            IReadOnlyList<string> roundTripArgv = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(originalArgv, roundTripArgv);
        }

        /// <summary>
        /// Tests systematic round-trip parsing starting from command line strings.
        /// This verifies that command lines parse correctly and can be reconstructed.
        /// </summary>
        [Theory]
        [InlineData("a b", new[] { "a", "b" })]
        [InlineData("\"a b\"", new[] { "a b" })]
        [InlineData("a\"\"b", new[] { "ab" })]
        [InlineData("a\"\"\"b", new[] { "a\"b" })]
        [InlineData("\"a b\" c", new[] { "a b", "c" })]
        [InlineData("\"a\\\\b\"", new[] { "a\\\\b" })]
        [InlineData("\"a\\\"b\"", new[] { "a\"b" })]
        [InlineData("\"a\\\\\\\"b\"", new[] { "a\\\"b" })]
        [InlineData("\"a\\\\\\\\\"b\"", new[] { "a\\\\b" })]
        [InlineData("C:\\Windows\\System32", new[] { "C:\\Windows\\System32" })]
        [InlineData("\"C:\\Program Files\\App\"", new[] { "C:\\Program Files\\App" })]
        [InlineData("argument \"with inner\" quotes", new[] { "argument", "with inner", "quotes" })]
        [InlineData("argument with\" inner quotes\"", new[] { "argument", "with inner quotes" })]
        [InlineData("\"C:\\My Dir\\\\\"", new[] { "C:\\My Dir\\" })]
        [InlineData("\"\"", new[] { "" })]
        [InlineData("one two \"three four\"", new[] { "one", "two", "three four" })]
        [InlineData("one \"two three\" four", new[] { "one", "two three", "four" })]
        [InlineData("one \"\" two", new[] { "one", "", "two" })]
        [InlineData("a\\ b", new[] { "a\\", "b" })]
        [InlineData("a\\\\ b", new[] { "a\\\\", "b" })]
        [InlineData("a\\\"b c", new[] { "a\"b", "c" })]
        [InlineData("a\\\\\"b c", new[] { "a\\b c" })]
        public void SystematicRoundTrip_CommandLineToArgumentListAndBack_PreservesArguments(string commandLine, string[] expectedArgv)
        {
            // Act
            IReadOnlyList<string> argv = CommandLineUtilities.CommandLineToArgumentList(commandLine);
            string newCommandLine = CommandLineUtilities.ArgumentListToCommandLine(argv)!;
            IReadOnlyList<string> newArgv = CommandLineUtilities.CommandLineToArgumentList(newCommandLine);

            // Assert
            Assert.Equal(expectedArgv, argv);
            Assert.Equal(argv, newArgv);
        }

        /// <summary>
        /// Tests real-world UNC path scenarios that might be encountered in enterprise environments.
        /// </summary>
        [Theory]
        [InlineData("\"\\\\fileserver.domain.com\\shared\\IT\\Software\\Installers\\MyApp v2.1\\setup.exe\" /S /D=\"C:\\Program Files\\MyApp\"",
                   new[] { "\\\\fileserver.domain.com\\shared\\IT\\Software\\Installers\\MyApp v2.1\\setup.exe", "/S", "/D=C:\\Program Files\\MyApp" })]
        [InlineData("msiexec.exe /i \"\\\\server\\msi-packages\\Application Suite.msi\" /qn TARGETDIR=\"\\\\server\\app-installs\\Application\\\\\"",
                   new[] { "msiexec.exe", "/i", "\\\\server\\msi-packages\\Application Suite.msi", "/qn", "TARGETDIR=\\\\server\\app-installs\\Application\\" })]
        [InlineData("powershell.exe -File \"\\\\scripts-server\\powershell\\Deploy-Application.ps1\" -ApplicationPath \"\\\\apps-server\\applications\\MyApp\\\\\"",
                   new[] { "powershell.exe", "-File", "\\\\scripts-server\\powershell\\Deploy-Application.ps1", "-ApplicationPath", "\\\\apps-server\\applications\\MyApp\\" })]
        public void CommandLineToArgumentList_RealWorldUncScenarios_ParsedCorrectly(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests complex real-world scenarios that combine multiple edge cases.
        /// These scenarios are based on actual command lines that might be encountered in Windows environments.
        /// </summary>
        [Theory]
        [InlineData("\"C:\\Program Files\\\\app.exe\" -arg \"value with \\\"quotes\\\"\"", 
                   new[] { "C:\\Program Files\\\\app.exe", "-arg", "value with \"quotes\"" })]
        [InlineData("command -o \"output file.txt\" --path \"C:\\Users\\Test User\\\\\"", 
                   new[] { "command", "-o", "output file.txt", "--path", "C:\\Users\\Test User\\" })]
        [InlineData("arg1 \"arg2 with \"\"quotes\"\"\" arg3", 
                   new[] { "arg1", "arg2 with \"quotes\"", "arg3" })]
        [InlineData("msiexec.exe /i \"C:\\Temp\\App Installer.msi\" /qn TARGETDIR=\"C:\\Program Files\\My App\\\"",
                   new[] { "msiexec.exe", "/i", "C:\\Temp\\App Installer.msi", "/qn", "TARGETDIR=C:\\Program Files\\My App\"" })]
        public void CommandLineToArgumentList_ComplexRealWorldScenarios_ParsedCorrectly(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests ArgumentListToCommandLine with complex real-world scenarios.
        /// These test the reverse operation to ensure proper escaping for complex arguments.
        /// </summary>
        [Theory]
        [InlineData(new[] { "C:\\Program Files\\app.exe", "-arg", "value with \"quotes\"" }, 
                   "\"C:\\Program Files\\app.exe\" -arg \"value with \\\"quotes\\\"\"")]
        [InlineData(new[] { "command", "-o", "output file.txt", "--path", "C:\\Users\\Test User\\" }, 
                   "command -o \"output file.txt\" --path \"C:\\Users\\Test User\\\\\"")]
        [InlineData(new[] { "a b \" c \\ d e f" }, 
                   "\"a b \\\" c \\ d e f\"")]
        [InlineData(new[] { "arg1", "arg2 with \"quotes\"", "arg3" }, 
                   "arg1 \"arg2 with \\\"quotes\\\"\" arg3")]
        [InlineData(new[] { "msiexec.exe", "/i", "C:\\Temp\\App Installer.msi", "/qn", "TARGETDIR=C:\\Program Files\\My App\\" },
                   "msiexec.exe /i \"C:\\Temp\\App Installer.msi\" /qn \"TARGETDIR=C:\\Program Files\\My App\\\\\"")]
        public void ArgumentListToCommandLine_ComplexRealWorldScenarios_EscapedCorrectly(string[] argv, string expected)
        {
            // Act
            string result = CommandLineUtilities.ArgumentListToCommandLine(argv)!;

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests backslashes not followed by quotes are treated as literal characters.
        /// This is an important distinction in Windows command line parsing.
        /// </summary>
        [Theory]
        [InlineData("a\\\\b", new[] { "a\\\\b" })]
        [InlineData("a\\\\\\b", new[] { "a\\\\\\b" })]
        [InlineData("path\\to\\file", new[] { "path\\to\\file" })]
        [InlineData("C:\\Windows\\System32\\cmd.exe", new[] { "C:\\Windows\\System32\\cmd.exe" })]
        public void CommandLineToArgumentList_BackslashesNotFollowedByQuote_AreLiteral(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests backslashes in quoted arguments where they're not followed by quotes.
        /// These should be treated as literal backslashes.
        /// </summary>
        [Theory]
        [InlineData("\"a\\\\b\"", new[] { "a\\\\b" })]
        [InlineData("\"a\\\\\\b\"", new[] { "a\\\\\\b" })]
        [InlineData("\"C:\\Program Files\\App\"", new[] { "C:\\Program Files\\App" })]
        public void CommandLineToArgumentList_BackslashesInQuotedArgumentNotFollowedByQuote_AreLiteral(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests trailing backslashes at the end of command line arguments.
        /// This is important for file path handling in Windows.
        /// </summary>
        [Theory]
        [InlineData("a\\\\", new[] { "a\\\\" })]
        [InlineData("a\\\\\\", new[] { "a\\\\\\" })]
        [InlineData("\"path\\\\\"", new[] { "path\\" })]
        [InlineData("\"path\\\\\\\\\"", new[] { "path\\\\" })]
        public void CommandLineToArgumentList_TrailingBackslashesAtEndOfArgument_AreLiteral(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests performance with moderately sized argument lists to ensure the implementation scales well.
        /// This complements the existing large command line test with a different scenario.
        /// </summary>
        [Fact]
        public void ArgumentListToCommandLine_ModerateArgumentCount_PerformsEfficiently()
        {
            // Arrange
            const int argCount = 100;
            string[] args = new string[argCount];
            for (int i = 0; i < argCount; i++)
            {
                args[i] = $"argument {i} with spaces and \"quotes\" and \\backslashes";
            }

            // Act
            DateTime start = DateTime.UtcNow;
            string commandLine = CommandLineUtilities.ArgumentListToCommandLine(args)!;
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine)!;
            TimeSpan elapsed = DateTime.UtcNow - start;

            // Assert
            Assert.Equal(args.Length, result.Count);
            Assert.True(elapsed.TotalMilliseconds < 50, $"Parsing took {elapsed.TotalMilliseconds}ms, expected < 50ms");
        }

        /// <summary>
        /// Tests comprehensive UNC path scenarios to ensure proper backslash handling and escaping.
        /// UNC paths are critical for Windows environments and present unique challenges for command line parsing.
        /// </summary>
        [Theory]
        [InlineData("\\\\server\\share", new[] { "\\\\server\\share" })]
        [InlineData("\\\\server\\share\\file.txt", new[] { "\\\\server\\share\\file.txt" })]
        [InlineData("\"\\\\server\\share\"", new[] { "\\\\server\\share" })]
        [InlineData("\"\\\\server\\share\\file.txt\"", new[] { "\\\\server\\share\\file.txt" })]
        [InlineData("\\\\server\\share\\ arg", new[] { "\\\\server\\share\\", "arg" })]
        [InlineData("\"\\\\server\\share\\\\\"", new[] { "\\\\server\\share\\" })]
        [InlineData("\"\\\\server\\share\\\\\\\\\"", new[] { "\\\\server\\share\\\\" })]
        public void CommandLineToArgumentList_UncPaths_ParsedCorrectly(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests UNC paths with spaces that require quoting.
        /// </summary>
        [Theory]
        [InlineData("\"\\\\server name\\share name\"", new[] { "\\\\server name\\share name" })]
        [InlineData("\"\\\\server\\share with spaces\\file.txt\"", new[] { "\\\\server\\share with spaces\\file.txt" })]
        [InlineData("\"\\\\server\\share\\folder with spaces\\\"", new[] { "\\\\server\\share\\folder with spaces\"" })]
        [InlineData("\"\\\\very long server name\\very long share name\\very long file name.txt\"", 
                   new[] { "\\\\very long server name\\very long share name\\very long file name.txt" })]
        public void CommandLineToArgumentList_UncPathsWithSpaces_ParsedCorrectly(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests UNC paths with quotes in the path names (rare but possible).
        /// </summary>
        [Theory]
        [InlineData("\"\\\\server\\share\\file\"\"name.txt\"", new[] { "\\\\server\\share\\file\"name.txt" })]
        [InlineData("\"\\\\server\\share \"\"with quotes\"\"\\file.txt\"", new[] { "\\\\server\\share \"with quotes\"\\file.txt" })]
        [InlineData("\"\\\\server\\share\\folder\\file\"\"with\"\"quotes.txt\"", new[] { "\\\\server\\share\\folder\\file\"with\"quotes.txt" })]
        public void CommandLineToArgumentList_UncPathsWithQuotes_ParsedCorrectly(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests ArgumentListToCommandLine with various UNC path scenarios.
        /// Ensures proper escaping for UNC paths when converting back to command lines.
        /// </summary>
        [Theory]
        [InlineData(new[] { "\\\\server\\share" }, "\\\\server\\share")]
        [InlineData(new[] { "\\\\server\\share\\file.txt" }, "\\\\server\\share\\file.txt")]
        [InlineData(new[] { "\\\\server name\\share name" }, "\"\\\\server name\\share name\"")]
        [InlineData(new[] { "\\\\server\\share with spaces\\file.txt" }, "\"\\\\server\\share with spaces\\file.txt\"")]
        [InlineData(new[] { "\\\\server\\share\\" }, "\\\\server\\share\\")]
        [InlineData(new[] { "\\\\server\\share\\folder with spaces\\" }, "\"\\\\server\\share\\folder with spaces\\\\\"")]
        [InlineData(new[] { "\\\\server\\share\\file\"name.txt" }, "\"\\\\server\\share\\file\\\"name.txt\"")]
        public void ArgumentListToCommandLine_UncPaths_EscapedCorrectly(string[] args, string expected)
        {
            // Act
            string result = CommandLineUtilities.ArgumentListToCommandLine(args)!;

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests complex UNC path scenarios with multiple arguments and mixed path types.
        /// </summary>
        [Theory]
        [InlineData("copy \"C:\\Local Folder\\\\\" \"\\\\server\\remote\\\\\" /E /Y",
                   new[] { "copy", "C:\\Local Folder\\", "\\\\server\\remote\\", "/E", "/Y" })]
        [InlineData("robocopy \"D:\\Backup Source\\\\\" \"\\\\backup-server\\daily-backups\\$(date)\\\\\" /MIR",
                   new[] { "robocopy", "D:\\Backup Source\\", "\\\\backup-server\\daily-backups\\$(date)\\", "/MIR" })]
        [InlineData("move \"\\\\temp-server\\uploads\\file.txt\" \"C:\\Processing Queue\\incoming\\\\\"",
                   new[] { "move", "\\\\temp-server\\uploads\\file.txt", "C:\\Processing Queue\\incoming\\" })]
        public void CommandLineToArgumentList_ComplexUncScenarios_ParsedCorrectly(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests ArgumentListToCommandLine with complex UNC scenarios to ensure proper escaping.
        /// </summary>
        [Theory]
        [InlineData(new[] { "copy", "C:\\local file.txt", "\\\\server\\share\\remote file.txt" },
                   "copy \"C:\\local file.txt\" \"\\\\server\\share\\remote file.txt\"")]
        [InlineData(new[] { "robocopy", "C:\\Source Folder\\", "\\\\server\\backup\\Destination\\", "/E", "/Z" },
                   "robocopy \"C:\\Source Folder\\\\\" \\\\server\\backup\\Destination\\ /E /Z")]
        [InlineData(new[] { "net", "use", "Z:", "\\\\server\\share with spaces", "/persistent:yes" },
                   "net use Z: \"\\\\server\\share with spaces\" /persistent:yes")]
        [InlineData(new[] { "\\\\server\\share\\app.exe", "--config", "\\\\config-server\\configs\\app.config" },
                   "\\\\server\\share\\app.exe --config \\\\config-server\\configs\\app.config")]
        public void ArgumentListToCommandLine_ComplexUncScenarios_EscapedCorrectly(string[] args, string expected)
        {
            // Act
            string result = CommandLineUtilities.ArgumentListToCommandLine(args)!;

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests round-trip scenarios for UNC paths to ensure they are preserved accurately.
        /// </summary>
        public static IEnumerable<object[]> UncPathRoundTripTestData()
        {
            yield return new object[] { new[] { "\\\\server\\share" } };
            yield return new object[] { new[] { "\\\\server\\share\\file.txt" } };
            yield return new object[] { new[] { "\\\\server name\\share name" } };
            yield return new object[] { new[] { "\\\\server\\share with spaces\\file.txt" } };
            yield return new object[] { new[] { "\\\\server\\share\\" } };
            yield return new object[] { new[] { "\\\\server\\share\\folder with spaces\\" } };
            yield return new object[] { new[] { "\\\\server\\share\\file\"name.txt" } };
            yield return new object[] { new[] { "\\\\server\\share \"with quotes\"\\file.txt" } };
            yield return new object[] { new[] { "\\\\server\\share\\folder\\", "local-file.txt", "\\\\other-server\\other-share\\target.txt" } };
            yield return new object[] { new[] { "\\\\domain.com\\dfs-share\\deep\\folder\\structure\\file.extension" } };
        }

        /// <summary>
        /// Tests UNC path round-trip scenarios to ensure perfect preservation.
        /// These tests verify that UNC paths can be converted to command line and back without any loss.
        /// </summary>
        [Theory]
        [MemberData(nameof(UncPathRoundTripTestData))]
        public void UncPaths_RoundTripConversion_PreservesExactArguments(string[] originalArgs)
        {
            // Act
            string commandLine = CommandLineUtilities.ArgumentListToCommandLine(originalArgs)!;
            IReadOnlyList<string> roundTripArgs = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(originalArgs, roundTripArgs);
        }

        /// <summary>
        /// Tests UNC paths with edge cases involving backslashes at various positions.
        /// </summary>
        [Theory]
        [InlineData("\"\\\\server\\share\\\\\"end", new[] { "\\\\server\\share\\end" })]
        [InlineData("\\\\server\\share\\\\file", new[] { "\\\\server\\share\\\\file" })]
        [InlineData("\"\\\\server\\share\\\\\\\"file\"", new[] { "\\\\server\\share\\\"file" })]
        [InlineData("\"\\\\server\\share\\folder\\\\\\\\\"", new[] { "\\\\server\\share\\folder\\\\" })]
        [InlineData("\\\\server\\share\\file\\\\ arg", new[] { "\\\\server\\share\\file\\\\", "arg" })]
        public void CommandLineToArgumentList_UncPathsWithComplexBackslashes_ParsedCorrectly(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests ArgumentListToCommandLine with UNC paths that have complex backslash patterns.
        /// </summary>
        [Theory]
        [InlineData(new[] { "\\\\server\\share\\", "end" }, "\\\\server\\share\\ end")]
        [InlineData(new[] { "\\\\server\\share\\\\file" }, "\\\\server\\share\\\\file")]
        [InlineData(new[] { "\\\\server\\share\\\"file" }, "\"\\\\server\\share\\\\\\\"file\"")]
        [InlineData(new[] { "\\\\server\\share\\folder\\\\" }, "\\\\server\\share\\folder\\\\")]
        [InlineData(new[] { "\\\\server\\share\\file\\\\", "arg" }, "\\\\server\\share\\file\\\\ arg")]
        public void ArgumentListToCommandLine_UncPathsWithComplexBackslashes_EscapedCorrectly(string[] args, string expected)
        {
            // Act
            string result = CommandLineUtilities.ArgumentListToCommandLine(args)!;

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests ArgumentListToCommandLine with real-world UNC scenarios to verify proper escaping.
        /// </summary>
        [Theory]
        [InlineData(new[] { "\\\\fileserver.domain.com\\shared\\IT\\Software\\Installers\\MyApp v2.1\\setup.exe", "/S", "/D=C:\\Program Files\\MyApp" },
                   "\"\\\\fileserver.domain.com\\shared\\IT\\Software\\Installers\\MyApp v2.1\\setup.exe\" /S \"/D=C:\\Program Files\\MyApp\"")]
        [InlineData(new[] { "msiexec.exe", "/i", "\\\\server\\msi-packages\\Application Suite.msi", "/qn", "TARGETDIR=\\\\server\\app-installs\\Application\\" },
                   "msiexec.exe /i \"\\\\server\\msi-packages\\Application Suite.msi\" /qn TARGETDIR=\\\\server\\app-installs\\Application\\")]
        [InlineData(new[] { "powershell.exe", "-File", "\\\\scripts-server\\powershell\\Deploy-Application.ps1", "-ApplicationPath", "\\\\apps-server\\applications\\MyApp\\" },
                   "powershell.exe -File \\\\scripts-server\\powershell\\Deploy-Application.ps1 -ApplicationPath \\\\apps-server\\applications\\MyApp\\")]
        public void ArgumentListToCommandLine_RealWorldUncScenarios_EscapedCorrectly(string[] argv, string expected)
        {
            // Act
            string result = CommandLineUtilities.ArgumentListToCommandLine(argv)!;

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests the new path detection functionality for unquoted paths with spaces.
        /// </summary>
        [Theory]
        [InlineData("C:\\Program Files\\MyApp\\myapp.exe /flag", 
                   new[] { "C:\\Program Files\\MyApp\\myapp.exe", "/flag" })]
        [InlineData("C:\\ProgramData\\Caphyon\\Advanced Installer\\{E928DFCD-4C3A-4301-872C-4655F1B18AC1}\\minitab22.3.1.0setup.x64.exe /i {E928DFCD-4C3A-4301-872C-4655F1B18AC1} AI_UNINSTALLER_CTP=1",
                   new[] { "C:\\ProgramData\\Caphyon\\Advanced Installer\\{E928DFCD-4C3A-4301-872C-4655F1B18AC1}\\minitab22.3.1.0setup.x64.exe", "/i", "{E928DFCD-4C3A-4301-872C-4655F1B18AC1}", "AI_UNINSTALLER_CTP=1" })]
        [InlineData("\\\\server\\share\\My App\\setup.exe /silent", 
                   new[] { "\\\\server\\share\\My App\\setup.exe", "/silent" })]
        [InlineData("D:\\Some Folder\\Another Folder\\app.msi PROPERTY=value",
                   new[] { "D:\\Some Folder\\Another Folder\\app.msi", "PROPERTY=value" })]
        [InlineData("C:\\Program Files (x86)\\Company Name\\Product Name\\installer.exe /S /D=C:\\InstallPath",
                   new[] { "C:\\Program Files (x86)\\Company Name\\Product Name\\installer.exe", "/S", "/D=C:\\InstallPath" })]
        public void CommandLineToArgumentList_UnquotedPathDetection_ParsedCorrectly(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine, detectUnquotedPaths: true);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests that path detection can be disabled and falls back to standard parsing.
        /// </summary>
        [Theory]
        [InlineData("C:\\Program Files\\MyApp\\myapp.exe /flag", 
                   new[] { "C:\\Program", "Files\\MyApp\\myapp.exe", "/flag" })]
        [InlineData("\\\\server\\share\\My App\\setup.exe /silent", 
                   new[] { "\\\\server\\share\\My", "App\\setup.exe", "/silent" })]
        public void CommandLineToArgumentList_PathDetectionDisabled_UsesStandardParsing(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine, detectUnquotedPaths: false);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests that quoted paths are still handled correctly with path detection enabled.
        /// </summary>
        [Theory]
        [InlineData("\"C:\\Program Files\\MyApp\\myapp.exe\" /flag", 
                   new[] { "C:\\Program Files\\MyApp\\myapp.exe", "/flag" })]
        [InlineData("\"\\\\server\\share\\My App\\setup.exe\" /silent", 
                   new[] { "\\\\server\\share\\My App\\setup.exe", "/silent" })]
        [InlineData("\"C:\\Program Files\\MyApp\\myapp.exe\" \"C:\\Some Other Path\\file.txt\" /option",
                   new[] { "C:\\Program Files\\MyApp\\myapp.exe", "C:\\Some Other Path\\file.txt", "/option" })]
        public void CommandLineToArgumentList_QuotedPathsWithDetection_ParsedCorrectly(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine, detectUnquotedPaths: true);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests complex scenarios mixing quoted and unquoted paths.
        /// </summary>
        [Theory]
        [InlineData("C:\\Program Files\\App\\app.exe \"C:\\Some Path\\config.txt\" /option",
                   new[] { "C:\\Program Files\\App\\app.exe", "C:\\Some Path\\config.txt", "/option" })]
        [InlineData("\"C:\\Quoted Path\\app.exe\" D:\\Unquoted Path\\data.txt /flag",
                   new[] { "C:\\Quoted Path\\app.exe", "D:\\Unquoted Path\\data.txt", "/flag" })]
        public void CommandLineToArgumentList_MixedQuotedUnquotedPaths_ParsedCorrectly(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine, detectUnquotedPaths: true);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests edge cases for path detection.
        /// </summary>
        [Theory]
        [InlineData("C: /flag", new[] { "C:", "/flag" })] // Just a drive letter, not a path
        [InlineData("C:\\ /flag", new[] { "C:\\", "/flag" })] // Root directory
        [InlineData("C:\\file.exe", new[] { "C:\\file.exe" })] // Single file, no spaces
        [InlineData("C:\\path\\to\\file /arg", new[] { "C:\\path\\to\\file", "/arg" })] // Path without extension
        public void CommandLineToArgumentList_PathDetectionEdgeCases_ParsedCorrectly(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine, detectUnquotedPaths: true);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests that non-path arguments starting with letters are not mistaken for paths.
        /// </summary>
        [Theory]
        [InlineData("program argument1 argument2", new[] { "program", "argument1", "argument2" })]
        [InlineData("command option=value key=data", new[] { "command", "option=value", "key=data" })]
        [InlineData("app /flag parameter", new[] { "app", "/flag", "parameter" })]
        public void CommandLineToArgumentList_NonPathArguments_NotDetectedAsPaths(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine, detectUnquotedPaths: true);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests UNC paths with path detection.
        /// </summary>
        [Theory]
        [InlineData("\\\\server\\share\\folder with spaces\\app.exe /option",
                   new[] { "\\\\server\\share\\folder with spaces\\app.exe", "/option" })]
        [InlineData("\\\\file-server\\shared folder\\setup files\\installer.msi /quiet",
                   new[] { "\\\\file-server\\shared folder\\setup files\\installer.msi", "/quiet" })]
        [InlineData("\\\\domain.local\\software distribution\\My Application Suite\\setup.exe TARGETDIR=C:\\Program Files",
                   new[] { "\\\\domain.local\\software distribution\\My Application Suite\\setup.exe", "TARGETDIR=C:\\Program Files" })]
        public void CommandLineToArgumentList_UncPathDetection_ParsedCorrectly(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine, detectUnquotedPaths: true);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests various executable extensions are properly detected as path endpoints.
        /// </summary>
        [Theory]
        [InlineData("C:\\My App\\program.exe /flag", new[] { "C:\\My App\\program.exe", "/flag" })]
        [InlineData("D:\\Setup Files\\installer.msi /quiet", new[] { "D:\\Setup Files\\installer.msi", "/quiet" })]
        [InlineData("E:\\Scripts\\batch file.bat parameter", new[] { "E:\\Scripts\\batch file.bat", "parameter" })]
        [InlineData("F:\\Tools\\command.cmd /option", new[] { "F:\\Tools\\command.cmd", "/option" })]
        [InlineData("G:\\Legacy\\old program.com /legacy", new[] { "G:\\Legacy\\old program.com", "/legacy" })]
        [InlineData("H:\\Screen\\saver.scr /configure", new[] { "H:\\Screen\\saver.scr", "/configure" })]
        public void CommandLineToArgumentList_ExecutableExtensions_DetectedCorrectly(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine, detectUnquotedPaths: true);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests that the new overload with detectUnquotedPaths=false behaves identically to the original method.
        /// </summary>
        [Theory]
        [InlineData("program arg1 arg2")]
        [InlineData("\"program with spaces\" \"arg with spaces\"")]
        [InlineData("program \"arg\\\"with\\\"quotes\"")]
        [InlineData("C:\\Program Files\\App\\app.exe /flag")] // This should split with detection disabled
        public void CommandLineToArgumentList_DetectionDisabledEquivalent_MatchesOriginalBehavior(string commandLine)
        {
            // Act
            IReadOnlyList<string> originalResult = CommandLineUtilities.CommandLineToArgumentList(commandLine);
            IReadOnlyList<string> newResult = CommandLineUtilities.CommandLineToArgumentList(commandLine, detectUnquotedPaths: false);

            // Assert
            Assert.Equal(originalResult, newResult);
        }

        /// <summary>
        /// Tests real-world installer command lines that commonly have unquoted paths.
        /// </summary>
        [Theory]
        [InlineData("C:\\ProgramData\\Package Cache\\{guid}\\Microsoft Visual C++ 2019 Redistributable\\vc_redist.x64.exe /install /quiet /norestart",
                   new[] { "C:\\ProgramData\\Package Cache\\{guid}\\Microsoft Visual C++ 2019 Redistributable\\vc_redist.x64.exe", "/install", "/quiet", "/norestart" })]
        [InlineData("D:\\Software Distribution\\Adobe Products\\Adobe Acrobat DC\\setup.exe /sAll /rs /msi EULA_ACCEPT=YES",
                   new[] { "D:\\Software Distribution\\Adobe Products\\Adobe Acrobat DC\\setup.exe", "/sAll", "/rs", "/msi", "EULA_ACCEPT=YES" })]
        [InlineData("\\\\deployment-server\\software\\Microsoft Office 365\\Office 2019\\setup.exe /configure configuration.xml",
                   new[] { "\\\\deployment-server\\software\\Microsoft Office 365\\Office 2019\\setup.exe", "/configure", "configuration.xml" })]
        public void CommandLineToArgumentList_RealWorldInstallerScenarios_ParsedCorrectly(string commandLine, IReadOnlyList<string> expected)
        {
            // Act
            IReadOnlyList<string> result = CommandLineUtilities.CommandLineToArgumentList(commandLine, detectUnquotedPaths: true);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
