namespace PSADT.DeviceManagement
{
    /// <summary>
    /// SMBIOS chassis (system enclosure) types, as specified in the SMBIOS Type 3 record.
    /// 33–255 are either reserved or OEM-defined.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "There is no zero value in the spec.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "The type is correct for the data.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1027:Mark enums with FlagsAttribute", Justification = "This isn't a bitfield...")]
    public enum SystemChassisType : ushort
    {
        /// <summary>
        /// Other
        /// </summary>
        Other = 1,

        /// <summary>
        /// Unknown
        /// </summary>
        Unknown = 2,

        /// <summary>
        /// Desktop
        /// </summary>
        Desktop = 3,

        /// <summary>
        /// Low Profile Desktop
        /// </summary>
        LowProfileDesktop = 4,

        /// <summary>
        /// Pizza Box
        /// </summary>
        PizzaBox = 5,

        /// <summary>
        /// Mini Tower
        /// </summary>
        MiniTower = 6,

        /// <summary>
        /// Tower
        /// </summary>
        Tower = 7,

        /// <summary>
        /// Portable
        /// </summary>
        Portable = 8,

        /// <summary>
        /// Laptop
        /// </summary>
        Laptop = 9,

        /// <summary>
        /// Notebook
        /// </summary>
        Notebook = 10,

        /// <summary>
        /// Hand Held
        /// </summary>
        HandHeld = 11,

        /// <summary>
        /// Docking Station
        /// </summary>
        DockingStation = 12,

        /// <summary>
        /// All in One
        /// </summary>
        AllInOne = 13,

        /// <summary>
        /// Sub Notebook
        /// </summary>
        SubNotebook = 14,

        /// <summary>
        /// Space-Saving
        /// </summary>
        SpaceSaving = 15,

        /// <summary>
        /// Lunch Box
        /// </summary>
        LunchBox = 16,

        /// <summary>
        /// Main System Chassis
        /// </summary>
        MainSystemChassis = 17,

        /// <summary>
        /// Expansion Chassis
        /// </summary>
        ExpansionChassis = 18,

        /// <summary>
        /// SubChassis
        /// </summary>
        SubChassis = 19,

        /// <summary>
        /// Bus Expansion Chassis
        /// </summary>
        BusExpansionChassis = 20,

        /// <summary>
        /// Peripheral Chassis
        /// </summary>
        PeripheralChassis = 21,

        /// <summary>
        /// Storage Chassis
        /// </summary>
        StorageChassis = 22,

        /// <summary>
        /// Rack Mount Chassis
        /// </summary>
        RackMountChassis = 23,

        /// <summary>
        /// Sealed-Case PC
        /// </summary>
        SealedCasePc = 24,

        // Values 25–29 are reserved in the SMBIOS spec
        /// <summary>
        /// Tablet
        /// </summary>
        Tablet = 30,

        /// <summary>
        /// Convertible
        /// </summary>
        Convertible = 31,

        /// <summary>
        /// Detachable
        /// </summary>
        Detachable = 32,
    }
}
