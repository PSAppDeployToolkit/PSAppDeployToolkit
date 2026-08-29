using System.Linq;
using PSADT.UserInterface.DialogResults;
using PSADT.UserInterface.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Tests.DialogResults
{
    /// <summary>
    /// Tests the four outcomes of the close-applications dialog.
    /// </summary>
    /// <remarks>
    /// A <c language="csharp">TypedConstant</c>, whose general behaviour - numeric conversion, comparison against strings
    /// and integers, equality - is tested where that type lives. What belongs here is the set of
    /// constants itself and the numbers behind them, since those decide what a deployment does next after
    /// asking the user to close their applications.
    /// </remarks>
    public sealed class CloseAppsDialogResultTests
    {
        /// <summary>
        /// Verifies the four outcomes and their values.
        /// </summary>
        /// <remarks>
        /// Discovered by reflection rather than listed, so a fifth outcome added without being considered
        /// here fails rather than passing unnoticed. The names come from
        /// <see cref="System.Runtime.CompilerServices.CallerMemberNameAttribute"/> on the constructor, so
        /// a renamed field silently renames the constant - and the name is what a PowerShell comparison
        /// matches on.
        /// </remarks>
        [Fact]
        public void Constants_AreTheFourOutcomes()
        {
            // Act
            (string Name, long Value)[] declared =
            [
                .. StaticConstants.Of<CloseAppsDialogResult>().Select(static constant => (constant.Name, constant.Value.ToInt64())),
            ];

            // Assert
            Assert.Equal([("Timeout", 0L), ("Close", 1L), ("Continue", 2L), ("Defer", 3L)], declared);
        }

        /// <summary>
        /// Verifies that each constant renders as its own name.
        /// </summary>
        /// <remarks>
        /// The name is taken from the field it is assigned to by caller-member-name capture, which is
        /// concise but means nothing states the name at the point it is used. This is what states it.
        /// </remarks>
        [Fact]
        public void Constants_RenderAsTheirNames()
        {
            Assert.Equal("Timeout", CloseAppsDialogResult.Timeout.ToString());
            Assert.Equal("Close", CloseAppsDialogResult.Close.ToString());
            Assert.Equal("Continue", CloseAppsDialogResult.Continue.ToString());
            Assert.Equal("Defer", CloseAppsDialogResult.Defer.ToString());
        }

        /// <summary>
        /// Verifies that the outcomes are distinct from one another.
        /// </summary>
        /// <remarks>
        /// Two of these comparing equal would be the difference between deferring a deployment and letting
        /// it proceed.
        /// </remarks>
        [Fact]
        public void Constants_AreDistinctFromOneAnother()
        {
            // Act
            CloseAppsDialogResult[] all = [.. StaticConstants.Of<CloseAppsDialogResult>().Select(static constant => constant.Value)];

            // Assert
            Assert.Equal(all.Length, all.Distinct().Count());
        }

        /// <summary>
        /// Verifies that an outcome compares equal to its name from PowerShell.
        /// </summary>
        /// <remarks>
        /// A deployment script tests the returned result with <c language="powershell">-eq 'Defer'</c>, which calls
        /// <c language="csharp">Equals(object)</c>. One case is enough to prove the base type's behaviour reaches this
        /// derivative; the general rules are tested with the base type itself.
        /// </remarks>
        [Fact]
        public void Constants_CompareEqualToTheirNameAsAString()
        {
            Assert.True(CloseAppsDialogResult.Defer.Equals("defer"));
            Assert.False(CloseAppsDialogResult.Defer.Equals("continue"));
        }
    }
}
