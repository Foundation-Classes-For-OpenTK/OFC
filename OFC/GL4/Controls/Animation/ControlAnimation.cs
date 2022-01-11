/*
 * Copyright 2019-2021 Robbyxp1 @ github.com
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
 */

using System;
#pragma warning disable 1591

namespace GLOFC.GL4.Controls
{
    public interface IControlAnimation
    {
        void Animate(GLBaseControl cs, ulong timems);       // return true to keep, false to delete animator
        Action<IControlAnimation, GLBaseControl, ulong> StartAction { get; set; }       // execute after setup
        Action<IControlAnimation, GLBaseControl, ulong> FinishAction { get; set; }      // execute after removal/end
    }

    // Build on this class to provide custom animations. Implement Start, Middle, End

    public abstract class AnimateTimeBase : IControlAnimation
    {
        public ulong StartTime { get; set; }    // after first tick, absolute time, before can be delta or absolute
        public ulong EndTime { get; set; }      // ditto
        public bool DeltaTime { get; set; }     // if set, StartTime/EndTime are deltas from the first tick
        public bool RemoveAfterEnd { get; set; } = false;   // remove from control at end point

        public Action<IControlAnimation, GLBaseControl, ulong> StartAction { get; set; }
        public Action<IControlAnimation, GLBaseControl, ulong> FinishAction { get; set; }

        public enum StateType { Waiting, Running, Done };
        public StateType State = StateType.Waiting;

        public AnimateTimeBase(ulong startime, ulong endtime, bool deltatime, bool removeafterend = false)
        {
            StartTime = startime; EndTime = endtime; DeltaTime = deltatime; RemoveAfterEnd = removeafterend;
        }

        protected abstract void Start(GLBaseControl cs);
        protected abstract void Middle(GLBaseControl cs,double delta);
        protected abstract void End(GLBaseControl cs);

        public void Animate(GLBaseControl cs, ulong timems)
        {
            if ( DeltaTime )                // if we were set up with delta times, then set the starttime/end time as moved on from timems
            {
                StartTime += timems;
                EndTime += timems;
                DeltaTime = false;
            }

            if (State == StateType.Waiting && timems >= StartTime)
            {
                State = StateType.Running;
                Start(cs);
                StartAction?.Invoke(this, cs, timems);
            }

            if (State == StateType.Running)
            {
                ulong elapsed = timems - StartTime;
                ulong timetomove = EndTime - StartTime;
                double deltain = (double)elapsed / (double)timetomove;      // % in, 0-1

                if (deltain >= 1.0)
                {
                    End(cs);

                    if ( RemoveAfterEnd)
                    {
                        cs.Animators.Remove(this);
                    }

                    State = StateType.Done;
                    FinishAction?.Invoke(this, cs, timems);
                }
                else
                {
                    Middle(cs,deltain);
                }
            }
        }
    }

}
