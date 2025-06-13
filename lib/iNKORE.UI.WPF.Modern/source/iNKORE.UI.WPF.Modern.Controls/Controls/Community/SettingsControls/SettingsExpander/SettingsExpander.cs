using System.Collections.Generic;
using System.Windows.Automation.Peers;
using System.Windows.Automation;
using System.Windows;

namespace iNKORE.UI.WPF.Modern.Controls
{
    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    // See the LICENSE file in the project root for more information.

    //// Note: ItemsRepeater will request all the available horizontal space: https://github.com/microsoft/microsoft-ui-xaml/issues/3842
    [TemplatePart(Name = PART_ItemsRepeater, Type = typeof(ItemsRepeater))]
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(FrameworkElement))]
    public partial class SettingsExpander : System.Windows.Controls.Control
    {
        private const string PART_ItemsRepeater = "PART_ItemsRepeater";

        private ItemsRepeater? _itemsRepeater;

        /// <summary>
        /// The SettingsExpander is a collapsable control to host multiple SettingsCards.
        /// </summary>
        public SettingsExpander()
        {
            this.DefaultStyleKey = typeof(SettingsExpander);
            Items = new List<object>();
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            SetAccessibleName();

            if (_itemsRepeater != null)
            {
                _itemsRepeater.ElementPrepared -= this.ItemsRepeater_ElementPrepared;
            }

            _itemsRepeater = GetTemplateChild(PART_ItemsRepeater) as ItemsRepeater;

            if (_itemsRepeater != null)
            {
                _itemsRepeater.ElementPrepared += this.ItemsRepeater_ElementPrepared;

                // Update it's source based on our current items properties.
                OnItemsConnectedPropertyChanged(this, new DependencyPropertyChangedEventArgs()); // Can't get it to accept type here? (DependencyPropertyChangedEventArgs)EventArgs.Empty
            }
        }

        private void SetAccessibleName()
        {
            if (string.IsNullOrEmpty(AutomationProperties.GetName(this)))
            {
                if (Header is string headerString && !string.IsNullOrEmpty(headerString))
                {
                    AutomationProperties.SetName(this, headerString);
                }
            }
        }

        /// <summary>
        /// Creates AutomationPeer
        /// </summary>
        /// <returns>An automation peer for <see cref="SettingsExpander"/>.</returns>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new SettingsExpanderAutomationPeer(this);
        }

        private void OnIsExpandedChanged(bool oldValue, bool newValue)
        {
            var peer = FrameworkElementAutomationPeer.FromElement(this) as SettingsExpanderAutomationPeer;
            peer?.RaiseExpandedChangedEvent(newValue);
        }

        //protected override bool IsItemItsOwnContainerOverride(object item)
        //{
        //    return item is SettingsCard;
        //}

        //protected override DependencyObject GetContainerForItemOverride()
        //{
        //    var item = new SettingsCard();
        //    return item;
        //}
    }
}
