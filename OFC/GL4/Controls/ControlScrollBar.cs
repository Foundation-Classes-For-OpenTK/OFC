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
    public struct ScrollEventArgs
    {
        public int NewValue { get; set; }
        public int OldValue { get; }
        public ScrollEventArgs(int oldv, int newv) { NewValue = newv; OldValue = oldv; }
    }

    public class GLScrollBar : GLBaseControl
    {
        public Action<GLScrollBar, ScrollEventArgs> Scroll { get; set; } = null;

        public int Value { get { return thumbvalue; } set { SetValues(value, maximum, minimum, largechange, smallchange); } }
        public int ValueLimited { get { return thumbvalue; } set { SetValues(value, maximum, minimum, largechange, smallchange, true); } }
        public int Maximum { get { return maximum; } set { SetValues(thumbvalue, value, minimum, largechange, smallchange); } }
        public int Minimum { get { return minimum; } set { SetValues(thumbvalue, maximum, value, largechange, smallchange); } }
        public int LargeChange { get { return largechange; } set { SetValues(thumbvalue, maximum, minimum, value, smallchange); } }
        public int SmallChange { get { return smallchange; } set { SetValues(thumbvalue, maximum, minimum, largechange, value); } }
        public void SetValueMaximum(int v, int m) { SetValues(v, m, minimum, largechange, smallchange); }
        public void SetValueMaximumLargeChange(int v, int m, int lc) { SetValues(v, m, minimum, lc, smallchange); }
        public void SetValueMaximumMinimum(int v, int max, int min) { SetValues(v, max, min, largechange, smallchange); }
        public bool HideScrollBar { get; set; } = false;                   // hide if no scroll needed
        public bool IsScrollBarOn { get { return thumbenable; } }           // is it on?

        public bool HorizontalScroll { get; set; } = false;             // set for horizonal orientation

        public Color ArrowColor { get { return arrowcolor; } set { arrowcolor = value; Invalidate(); } }       // of text
        public Color SliderColor { get { return slidercolor; } set { slidercolor = value; Invalidate(); } }

        public Color ArrowButtonColor { get { return arrowButtonColor; } set { arrowButtonColor = value; Invalidate(); } }
        public Color ArrowBorderColor { get { return arrowBorderColor; } set { arrowBorderColor = value; Invalidate(); } }
        public float ArrowDecreaseDrawAngle { get { return arrowUpDrawAngle; } set { arrowUpDrawAngle = value; Invalidate(); } }
        public float ArrowIncreaseDrawAngle { get { return arrowDownDrawAngle; } set { arrowDownDrawAngle = value; Invalidate(); } }
        public float ArrowColorScaling { get { return arrowColorScaling; } set { arrowColorScaling = value; Invalidate(); } }

        public Color MouseOverButtonColor { get { return mouseOverColor; } set { mouseOverColor = value; Invalidate(); } }
        public Color MousePressedButtonColor { get { return mouseDownColor; } set { mouseDownColor = value; Invalidate(); } }
        public Color ThumbButtonColor { get { return thumbButtonColor; } set { thumbButtonColor = value; Invalidate(); } }
        public Color ThumbBorderColor { get { return thumbBorderColor; } set { thumbBorderColor = value; Invalidate(); } }
        public float ThumbColorScaling { get { return thumbColorScaling; } set { thumbColorScaling = value; Invalidate(); } }
        public float ThumbDrawAngle { get { return thumbDrawAngle; } set { thumbDrawAngle = value; Invalidate(); } }

        public new bool AutoSize { get { return false; } set { throw new NotImplementedException(); } }

        public GLScrollBar(string name, Rectangle pos, int min, int max) : base(name, pos)
        {
            thumbvalue = minimum = min;
            maximum = max;
            BorderColorNI = DefaultScrollbarBorderColor;
            BackColorGradientAltNI = BackColorNI = DefaultScrollbarBackColor;
        }

        public GLScrollBar(string name = "SB?") : this(name, DefaultWindowRectangle, 0, 100)
        {
        }

        protected override void Paint(Graphics gr)
        {
            using (Brush br = new SolidBrush(this.SliderColor))
                gr.FillRectangle(br, new Rectangle(sliderarea.Left, sliderarea.Top, sliderarea.Width, sliderarea.Height));

            DrawButton(gr, new Rectangle(decreasebuttonarea.Left, decreasebuttonarea.Top, decreasebuttonarea.Width, decreasebuttonarea.Height), MouseOver.MouseOverDecrease);
            DrawButton(gr, new Rectangle(increasebuttonarea.Left, increasebuttonarea.Top, increasebuttonarea.Width, increasebuttonarea.Height), MouseOver.MouseOverIncrease);
            DrawButton(gr, new Rectangle(thumbbuttonarea.Left, thumbbuttonarea.Top, thumbbuttonarea.Width, thumbbuttonarea.Height), MouseOver.MouseOverThumb);
        }

        private void DrawButton(Graphics g, Rectangle rect, MouseOver but)
        {
            if (rect.Height < 4 || rect.Width < 4)
                return;

            Color c1, c2;
            float angle;
            bool isthumb = but == MouseOver.MouseOverThumb;

            if (isthumb)
            {
                if (!thumbenable)
                    return;

                c1 = (mousepressed == but) ? MousePressedButtonColor : ((mouseover == but) ? MouseOverButtonColor : ThumbButtonColor);
                c2 = c1.Multiply(ThumbColorScaling);
                angle = ThumbDrawAngle;
            }
            else
            {
                c1 = (mousepressed == but) ? MousePressedButtonColor : ((mouseover == but) ? MouseOverButtonColor : ArrowButtonColor);
                c2 = c1.Multiply(ArrowColorScaling);
                angle = (but == MouseOver.MouseOverDecrease) ? ArrowDecreaseDrawAngle : ArrowIncreaseDrawAngle;
            }

            using (Brush bbck = new System.Drawing.Drawing2D.LinearGradientBrush(rect, c1, c2, angle))
                g.FillRectangle(bbck, rect);

            if (Enabled && thumbenable && !isthumb)
            {
                using (Pen p2 = new Pen(ArrowColor))
                {
                    int hoffset = rect.Width / 3;
                    int voffset = rect.Height / 3;

                    Point arrowpt1inc, arrowpt2inc, arrowpt3inc, arrowpt1dec, arrowpt2dec, arrowpt3dec;

                    if (HorizontalScroll)
                    {
                        arrowpt1inc = new Point(rect.X + hoffset, rect.Y + voffset);
                        arrowpt2inc = new Point(rect.Right - hoffset, rect.Y + rect.Height / 2);
                        arrowpt3inc = new Point(rect.X + hoffset, rect.Bottom - voffset);

                        arrowpt1dec = new Point(arrowpt2inc.X, arrowpt1inc.Y);
                        arrowpt2dec = new Point(arrowpt1inc.X, arrowpt2inc.Y);
                        arrowpt3dec = new Point(arrowpt2inc.X, arrowpt3inc.Y);
                    }
                    else
                    {
                        arrowpt1inc = new Point(rect.X + hoffset, rect.Y + voffset);
                        arrowpt2inc = new Point(rect.X + rect.Width / 2, rect.Bottom - voffset);
                        arrowpt3inc = new Point(rect.Right - hoffset, arrowpt1inc.Y);

                        arrowpt1dec = new Point(arrowpt1inc.X, arrowpt2inc.Y);
                        arrowpt2dec = new Point(arrowpt2inc.X, arrowpt1inc.Y);
                        arrowpt3dec = new Point(arrowpt3inc.X, arrowpt2inc.Y);
                    }

                    //System.Diagnostics.Debug.WriteLine("{0} {1} {2} r{3} ", arrowpt1inc, arrowpt2inc, arrowpt3inc, rect);

                    if (but == MouseOver.MouseOverIncrease)
                    {
                        g.DrawLine(p2, arrowpt1inc, arrowpt2inc);            // the arrow!
                        g.DrawLine(p2, arrowpt2inc, arrowpt3inc);
                    }
                    else
                    {
                        g.DrawLine(p2, arrowpt1dec, arrowpt2dec);            // the arrow!
                        g.DrawLine(p2, arrowpt2dec, arrowpt3dec);
                    }
                }
            }

            if (but == mouseover || isthumb)
            {
                using (Pen p = new Pen(isthumb ? ThumbBorderColor : ArrowBorderColor))
                {
                    Rectangle border = rect;
                    border.Width--; border.Height--;
                    g.DrawRectangle(p, border);
                }
            }
        }

        protected override void OnMouseMove(GLMouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (!Enabled || !thumbenable)
                return;

            if (thumbmove)                        // if moving thumb, we calculate where we are in value
            {
                int offset, sliderrangepx;
                if (HorizontalScroll)
                {
                    offset = e.Location.X - (sliderarea.X + thumbmovecaptureoffset);
                    sliderrangepx = sliderarea.Width - thumbbuttonarea.Width;      // range of values to represent Min-Max.
                }
                else
                {
                    offset = e.Location.Y - (sliderarea.Y + thumbmovecaptureoffset);
                    sliderrangepx = sliderarea.Height - thumbbuttonarea.Height;      // range of values to represent Min-Max.
                }

                offset = Math.Min(Math.Max(offset, 0), sliderrangepx);        // bound within slider range
                float percent = (float)offset / (float)sliderrangepx;         // % in
                int newthumbvalue = minimum + (int)((float)(UserMaximum - minimum) * percent);  // thumb value

                //System.Diagnostics.Debug.WriteLine("Slider px" + offset + " over " + sliderrangepx + " to value " + newthumbvalue);

                if (newthumbvalue != thumbvalue)        // and if changed, apply it.
                {
                    thumbvalue = newthumbvalue;
                    OnScroll(new ScrollEventArgs(thumbvalue, newthumbvalue));
                    CalculateThumb();
                    Invalidate();
                }
            }
            else if (decreasebuttonarea.Contains(e.Location))
            {
                if (mouseover != MouseOver.MouseOverDecrease)
                {
                    mouseover = MouseOver.MouseOverDecrease;
                    Invalidate();
                }
            }
            else if (increasebuttonarea.Contains(e.Location))
            {
                if (mouseover != MouseOver.MouseOverIncrease)
                {
                    mouseover = MouseOver.MouseOverIncrease;
                    Invalidate();
                }
            }
            else if (thumbbuttonarea.Contains(e.Location))
            {
                if (mouseover != MouseOver.MouseOverThumb)
                {
                    mouseover = MouseOver.MouseOverThumb;
                    Invalidate();
                }
            }
            else if (mouseover != MouseOver.MouseOverNone)
            {
                mouseover = MouseOver.MouseOverNone;
                Invalidate();
            }
        }

        protected override void OnMouseDown(GLMouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (!Enabled || !thumbenable)
                return;

            if (decreasebuttonarea.Contains(e.Location))
            {
                mousepressed = MouseOver.MouseOverDecrease;
                Invalidate();
                MoveThumb(-smallchange);
            }
            else if (increasebuttonarea.Contains(e.Location))
            {
                mousepressed = MouseOver.MouseOverIncrease;
                Invalidate();
                MoveThumb(smallchange);
            }
            else if (thumbbuttonarea.Contains(e.Location))
            {
                mousepressed = MouseOver.MouseOverThumb;
                Invalidate();
                thumbmove = true;                           // and mouseover should be on as well
                thumbmovecaptureoffset = HorizontalScroll ? (e.Location.X - thumbbuttonarea.X) : (e.Location.Y - thumbbuttonarea.Y);      // pixels down the thumb when captured..
                                                                                                                                          // System.Diagnostics.Debug.WriteLine("Thumb captured at " + thumbmovecaptureoffset);
            }
            else if (sliderarea.Contains(e.Location))      // slider, but not thumb..
            {
                bool decdir = HorizontalScroll ? (e.Location.X < thumbbuttonarea.X) : (e.Location.Y < thumbbuttonarea.Y);
                MoveThumb(decdir ? -largechange : largechange);
            }
        }

        protected override void OnMouseUp(GLMouseEventArgs e)
        {
            if (mousepressed != MouseOver.MouseOverNone)
            {
                mousepressed = MouseOver.MouseOverNone;
                Invalidate();
            }

            if (thumbmove)
            {
                thumbmove = false;
                Invalidate();
            }

            //repeatclick.Stop();

            base.OnMouseUp(e);
        }


        protected override void OnMouseLeave(GLMouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (!thumbmove && mouseover != MouseOver.MouseOverNone)
            {
                mouseover = MouseOver.MouseOverNone;
                Invalidate();
            }
        }

        protected override void OnMouseWheel(GLMouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (e.Delta < 0)
                MoveThumb(smallchange);
            else
                MoveThumb(-smallchange);
        }

        protected override void PerformRecursiveLayout()       // do this in recursive layout, better than Layout as it gets called by an update to any parameters
        {
            base.PerformRecursiveLayout();

            //System.Diagnostics.Debug.WriteLine("Scroll Layout " + Name + " " + ClientRectangle);

            sliderarea = ClientRectangle;

            if (HorizontalScroll)
            {
                int buttonsize = sliderarea.Height;
                if (buttonsize * 2 > sliderarea.Width / 3)  // don't take up too much of the slider with the buttons
                    buttonsize = sliderarea.Width / 6;

                decreasebuttonarea = sliderarea;
                decreasebuttonarea.Width = buttonsize;
                increasebuttonarea = sliderarea;
                increasebuttonarea.X = sliderarea.Right - buttonsize;
                increasebuttonarea.Width = buttonsize;
                sliderarea.X += buttonsize;
                sliderarea.Width -= 2 * buttonsize;
            }
            else
            {
                int buttonsize = sliderarea.Width;
                if (buttonsize * 2 > sliderarea.Height / 3)  // don't take up too much of the slider with the buttons
                    buttonsize = sliderarea.Height / 6;

                decreasebuttonarea = sliderarea;
                decreasebuttonarea.Height = buttonsize;
                increasebuttonarea = sliderarea;
                increasebuttonarea.Y = sliderarea.Bottom - buttonsize;
                increasebuttonarea.Height = buttonsize;
                sliderarea.Y += buttonsize;
                sliderarea.Height -= 2 * buttonsize;
            }

            CalculateThumb();
        }

        private void CalculateThumb()
        {
            int userrange = maximum - minimum + 1;           // number of positions..

            if (largechange < userrange)                   // largerange is less than number of individual positions
            {
                int useablearea = HorizontalScroll ? sliderarea.Width : sliderarea.Height;
                int minthumbsize = HorizontalScroll ? sliderarea.Height : sliderarea.Width;

                int thumbsize = (int)(((float)largechange / (float)userrange) * useablearea);   // calculate a thumsize
                if (thumbsize < minthumbsize)             // too small, adjust
                    thumbsize = minthumbsize;

                int sliderrangev = UserMaximum - minimum;       // Usermaximum will be > minimum, due to above < test.
                int lthumb = Math.Min(thumbvalue, UserMaximum);         // values beyond User maximum screened out

                float fposition = (float)(lthumb - minimum) / (float)sliderrangev;

                int sliderrangepx = useablearea - thumbsize;      // range of values to represent Min-Max.
                int thumboffsetpx = (int)((float)sliderrangepx * fposition);
                thumboffsetpx = Math.Min(thumboffsetpx, sliderrangepx);     // LIMIT, because we can go over slider range if value=maximum

                thumbbuttonarea = HorizontalScroll ? new Rectangle(sliderarea.X + thumboffsetpx, sliderarea.Y, thumbsize, sliderarea.Height) :
                                new Rectangle(sliderarea.X, sliderarea.Y + thumboffsetpx, sliderarea.Width, thumbsize);

                thumbenable = true;
            }
            else
            {
                thumbenable = false;                        // else disable the thumb and scroll bar
                thumbmove = false;
                mouseover = MouseOver.MouseOverNone;
                mousepressed = MouseOver.MouseOverNone;
            }
        }

        private void MoveThumb(int vchange)
        {
            int oldvalue = thumbvalue;

            if (vchange < 0 && thumbvalue > minimum)
            {
                thumbvalue += vchange;
                thumbvalue = Math.Max(thumbvalue, minimum);
                OnScroll(new ScrollEventArgs(oldvalue, Value));
                CalculateThumb();
                Invalidate();
            }
            else if (vchange > 0 && thumbvalue < UserMaximum)
            {
                thumbvalue += vchange;
                thumbvalue = Math.Min(thumbvalue, UserMaximum);
                OnScroll(new ScrollEventArgs(oldvalue, Value));
                CalculateThumb();
                Invalidate();
            }

            //Console.WriteLine("Slider is " + thumbvalue + " from " + minimum + " to " + maximum);
        }


        private void SetValues(int v, int max, int min, int lc, int sc, bool limittousermax = false)   // this allows it to be set to maximum..
        {
            //System.Diagnostics.Debug.WriteLine("Set Scroll " + v + " min " + min + " max " + max + " lc "+ lc + " sc "+ sc + " Usermax "+ UserMaximum);
            smallchange = sc;                                   // has no effect on display of control
            bool iv = false;

            if (max != maximum || min != minimum || lc != largechange) // these do..
            {           // only invalidate if actually changed something
                maximum = max;
                minimum = min;
                largechange = lc;
                iv = true;
            }

            int newthumbvalue = Math.Min(Math.Max(v, minimum), maximum);

            if (limittousermax)
                newthumbvalue = Math.Min(newthumbvalue, UserMaximum);

            if (newthumbvalue != thumbvalue)        // if changed..
            {
                thumbvalue = newthumbvalue;
                iv = true;
            }

            if (iv)
            {
                CalculateThumb();
                Invalidate();
            }
        }

        private int UserMaximum { get { return Math.Max(maximum - largechange + 1, minimum); } }    // make sure it does not go below minimum whatever largechange is set to.

        protected virtual void OnScroll(ScrollEventArgs se)
        {
            Scroll?.Invoke(this, se);
        }


        private Color slidercolor { get; set; } = DefaultScrollbarSliderColor;
        private Color arrowcolor { get; set; } = DefaultScrollbarArrowColor;
        private Color arrowButtonColor { get; set; } = DefaultScrollbarArrowButtonFaceColor;
        private Color arrowBorderColor { get; set; } = DefaultScrollbarArrowButtonBorderColor;
        private float arrowUpDrawAngle { get; set; } = 90F;
        private float arrowDownDrawAngle { get; set; } = 270F;
        private float arrowColorScaling { get; set; } = 0.5F;

        private Color mouseOverColor { get; set; } = DefaultScrollbarMouseOverColor;
        private Color mouseDownColor { get; set; } = DefaultScrollbarMouseDownColor;
        private Color thumbButtonColor { get; set; } = DefaultScrollbarThumbColor;
        private Color thumbBorderColor { get; set; } = DefaultScrollbarThumbBorderColor;
        private float thumbColorScaling { get; set; } = 0.5F;
        private float thumbDrawAngle { get; set; } = 0F;

        private Rectangle sliderarea;
        private Rectangle decreasebuttonarea;
        private Rectangle increasebuttonarea;
        private Rectangle thumbbuttonarea;

        private int maximum = 100;
        private int minimum = 0;
        private int largechange = 10;
        private int smallchange = 1;
        private int thumbvalue = 0;
        private bool thumbenable = true;
        private bool thumbmove = false;

        enum MouseOver { MouseOverNone, MouseOverDecrease, MouseOverIncrease, MouseOverThumb };
        private MouseOver mouseover = MouseOver.MouseOverNone;
        private MouseOver mousepressed = MouseOver.MouseOverNone;
        private int thumbmovecaptureoffset = 0;     // px down the thumb when captured..
    }

    public class GLHorizontalScrollBar : GLScrollBar
    {
        public GLHorizontalScrollBar(string name, Rectangle pos, int min, int max) : base(name, pos, min, max)
        {
            HorizontalScroll = true;
        }

        public GLHorizontalScrollBar(string name = "SBH?") : base(name)
        {
            HorizontalScroll = true;
        }

    }
    public class GLVerticalScrollBar : GLScrollBar
    {
        public GLVerticalScrollBar(string name, Rectangle pos, int min, int max) : base(name, pos, min, max)
        {
            HorizontalScroll = false;
        }

        public GLVerticalScrollBar(string name = "SBV?") : base(name)
        {
            HorizontalScroll = false;
        }

    }
}

