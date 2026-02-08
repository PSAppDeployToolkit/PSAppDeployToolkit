using PSADT.UserInterface.DialogOptions;
using Newtonsoft.Json;

namespace PSADT.ClientServer.Payloads
{
    /// <summary>
    /// Payload for the ShowBalloonTip command.
    /// </summary>
    internal sealed record ShowBalloonTipPayload : IPayload
    {
        /// <summary>
        /// The balloon tip options.
        /// </summary>
        [JsonProperty]
        internal readonly BalloonTipOptions Options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowBalloonTipPayload"/> class.
        /// </summary>
        /// <param name="options">The balloon tip options.</param>
        [JsonConstructor]
        internal ShowBalloonTipPayload(BalloonTipOptions options)
        {
            Options = options;
        }
    }
}
