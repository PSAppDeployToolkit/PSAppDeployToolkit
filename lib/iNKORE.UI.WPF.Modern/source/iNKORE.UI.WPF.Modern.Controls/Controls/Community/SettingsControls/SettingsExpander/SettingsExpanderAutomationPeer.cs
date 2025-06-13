using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using System.Windows.Automation;

namespace iNKORE.UI.WPF.Modern.Controls
{

    /// <summary>
    /// AutomationPeer for SettingsExpander
    /// </summary>
    public class SettingsExpanderAutomationPeer : FrameworkElementAutomationPeer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsExpander"/> class.
        /// </summary>
        /// <param name="owner">SettingsExpander</param>
        public SettingsExpanderAutomationPeer(SettingsExpander owner)
            : base(owner)
        {
        }

        /// <summary>
        /// Gets the control type for the element that is associated with the UI Automation peer.
        /// </summary>
        /// <returns>The control type.</returns>
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }

        /// <summary>
        /// Called by GetClassName that gets a human readable name that, in addition to AutomationControlType,
        /// differentiates the control represented by this AutomationPeer.
        /// </summary>
        /// <returns>The string that contains the name.</returns>
        protected override string GetClassNameCore()
        {
            return Owner.GetType().Name;
        }

        protected override string GetNameCore()
        {
            string name = base.GetNameCore();

            if (Owner is SettingsExpander owner)
            {
                if (!string.IsNullOrEmpty(AutomationProperties.GetName(owner)))
                {
                    name = AutomationProperties.GetName(owner);
                }
                else
                {
                    if (owner.Header is string headerString && !string.IsNullOrEmpty(headerString))
                    {
                        name = headerString;
                    }
                }
            }
            return name;
        }

        /// <summary>
        /// Raises the property changed event for this AutomationPeer for the provided identifier.
        /// Narrator does not announce this due to: https://github.com/microsoft/microsoft-ui-xaml/issues/3469
        /// </summary>
        /// <param name="newValue">New Expanded state</param>
        public void RaiseExpandedChangedEvent(bool newValue)
        {
            ExpandCollapseState newState = (newValue == true) ?
              ExpandCollapseState.Expanded :
              ExpandCollapseState.Collapsed;

            ExpandCollapseState oldState = (newState == ExpandCollapseState.Expanded) ?
              ExpandCollapseState.Collapsed :
              ExpandCollapseState.Expanded;

#if !HAS_UNO
            RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, oldState, newState);
#endif
        }
    }
}
