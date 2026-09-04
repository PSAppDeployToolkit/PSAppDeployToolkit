using System.Collections.Generic;
using PSADT.Utilities;
using Xunit;

namespace PSADT.Tests.Utilities
{
    /// <summary>
    /// Tests the AArch64 instruction encoders.
    /// </summary>
    /// <remarks>
    /// These build the thread-injection stub <c language="csharp">FileHandleManager</c> runs inside another process on ARM
    /// machines. A wrong bit here does not fail a build or throw an exception; it produces machine code
    /// that executes in a foreign process and corrupts or crashes it, which is close to undiagnosable
    /// after the fact. Every expected value below is the real encoding of the named instruction, taken
    /// from the field layout in the architecture reference rather than from this implementation:
    /// <c language="text">MOVZ</c> and <c language="text">MOVK</c> are <c language="text">sf opc 100101 hw imm16 Rd</c> with the register in bits 0-4,
    /// the immediate in bits 5-20 and the shift selector in bits 21-22; <c language="text">BR</c> and <c language="text">BLR</c> carry
    /// their register in bits 5-9.
    /// </remarks>
    public sealed class NativeUtilitiesTests
    {
        /// <summary>
        /// Verifies the encoding of a 64-bit move-wide-with-zero, across registers, immediates and all
        /// four shift positions.
        /// </summary>
        /// <param name="register">The destination register number.</param>
        /// <param name="immediate">The 16-bit immediate.</param>
        /// <param name="shift">The shift selector, 0 to 3, meaning a left shift of 0, 16, 32 or 48.</param>
        /// <param name="expected">The expected instruction word.</param>
        [Theory]
        [InlineData(0, (ushort)0x0000, 0, 0xD280_0000u)]
        [InlineData(0, (ushort)0x1234, 0, 0xD282_4680u)]
        [InlineData(1, (ushort)0x1234, 0, 0xD282_4681u)]
        [InlineData(30, (ushort)0x1234, 0, 0xD282_469Eu)]
        [InlineData(31, (ushort)0x0000, 0, 0xD280_001Fu)]
        [InlineData(0, (ushort)0xFFFF, 0, 0xD29F_FFE0u)]
        [InlineData(0, (ushort)0x1234, 1, 0xD2A2_4680u)]
        [InlineData(0, (ushort)0x1234, 2, 0xD2C2_4680u)]
        [InlineData(0, (ushort)0x1234, 3, 0xD2E2_4680u)]
        [InlineData(16, (ushort)0xBEEF, 0, 0xD297_DDF0u)]
        public void EncodeMovZ_MatchesTheArchitectureEncoding(int register, ushort immediate, int shift, uint expected)
        {
            Assert.Equal(expected, NativeUtilities.EncodeMovZ(register, immediate, shift));
        }

        /// <summary>
        /// Verifies the encoding of a 64-bit move-wide-with-keep, which differs from move-wide-with-zero
        /// only in the opcode field.
        /// </summary>
        /// <param name="register">The destination register number.</param>
        /// <param name="immediate">The 16-bit immediate.</param>
        /// <param name="shift">The shift selector, 0 to 3.</param>
        /// <param name="expected">The expected instruction word.</param>
        [Theory]
        [InlineData(0, (ushort)0x0000, 0, 0xF280_0000u)]
        [InlineData(0, (ushort)0x1234, 0, 0xF282_4680u)]
        [InlineData(0, (ushort)0x1234, 1, 0xF2A2_4680u)]
        [InlineData(0, (ushort)0x1234, 2, 0xF2C2_4680u)]
        [InlineData(0, (ushort)0x1234, 3, 0xF2E2_4680u)]
        [InlineData(0, (ushort)0xFFFF, 3, 0xF2FF_FFE0u)]
        [InlineData(17, (ushort)0xBEEF, 1, 0xF2B7_DDF1u)]
        public void EncodeMovK_MatchesTheArchitectureEncoding(int register, ushort immediate, int shift, uint expected)
        {
            Assert.Equal(expected, NativeUtilities.EncodeMovK(register, immediate, shift));
        }

        /// <summary>
        /// Verifies that the two move-wide encoders differ only in the opcode bit that distinguishes
        /// them, so a change to one cannot silently drift from the other.
        /// </summary>
        /// <param name="register">The destination register number.</param>
        /// <param name="immediate">The 16-bit immediate.</param>
        /// <param name="shift">The shift selector, 0 to 3.</param>
        [Theory]
        [InlineData(0, (ushort)0x1234, 0)]
        [InlineData(16, (ushort)0xBEEF, 2)]
        [InlineData(31, (ushort)0xFFFF, 3)]
        public void EncodeMovZ_DiffersFromEncodeMovKOnlyInTheOpcode(int register, ushort immediate, int shift)
        {
            Assert.Equal(
                0x2000_0000u,
                NativeUtilities.EncodeMovK(register, immediate, shift) ^ NativeUtilities.EncodeMovZ(register, immediate, shift));
        }

