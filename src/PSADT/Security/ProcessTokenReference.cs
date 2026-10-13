using System;
using Windows.Win32.Foundation;

namespace PSADT.Security
{
    /// <summary>
    /// Captures the one or two Winlogon records that make up a single desktop logon.
    /// </summary>
    /// <remarks>A UAC split-token administrator has two LSA records for one logon and nothing in either record
    /// distinguishes them, so both are carried together and the kernel's token link is what proves a candidate
    /// belongs to the pair.</remarks>
    internal sealed record class ProcessTokenReference
    {
        /// <summary>
        /// Initialises a reference, ordering a split pair so that equality does not depend on LSA enumeration order.
        /// </summary>
        /// <param name="logon">One record of the logon.</param>
        /// <param name="counterpart">The opposite half of a split logon, or null when the logon is not split.</param>
        /// <exception cref="ArgumentException">The two halves name the same logon session.</exception>
        internal ProcessTokenReference(ProcessTokenLogon logon, ProcessTokenLogon? counterpart = null)
        {
            bool swap = false;
            if (counterpart is not null)
            {
                int order = Compare(in counterpart.AuthenticationId, in logon.AuthenticationId);
                if (order is 0)
                {
                    throw new ArgumentException("A split logon is two distinct logon sessions, so a reference cannot hold one of them twice.", nameof(counterpart));
                }
                swap = order < 0;
            }
            Logon = swap ? counterpart! : logon;
            Counterpart = swap ? logon : counterpart;
        }

        /// <summary>
        /// Determines whether a record is one of the halves of this logon.
        /// </summary>
        /// <param name="logon">The record to test.</param>
        /// <returns>Whether the record belongs to this logon.</returns>
        internal bool Includes(ProcessTokenLogon logon)
        {
            return Logon == logon || Counterpart == logon;
        }

        /// <summary>
        /// Orders two logon identifiers so that a split pair has exactly one canonical form.
        /// </summary>
        /// <param name="left">The first identifier.</param>
        /// <param name="right">The second identifier.</param>
        /// <returns>A negative value, zero, or a positive value as the first identifier sorts before, with, or after the second.</returns>
        private static int Compare(in LUID left, in LUID right)
        {
            return left.HighPart != right.HighPart ? left.HighPart.CompareTo(right.HighPart) : left.LowPart.CompareTo(right.LowPart);
        }

        /// <summary>
        /// The logon record, or the lower-ordered half when the logon is split.
        /// </summary>
        internal readonly ProcessTokenLogon Logon;

        /// <summary>
        /// The opposite half of a split logon, or null when the logon is not split.
        /// </summary>
        internal readonly ProcessTokenLogon? Counterpart;
    }
}
