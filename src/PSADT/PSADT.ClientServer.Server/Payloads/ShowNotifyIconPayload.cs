using System.Runtime.Serialization;
using PSADT.UserInterface.DialogOptions;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the ShowBalloonTip command.
    /// </summary>
    /// <param name="Options">The notify icon options.</param>
    [DataContract]
    internal sealed record class ShowNotifyIconPayload(NotifyIconOptions Options) : IClientServerPayload
    {
        /// <summary>
        /// The balloon tip options.
        /// </summary>
        [DataMember]
        internal readonly NotifyIconOptions Options = Options;
    }
}
