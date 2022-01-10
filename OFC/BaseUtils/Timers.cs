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

namespace GLOFC.Timers
{
    
    // timer is thread safe

    public class Timer : IDisposable
    {
        public bool Running { get; private set; } = false;
        public Action<Timer, long> Tick { get; set; } = null;
        public Object Tag { get; set; } = null;

        public Timer()
        {
        }

        public Timer(int initialdelayms, Action<Timer, long> tickaction, int repeatdelay = 0)
        {
            Tick = tickaction;
            Start(initialdelayms, repeatdelay);
        }

        public Timer(int initialdelayms, int repeatdelay = 0)
        {
            Start(initialdelayms, repeatdelay);
        }

        public void FireNow()
        {
            Start(0);
        }

        public void Start(int initialdelayms, int repeatdelay = 0)  // can call repeatedly on same timer, just resets the time
        {
            if (!mastertimer.IsRunning)
                mastertimer.Start();

            Stop();

            recurringtickdelta = Stopwatch.Frequency * repeatdelay / 1000;

            long timeout = mastertimer.ElapsedTicks + (Stopwatch.Frequency * initialdelayms / 1000);

            this.Running = true;

            lock (timerlist)
            {
                timerlist.Add(timeout, this);
            }

            //System.Diagnostics.Debug.WriteLine("Start timer");
        }

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

        public static void ProcessTimers()      // Someone needs to call this..
        {
            lock (timerlist)
            {
                long timenow = mastertimer.ElapsedTicks;

                while (timerlist.Count > 0 && timerlist.Keys[0] < timenow)     // for all timers which have ticked out
                {
                    long tickout = timerlist.Keys[0];   // remember its tick

                    Timer t = timerlist.Values[0];      // get the timer

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

        public void Dispose()
        {
            Stop();
        }

        long recurringtickdelta;

        static SortedList<long,Timer> timerlist = new SortedList<long,Timer>();
        static Stopwatch mastertimer = new Stopwatch();

    }
}
