namespace PSADT.Security
{
    /// <summary>
    /// Specifies the types of elevated tokens that can be used for access control and security context.
    /// </summary>
    /// <remarks>This enumeration defines the different levels of elevated tokens that can be requested, which
    /// influence the permissions granted to a process. The values include 'None', which indicates no elevation,
    /// 'HighestAvailable', which requests the highest available token, and 'HighestMandatory', which enforces the
    /// highest mandatory token level.</remarks>
    public enum ElevatedTokenType
    {
        /// <summary>
        /// Specifies that a base token is to be retrieved.
        /// </summary>
        None,

        /// <summary>
        /// Specifies that a linked admin token should be retrieved if possible and available.
        /// </summary>
        HighestAvailable,

        /// <summary>
        /// Specifies that a linked admin token must be retrieved, throwing if unable to do so.
        /// </summary>
        HighestMandatory,
    }
}
