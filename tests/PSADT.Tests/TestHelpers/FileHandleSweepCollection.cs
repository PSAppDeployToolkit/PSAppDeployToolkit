using Xunit;

namespace PSADT.Tests.TestHelpers
{
    /// <summary>
    /// The collection every test that sweeps the machine's handle table belongs to.
    /// </summary>
    /// <remarks>
    /// A sweep walks every handle open on the machine, better than two hundred thousand of them, and starts a
    /// thread per file handle to ask it for its name - upwards of thirteen thousand threads for a single call. Two
    /// of those at once doubles the load for no gain and slows both, and xunit runs a class that names no
    /// collection as a collection of its own, in parallel with the rest. Naming one here is what keeps the two
    /// classes of sweep test from overlapping.
    /// <para>
    /// Only the sweeps are serialised against each other; the collection still runs alongside the rest of the
    /// assembly, which is what keeps this from costing the suite its wall time. It was briefly given the assembly
    /// to itself while a deadlock was being chased, at a cost of the thirteen seconds the sweeps take. That
    /// deadlock was the name query's own thread taking the loader lock and being terminated while holding it, and
    /// it is fixed where that thread is created rather than here, so do not reach for DisableParallelization
    /// again on its account.
    /// </para>
    /// </remarks>
    [CollectionDefinition(Name)]
    public sealed class FileHandleSweepCollection
    {
        /// <summary>
        /// The collection's name, for <see cref="CollectionAttribute"/>.
        /// </summary>
        public const string Name = "FileHandleSweep";
    }
}
