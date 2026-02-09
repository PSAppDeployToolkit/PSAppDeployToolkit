using System.Runtime.Serialization;
using PSADT.UserInterface.DialogOptions;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the ShowBalloonTip command.
    /// </summary>
    [DataContract]
    internal sealed record ShowBalloonTipPayload : IPayload
    {
        /// <summary>
        /// The balloon tip options.
        /// </summary>
        [DataMember]
        internal readonly BalloonTipOptions Options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowBalloonTipPayload"/> class.
        /// </summary>
        /// <param name="options">The balloon tip options.</param>
        internal ShowBalloonTipPayload(BalloonTipOptions options)
        {
            Options = options;
        }
    }
}
