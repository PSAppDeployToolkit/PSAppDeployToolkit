namespace PSADT.PackageManagement
{
    /// <summary>
    /// The processor architecture that a given package was built for.
    /// </summary>
    public enum ProcessorArchitecture : uint
    {
        /// <summary/>
        X86 = 0,
        /// <summary/>
        Arm = 5,
        /// <summary/>
        X64 = 9,
        /// <summary/>
        Neutral = 11,
        /// <summary/>
        Arm64 = 12,
        /// <summary/>
        X86OnArm64 = 14,
        /// <summary/>
        Unknown = 65535
    }
}
