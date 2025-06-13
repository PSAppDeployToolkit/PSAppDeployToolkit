// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Windows.Automation.Peers;
using iNKORE.UI.WPF.Modern.Common;
using iNKORE.UI.WPF.Modern.Controls;
using static iNKORE.UI.WPF.Modern.Common.ResourceAccessor;

namespace iNKORE.UI.WPF.Modern.Automation.Peers
{
    public class ProgressRingAutomationPeer : FrameworkElementAutomationPeer
    {
        private static readonly ControlStrings ResourceAccessor = new ControlStrings(typeof(ProgressRing), ModernControlCategory.Windows);

        public ProgressRingAutomationPeer(ProgressRing owner) : base(owner)
        {
        }

        protected override string GetClassNameCore()
        {
            return nameof(ProgressRing);
        }

        protected override string GetNameCore()
        {
            string name = base.GetNameCore();

            if (Owner is ProgressRing progressRing)
            {
                if (progressRing.IsActive)
                {
                    return ResourceAccessor.GetLocalizedStringResource(SR_ProgressRingIndeterminateStatus) + name;
                }
            }
            return name;
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ProgressBar;
        }

        protected override string GetLocalizedControlTypeCore()
        {
            return ResourceAccessor.GetLocalizedStringResource(SR_ProgressRingName);
        }
    }
}
