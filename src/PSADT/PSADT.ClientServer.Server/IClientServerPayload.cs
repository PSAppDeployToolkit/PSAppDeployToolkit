namespace PSADT.ClientServer
{
    /// <summary>
    /// Marker interface for all pipe command payloads.
    /// </summary>
    /// <remarks>This interface is used to provide compile-time type safety for payload objects
    /// passed through the pipe communication channel.</remarks>
    internal interface IClientServerPayload
    {
    }
}
