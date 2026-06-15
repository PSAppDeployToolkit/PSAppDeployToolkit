/*
 * Copyright 2026 Dan Cunningham
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 */

// Slot-wiring infrastructure for demo sample pages.
//
// WPF raises error MC3093 ("Cannot set Name attribute value on element ... inside a template")
// when you try to give an x:Name to a control declared directly inside a DemoSampleControl
// property element, because the property element is treated like a template scope. To work
// around this, each gallery page declares hidden ContentControl "slots" at the page root
// (outside DemoSampleControl) and calls DemoSamplePageWiring.Apply to move the live controls
// into the right DemoSampleControl zones at startup.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    /// <summary>
    /// Transfers live controls and source text from page-level hidden slots into their matching
    /// <c>DemoSampleControl</c> cards on a gallery page.
    /// </summary>
    /// <remarks>
    /// <para>
    /// WPF error MC3093 prevents giving an <c>x:Name</c> to a control declared directly inside a
    /// <c>DemoSampleControl</c> property element, because WPF treats that property element as a
    /// template scope. Gallery pages therefore declare named <see cref="ContentControl"/> slots at
    /// the page root using the convention <c>DemoSampleSlotNNDemoContentHost</c>,
    /// <c>DemoSampleSlotNNOutputContentHost</c>, and <c>DemoSampleSlotNNRightRailContentHost</c>
    /// (where NN is the 1-based sample index). Calling <see cref="Apply"/> finds every such slot,
    /// moves its child into the matching sample card zone, assigns the XAML and C# source text, and
    /// clears the now-empty slot. After <see cref="Apply"/> returns the hidden slots are empty and
    /// all live controls live inside their <c>DemoSampleControl</c> cards.
    /// </para>
    /// <para>
    /// If the number of <see cref="DemoSampleSource"/> arguments does not equal the number of
    /// <c>DemoSampleControl</c> instances on the page, <see cref="Apply"/> throws so the mismatch
    /// is caught at startup rather than silently producing a card with no source code.
    /// </para>
    /// </remarks>
    internal static class DemoSamplePageWiring
    {
        /// <summary>
        /// Wires every <c>DemoSampleControl</c> on a page to its live content and source code.
        /// </summary>
        /// <remarks>
        /// WPF will not let you give an <c>x:Name</c> to a control declared directly inside a
        /// <c>DemoSampleControl</c> property element (error MC3093). So pages instead declare
        /// hidden <see cref="ContentControl"/> "slots" named
        /// <c>DemoSampleSlotNNDemoContentHost</c> / <c>...OutputContentHost</c> /
        /// <c>...RightRailContentHost</c> (NN is the 1-based sample index). This method finds each
        /// slot, transfers its child into the matching sample card, attaches the XAML/C# source, and
        /// clears the slot. The Nth <see cref="DemoSampleSource"/> argument supplies the Nth card's
        /// source text, so the source count must equal the sample count.
        /// </remarks>
        /// <param name="root">The page's content root to search.</param>
        /// <param name="sources">Source-code entries, one per sample, in document order.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="root"/> or <paramref name="sources"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the number of <see cref="DemoSampleSource"/> entries does not match the number of <c>DemoSampleControl</c> instances on the page.</exception>
        internal static void Apply(DependencyObject root, params DemoSampleSource[] sources)
        {
            if (root is null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (sources is null)
            {
                throw new ArgumentNullException(nameof(sources));
            }

            List<DemoSampleControl> samples = [];
            CollectDemoSampleControls(root, samples);

            Dictionary<int, DemoSampleSource> sourceBySlot = CreateSourceMap(sources, samples.Count);
            if (sourceBySlot.Count != samples.Count)
            {
                throw new InvalidOperationException(
                    "Every DemoSampleControl in the page must have exactly one DemoSampleSource registration.");
            }

            ApplyContentSlots(root, samples);

            for (int i = 0; i < samples.Count; i++)
            {
                int slot = i + 1;
                DemoSampleSource source = sourceBySlot[slot];
                DemoSampleControl sample = samples[i];
                sample.XamlSource = source.XamlSource;
                sample.CSharpSource = source.CSharpSource;
            }
        }

        // Build a slot -> source lookup so each card can find its source in O(1) regardless of
        // the order the caller listed the DemoSampleSource arguments.
        private static Dictionary<int, DemoSampleSource> CreateSourceMap(
            IEnumerable<DemoSampleSource> sources,
            int sampleCount)
        {
            Dictionary<int, DemoSampleSource> sourceBySlot = [];
            foreach (DemoSampleSource source in sources)
            {
                if (source.Slot <= 0)
                {
                    throw new InvalidOperationException("DemoSampleSource slot must be greater than zero.");
                }

                if (source.Slot > sampleCount)
                {
                    throw new InvalidOperationException(
                        "DemoSampleSource slot " + source.Slot.ToString(CultureInfo.InvariantCulture) +
                        " does not match a DemoSampleControl.");
                }

                if (sourceBySlot.ContainsKey(source.Slot))
                {
                    throw new InvalidOperationException(
                        "Duplicate DemoSampleSource slot " + source.Slot.ToString(CultureInfo.InvariantCulture) + ".");
                }

                sourceBySlot.Add(source.Slot, source);
            }

            return sourceBySlot;
        }

        // Move each slot's child element into the correct DemoSampleControl zone, then clear the
        // slot so the hidden ContentControl no longer holds a live reference to the transferred
        // element.
        private static void ApplyContentSlots(DependencyObject root, IList<DemoSampleControl> samples)
        {
            Dictionary<int, ContentControl> demoSlots = [];
            Dictionary<int, ContentControl> outputSlots = [];
            Dictionary<int, ContentControl> rightRailSlots = [];
            CollectContentSlots(root, demoSlots, outputSlots, rightRailSlots);
            ValidateSlotTargets(samples.Count, demoSlots, "DemoContentHost");
            ValidateSlotTargets(samples.Count, outputSlots, "OutputContentHost");
            ValidateSlotTargets(samples.Count, rightRailSlots, "RightRailContentHost");

            for (int i = 0; i < samples.Count; i++)
            {
                int slotIndex = i + 1;
                DemoSampleControl sample = samples[i];
                if (demoSlots.TryGetValue(slotIndex, out ContentControl? demoSlot))
                {
                    sample.DemoContent = TakeSlotContent(demoSlot);
                }

                if (outputSlots.TryGetValue(slotIndex, out ContentControl? outputSlot))
                {
                    sample.OutputContent = TakeSlotContent(outputSlot);
                }

                if (rightRailSlots.TryGetValue(slotIndex, out ContentControl? rightRailSlot))
                {
                    sample.RightRailContent = TakeSlotContent(rightRailSlot);
                }
            }
        }

        // Catch page-authoring mistakes where a slot name references a card index that does not
        // exist; fail fast at startup rather than silently ignoring the orphaned slot.
        private static void ValidateSlotTargets(
            int sampleCount,
            IDictionary<int, ContentControl> slots,
            string suffix)
        {
            foreach (int slot in slots.Keys)
            {
                if (slot > sampleCount)
                {
                    throw new InvalidOperationException(
                        "DemoSampleSlot" + slot.ToString("00", CultureInfo.InvariantCulture) +
                        suffix + " does not match a DemoSampleControl.");
                }
            }
        }

        // Remove the child from the hidden slot and return it so it can be re-parented into the
        // DemoSampleControl; a WPF element can only have one logical parent at a time.
        private static object? TakeSlotContent(ContentControl slot)
        {
            object? content = slot.Content;
            slot.Content = null;
            return content;
        }

        // Walk the logical tree (not the visual tree, which is not yet built) to collect all
        // hidden ContentControl slots by name suffix into three typed dictionaries.
        private static void CollectContentSlots(
            DependencyObject current,
            IDictionary<int, ContentControl> demoSlots,
            IDictionary<int, ContentControl> outputSlots,
            IDictionary<int, ContentControl> rightRailSlots)
        {
            if (current is ContentControl contentControl && !string.IsNullOrWhiteSpace(contentControl.Name))
            {
                AddSlotIfMatched(contentControl, "DemoContentHost", demoSlots);
                AddSlotIfMatched(contentControl, "OutputContentHost", outputSlots);
                AddSlotIfMatched(contentControl, "RightRailContentHost", rightRailSlots);
            }

            foreach (object child in LogicalTreeHelper.GetChildren(current))
            {
                if (child is DependencyObject childObject)
                {
                    CollectContentSlots(childObject, demoSlots, outputSlots, rightRailSlots);
                }
            }
        }

        // Parse the numeric index out of a slot name that follows the DemoSampleSlotNNSuffix
        // convention; ignore names that do not match the expected prefix + suffix pattern.
        private static void AddSlotIfMatched(
            ContentControl slot,
            string suffix,
            IDictionary<int, ContentControl> slots)
        {
            const string prefix = "DemoSampleSlot";
            string name = slot.Name;
            if (!name.StartsWith(prefix, StringComparison.Ordinal) ||
                !name.EndsWith(suffix, StringComparison.Ordinal))
            {
                return;
            }

            string indexText = name.Substring(prefix.Length, name.Length - prefix.Length - suffix.Length);
            if (!int.TryParse(indexText, NumberStyles.None, CultureInfo.InvariantCulture, out int index))
            {
                throw new InvalidOperationException("Invalid demo sample slot name: " + name + ".");
            }

            if (index <= 0)
            {
                throw new InvalidOperationException("Demo sample slot index must be greater than zero: " + name + ".");
            }

            if (slots.ContainsKey(index))
            {
                throw new InvalidOperationException("Duplicate demo sample slot name for " + name + ".");
            }

            slots.Add(index, slot);
        }

        // Walk the logical tree in document order to collect DemoSampleControls so the resulting
        // list index matches the 1-based slot numbering used by the slot naming convention.
        private static void CollectDemoSampleControls(DependencyObject current, ICollection<DemoSampleControl> samples)
        {
            if (current is DemoSampleControl sample)
            {
                samples.Add(sample);
                return;
            }

            foreach (object child in LogicalTreeHelper.GetChildren(current))
            {
                if (child is DependencyObject childObject)
                {
                    CollectDemoSampleControls(childObject, samples);
                }
            }
        }
    }
}
