﻿/*
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
    public class AnimateTranslate : AnimateTimeBase
    {
        public Point Target { get; set; }
        private Point? Begin { get; set; }

        public AnimateTranslate(ulong starttime, ulong endtime, Point target, Point? begin = null) : base(starttime, endtime)
        {
            Target = target;
            Begin = begin;
        }

        protected override void Start(GLBaseControl cs)
        {
            if ( Begin == null)
                Begin = cs.Location;
        }

        protected override void Middle(GLBaseControl cs, double delta)
        {
            var p = new Point((int)(Begin.Value.X + (double)(Target.X - Begin.Value.X) * delta), (int)(Begin.Value.Y + (double)(Target.Y - Begin.Value.Y) * delta));
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
        public Size? Begin { get; set; } = null;

        public AnimateSize(ulong starttime, ulong endtime, Size target, Size? begin = null) : base(starttime, endtime)
        {
            Target = target;
            Begin = begin;
        }

        protected override void Start(GLBaseControl cs)
        {
            if ( Begin == null)
                Begin = cs.Size;
        }

        protected override void Middle(GLBaseControl cs, double delta)
        {
            var s = new Size((int)(Begin.Value.Width + (double)(Target.Width - Begin.Value.Width) * delta), (int)(Begin.Value.Width + (double)(Target.Width - Begin.Value.Width) * delta));
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

    public class AnimateScale : AnimateTimeBase
    {
        public SizeF Target { get; set; }
        public SizeF? Begin { get; set; } = null;

        public AnimateScale(ulong starttime, ulong endtime, SizeF target, SizeF? begin = null) : base(starttime, endtime)
        {
            Target = target;
            Begin = begin;
        }

        protected override void Start(GLBaseControl cs)
        {
            if ( Begin == null )
                Begin = cs.ScaleWindow ?? new SizeF(1, 1);
        }

        protected override void Middle(GLBaseControl cs, double delta)
        {
            var s = new SizeF(Begin.Value.Width + (Target.Width - Begin.Value.Width) * (float)delta,
                              Begin.Value.Height + (Target.Height - Begin.Value.Height) * (float)delta);

            System.Diagnostics.Debug.WriteLine("Animate {0} to scale {1}", cs.Name, s);
            cs.ScaleWindow = s;
        }

        protected override void End(GLBaseControl cs)
        {
            cs.ScaleWindow = Target;
        }
    }

}
