using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using PSADT.UserInterface.DialogOptions;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the ShowBalloonTip command.
    /// </summary>
    [DataContract]
    internal sealed record ShowNotifyIconPayload : IClientServerPayload
    {
        /// <summary>
        /// The balloon tip options.
        /// </summary>
        [DataMember]
        internal readonly NotifyIconOptions Options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowNotifyIconPayload"/> class.
        /// </summary>
        /// <param name="options">The notify icon options.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ShowNotifyIconPayload(NotifyIconOptions options)
        {
            Options = options;
        }
    }
}
