namespace PSADT.Module
{
    /// <summary>
    /// The type of callback to be executed.
    /// </summary>
    public enum CallbackType
    {
        /// <summary>
        /// The callback is executed before the module is initialized.
        /// </summary>
        OnInit,

        /// <summary>
        /// The callback is executed before the first deployment session is opened.
        /// </summary>
        OnStart,

        /// <summary>
        /// The callback is executed before a deployment session is opened.
        /// </summary>
        PreOpen,

        /// <summary>
        /// The callback is executed after a deployment session is opened.
        /// </summary>
        PostOpen,

        /// <summary>
        /// The callback is executed before the deployment session is closed.
        /// </summary>
        PreClose,

        /// <summary>
        /// The callback is executed after the deployment session is closed.
        /// </summary>
        PostClose,

        /// <summary>
        /// The callback is executed before the last deployment session is closed.
        /// </summary>
        OnFinish,

        /// <summary>
        /// The callback is executed after the last deployment session is closed.
        /// </summary>
        OnExit,
    }
}
