// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
namespace Microsoft.Xaml.Behaviors
{
    using System;
    using System.Reflection;

    /// <summary>
    /// EventObserver is designed to help manage event handlers by detatching when disposed. Creating this object will also attach in the constructor.
    /// </summary>
    public sealed class EventObserver : IDisposable
    {
        private EventInfo eventInfo;
        private object target;
        private Delegate handler;

        /// <summary>
        /// Creates an instance of EventObserver and attaches to the supplied event on the supplied target. Call dispose to detach.
        /// </summary>
        /// <param name="eventInfo">The event to attach and detach from.</param>
        /// <param name="target">The target object the event is defined on. Null if the method is static.</param>
        /// <param name="handler">The delegate to attach to the event.</param>
        public EventObserver(EventInfo eventInfo, object target, Delegate handler)
        {
            if (eventInfo == null)
            {
                throw new ArgumentNullException("eventInfo");
            }

            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            this.eventInfo = eventInfo;
            this.target = target;
            this.handler = handler;
            this.eventInfo.AddEventHandler(this.target, handler);
        }

        /// <summary>
        /// Detaches the handler from the event.
        /// </summary>
        public void Dispose()
        {
            this.eventInfo.RemoveEventHandler(this.target, this.handler);
        }
    }
}
