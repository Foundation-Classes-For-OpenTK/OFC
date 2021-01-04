/*
 * Copyright 2019-2020 Robbyxp1 @ github.com
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
using System.Drawing.Drawing2D;

namespace OFC.GL4.Controls
{
    public class GLUpDownControl: GLButtonBase
    {
        public Action<GLBaseControl, int> Clicked { get; set; } = null;           // int holds the direction

        public float MouseSelectedColorScaling { get { return mouseSelectedColorScaling; } set { mouseSelectedColorScaling = value; Invalidate(); } }
        public int UpDownInitialDelay { get; set; } = 500;
        public int UpDownRepeatRate { get; set; } = 200;

        public GLUpDownControl(string name, Rectangle location) : base(name, location)
        {
            Focusable = true;
            InvalidateOnFocusChange = true;
            repeattimer.Tick += RepeatClick;
            BackColor = DefaultButtonBackColor;
        }

        public GLUpDownControl() : this("UD?", DefaultWindowRectangle)
        {
        }

        public override void PerformRecursiveLayout()
        {
            base.PerformRecursiveLayout();
        }

        // called after the background of the panel has been drawn - so it will be clear to write.

        protected override void Paint(Rectangle area, Graphics gr)
        {
            if (ShowFocusBox)
            {
                if (Focused)
                {
                    using (var p = new Pen(MouseDownBackColor) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
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

            Color pcup = (Enabled) ? ((mousedown == MouseOver.MouseOverUp) ? MouseDownBackColor : ((mouseover == MouseOver.MouseOverUp) ? MouseOverBackColor : this.BackColor)) : this.BackColor.Multiply(DisabledScaling);
            using (Brush b = new LinearGradientBrush(sareaupper, pcup, pcup.Multiply(BackColorScaling), 90))
                gr.FillRectangle(b, drawupper);

            Color pcdown = (Enabled) ? ((mousedown == MouseOver.MouseOverDown) ? MouseDownBackColor : ((mouseover == MouseOver.MouseOverDown) ? MouseOverBackColor : this.BackColor)) : this.BackColor.Multiply(DisabledScaling);
            using (Brush b = new LinearGradientBrush(sarealower, pcdown, pcdown.Multiply(BackColorScaling), 270))
                gr.FillRectangle(b, drawlower);

            Color pencolorup = Enabled ? (mousedown == MouseOver.MouseOverUp ? ForeColor.Multiply(MouseSelectedColorScaling) : ForeColor) : ForeColor.Multiply(DisabledScaling);
            Color pencolordown = Enabled ? (mousedown == MouseOver.MouseOverDown ? ForeColor.Multiply(MouseSelectedColorScaling) : ForeColor) : ForeColor.Multiply(DisabledScaling);

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

        public override void OnMouseMove(GLMouseEventArgs eventargs)
        {
            base.OnMouseMove(eventargs);

            if (!eventargs.Handled)
            {
                int halfway = ClientRectangle.Height / 2;
                if ( eventargs.Location.Y < halfway)
                { 
                    if (mouseover != MouseOver.MouseOverUp)
                    {
                        mouseover = MouseOver.MouseOverUp;
                        Invalidate();
                    }
                }
                else if (eventargs.Location.Y > halfway)
                {
                    if (mouseover != MouseOver.MouseOverDown)
                    {
                        mouseover = MouseOver.MouseOverDown;
                        Invalidate();
                    }
                }
                else if (mouseover != MouseOver.MouseOverNone)
                {
                    mouseover = MouseOver.MouseOverNone;
                    Invalidate();
                }
            }
        }

        public override void OnMouseLeave(GLMouseEventArgs e)
        {
            base.OnMouseLeave(e);
            mouseover = MouseOver.MouseOverNone;
            mousedown = MouseOver.MouseOverNone;
            Invalidate();
        }

        public override void OnMouseDown(GLMouseEventArgs mevent)
        {
            base.OnMouseDown(mevent);

            if (!mevent.Handled)
            {
                int halfway = ClientRectangle.Height / 2;
                if (mevent.Location.Y < halfway)
                {
                    mousedown = MouseOver.MouseOverUp;
                    Invalidate();
                    OnClicked(1);
                    StartRepeatClick(1);
                }
                else if (mevent.Location.Y > halfway)
                {
                    mousedown = MouseOver.MouseOverDown;
                    Invalidate();
                    OnClicked(-1);
                    StartRepeatClick(-1);
                }
            }
        }

        public override void OnMouseUp(GLMouseEventArgs mevent)
        {
            base.OnMouseUp(mevent);
            mousedown = MouseOver.MouseOverNone;
            repeattimer.Stop();
            Invalidate();
        }

        protected virtual void OnClicked(int dir)
        {
            Clicked?.Invoke(this,dir);
        }

        private void StartRepeatClick(int dir)
        {
            if (!repeattimer.Running)
            {
                saveddir = dir;
                repeattimer.Start(UpDownInitialDelay, UpDownRepeatRate);
            }
        }

        private void RepeatClick(Timers.Timer t, long timeout)
        {
            OnClicked(saveddir);
        }

        // tbd animate this, maybe also button click..
        public override void OnKeyDown(GLKeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!e.Handled)
            {
                //System.Diagnostics.Debug.WriteLine("KDown " + Name + " " + e.KeyCode);

                if (e.KeyCode == System.Windows.Forms.Keys.Up)
                {
                    OnClicked(1);
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.Down)
                {
                    OnClicked(-1);
                }
            }
        }


        enum MouseOver { MouseOverUp, MouseOverDown, MouseOverNone };
        private MouseOver mouseover = MouseOver.MouseOverNone;
        private MouseOver mousedown = MouseOver.MouseOverNone;
        private float mouseSelectedColorScaling { get; set; } = 1.5F;
        private OFC.Timers.Timer repeattimer = new Timers.Timer();
        private int saveddir;


    }
}

