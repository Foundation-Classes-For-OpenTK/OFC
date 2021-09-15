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
using System.Drawing;

namespace GLOFC.GL4.Controls
{
    public interface IControlAnimation
    {
        void Animate(GLBaseControl cs, ulong timems);
        Action<IControlAnimation, GLBaseControl, ulong> StartAction { get; set; }
        Action<IControlAnimation, GLBaseControl, ulong> FinishAction { get; set; }
    }

    // Build on this class to provide custom animations. Implement Start, Middle, End

    public abstract class AnimateTimeBase : IControlAnimation
    {
        public ulong StartTime { get; set; }
        public ulong EndTime { get; set; }

        public Action<IControlAnimation, GLBaseControl, ulong> StartAction { get; set; }
        public Action<IControlAnimation, GLBaseControl, ulong> FinishAction { get; set; }

        public enum StateType { Waiting, Running, Done };
        public StateType State = StateType.Waiting;

        public AnimateTimeBase(ulong startime, ulong endtime)
        {
            StartTime = startime; EndTime = endtime;
        }

        protected abstract void Start(GLBaseControl cs);
        protected abstract void Middle(GLBaseControl cs,double delta);
        protected abstract void End(GLBaseControl cs);

        public void Animate(GLBaseControl cs, ulong timems)
        {
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
                    FinishAction?.Invoke(this, cs, timems);
                    State = StateType.Done;
                }
                else
                {
                    Middle(cs,deltain);
                }
            }
        }
    }

}
