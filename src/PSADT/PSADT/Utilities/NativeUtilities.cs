using System.Collections.Generic;

namespace PSADT.Utilities
{
    /// <summary>
    /// Utility class for working directly with machine code.
    /// </summary>
    internal static class NativeUtilities
    {
        /// <summary>
        /// Encodes a MOVZ (Move Wide with Zero) instruction for the ARM64 architecture using the specified destination
        /// register, immediate value, and shift amount.
        /// </summary>
        /// <remarks>The method constructs the instruction by combining the base opcode with the provided
        /// register, immediate, and shift values. Supplying values outside the valid ranges may result in an invalid
        /// instruction encoding.</remarks>
        /// <param name="reg">The destination register number to encode. Must be in the range 0 to 31.</param>
        /// <param name="imm16">The 16-bit immediate value to encode into the instruction.</param>
        /// <param name="shift">The left shift amount for the immediate value, in multiples of 16 bits. Must be between 0 and 3.</param>
        /// <returns>A 32-bit unsigned integer representing the encoded MOVZ instruction.</returns>
        internal static uint EncodeMovZ(int reg, ushort imm16, int shift)
        {
            return 0xD2800000 | ((uint)reg & 0x1F) | (((uint)shift & 3) << 21) | (((uint)imm16 & 0xFFFF) << 5);
        }

        /// <summary>
        /// Encodes a MOVK instruction for the ARM64 architecture using the specified register, immediate value, and
        /// shift amount.
        /// </summary>
        /// <remarks>Ensure that all parameters are within their valid ranges to produce a correct
        /// instruction encoding. The MOVK instruction is used to move a 16-bit immediate value into a specified
        /// position within a register, preserving the other bits.</remarks>
        /// <param name="reg">The destination register number to encode. Must be in the range 0 to 31.</param>
        /// <param name="imm16">The 16-bit immediate value to be encoded into the instruction.</param>
        /// <param name="shift">The shift amount, in 16-bit units, used to position the immediate value. Must be in the range 0 to 3.</param>
        /// <returns>A 32-bit unsigned integer representing the encoded MOVK instruction.</returns>
        internal static uint EncodeMovK(int reg, ushort imm16, int shift)
        {
            return 0xF2800000 | ((uint)reg & 0x1F) | (((uint)shift & 3) << 21) | (((uint)imm16 & 0xFFFF) << 5);
        }

        /// <summary>
        /// Encodes the specified register value into a 32-bit unsigned integer suitable for use in instruction
        /// encoding.
        /// </summary>
        /// <remarks>This method is intended for internal use in instruction encoding and should not be
        /// called directly by external code.</remarks>
        /// <param name="reg">The register value to encode. Must be in the range 0 to 31.</param>
        /// <returns>A 32-bit unsigned integer representing the encoded register value, with the upper bits set to a fixed
        /// pattern.</returns>
        internal static uint EncodeBr(int reg)
        {
            return (uint)(0xD61F0000 | ((reg & 0x1F) << 5));
        }

        /// <summary>
        /// Encodes the specified register value into a 32-bit unsigned integer suitable for instruction encoding.
        /// </summary>
        /// <remarks>This method is intended for internal use as part of the instruction encoding process.
        /// Supplying a value outside the valid range may result in incorrect encoding.</remarks>
        /// <param name="reg">The register value to encode. Must be in the range 0 to 31; only the lower 5 bits are used.</param>
        /// <returns>A 32-bit unsigned integer representing the encoded register value, with the upper bits set to a fixed
        /// pattern.</returns>
        internal static uint EncodeBlr(int reg)
        {
            return (uint)(0xD63F0000 | ((reg & 0x1F) << 5));
        }

        /// <summary>
        /// Generates a sequence of encoded instructions to load a 64-bit unsigned integer value into a specified
        /// register using 32-bit operations.
        /// </summary>
        /// <remarks>This method produces a series of instructions that can be used to efficiently load a
        /// 64-bit value into a register, typically for use in low-level or native interop scenarios. Each instruction
        /// encodes a portion of the value, allowing for full reconstruction in the target register.</remarks>
        /// <param name="reg">The index of the target register into which the value will be loaded.</param>
        /// <param name="value">The 64-bit unsigned integer value to encode and load into the register.</param>
        /// <returns>An enumerable collection of 32-bit unsigned integers representing the encoded instructions required to load
        /// the specified value into the target register.</returns>
        internal static IEnumerable<uint> Load64(int reg, ulong value)
        {
            yield return EncodeMovZ(reg, (ushort)(value >> 0), 0);
            yield return EncodeMovK(reg, (ushort)(value >> 16), 1);
            yield return EncodeMovK(reg, (ushort)(value >> 32), 2);
            yield return EncodeMovK(reg, (ushort)(value >> 48), 3);
        }
    }
}
