/*
 * 
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

using GLOFC.Utils;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// Up down control
    /// </summary>
    public class GLUpDownControl: GLButtonBase
    {
        /// <summary> Callback when clicked on. Int is direction, -1 or +1 down/up </summary>
        public Action<GLBaseControl, int> Clicked { get; set; } = null;
        /// <summary> Fore color scaling when held down to show its held </summary>
        public float MouseSelectedColorScaling { get { return mouseSelectedColorScaling; } set { mouseSelectedColorScaling = value; Invalidate(); } }
        /// <summary> Initial delay in ms before auto repeat </summary>
        public int UpDownInitialDelay { get; set; } = 500;
        /// <summary> Repeat rate in ms when held down </summary>
        public int UpDownRepeatRate { get; set; } = 200;
        /// <summary> Autosize is disabled on this control </summary>
        public new bool AutoSize { get { return false; } set { throw new System.NotImplementedException(); } }

        /// <summary> Constructor with name and bounds</summary>
        public GLUpDownControl(string name, Rectangle location) : base(name, location)
        {
            Focusable = true;

            InvalidateOnFocusChange = true;
            InvalidateOnEnterLeave = true;
            InvalidateOnMouseDownUp = true;
            InvalidateOnMouseMove = true;

            repeattimer.Tick += RepeatClick;
            amitimer.Tick += AmiTick;
            BackColor = DefaultButtonBackColor;
        }

        /// <summary> Empty constructor </summary>
        public GLUpDownControl() : this("UD?", DefaultWindowRectangle)
        {
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.Paint"/>
        protected override void Paint(Graphics gr)
        {
            Rectangle area = ClientRectangle;

            if (ShowFocusBox)
            {
                if (Focused)
                {
                    using (var p = new Pen(MouseDownColor) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                    {
                        gr.DrawRectangle(p, new Rectangle(area.Left, area.Top, area.Width - 1, area.Height - 1));
                    }
                }
                area.Inflate(-1, -1);
            }

            int halfway = area.Height / 2;

            Rectangle drawupper = new Rectangle(area.Left , area.Top, area.Width, halfway-1);  
            Rectangle drawlower = new Rectangle(area.Left , area.Top + halfway, area.Width, halfway-1);  
            Rectangle sareaupper = drawupper;
            sareaupper.Height++;
            Rectangle sarealower = drawlower;
            sarealower.Height++;

            Color pcup, pcdown, pencolorup, pencolordown;
            pcup = pcdown = Enabled ? BackColor : BackColor.Multiply(BackDisabledScaling);
            pencolordown = pencolorup = Enabled ? ForeColor : ForeColor.Multiply(ForeDisabledScaling);
            // System.Diagnostics.Debug.WriteLine("Colours " + pcup + " " + pencolordown);

            //System.Diagnostics.Debug.WriteLine("Ami running" + amitimer.Running);

            if ( Enabled && (Hover || amitimer.Running) )
            {
                bool mbd = MouseButtonsDown == GLMouseEventArgs.MouseButtons.Left || amitimer.Running;
                Color back = mbd ? MouseDownColor : MouseOverColor;
                Color fore = mbd ? ForeColor.Multiply(MouseSelectedColorScaling) : ForeColor;

                if (amitimer.Running ? (repeatdir==-1) : mouseoverbottom)           // if ami, we use savedir, else mouse pos
                {
                    pcdown = back;
                    pencolordown = fore;
                }
                else
                {
                    pcup = back;
                    pencolorup = fore; 
                }
            }

            using (Brush b = new LinearGradientBrush(sareaupper, pcup, pcup.Multiply(FaceColorScaling), 90))
                gr.FillRectangle(b, drawupper);

            using (Brush b = new LinearGradientBrush(sarealower, pcdown, pcdown.Multiply(FaceColorScaling), 270))
                gr.FillRectangle(b, drawlower);

            using (Pen p = new Pen(pencolorup))
            {
                int hoffset = drawupper.Width / 3;
                int voffset = drawupper.Height / 3;

                Point arrowpt1u = new Point(drawupper.X + hoffset, drawupper.Y + drawupper.Height - voffset);
                Point arrowpt2u = new Point(drawupper.X + drawupper.Width / 2, drawupper.Y + voffset);
                Point arrowpt3u = new Point(drawupper.X + drawupper.Width - hoffset, arrowpt1u.Y);
                gr.DrawLine(p, arrowpt1u, arrowpt2u);            // the arrow!
                gr.DrawLine(p, arrowpt2u, arrowpt3u);
            }

            using (Pen p = new Pen(pencolordown))
            {
                int hoffset = drawlower.Width / 3;
                int voffset = drawlower.Height / 3;

                Point arrowpt1d = new Point(drawlower.X + hoffset, drawlower.Y + voffset);
                Point arrowpt2d = new Point(drawlower.X + drawlower.Width / 2, drawlower.Y + drawlower.Height - voffset);
                Point arrowpt3d = new Point(drawlower.X + drawlower.Width - hoffset, arrowpt1d.Y);

                gr.DrawLine(p, arrowpt1d, arrowpt2d);            // the arrow!
                gr.DrawLine(p, arrowpt2d, arrowpt3d);

            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseDown(GLMouseEventArgs)"/>
        protected override void OnMouseDown(GLMouseEventArgs e)
        {
            base.OnMouseDown(e);
            if ( !e.Handled)
            {
                int halfway = ClientRectangle.Height / 2;
                int dir = e.Location.Y > halfway ? -1 : 1;
                OnClicked(dir);
                if (!repeattimer.Running)
                {
                    repeatdir = dir;
                    repeattimer.Start(UpDownInitialDelay, UpDownRepeatRate);
                }
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseMove(GLMouseEventArgs)"/>
        protected override void OnMouseMove(GLMouseEventArgs e)
        {
            base.OnMouseMove(e);
            mouseoverbottom = (e.Location.Y >= Height / 2);
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseUp(GLMouseEventArgs)"/>
        protected override void OnMouseUp(GLMouseEventArgs e)
        {
            base.OnMouseUp(e);
            repeattimer.Stop();
        }

        private void OnClicked(int dir)
        {
            Clicked?.Invoke(this,dir);
        }

        private void RepeatClick(PolledTimer t, long timeout)
        {
            OnClicked(repeatdir);
        }

        private void AmiTick(PolledTimer t, long timeout)
        {
            //System.Diagnostics.Debug.WriteLine("Ami stop");
            Invalidate();       // make it repaint without it being amimated
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnKeyDown(GLKeyEventArgs)"/>
        protected override void OnKeyDown(GLKeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!e.Handled)
            {
                //System.Diagnostics.Debug.WriteLine("KDown " + Name + " " + e.KeyCode);

                if (e.KeyCode == System.Windows.Forms.Keys.Up)
                {
                    repeatdir = 1;
                    OnClicked(1);
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.Down)
                {
                    repeatdir = -1;
                    OnClicked(-1);
                }

                //System.Diagnostics.Debug.WriteLine("Ami start");
                amitimer.Start(100);
                Invalidate();
            }
        }

        private float mouseSelectedColorScaling { get; set; } = 1.5F;
        private PolledTimer repeattimer = new PolledTimer();
        private PolledTimer amitimer = new PolledTimer();
        private int repeatdir;
        private bool mouseoverbottom;
    }
}

