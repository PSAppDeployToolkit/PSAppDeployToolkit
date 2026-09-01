using System;
using PSAppDeployToolkit.Foundation;
using Xunit;

namespace PSAppDeployToolkit.Tests.Foundation
{
    /// <summary>
    /// Tests the record describing how much deferral a deployment has left.
    /// </summary>
    /// <remarks>
    /// Short, and here as the control case for the equality work. Every member is a nullable value type, so this
    /// is the one record in the assembly that honoured what a record advertises without anything being done to
    /// it - which makes it worth a test, because it is the baseline the others are being brought up to and a
    /// change that broke it would otherwise go unnoticed.
    /// <para>
    /// Its equality is not decorative either. It is read out of the registry on one run and compared against
    /// what a later run reads, so two histories describing the same state have to match.
    /// </para>
    /// </remarks>
    public sealed class DeferHistoryTests
    {
        /// <summary>
        /// Verifies that two histories describing the same state are equal and hash alike.
        /// </summary>
        [Fact]
        public void Equals_IsByTheValuesGiven()
        {
            // Arrange
            DeferHistory first = new(DeferTimesRemaining: 3, Deadline, LastRun);
            DeferHistory second = new(DeferTimesRemaining: 3, Deadline, LastRun);

            // Assert
            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
        }

        /// <summary>
        /// Verifies that every member is part of the comparison.
        /// </summary>
        [Fact]
        public void Equals_TakesEveryMemberIntoAccount()
        {
            // Arrange
            DeferHistory baseline = new(DeferTimesRemaining: 3, Deadline, LastRun);

            // Assert
            Assert.NotEqual(baseline, new DeferHistory(DeferTimesRemaining: 2, Deadline, LastRun));
            Assert.NotEqual(baseline, new DeferHistory(DeferTimesRemaining: 3, Deadline.AddDays(1), LastRun));
            Assert.NotEqual(baseline, new DeferHistory(DeferTimesRemaining: 3, Deadline, LastRun.AddMinutes(1)));
            Assert.NotEqual(baseline, new DeferHistory(DeferTimesRemaining: 3, Deadline, LastRun, TimeSpan.FromHours(1)));
        }

        /// <summary>
        /// Verifies that two histories with nothing set are equal, and unequal to one with anything set.
        /// </summary>
        /// <remarks>
        /// The all-null case is how a deployment with no deferral state reads back, so it has to compare as a
        /// value rather than being treated as absent.
        /// </remarks>
        [Fact]
        public void Equals_TreatsAnEmptyHistoryAsAValue()
        {
            // Arrange
            DeferHistory empty = new(DeferTimesRemaining: null, DeferDeadline: null, DeferRunIntervalLastTime: null);

            // Assert
            Assert.Equal(empty, new DeferHistory(DeferTimesRemaining: null, DeferDeadline: null, DeferRunIntervalLastTime: null));
            Assert.Equal(empty.GetHashCode(), new DeferHistory(DeferTimesRemaining: null, DeferDeadline: null, DeferRunIntervalLastTime: null).GetHashCode());
            Assert.NotEqual(empty, new DeferHistory(DeferTimesRemaining: 0, DeferDeadline: null, DeferRunIntervalLastTime: null));
        }

        /// <summary>
        /// Verifies that a history reports what it was built from.
        /// </summary>
        /// <remarks>
        /// The type redeclares every positional parameter as a property with an initialiser, which is
        /// redundant but legal; this confirms the redeclaration did not shadow the parameter with something else.
        /// </remarks>
        [Fact]
        public void DeferHistory_ReportsWhatItWasBuiltFrom()
        {
            // Arrange
            DeferHistory history = new(DeferTimesRemaining: 3, Deadline, LastRun, TimeSpan.FromMinutes(90));

            // Assert
            Assert.Equal(3u, history.DeferTimesRemaining);
            Assert.Equal(Deadline, history.DeferDeadline);
            Assert.Equal(LastRun, history.DeferRunIntervalLastTime);
            Assert.Equal(TimeSpan.FromMinutes(90), history.DeferRunInterval);
        }

        /// <summary>
        /// Verifies that the run interval defaults to absent when it is not supplied.
        /// </summary>
        /// <remarks>
        /// It was added after the other three and carries a default so that existing positional construction
        /// keeps working, which only holds while the default really is null.
        /// </remarks>
        [Fact]
        public void DeferRunInterval_IsAbsentWhenNotSupplied()
        {
            // Act
            DeferHistory history = new(DeferTimesRemaining: 3, Deadline, LastRun);

            // Assert
            Assert.Null(history.DeferRunInterval);
        }

        /// <summary>
        /// A deferral deadline, fixed so a comparison is the same on every run.
        /// </summary>
        private static readonly DateTime Deadline = new(2026, 4, 1, 9, 0, 0, DateTimeKind.Local);

        /// <summary>
        /// The time a deferral interval was last recorded.
        /// </summary>
        private static readonly DateTime LastRun = new(2026, 3, 4, 5, 6, 7, DateTimeKind.Local);
    }
}
