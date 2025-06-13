// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Windows.Automation.Peers;
using iNKORE.UI.WPF.Modern.Controls;

namespace iNKORE.UI.WPF.Modern.Automation.Peers
{
    public class PersonPictureAutomationPeer : FrameworkElementAutomationPeer
    {
        public PersonPictureAutomationPeer(PersonPicture owner) : base(owner)
        {
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Text;
        }

        protected override string GetClassNameCore()
        {
            return nameof(PersonPicture);
        }
    }
}
