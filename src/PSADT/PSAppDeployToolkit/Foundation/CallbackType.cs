namespace PSAppDeployToolkit.Foundation
{
    /// <summary>
    /// The type of callback to be executed.
    /// </summary>
    public enum CallbackType
    {
        /// <summary>
        /// The callback is executed before the module is initialized.
        /// </summary>
        OnInit = 0,

        /// <summary>
        /// The callback is executed before the first deployment session is opened.
        /// </summary>
        OnStart = 1,

        /// <summary>
        /// The callback is executed before a deployment session is opened.
        /// </summary>
        PreOpen = 2,

        /// <summary>
        /// The callback is executed after a deployment session is opened.
        /// </summary>
        PostOpen = 3,

        /// <summary>
        /// The callback is executed after a message is logged.
        /// </summary>
        OnLogEntry = 4,

        /// <summary>
        /// The callback is executed when a user defers the active deployment.
        /// </summary>
        OnDefer = 5,

        /// <summary>
        /// The callback is executed before the deployment session is closed.
        /// </summary>
        PreClose = 6,

        /// <summary>
        /// The callback is executed after the deployment session is closed.
        /// </summary>
        PostClose = 7,

        /// <summary>
        /// The callback is executed before the last deployment session is closed.
        /// </summary>
        OnFinish = 8,

        /// <summary>
        /// The callback is executed after the last deployment session is closed.
        /// </summary>
        OnExit = 9,
    }
}
