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

namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// Animator to move control
    /// </summary>

    public class AnimateTranslate : AnimateTimeBase
    {
        /// <summary> Target point to go to</summary>
        public Point Target { get; set; }
        /// <summary> Beginning point. May be null which indicates animation has not started and will animate from current position.</summary>
        private Point? Begin { get; set; }

        /// <summary>
        /// Construct a translate animation
        /// </summary>
        /// <param name="starttime">Start time</param>
        /// <param name="endtime">End Time</param>
        /// <param name="deltatime">Indicate if start/end is delta time from next tick</param>
        /// <param name="target">Target point to go</param>
        /// <param name="begin">Optional beginning point. If null, animate from current control position</param>
        /// <param name="removeafterend">Remove animation from control at end</param>
        public AnimateTranslate(ulong starttime, ulong endtime, bool deltatime, Point target, Point? begin = null, bool removeafterend = false) : base(starttime, endtime, deltatime, removeafterend)
        {
            Target = target;
            Begin = begin;
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.AnimateTimeBase.Start(GLBaseControl)"/>
        protected override void Start(GLBaseControl cs)
        {
            if ( Begin == null)
                Begin = cs.Location;
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.AnimateTimeBase.Middle(GLBaseControl, double)"/>
        protected override void Middle(GLBaseControl cs, double delta)
        {
            var p = new Point((int)(Begin.Value.X + (double)(Target.X - Begin.Value.X) * delta), (int)(Begin.Value.Y + (double)(Target.Y - Begin.Value.Y) * delta));
            //System.Diagnostics.Debug.WriteLine("Animate {0} to pos {1}", cs.Name, p);
            if (cs.Dock != GLBaseControl.DockingType.None)
                cs.Dock = GLBaseControl.DockingType.None;
            cs.Location = p;
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.AnimateTimeBase.End(GLBaseControl)"/>
        protected override void End(GLBaseControl cs)
        {
            cs.Location = Target;
        }
    }

    /// <summary>
    /// Animator for size
    /// </summary>
    public class AnimateSize : AnimateTimeBase
    {
        /// <summary> Target size to go to</summary>
        public Size Target { get; set; }
        /// <summary> Beginning size. May be null which indicates animation has not started and will animate from current size.</summary>
        public Size? Begin { get; set; } = null;

        /// <summary>
        /// Construct a translate animation
        /// </summary>
        /// <param name="starttime">Start time</param>
        /// <param name="endtime">End Time</param>
        /// <param name="deltatime">Indicate if start/end is delta time from next tick</param>
        /// <param name="target">Target size to go to</param>
        /// <param name="begin">Optional beginning size. If null, animate from current control size</param>
        /// <param name="removeafterend">Remove animation from control at end</param>
        public AnimateSize(ulong starttime, ulong endtime, bool deltatime, Size target, Size? begin = null, bool removeafterend = false) : base(starttime, endtime, deltatime, removeafterend)
        {
            Target = target;
            Begin = begin;
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.AnimateTimeBase.Start(GLBaseControl)"/>
        protected override void Start(GLBaseControl cs)
        {
            if ( Begin == null)
                Begin = cs.Size;
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.AnimateTimeBase.Middle(GLBaseControl, double)"/>
        protected override void Middle(GLBaseControl cs, double delta)
        {
            var s = new Size((int)(Begin.Value.Width + (double)(Target.Width - Begin.Value.Width) * delta), (int)(Begin.Value.Width + (double)(Target.Width - Begin.Value.Width) * delta));
            //System.Diagnostics.Debug.WriteLine("Animate {0} to size {1}", cs.Name, s);
            if (cs.Dock != GLBaseControl.DockingType.None)
                cs.Dock = GLBaseControl.DockingType.None;
            cs.Size = s;
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.AnimateTimeBase.End(GLBaseControl)"/>
        protected override void End(GLBaseControl cs)
        {
            cs.Size = Target;
        }
    }

    /// <summary>
    /// Animator for ScaleWindow. Top level controls only.
    /// </summary>
    public class AnimateScale : AnimateTimeBase
    {
        /// <summary> Target scale to go to</summary>
        public SizeF Target { get; set; }
        /// <summary> Beginning scale. May be null which indicates animation has not started and will animate from current ScaleWindow.</summary>
        public SizeF? Begin { get; set; } = null;

        /// <summary>
        /// Construct scale animation
        /// </summary>
        /// <param name="starttime">Start time</param>
        /// <param name="endtime">End Time</param>
        /// <param name="deltatime">Indicate if start/end is delta time from next tick</param>
        /// <param name="target">Target scale to go to</param>
        /// <param name="begin">Optional beginning scale. If null, animate from current control scale</param>
        /// <param name="removeafterend">Remove animation from control at end</param>
        public AnimateScale(ulong starttime, ulong endtime, bool deltatime, SizeF target, SizeF? begin = null, bool removeafterend = false) : base(starttime, endtime,deltatime, removeafterend)
        {
            Target = target;
            Begin = begin;
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.AnimateTimeBase.Start(GLBaseControl)"/>
        protected override void Start(GLBaseControl cs)
        {
            if (Begin == null)
                Begin = cs.ScaleWindow ?? new SizeF(1, 1);
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.AnimateTimeBase.Middle(GLBaseControl, double)"/>
        protected override void Middle(GLBaseControl cs, double delta)
        {
            var s = new SizeF(Begin.Value.Width + (Target.Width - Begin.Value.Width) * (float)delta,
                              Begin.Value.Height + (Target.Height - Begin.Value.Height) * (float)delta);

            //System.Diagnostics.Debug.WriteLine("Animate {0} to scale {1}", cs.Name, s);
            cs.ScaleWindow = s;
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.AnimateTimeBase.End(GLBaseControl)"/>
        protected override void End(GLBaseControl cs)
        {
            cs.ScaleWindow = Target;
        }
    }
    /// <summary>
    /// Animator for Opacity. Top level controls only.
    /// </summary>
    public class AnimateOpacity : AnimateTimeBase
    {
        /// <summary> Target opacity to go to</summary>
        public float Target { get; set; }
        /// <summary> Beginning opacity. May be null which indicates animation has not started and will animate from current opacity.</summary>
        public float? Begin { get; set; } = null;

        /// <summary>
        /// Construct opacity animation
        /// </summary>
        /// <param name="starttime">Start time</param>
        /// <param name="endtime">End Time</param>
        /// <param name="deltatime">Indicate if start/end is delta time from next tick</param>
        /// <param name="target">Target opacity to go to</param>
        /// <param name="begin">Optional beginning opacity. If null, animate from current control opacity</param>
        /// <param name="removeafterend">Remove animation from control at end</param>
        public AnimateOpacity(ulong starttime, ulong endtime, bool deltatime, float target, float? begin = null, bool removeafterend = false) : base(starttime, endtime, deltatime, removeafterend)
        {
            Target = target;
            Begin = begin;
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.AnimateTimeBase.Start(GLBaseControl)"/>
        protected override void Start(GLBaseControl cs)
        {
            if (Begin == null)
                Begin = cs.Opacity;
            cs.Opacity = Begin.Value;
            //System.Diagnostics.Debug.WriteLine("Animate {0} begin {1}", cs.Name, cs.Opacity);
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.AnimateTimeBase.Middle(GLBaseControl, double)"/>
        protected override void Middle(GLBaseControl cs, double delta)
        {
            var s = Begin.Value + (Target - Begin.Value) * (float)delta;

            cs.Opacity = s;
//            System.Diagnostics.Debug.WriteLine("Animate {0} to opacity {1}", cs.Name, cs.Opacity);
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.AnimateTimeBase.End(GLBaseControl)"/>
        protected override void End(GLBaseControl cs)
        {
            cs.Opacity = Target;
         //   System.Diagnostics.Debug.WriteLine("Animate {0} final {1}", cs.Name, cs.Opacity);
        }
    }

}
