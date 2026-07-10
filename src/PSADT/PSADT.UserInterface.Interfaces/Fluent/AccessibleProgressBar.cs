using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace PSADT.UserInterface.Interfaces.Fluent
{
    /// <summary>
    /// A <see cref="Fluence.Wpf.Controls.ProgressBar"/> whose automation peer omits the spoken control-type
    /// word ("progress bar"), so a screen reader reading the focused bar announces only the percentage (from
    /// the RangeValue pattern), e.g. "45 percent". Reuses Fluence's implicit progress-bar style via a
    /// <see cref="System.Windows.FrameworkElement.DefaultStyleKey"/> override, because a subclass does not
    /// inherit the base type's implicit style automatically.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0182: Avoid unused internal types.", Justification = "Instantiated from XAML (BAML), which the analyzer cannot see.")]
    internal sealed class AccessibleProgressBar : Fluence.Wpf.Controls.ProgressBar
    {
        /// <summary>
        /// Overrides the default style key so this subclass reuses <see cref="Fluence.Wpf.Controls.ProgressBar"/>'s
        /// implicit style and template rather than looking for a (non-existent) style keyed on its own type.
        /// </summary>
        static AccessibleProgressBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(AccessibleProgressBar),
                new FrameworkPropertyMetadata(typeof(Fluence.Wpf.Controls.ProgressBar)));
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new PercentOnlyAutomationPeer(this);
        }

        /// <summary>
        /// A <see cref="ProgressBarAutomationPeer"/> that returns an empty localized control type, so a
        /// screen reader does not speak "progress bar" while still reading the percentage via RangeValue.
        /// </summary>
        /// <param name="owner">The owning progress bar.</param>
        private sealed class PercentOnlyAutomationPeer(ProgressBar owner) : ProgressBarAutomationPeer(owner)
        {
            /// <inheritdoc />
            protected override string GetLocalizedControlTypeCore()
            {
                return string.Empty;
            }
        }
    }
}
