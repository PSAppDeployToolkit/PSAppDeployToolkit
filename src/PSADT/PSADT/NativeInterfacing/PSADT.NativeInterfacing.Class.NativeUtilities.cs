using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PSADT.LibraryInterfaces;
using Windows.Win32.System.Memory;

namespace PSADT.NativeInterfacing
{
    /// <summary>
    /// Utility class for building trampolines to call native functions.
    /// </summary>
    internal static class NativeUtilities
    {
        /// <summary>
        /// Allocates executable memory and copies the code into it.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static IntPtr AllocateExecutableMemory(byte[] code)
        {
            IntPtr mem = Kernel32.VirtualAlloc(IntPtr.Zero, (UIntPtr)code.Length, VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT | VIRTUAL_ALLOCATION_TYPE.MEM_RESERVE, PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE);
            Marshal.Copy(code, 0, mem, code.Length);
            return mem;
        }

        /// <summary>
        /// Encodes a MOVZ instruction for ARM64.
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="imm16"></param>
        /// <param name="shift"></param>
        /// <returns></returns>
        internal static uint EncodeMovZ(int reg, ushort imm16, int shift) => 0xD2800000 | (uint)reg & 0x1F | ((uint)shift & 3) << 21 | ((uint)imm16 & 0xFFFF) << 5;

        /// <summary>
        /// Encodes a MOVK instruction for ARM64.
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="imm16"></param>
        /// <param name="shift"></param>
        /// <returns></returns>
        internal static uint EncodeMovK(int reg, ushort imm16, int shift) => 0xF2800000 | (uint)reg & 0x1F | ((uint)shift & 3) << 21 | ((uint)imm16 & 0xFFFF) << 5;

        /// <summary>
        /// Encodes a BR instruction for ARM64.
        /// </summary>
        /// <param name="reg"></param>
        /// <returns></returns>
        internal static uint EncodeBr(int reg) => (uint)(0xD61F0000 | (reg & 0x1F) << 5);

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