        /// <summary>
        /// Verifies that the register and shift fields are masked to their widths, so an out-of-range
        /// caller cannot corrupt a neighbouring field of the instruction.
        /// </summary>
        [Fact]
        public void EncodeMovZ_MasksTheRegisterAndShiftFields()
        {
            // Assert: register 32 wraps to 0 rather than spilling into the immediate
            Assert.Equal(NativeUtilities.EncodeMovZ(0, 0x1234, 0), NativeUtilities.EncodeMovZ(32, 0x1234, 0));

            // Assert: shift 4 wraps to 0 rather than spilling into the opcode
            Assert.Equal(NativeUtilities.EncodeMovZ(0, 0x1234, 0), NativeUtilities.EncodeMovZ(0, 0x1234, 4));
        }

        /// <summary>
        /// Verifies the encoding of an unconditional branch to a register.
        /// </summary>
        /// <param name="register">The register holding the target address.</param>
        /// <param name="expected">The expected instruction word.</param>
        [Theory]
        [InlineData(0, 0xD61F_0000u)]
        [InlineData(8, 0xD61F_0100u)]
        [InlineData(16, 0xD61F_0200u)]
        [InlineData(17, 0xD61F_0220u)]
        [InlineData(30, 0xD61F_03C0u)]
        public void EncodeBr_MatchesTheArchitectureEncoding(int register, uint expected)
        {
            Assert.Equal(expected, NativeUtilities.EncodeBr(register));
        }

        /// <summary>
        /// Verifies the encoding of a branch with link to a register, which differs from a plain branch
        /// only in the operation field.
        /// </summary>
        /// <param name="register">The register holding the target address.</param>
        /// <param name="expected">The expected instruction word.</param>
        [Theory]
        [InlineData(0, 0xD63F_0000u)]
        [InlineData(16, 0xD63F_0200u)]
        [InlineData(17, 0xD63F_0220u)]
        [InlineData(30, 0xD63F_03C0u)]
        public void EncodeBlr_MatchesTheArchitectureEncoding(int register, uint expected)
        {
            Assert.Equal(expected, NativeUtilities.EncodeBlr(register));
        }

        /// <summary>
        /// Verifies that the branch encoders mask the register to five bits.
        /// </summary>
        [Fact]
        public void BranchEncoders_MaskTheRegisterField()
        {
            Assert.Equal(NativeUtilities.EncodeBr(0), NativeUtilities.EncodeBr(32));
            Assert.Equal(NativeUtilities.EncodeBlr(0), NativeUtilities.EncodeBlr(32));
        }

        /// <summary>
        /// Verifies that loading a 64-bit constant emits one move-wide-with-zero followed by three
        /// move-wide-with-keep, each carrying the matching quarter of the value at the matching shift.
        /// </summary>
        [Fact]
        public void Load64_EmitsAZeroingMoveThenThreeKeepingMoves()
        {
            // Arrange
            const ulong value = 0x11223344_55667788UL;
            const int register = 16;

            // Act
            IReadOnlyList<uint> instructions = NativeUtilities.Load64(register, value);

            // Assert
            Assert.Equal(4, instructions.Count);
            Assert.Equal(NativeUtilities.EncodeMovZ(register, 0x7788, 0), instructions[0]);
            Assert.Equal(NativeUtilities.EncodeMovK(register, 0x5566, 1), instructions[1]);
            Assert.Equal(NativeUtilities.EncodeMovK(register, 0x3344, 2), instructions[2]);
            Assert.Equal(NativeUtilities.EncodeMovK(register, 0x1122, 3), instructions[3]);
        }

        /// <summary>
        /// Verifies that the four emitted immediates reassemble into the value that was asked for, which
        /// is the property the stub depends on when it needs an absolute address in a register.
        /// </summary>
        /// <param name="value">The constant to load.</param>
        [Theory]
        [InlineData(0UL)]
        [InlineData(1UL)]
        [InlineData(0xFFFFFFFF_FFFFFFFFUL)]
        [InlineData(0x11223344_55667788UL)]
        [InlineData(0x00000000_0000FFFFUL)]
        [InlineData(0xFFFF0000_00000000UL)]
        [InlineData(0x7FF78000_00001234UL)]
        public void Load64_ReassemblesIntoTheRequestedValue(ulong value)
        {
            // Act
            IReadOnlyList<uint> instructions = NativeUtilities.Load64(0, value);

            // Assert: recover each quarter from the immediate field, bits 5 to 20
            ulong recovered = 0;
            for (int i = 0; i < instructions.Count; i++)
            {
                recovered |= (ulong)((instructions[i] >> 5) & 0xFFFF) << (i * 16);
            }
            Assert.Equal(value, recovered);
        }

        /// <summary>
        /// Verifies that every instruction in the sequence targets the register that was asked for, since
        /// a stray register would leave the address in the wrong place.
        /// </summary>
        /// <param name="register">The register to load into.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(8)]
        [InlineData(16)]
        [InlineData(30)]
        public void Load64_TargetsTheRequestedRegisterThroughout(int register)
        {
            Assert.All(
                NativeUtilities.Load64(register, 0x11223344_55667788UL),
                instruction => Assert.Equal((uint)register, instruction & 0x1F));
        }
    }
}
