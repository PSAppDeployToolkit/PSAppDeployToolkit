using System;
using System.Globalization;
using System.Windows;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests
{
    /// <summary>
    /// Tests the converter that shows an element only when a count is above zero.
    /// </summary>
    /// <remarks>
    /// Bound in the Fluent dialog's XAML, so a converter that threw would surface as a silent binding
    /// failure and an element stuck in whichever state it happened to start in, rather than as an error
    /// anyone would see. The type test it does rather than a cast is what keeps that from happening, so
    /// the awkward inputs are covered here alongside the ordinary ones.
    /// </remarks>
    public sealed class IntToVisibilityConverterTests
    {
        /// <summary>
        /// Verifies that a count above zero shows the element.
        /// </summary>
        /// <param name="count">The count to convert.</param>
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(int.MaxValue)]
        public void Convert_ShowsWhenTheCountIsAboveZero(int count)
        {
            // Act
            object result = new IntToVisibilityConverter().Convert(count, typeof(Visibility), parameter: null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Equal(Visibility.Visible, result);
        }

        /// <summary>
        /// Verifies that a count of zero or below collapses the element.
        /// </summary>
        /// <param name="count">The count to convert.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void Convert_CollapsesWhenTheCountIsNotAboveZero(int count)
        {
            // Act
            object result = new IntToVisibilityConverter().Convert(count, typeof(Visibility), parameter: null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Equal(Visibility.Collapsed, result);
        }

        /// <summary>
        /// Verifies that a value which is not an <see cref="int"/> collapses rather than throwing.
        /// </summary>
        /// <remarks>
        /// The conversion is <c>value as int?</c>, which unboxes only an exact <see cref="int"/>. A
        /// binding whose source is a <see cref="long"/> or a <see cref="uint"/> - which is what
        /// <c>DeferralsRemaining</c> is elsewhere in this project - therefore reads as collapsed no
        /// matter how large it is. That is worth pinning: it is the difference between a hidden panel and
        /// a crash, and it means a future binding has to hand this an <see cref="int"/> to work at all.
        /// </remarks>
        /// <param name="value">The value to convert.</param>
        [Theory]
        [InlineData(null)]
        [InlineData(1L)]
        [InlineData((uint)1)]
        [InlineData((short)1)]
        [InlineData(1.0)]
        [InlineData("1")]
        [InlineData(true)]
        public void Convert_CollapsesAnythingThatIsNotAnInt(object? value)
        {
            // Act
            object result = new IntToVisibilityConverter().Convert(value, typeof(Visibility), parameter: null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Equal(Visibility.Collapsed, result);
        }

        /// <summary>
        /// Verifies that the converter parameter is ignored.
        /// </summary>
        /// <remarks>
        /// The class summary used to promise that a parameter of <c>'True'</c> inverted the result and
        /// that <c>'ListView'</c> selected a special case. Neither was ever implemented. The summary now
        /// says so, and this holds the code to the documented behaviour so the two cannot drift apart
        /// again without a test noticing.
        /// </remarks>
        /// <param name="parameter">The converter parameter to pass.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("True")]
        [InlineData("ListView")]
        public void Convert_IgnoresTheConverterParameter(object? parameter)
        {
            // Arrange
            IntToVisibilityConverter converter = new();

            // Act
            object visible = converter.Convert(1, typeof(Visibility), parameter, CultureInfo.InvariantCulture);
            object collapsed = converter.Convert(0, typeof(Visibility), parameter, CultureInfo.InvariantCulture);

            // Assert
            Assert.Equal(Visibility.Visible, visible);
            Assert.Equal(Visibility.Collapsed, collapsed);
        }

        /// <summary>
        /// Verifies that converting back is refused.
        /// </summary>
        [Fact]
        public void ConvertBack_IsNotSupported()
        {
            // Act & Assert
            _ = Assert.Throws<NotSupportedException>(static () => new IntToVisibilityConverter().ConvertBack(Visibility.Visible, typeof(int), parameter: null, CultureInfo.InvariantCulture));
        }
    }
}
