using System.Collections.Generic;

namespace PSADT.Utilities
{
    /// <summary>
    /// Utility class for working directly with machine code.
    /// </summary>
    internal static class NativeUtilities
    {
        /// <summary>
        /// Encodes a MOVZ instruction for ARM64.
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="imm16"></param>
        /// <param name="shift"></param>
        /// <returns></returns>
        internal static uint EncodeMovZ(int reg, ushort imm16, int shift)
        {
            return 0xD2800000 | ((uint)reg & 0x1F) | (((uint)shift & 3) << 21) | (((uint)imm16 & 0xFFFF) << 5);
        }

        /// <summary>
        /// Encodes a MOVK instruction for ARM64.
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="imm16"></param>
        /// <param name="shift"></param>
        /// <returns></returns>
        internal static uint EncodeMovK(int reg, ushort imm16, int shift)
        {
            return 0xF2800000 | ((uint)reg & 0x1F) | (((uint)shift & 3) << 21) | (((uint)imm16 & 0xFFFF) << 5);
        }

        /// <summary>
        /// Encodes a BR (branch to register) instruction for ARM64.
        /// </summary>
        /// <param name="reg"></param>
        /// <returns></returns>
        internal static uint EncodeBr(int reg)
        {
            return (uint)(0xD61F0000 | ((reg & 0x1F) << 5));
        }

        /// <summary>
        /// Encodes a BLR (branch with link to register) instruction for ARM64.
        /// This is used for function calls where the return address is saved.
        /// </summary>
        /// <param name="reg"></param>
        /// <returns></returns>
        internal static uint EncodeBlr(int reg)
        {
            return (uint)(0xD63F0000 | ((reg & 0x1F) << 5));
        }

        /// <summary>
        /// Encodes a MOVZ instruction for ARM64 with a 64-bit immediate value.
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static IEnumerable<uint> Load64(int reg, ulong value)
        {
            yield return EncodeMovZ(reg, (ushort)(value >> 0), 0);
            yield return EncodeMovK(reg, (ushort)(value >> 16), 1);
            yield return EncodeMovK(reg, (ushort)(value >> 32), 2);
            yield return EncodeMovK(reg, (ushort)(value >> 48), 3);
        }
    }
}
