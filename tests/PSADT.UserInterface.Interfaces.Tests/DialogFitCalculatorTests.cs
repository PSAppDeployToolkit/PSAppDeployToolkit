using PSADT.UserInterface.DialogOptions;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests
{
    /// <summary>
    /// Tests how a dialog is fitted within the space available to it.
    /// </summary>
    public sealed class DialogFitCalculatorTests
    {
        /// <summary>
        /// Verifies that a dialog which already fits keeps the requested scale and leaves its regions unconstrained.
        /// </summary>
        /// <param name="scale">The requested scale.</param>
        [Theory]
        [InlineData(0.5)]
        [InlineData(0.9)]
        [InlineData(1.0)]
        public void Calculate_KeepsTheRequestedScaleWhenTheDialogFits(double scale)
        {
            // Act
            DialogFit fit = DialogFitCalculator.Calculate(580, 600, 1000, 700, scale, [new(200, 130)]);

            // Assert
            Assert.Equal(scale, fit.Scale, 6);
            Assert.Equal(double.PositiveInfinity, fit.RegionMaxHeights[0]);
        }

        /// <summary>
        /// Verifies that a dialog slightly too tall scrolls rather than scaling.
        /// </summary>
        [Fact]
        public void Calculate_ScrollsBeforeScaling()
        {
            // Act
            DialogFit fit = DialogFitCalculator.Calculate(580, 750, 1000, 700, 1.0, [new(200, 130)]);

            // Assert
            Assert.Equal(1.0, fit.Scale, 6);
            Assert.Equal(150, fit.RegionMaxHeights[0], 6);
        }

        /// <summary>
        /// Verifies that the regions give up height in order, each down to its minimum, before any scaling.
        /// </summary>
        [Fact]
        public void Calculate_ShrinksTheRegionsInOrder()
        {
            // Act
            DialogFit fit = DialogFitCalculator.Calculate(580, 900, 1000, 700, 1.0, [new(200, 130), new(300, 60), new(100, 50)]);

            // Assert
            Assert.Equal(1.0, fit.Scale, 6);
            Assert.Equal(130, fit.RegionMaxHeights[0], 6);
            Assert.Equal(300 - (200 - 70), fit.RegionMaxHeights[1], 6);
            Assert.Equal(double.PositiveInfinity, fit.RegionMaxHeights[2]);
        }

        /// <summary>
        /// Verifies that a requested scale below 1.0 is kept while the regions absorb the excess at that scale.
        /// </summary>
        [Fact]
        public void Calculate_ScrollsAtTheRequestedScale()
        {
            // Act
            DialogFit fit = DialogFitCalculator.Calculate(580, 900, 1000, 700, 0.8, [new(200, 130)]);

            // Assert
            Assert.Equal(0.8, fit.Scale, 6);
            Assert.Equal(200 - (900 - (700 / 0.8)), fit.RegionMaxHeights[0], 6);
        }

        /// <summary>
        /// Verifies that a dialog still too tall with every region at its minimum is then scaled to fit.
        /// </summary>
        [Fact]
        public void Calculate_ScalesOnceTheRegionsAreAtTheirMinimums()
        {
            // Act
            DialogFit fit = DialogFitCalculator.Calculate(580, 1200, 1000, 700, 1.0, [new(200, 130)]);

            // Assert
            Assert.Equal(700.0 / 1130.0, fit.Scale, 6);
            Assert.Equal(130, fit.RegionMaxHeights[0], 6);
        }

        /// <summary>
        /// Verifies that scaling never goes below the smallest accepted scale.
        /// </summary>
        [Fact]
        public void Calculate_NeverScalesBelowTheMinimumDialogScale()
        {
            // Act
            DialogFit fit = DialogFitCalculator.Calculate(580, 3000, 1000, 700, 1.0, []);

            // Assert
            Assert.Equal(BaseDialogOptions.MinimumDialogScale, fit.Scale, 6);
        }

        /// <summary>
        /// Verifies that a dialog too wide for the space is scaled to fit its width.
        /// </summary>
        [Fact]
        public void Calculate_ScalesToFitTheWidth()
        {
            // Act
            DialogFit fit = DialogFitCalculator.Calculate(580, 600, 400, 700, 1.0, [new(200, 130)]);

            // Assert
            Assert.Equal(400.0 / 580.0, fit.Scale, 6);
            Assert.Equal(double.PositiveInfinity, fit.RegionMaxHeights[0]);
        }

        /// <summary>
        /// Verifies that a region already at or below its minimum gives up nothing.
        /// </summary>
        [Fact]
        public void Calculate_SkipsARegionWithNothingToGiveUp()
        {
            // Act
            DialogFit fit = DialogFitCalculator.Calculate(580, 800, 1000, 700, 1.0, [new(50, 60), new(200, 100)]);

            // Assert
            Assert.Equal(1.0, fit.Scale, 6);
            Assert.Equal(double.PositiveInfinity, fit.RegionMaxHeights[0]);
            Assert.Equal(100, fit.RegionMaxHeights[1], 6);
        }

        /// <summary>
        /// Verifies that sizes which cannot be measured leave the requested scale untouched.
        /// </summary>
        /// <param name="naturalHeight">The natural height to supply.</param>
        /// <param name="availableHeight">The available height to supply.</param>
        [Theory]
        [InlineData(0, 700)]
        [InlineData(600, 0)]
        [InlineData(-1, 700)]
        public void Calculate_KeepsTheRequestedScaleForUnmeasurableSizes(double naturalHeight, double availableHeight)
        {
            // Act
            DialogFit fit = DialogFitCalculator.Calculate(580, naturalHeight, 1000, availableHeight, 1.0, [new(200, 130)]);

            // Assert
            Assert.Equal(1.0, fit.Scale, 6);
            Assert.Equal(double.PositiveInfinity, fit.RegionMaxHeights[0]);
        }
    }
}
