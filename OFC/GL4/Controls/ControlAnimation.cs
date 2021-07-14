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

using System.Drawing;

namespace OFC.GL4.Controls
{
    public interface IControlAnimation
    {
        void Animate(GLBaseControl cs, ulong timems);
    }

    public abstract class AnimateTimeBase : IControlAnimation
    {
        public ulong StartTime { get; set; }
        public ulong EndTime { get; set; }

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
            }

            if (State == StateType.Running)
            {
                ulong elapsed = timems - StartTime;
                ulong timetomove = EndTime - StartTime;
                double deltain = (double)elapsed / (double)timetomove;      // % in, 0-1

                if (deltain >= 1.0)
                {
                    End(cs);
                    State = StateType.Done;
                }
                else
                {
                    Middle(cs,deltain);
                }
            }
        }
    }

    public class AnimateMove : AnimateTimeBase
    {
        public Point Target { get; set; }
        private Point beginpoint;

        public AnimateMove(ulong starttime, ulong endtime, Point target) : base(starttime, endtime)
        {
            Target = target;
        }

        protected override void Start(GLBaseControl cs)
        {
            beginpoint = cs.Location;
        }

        protected override void Middle(GLBaseControl cs, double delta)
        {
            var p = new Point((int)(beginpoint.X + (double)(Target.X - beginpoint.X) * delta), (int)(beginpoint.Y + (double)(Target.Y - beginpoint.Y) * delta));
            System.Diagnostics.Debug.WriteLine("Animate {0} to pos {1}", cs.Name, p);
            if (cs.Dock != DockingType.None)
                cs.Dock = DockingType.None;
            cs.Location = p;
        }

        protected override void End(GLBaseControl cs)
        {
            cs.Location = Target;
        }
    }

    public class AnimateSize : AnimateTimeBase
    {
        public Size Target { get; set; }
        private Size beginsize;

        public AnimateSize(ulong starttime, ulong endtime, Size target) : base(starttime, endtime)
        {
            Target = target;
        }

        protected override void Start(GLBaseControl cs)
        {
            beginsize = cs.Size;
        }

        protected override void Middle(GLBaseControl cs, double delta)
        {
            var s = new Size((int)(beginsize.Width + (double)(Target.Width - beginsize.Width) * delta), (int)(beginsize.Width + (double)(Target.Width - beginsize.Width) * delta));
            System.Diagnostics.Debug.WriteLine("Animate {0} to size {1}", cs.Name, s);
            if (cs.Dock != DockingType.None)
                cs.Dock = DockingType.None;
            cs.Size = s;
        }

        protected override void End(GLBaseControl cs)
        {
            cs.Size = Target;
        }
    }

    public class AnimateAlternatePos : AnimateTimeBase
    {
        public RectangleF Target { get; set; }
        private RectangleF begin;

        public AnimateAlternatePos(ulong starttime, ulong endtime, RectangleF target) : base(starttime, endtime)
        {
            Target = target ;
        }

        protected override void Start(GLBaseControl cs)
        {
            begin = cs.AlternatePos != null ? cs.AlternatePos.Value : new RectangleF(cs.Bounds.Left,cs.Bounds.Top,cs.Bounds.Width,cs.Bounds.Height);
        }

        protected override void Middle(GLBaseControl cs, double delta)
        {
            var s = new RectangleF((float)(begin.Left + (Target.Left - begin.Left) * delta), (float)(begin.Top + (Target.Top - begin.Top) * delta),
                                    (float)(begin.Width + (Target.Width - begin.Width) * delta),(float)(begin.Height + (Target.Height - begin.Height) * delta));

            System.Diagnostics.Debug.WriteLine("Animate {0} to altpos {1}", cs.Name, s);
            cs.AlternatePos = s;
        }

        protected override void End(GLBaseControl cs)
        {
            cs.AlternatePos = Target;
        }
    }

}
