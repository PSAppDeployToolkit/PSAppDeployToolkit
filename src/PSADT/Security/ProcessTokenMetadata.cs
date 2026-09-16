using System.Security.Principal;
using Windows.Win32.Foundation;
using Windows.Win32.Security;

namespace PSADT.Security
{
    /// <summary>
    /// Captures token suitability independently of the desktop logon reference.
    /// </summary>
    /// <param name="Sid">The token owner SID.</param>
    /// <param name="SessionId">The token session identifier.</param>
    /// <param name="AuthenticationId">The token authentication identifier.</param>
    /// <param name="TokenType">The native token type.</param>
    /// <param name="Elevated">Whether the token is elevated.</param>
    /// <param name="IntegritySid">The token integrity SID.</param>
    /// <param name="Restricted">Whether the token has restricting SIDs.</param>
    /// <param name="AppContainer">Whether the token is an AppContainer token.</param>
    /// <param name="UIAccess">Whether UIAccess is enabled.</param>
    /// <param name="Logon">The token's independently queried LSA record.</param>
    /// <param name="ElevationType">Whether the token is unsplit, full, or limited.</param>
    internal sealed record class ProcessTokenMetadata(SecurityIdentifier Sid, uint SessionId, LUID AuthenticationId, TOKEN_TYPE TokenType, bool Elevated, SecurityIdentifier IntegritySid, bool Restricted, bool AppContainer, bool UIAccess, ProcessTokenLogon Logon, TOKEN_ELEVATION_TYPE ElevationType = TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault)
    {
        /// <summary>
        /// The token owner SID.
        /// </summary>
        internal readonly SecurityIdentifier Sid = Sid;

        /// <summary>
        /// The token session identifier.
        /// </summary>
        internal readonly uint SessionId = SessionId;

        /// <summary>
        /// The authentication identifier.
        /// </summary>
        internal readonly LUID AuthenticationId = AuthenticationId;

        /// <summary>
        /// The native token type.
        /// </summary>
        internal readonly TOKEN_TYPE TokenType = TokenType;

        /// <summary>
        /// Whether the token is elevated.
        /// </summary>
        internal readonly bool Elevated = Elevated;

        /// <summary>
        /// The token integrity SID.
        /// </summary>
        internal readonly SecurityIdentifier IntegritySid = IntegritySid;

        /// <summary>
        /// Whether restricting SIDs are present.
        /// </summary>
        internal readonly bool Restricted = Restricted;

        /// <summary>
        /// Whether this is an AppContainer token.
        /// </summary>
        internal readonly bool AppContainer = AppContainer;

        /// <summary>
        /// Whether UIAccess is enabled.
        /// </summary>
        internal readonly bool UIAccess = UIAccess;

        /// <summary>
        /// The token's LSA logon record.
        /// </summary>
        internal readonly ProcessTokenLogon Logon = Logon;

        /// <summary>
        /// Whether the token is unsplit, full, or limited.
        /// </summary>
        internal readonly TOKEN_ELEVATION_TYPE ElevationType = ElevationType;
    }
}
