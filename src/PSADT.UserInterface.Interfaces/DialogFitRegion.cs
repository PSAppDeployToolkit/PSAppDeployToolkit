namespace PSADT.UserInterface.Interfaces
{
    /// <summary>
    /// A region of a dialog that can scroll to give up height.
    /// </summary>
    /// <param name="NaturalHeight">The region's height at a scale of 1.0 when unconstrained.</param>
    /// <param name="MinimumHeight">The smallest height, at a scale of 1.0, the region may be reduced to.</param>
    internal sealed record class DialogFitRegion(double NaturalHeight, double MinimumHeight)
    {
        /// <summary>
        /// The region's height at a scale of 1.0 when unconstrained.
        /// </summary>
        internal readonly double NaturalHeight = NaturalHeight;

        /// <summary>
        /// The smallest height, at a scale of 1.0, the region may be reduced to.
        /// </summary>
        internal readonly double MinimumHeight = MinimumHeight;
    }
}
