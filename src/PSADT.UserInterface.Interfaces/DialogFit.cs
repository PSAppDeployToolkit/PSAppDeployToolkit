using System.Collections.ObjectModel;

namespace PSADT.UserInterface.Interfaces
{
    /// <summary>
    /// The outcome of fitting a dialog within the space available to it.
    /// </summary>
    /// <param name="Scale">The scale to apply to the dialog.</param>
    /// <param name="RegionMaxHeights">The maximum height of each scrollable region at a scale of 1.0, or <see cref="double.PositiveInfinity"/> where unconstrained.</param>
    internal sealed record class DialogFit(double Scale, ReadOnlyCollection<double> RegionMaxHeights)
    {
        /// <summary>
        /// The scale to apply to the dialog.
        /// </summary>
        internal readonly double Scale = Scale;

        /// <summary>
        /// The maximum height of each scrollable region at a scale of 1.0, or <see cref="double.PositiveInfinity"/> where unconstrained.
        /// </summary>
        internal readonly ReadOnlyCollection<double> RegionMaxHeights = RegionMaxHeights;
    }
}
