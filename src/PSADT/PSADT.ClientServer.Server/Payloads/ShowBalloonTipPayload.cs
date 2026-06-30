using System.Runtime.Serialization;
using PSADT.UserInterface.DialogOptions;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the ShowBalloonTip command.
    /// </summary>
    /// <param name="Options">The balloon tip options.</param>
    [DataContract]
    internal sealed record class ShowBalloonTipPayload(BalloonTipOptions Options) : IClientServerPayload
    {
        /// <summary>
        /// The balloon tip options.
        /// </summary>
        [DataMember]
        internal readonly BalloonTipOptions Options = Options;
    }
}
