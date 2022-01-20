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

namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// Interface for Animators
    /// </summary>
    public interface IGLControlAnimation
    {
        /// <summary> Perform animation on control at this time.</summary>
        void Animate(GLBaseControl cs, ulong timems);       
        /// <summary> Called at start of animation </summary>
        Action<IGLControlAnimation, GLBaseControl, ulong> StartAction { get; set; } 
        /// <summary> Called on removal/end of animation</summary>
        Action<IGLControlAnimation, GLBaseControl, ulong> FinishAction { get; set; }
    }

    /// <summary>
    /// Animation Time Base class to provide most of the animation control logic
    /// </summary>
    public abstract class GLControlAnimateTimeBase : IGLControlAnimation
    {
        /// <summary> Start time of animation. 
        /// After the first tick it is absolute time, before can be delta from now, or absolute
        /// </summary>
        public ulong StartTime { get; set; }
        /// <summary> End of time of animation
        /// After the first tick it is absolute time, before can be delta from now, or absolute
        /// </summary>
        public ulong EndTime { get; set; }   
        /// <summary> Indicate if Start/End time is deltas from now, or absolute time (false). Cleared on first tick when start/end time becomes absolute</summary>
        public bool DeltaTime { get; set; }     
        /// <summary> Remove animation after execution from control </summary>
        public bool RemoveAfterEnd { get; set; } = false;

        /// <summary> Callback, called on start animation with animator, control and current time </summary>
        public Action<IGLControlAnimation, GLBaseControl, ulong> StartAction { get; set; }
        /// <summary> Callback, called on end animation with animator, control and current time</summary>
        public Action<IGLControlAnimation, GLBaseControl, ulong> FinishAction { get; set; }

        /// <summary> Animation state</summary>
        public enum StateType {
            /// <summary> Waiting to run </summary>
            Waiting,
            /// <summary> Running</summary>
            Running,
            /// <summary> Animation done</summary>
            Done
        };

        /// <summary> Animation state</summary>
        public StateType State = StateType.Waiting;

        /// <summary> Construct an Animator </summary>
        /// <param name="startime">Start time, either absolute or delta from next tick</param>
        /// <param name="endtime">End time, either absolute or delta from next tick</param>
        /// <param name="deltatime">Delta time indicator, true if times are delta from next tick</param>
        /// <param name="removeafterend">True to remove animator from control at end of animation</param>
        public GLControlAnimateTimeBase(ulong startime, ulong endtime, bool deltatime, bool removeafterend = false)
        {
            StartTime = startime; EndTime = endtime; DeltaTime = deltatime; RemoveAfterEnd = removeafterend;
        }

        /// <summary> Restart the animation. May be done in FinishAction if required.</summary>
        /// <param name="startime">Start time, either absolute or delta from next tick</param>
        /// <param name="endtime">End time, either absolute or delta from next tick</param>
        /// <param name="deltatime">Delta time indicator, true if times are delta from next tick</param>
        public void Restart(ulong startime, ulong endtime, bool deltatime)
        {
            StartTime = startime; EndTime = endtime; DeltaTime = deltatime;
            State = StateType.Waiting;
        }

        // must be protected, not private protected, as we want other assemblies to be able to implement a version of this

        /// <summary> Start animation call. Used by animation system internally. </summary>
        protected abstract void Start(GLBaseControl cs);
        /// <summary> In middle of animation call. delta is from 0 to 1. Used by animation system internally. </summary>
        protected abstract void Middle(GLBaseControl cs,double delta);
        /// <summary> At end of animation call. Used by animation system internally. </summary>
        protected abstract void End(GLBaseControl cs);

        /// <summary> Perform animation on control at this time in ms. Called internally by Control.Animate
        /// Animation is performed on a system tick by calling GLControlDisplay.Animate(timestamp);
        /// </summary>
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
