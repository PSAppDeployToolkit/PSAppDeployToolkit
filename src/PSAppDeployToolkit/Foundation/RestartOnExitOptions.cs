using System;
using System.Runtime.Serialization;

namespace PSAppDeployToolkit.Foundation
{
    /// <summary>
    /// Represents the data required to restart the module on exit.
    /// </summary>
    /// <remarks>This is handed to the client executable as its serialized options rather than read in-process, so
    /// it is declared as a data contract with each member named. A member the serializer was not told to carry is
    /// left out in silence, and the client would restart the device on whatever the CLR defaults gave it.</remarks>
    [DataContract]
    public sealed record class RestartOnExitOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestartOnExitOptions"/> class with the specified countdown, noForceCloseApps flag, and reason.
        /// </summary>
        /// <param name="countdown">The countdown duration before restarting on exit.</param>
        /// <param name="reason">The reason text for shutting down the module.</param>
        /// <param name="noForceCloseApps">A value indicating whether to force close applications during shutdown.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the countdown is not greater than zero.</exception>
        /// <exception cref="ArgumentException">Thrown when the reason is empty, whitespace, or exceeds 512 characters.</exception>
        public RestartOnExitOptions(TimeSpan countdown, string? reason, bool noForceCloseApps)
        {
            if (countdown <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(countdown), countdown, "Countdown must be greater than zero.");
            }
            if (reason is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(reason);
                if (reason.Length > 512)
                {
                    throw new ArgumentException("Reason cannot exceed 512 characters.", nameof(reason));
                }
            }
            Countdown = countdown;
            Reason = reason;
            NoForceCloseApps = noForceCloseApps;
        }

        /// <summary>
        /// Confirms this data's invariants once it has been read off the wire.
        /// </summary>
        /// <remarks>DataContractSerializer allocates without running a constructor, so a member the sender left
        /// out keeps its CLR default and what the constructor refuses arrives unrefused. The client acts on this by
        /// restarting the device, so a countdown of nothing at all would restart it immediately.</remarks>
        /// <param name="context">The streaming context, which is not used.</param>
        /// <exception cref="SerializationException">Thrown if the data arrived without a countdown, or with a reason the restart cannot carry.</exception>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (Countdown <= TimeSpan.Zero)
            {
                throw new SerializationException($"The deserialized {nameof(RestartOnExitOptions)} has no countdown.");
            }
            if (Reason is not null && (string.IsNullOrWhiteSpace(Reason) || Reason.Length > 512))
            {
                throw new SerializationException($"The deserialized {nameof(RestartOnExitOptions)} has an invalid reason.");
            }
        }

        /// <summary>
        /// Gets the countdown value for restarting the module on exit.
        /// </summary>
        [DataMember]
        public readonly TimeSpan Countdown;

        /// <summary>
        /// Gets the reason text for shutting down the module.
        /// </summary>
        /// <remarks>Capped at 512 characters because that is all shutdown.exe's '/c' switch accepts.</remarks>
        [DataMember]
        public readonly string? Reason;

        /// <summary>
        /// Gets a value indicating whether to force close applications during shutdown.
        /// </summary>
        [DataMember]
        public readonly bool NoForceCloseApps;
    }
}
