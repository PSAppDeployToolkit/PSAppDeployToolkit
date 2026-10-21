using System;
using System.Collections.Generic;
using PSADT.UserInterface.DialogOptions;

namespace PSADT.UserInterface.Interfaces
{
    /// <summary>
    /// Works out how a dialog fits within the space available to it.
    /// </summary>
    /// <remarks>
    /// A dialog that is too tall first lets its scrollable regions give up height in the order supplied, down to their
    /// minimums, so its text stays at the size Windows' display scaling asks for. Only if it still does not fit is it
    /// scaled down. Width can only be recovered by scaling. All natural sizes are at a scale of 1.0, while the available
    /// size is in the units the window itself is measured in.
    /// </remarks>
    internal static class DialogFitCalculator
    {
        /// <summary>
        /// Calculates the scale and scrollable region heights that fit a dialog within the available space.
        /// </summary>
        /// <param name="naturalWidth">The dialog's width at a scale of 1.0.</param>
        /// <param name="naturalHeight">The dialog's height at a scale of 1.0, with every scrollable region at its natural height.</param>
        /// <param name="availableWidth">The width available to the dialog.</param>
        /// <param name="availableHeight">The height available to the dialog.</param>
        /// <param name="scale">The requested scale.</param>
        /// <param name="scrollableRegions">The regions that can scroll, in the order they should give up height.</param>
        /// <returns>The scale to apply and the maximum height for each scrollable region, at a scale of 1.0.</returns>
        internal static DialogFit Calculate(double naturalWidth, double naturalHeight, double availableWidth, double availableHeight, double scale, IReadOnlyList<DialogFitRegion> scrollableRegions)
        {
            ArgumentNullException.ThrowIfNull(scrollableRegions);
            double[] regionMaxHeights = new double[scrollableRegions.Count];
            for (int i = 0; i < regionMaxHeights.Length; i++)
            {
                regionMaxHeights[i] = double.PositiveInfinity;
            }
            if (naturalWidth <= 0 || naturalHeight <= 0 || availableWidth <= 0 || availableHeight <= 0)
            {
                return new(scale, new(regionMaxHeights));
            }

            // Take any excess height from the scrollable regions at the requested scale, or narrower if the width demands it.
            double widthScale = availableWidth / naturalWidth;
            double fittedScale = Math.Min(scale, widthScale);
            double excessHeight = naturalHeight - (availableHeight / fittedScale);
            if (excessHeight <= 0)
            {
                return new(fittedScale, new(regionMaxHeights));
            }
            double reclaimedHeight = 0;
            for (int i = 0; i < regionMaxHeights.Length; i++)
            {
                DialogFitRegion region = scrollableRegions[i];
                double reclaimed = Math.Min(Math.Max(0, region.NaturalHeight - region.MinimumHeight), excessHeight);
                if (reclaimed > 0)
                {
                    regionMaxHeights[i] = region.NaturalHeight - reclaimed;
                    reclaimedHeight += reclaimed;
                    excessHeight -= reclaimed;
                }
                if (excessHeight <= 0)
                {
                    return new(fittedScale, new(regionMaxHeights));
                }
            }

            // Then scale the compacted dialog down to fit, but never below the smallest accepted scale.
            fittedScale = Math.Max(BaseDialogOptions.MinimumDialogScale, Math.Min(fittedScale, availableHeight / (naturalHeight - reclaimedHeight)));
            return new(fittedScale, new(regionMaxHeights));
        }
    }
}
