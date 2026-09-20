using System.Collections.Generic;
using System.Management.Automation;

namespace PSAppDeployToolkit.Foundation
{
    /// <summary>
    /// Represents the callbacks for a module in the PSAppDeployToolkit.Foundation namespace.
    /// </summary>
    public sealed class ModuleCallbacks
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleCallbacks"/> class.
        /// </summary>
        internal ModuleCallbacks()
        {
        }

        /// <summary>
        /// The callback is executed before the module is initialized.
        /// </summary>
        public IList<CommandInfo> OnInit { get; } = [];

        /// <summary>
        /// The callback is executed before the first deployment session is opened.
        /// </summary>
        public IList<CommandInfo> OnStart { get; } = [];

        /// <summary>
        /// The callback is executed before a deployment session is opened.
        /// </summary>
        public IList<CommandInfo> PreOpen { get; } = [];

        /// <summary>
        /// The callback is executed after a deployment session is opened.
        /// </summary>
        public IList<CommandInfo> PostOpen { get; } = [];

        /// <summary>
        /// The callback is executed after a message is logged.
        /// </summary>
        public IList<CommandInfo> OnLogEntry { get; } = [];

        /// <summary>
        /// The callback is executed when a user defers the active deployment.
        /// </summary>
        public IList<CommandInfo> OnDefer { get; } = [];

        /// <summary>
        /// The callback is executed before the deployment session is closed.
        /// </summary>
        public IList<CommandInfo> PreClose { get; } = [];

        /// <summary>
        /// The callback is executed after the deployment session is closed.
        /// </summary>
        public IList<CommandInfo> PostClose { get; } = [];

        /// <summary>
        /// The callback is executed before the last deployment session is closed.
        /// </summary>
        public IList<CommandInfo> OnFinish { get; } = [];

        /// <summary>
        /// The callback is executed after the last deployment session is closed.
        /// </summary>
        public IList<CommandInfo> OnExit { get; } = [];
    }
}
