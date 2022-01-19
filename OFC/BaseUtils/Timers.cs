/*
 * Copyright © 2019-2021 Robbyxp1 @ github.com
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 * 
 * 
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GLOFC.Utils
{
    /// <summary>
    /// A polled timer, handling as many timers as required.
    /// Used in GL Controls, and you must call PolledTimer.ProcessTimers() in your system tick to make the timers work.
    /// </summary>

    public class PolledTimer : IDisposable
    {
        /// <summary> Is Timer running? </summary>
        public bool Running { get; private set; } = false;
        /// <summary> Timer callback </summary>
        public Action<PolledTimer, long> Tick { get; set; } = null;
        /// <summary> Timer tag </summary>
        public object Tag { get; set; } = null;

        /// <summary> Time tick in ms </summary>
        public ulong Time { get { return (ulong)mastertimer.ElapsedMilliseconds; } }

        /// <summary> Time tick in ns </summary>
        public ulong TimeNs { get { return (ulong)mastertimer.ElapsedTicks / (ulong)(Stopwatch.Frequency / 1000000); } }

        /// <summary> Constructor </summary>
        public PolledTimer()
        {
        }

        /// <summary>
        /// Create and start a timer
        /// </summary>
        /// <param name="initialdelayms">Initial delay in ms</param>
        /// <param name="tickaction">Timer action</param>
        /// <param name="repeatdelay">Repeat delay, 0 means no repeat</param>
        public PolledTimer(int initialdelayms, Action<PolledTimer, long> tickaction, int repeatdelay = 0)
        {
            Tick = tickaction;
            Start(initialdelayms, repeatdelay);
        }

        /// <summary>
        /// Start a timer
        /// Can call repeatedly on same timer during the period, just resets the time and starts the timer from now.
        /// </summary>
        /// <param name="initialdelayms">Initial delay in ms</param>
        /// <param name="repeatdelay">Repeat delay, 0 means no repeat</param>

        public void Start(int initialdelayms, int repeatdelay = 0)  // 
        {
            Stop();

            recurringtickdelta = Stopwatch.Frequency * repeatdelay / 1000;

            long timeout = mastertimer.ElapsedTicks + Stopwatch.Frequency * initialdelayms / 1000;

            Running = true;

            lock (timerlist)
            {
                timerlist.Add(timeout, this);
            }

            //System.Diagnostics.Debug.WriteLine("Start timer");
        }

        /// <summary>
        /// Stop the timer.
        /// </summary>
        public void Stop()
        {
            lock (timerlist)
            {
                int i = timerlist.IndexOfValue(this);
                if (i >= 0)
                {
                    timerlist.RemoveAt(i);
                    Running = false;
                    //  System.Diagnostics.Debug.WriteLine("Stop timer");
                }
            }
        }

        /// <summary> Fire now the timer </summary>
        public void FireNow()
        {
            Start(0);
        }

        /// <summary>
        /// Static call to process and fire timers, call in your main loop
        /// </summary>
        public static void ProcessTimers()      // Someone needs to call this..
        {
            lock (timerlist)
            {
                long timenow = mastertimer.ElapsedTicks;

                while (timerlist.Count > 0 && timerlist.Keys[0] < timenow)     // for all timers which have ticked out
                {
                    long tickout = timerlist.Keys[0];   // remember its tick

                    PolledTimer t = timerlist.Values[0];      // get the timer

                    //System.Diagnostics.Debug.WriteLine("Remove timer " );
                    timerlist.RemoveAt(0);          // remove from list, must be first

                    t.Tick?.Invoke(t, mastertimer.ElapsedMilliseconds);   // fire event

                    if (t.recurringtickdelta > 0)       // add back if recurring
                    {
                        timerlist.Add(tickout + t.recurringtickdelta, t);     // add back to list
                    }
                    else
                        t.Running = false;              // timer expired, not running
                }
            }
        }

        /// <summary>
        /// Dispose of timer.
        /// </summary>

        public void Dispose()
        {
            Stop();
        }

        private long recurringtickdelta;

        private static SortedList<long, PolledTimer> timerlist = new SortedList<long, PolledTimer>();

        private static Stopwatch mastertimer = new Stopwatch();
        static PolledTimer()
        {
            mastertimer.Start();    // create master timer
        }


    }
}
