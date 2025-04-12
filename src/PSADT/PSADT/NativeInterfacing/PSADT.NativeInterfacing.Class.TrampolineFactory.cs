using System;
using System.Collections.Generic;
using PSADT.OperatingSystem;
using PSADT.Types;

namespace PSADT.NativeInterfacing
{
    /// <summary>
    /// Utility class for building trampolines to call native functions.
    /// </summary>
    internal static class TrampolineFactory
    {
        /// <summary>
        /// Create a trampoline to call a native function with a parameter.
        /// </summary>
        /// <param name="targetFunction"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        /// <exception cref="PlatformNotSupportedException"></exception>
        internal static unsafe IntPtr Create(IntPtr targetFunction, IntPtr parameter)
        {
            switch (OSVersionInfo.Current.Architecture)
            {
                case SystemArchitecture.AMD64:
                    return BuildX64Trampoline((void*)targetFunction, (void*)parameter);
                case SystemArchitecture.i386:
                    return BuildX86Trampoline((void*)targetFunction, (void*)parameter);
                case SystemArchitecture.ARM64:
                    return BuildArm64Trampoline((ulong)targetFunction, (ulong)parameter);
                default:
                    throw new PlatformNotSupportedException("Unsupported architecture: " + OSVersionInfo.Current.Architecture);
            }
        }

        /// <summary>
        /// Builds a trampoline to call a native function with a parameter.
        /// </summary>
        /// <param name="targetFunction"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        private static unsafe IntPtr BuildX64Trampoline(void* targetFunction, void* argument)
        {
            byte[] stub = new byte[22];

            fixed (byte* p = stub)
            {
                // mov rcx, <arg>
                p[0] = 0x48; p[1] = 0xB9;
                *(ulong*)(p + 2) = (ulong)argument;

                // mov rax, <target>
                p[10] = 0x48; p[11] = 0xB8;
                *(ulong*)(p + 12) = (ulong)targetFunction;

                // jmp rax
                p[20] = 0xFF; p[21] = 0xE0;
            }

            return NativeUtilities.AllocateExecutableMemory(stub);
        }

        /// <summary>
        /// Builds a trampoline to call a native function with a parameter.
        /// </summary>
        /// <param name="targetFunction"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        private static unsafe IntPtr BuildX86Trampoline(void* targetFunction, void* argument)
        {
            byte[] stub = new byte[11];

            fixed (byte* p = stub)
            {
                // push <arg>
                p[0] = 0x68;
                *(uint*)(p + 1) = (uint)argument;

                // mov eax, <target>
                p[5] = 0xB8;
                *(uint*)(p + 6) = (uint)targetFunction;

                // call eax
                p[9] = 0xFF;
                p[10] = 0xD0;
            }

            return NativeUtilities.AllocateExecutableMemory(stub);
        }

        /// <summary>
        /// Builds a trampoline to call a native function with a parameter on ARM64.
        /// </summary>
        /// <param name="targetFunction"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private static unsafe IntPtr BuildArm64Trampoline(ulong targetFunction, ulong parameter)
        {
            var instructions = new List<uint>();

            // x0 = param
            instructions.AddRange(NativeUtilities.Load64(0, parameter));

            // x16 = targetFunc
            instructions.AddRange(NativeUtilities.Load64(16, targetFunction));

            // br x16
            instructions.Add(NativeUtilities.EncodeBr(16));

            byte[] stub = new byte[instructions.Count * 4];
            for (int i = 0; i < instructions.Count; i++)
            {
                byte[] bytes = BitConverter.GetBytes(instructions[i]);
                Buffer.BlockCopy(bytes, 0, stub, i * 4, 4);
            }

            return NativeUtilities.AllocateExecutableMemory(stub);
        }
    }
}
