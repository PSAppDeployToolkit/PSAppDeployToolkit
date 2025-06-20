// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
namespace Microsoft.Xaml.Behaviors.Core
{
    using System;
    using System.Windows;
    using System.Windows.Threading;

    /// <summary>
    /// A trigger that is triggered by a specified event occurring on its source and fires after a delay when that event is fired.
    /// </summary>
    public class TimerTrigger : Microsoft.Xaml.Behaviors.EventTrigger
    {
        public static readonly DependencyProperty MillisecondsPerTickProperty = DependencyProperty.Register("MillisecondsPerTick",
                                                                                                    typeof(double),
                                                                                                    typeof(TimerTrigger),
                                                                                                    new FrameworkPropertyMetadata(1000.0)
                                                                                                    );

        public static readonly DependencyProperty TotalTicksProperty = DependencyProperty.Register("TotalTicks",
                                                                                                    typeof(int),
                                                                                                    typeof(TimerTrigger),
                                                                                                    new FrameworkPropertyMetadata(-1)
                                                                                                    );

        private ITickTimer timer;
        private EventArgs eventArgs;
        private int tickCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerTrigger"/> class.
        /// </summary>
        public TimerTrigger() :
            this(new DispatcherTickTimer())
        {
        }

        internal TimerTrigger(ITickTimer timer)
        {
            this.timer = timer;
        }


        /// <summary>
        /// Gets or sets the number of milliseconds to wait between ticks. This is a dependency property.
        /// </summary>
        public double MillisecondsPerTick
        {
            get { return (double)this.GetValue(MillisecondsPerTickProperty); }
            set { this.SetValue(MillisecondsPerTickProperty, value); }
        }

        /// <summary>
        /// Gets or sets the total number of ticks to be fired before the trigger is finished.  This is a dependency property.
        /// </summary>
        public int TotalTicks
        {
            get { return (int)this.GetValue(TotalTicksProperty); }
            set { this.SetValue(TotalTicksProperty, value); }
        }

        protected override void OnEvent(EventArgs eventArgs)
        {
            this.StopTimer();

            this.eventArgs = eventArgs;
            this.tickCount = 0;

            this.StartTimer();
        }

        protected override void OnDetaching()
        {
            this.StopTimer();

            base.OnDetaching();
        }

        internal void StartTimer()
        {
            if (this.timer != null)
            {
                this.timer.Interval = TimeSpan.FromMilliseconds(this.MillisecondsPerTick);
                this.timer.Tick += this.OnTimerTick;
                this.timer.Start();
            }
        }

        internal void StopTimer()
        {
            if (this.timer != null)
            {
                this.timer.Stop();
                this.timer.Tick -= this.OnTimerTick;
            }
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (this.TotalTicks > 0 && ++this.tickCount >= this.TotalTicks)
            {
                this.StopTimer();
            }

            this.InvokeActions(this.eventArgs);
        }

        internal class DispatcherTickTimer : ITickTimer
        {
            private DispatcherTimer dispatcherTimer;

            public DispatcherTickTimer()
            {
                this.dispatcherTimer = new DispatcherTimer();
            }

            public event EventHandler Tick
            {
                add { this.dispatcherTimer.Tick += value; }
                remove { this.dispatcherTimer.Tick -= value; }
            }

            public TimeSpan Interval
            {
                get { return this.dispatcherTimer.Interval; }
                set { this.dispatcherTimer.Interval = value; }
            }

            public void Start()
            {
                this.dispatcherTimer.Start();
            }

            public void Stop()
            {
                this.dispatcherTimer.Stop();
            }
        }
    }
}
