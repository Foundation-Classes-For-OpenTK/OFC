/*
 * 
 * Copyright 2019-2022 Robbyxp1 @ github.com
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
    /// track Bar control
    /// </summary>
    public class GLTrackBar: GLButtonBase
    {
        /// <summary> Callback when clicked on. Int is direction, -1 or +1 down/up </summary>
        public Action<GLBaseControl, int> ValueChanged { get; set; } = null;

        /// <summary> Bar outline color, ForeColor is inner part </summary>
        public Color BarOutline { get { return baroutline; } set { if (value != baroutline) { baroutline = value; Invalidate(); } } }
        /// <summary> Bar tick color, ForeColor is inner part </summary>
        public Color TickColor { get { return tickcolor; } set { if (value != tickcolor) { tickcolor = value; Invalidate(); } } }

        /// <summary> Bar size in percentage (0-1)</summary>
        public float BarSize { get { return barsize; } set { if (value != barsize) { barsize = value; Invalidate(); } } }
        /// <summary> Bar centre position in percentage (0-1) across the control.</summary>
        public float BarPosition { get { return barposition; } set { if (value != barposition) { barposition = value; Invalidate(); } } }

        /// <summary> Tick size in percentage (0-1)</summary>
        public float TickSize { get { return ticksize; } set { if (value != ticksize) { ticksize = value; Invalidate(); } } }
        /// <summary> Tick centre position in percentage (0-1) across the control.</summary>
        public float TickPosition { get { return tickposition; } set { if (value != tickposition) { tickposition = value; Invalidate(); } } }

        /// <summary> Needle size in percentage (0-1). The needle limits to the left/top ensuring its always on screen</summary>
        public float NeedleSize { get { return needlesize; } set { if (value != needlesize) { needlesize = value; Invalidate(); } } }

        /// <summary> Tick frequency. 0 means off </summary>
        public int TickFrequency { get { return tickfrequency; } set { if (value != tickfrequency && value >= 0) { tickfrequency = value; Invalidate(); } } }
        /// <summary> Is the track bar in horizonal mode? </summary>
        public bool HorizontalTrackbar { get { return horzmode; } set { if (value != horzmode) { horzmode = value; Invalidate(); } } }
        /// <summary> Return or set the thumb value, or set it. Does not call ValueChanged </summary>
        public int Value { get { return thumbvalue; } set { SetValues(value, maximum, minimum, largechange, smallchange); } }
        /// <summary> Maximum </summary>
        public int Maximum { get { return maximum; } set { SetValues(thumbvalue, value, minimum, largechange, smallchange); } }
        /// <summary> Minimum </summary>
        public int Minimum { get { return minimum; } set { SetValues(thumbvalue, maximum, value, largechange, smallchange); } }
        /// <summary> Amount clicking on the slider moves up or down, and defines the size of the page in effect.  </summary>
        public int LargeChange { get { return largechange; } set { SetValues(thumbvalue, maximum, minimum, value, smallchange); } }
        /// <summary> Amount the up or down buttons apply </summary>
        public int SmallChange { get { return smallchange; } set { SetValues(thumbvalue, maximum, minimum, largechange, value); } }
        /// <summary> Set values for value, maximum and large change in one call</summary>
        public void SetValueMaximumLargeChange(int v, int m, int lc) { SetValues(v, m, minimum, lc, smallchange); }
        
        /// <summary> Autosize is disabled on this control </summary>
        public new bool AutoSize { get { return false; } set { throw new System.NotImplementedException(); } }

        /// <summary> Constructor with name and bounds</summary>
        public GLTrackBar(string name, Rectangle location) : base(name, location)
        {
            Focusable = true;

            BackColor = DefaultButtonBackColor;
            ForeColor = DefaultTrackBarBarColor;
            FaceColorScaling = 1.0f;
            MouseOverColor = DefaultTrackMouseOverColor;
            MouseDownColor = DefaultTrackMouseDownColor;
            ButtonFaceColor = DefaultTrackBarTickColor;
            baroutline = Color.Gray.Multiply(0.8f);
            tickcolor = DefaultTrackBarBarColor;
            Padding = new PaddingType(2, 2, 2, 2);
            InvalidateOnFocusChange = true;
            ImageStretch = true;        // because you normally want the image to be matched to the control
        }

        /// <summary> Default Constructor </summary>
        public GLTrackBar() : this("TB?", DefaultWindowRectangle)
        {
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.Paint"/>
        protected override void Paint(Graphics gr)
        {
            //using (Brush b = new SolidBrush(Color.FromArgb(255,150,150,150)))  gr.FillRectangle(b, ClientRectangle); // debug

           Areas(out Rectangle bararea, out Rectangle tickmarkarea, out Rectangle needlearea, out float pixelscalar);

            using (Brush b = new LinearGradientBrush(bararea, ForeColor, ForeColor.Multiply(FaceColorScaling), 90))
                gr.FillRectangle(b, bararea);

            using (Pen p = new Pen(BarOutline))
                gr.DrawRectangle(p, bararea);

            if (tickfrequency > 0)
            {
                using (Pen p = new Pen(TickColor))
                {
                    if (horzmode)
                    {
                        float pos = tickmarkarea.Left;

                        for (int i = minimum; i < maximum; i += TickFrequency)
                        {
                            gr.DrawLine(p, new Point((int)pos, tickmarkarea.Top), new Point((int)pos, tickmarkarea.Bottom));
                            pos += (float)tickfrequency * pixelscalar;
                        }

                        gr.DrawLine(p, new Point(tickmarkarea.Right, tickmarkarea.Top), new Point(tickmarkarea.Right, tickmarkarea.Bottom));
                    }
                    else
                    {
                        float pos = tickmarkarea.Top;

                        for (int i = minimum; i < maximum; i += TickFrequency)
                        {
                            gr.DrawLine(p, new Point(tickmarkarea.Left,(int)pos), new Point(tickmarkarea.Right, (int)pos));
                            pos += (float)tickfrequency * pixelscalar;
                        }

                        gr.DrawLine(p, new Point(tickmarkarea.Left, tickmarkarea.Bottom), new Point(tickmarkarea.Right, tickmarkarea.Bottom));

                    }
                }
            }

            if (Image == null)
            {
                gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                GraphicsPath path = new GraphicsPath();
                if (horzmode)
                {
                    path.AddLines(new Point[] { new Point(needlearea.Left, needlearea.Top), new Point(needlearea.Right, needlearea.Top),
                                    new Point(needlearea.Right, needlearea.Top + needlearea.Height * sliderarrowpercentage / 16), new Point(needlearea.XCenter(), needlearea.Bottom) ,
                                    new Point(needlearea.Left, needlearea.Top + needlearea.Height * sliderarrowpercentage / 16) });
                }
                else
                {
                    path.AddLines(new Point[] {new Point(needlearea.Left, needlearea.Top), new Point(needlearea.Left + needlearea.Width * sliderarrowpercentage / 16, needlearea.Top) ,
                                    new Point(needlearea.Right, needlearea.YCenter()), new Point(needlearea.Left + needlearea.Width * sliderarrowpercentage / 16, needlearea.Bottom),
                                    new Point(needlearea.Left, needlearea.Bottom) });
                }

                path.CloseFigure();

                Color c = mousedrag ? MouseDownColor : mouseovertick ? MouseOverColor : ButtonFaceColor;
                using (Brush b = new LinearGradientBrush(needlearea, c, c.Multiply(FaceColorScaling), horzmode ? 90 : 0))
                {
                    gr.FillPath(b, path);
                }
            }
            else
            {
                gr.DrawImage(Image, needlearea);

            }

            if (ShowFocusBox && Focused)
            {
                using (var p = new Pen(MouseDownColor) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                {
                    Rectangle fr = needlearea;
                    fr.Inflate(1, 1);
                    gr.DrawRectangle(p, fr);
                }
            }
        }

        const int sliderarrowpercentage = 10;        // in 16ths
        const int sliderbaroffset = 7; // in 16th

        private void Areas(out Rectangle bararea, out Rectangle tickmarkarea, out Rectangle needlearea, out float pixelscalar )
        {
            Rectangle area = ClientRectangle;

            int basev = horzmode ? area.Height : area.Width;
            int pbarcentre = (int)(basev * barposition);
            int pbarsize = (int)(basev * barsize);
            int ptickcentre = (int)(basev * tickposition);
            int pticksize = (int)(basev * ticksize);
            int pshortneedlesize = (int)(basev * needlesize / 2.0f + 0.5f);
            int plongneedlesize = (int)(basev * needlesize);

            if (horzmode)
            {
                bararea = new Rectangle(area.Left, pbarcentre - pbarsize / 2, area.Width - 1, pbarsize);
                tickmarkarea = new Rectangle(bararea.Left + pshortneedlesize / 2, ptickcentre - pticksize / 2, bararea.Width - pshortneedlesize, pticksize);
                pixelscalar = (float)tickmarkarea.Width / (float)(maximum - minimum);
                if (Image == null)
                {
                    // note bumping needle area against 0
                    needlearea = new Rectangle((int)(tickmarkarea.Left + (thumbvalue-minimum) * pixelscalar) - pshortneedlesize / 2, Math.Max(0, pbarcentre - plongneedlesize * sliderbaroffset / 16), pshortneedlesize, plongneedlesize);
                }
                else
                {
                    if (ImageStretch == false)      // if no stretching, the needle size is the length of the image
                        plongneedlesize = Image.Height;  // if stretching, its at plongneedlesize as calculated by control

                    // image is placed in centre of bar, at plongneedlesize
                    needlearea = new Rectangle((int)(tickmarkarea.Left + (thumbvalue-minimum) * pixelscalar) - plongneedlesize / 2, Math.Max(0, pbarcentre - plongneedlesize /2), plongneedlesize, plongneedlesize);
                }
            }
            else
            {
                bararea = new Rectangle(pbarcentre - pbarsize / 2, area.Top, pbarsize, area.Height - 1);
                tickmarkarea = new Rectangle(ptickcentre - pticksize / 2, bararea.Top + pshortneedlesize / 2, pticksize, bararea.Height - pshortneedlesize);
                pixelscalar = (float)tickmarkarea.Height / (float)(maximum - minimum);

                if (Image == null)
                {
                    // note bumping needle area against 0
                    needlearea = new Rectangle(Math.Max(0, pbarcentre - plongneedlesize * sliderbaroffset / 16), (int)(tickmarkarea.Top + (thumbvalue-minimum) * pixelscalar) - pshortneedlesize / 2, plongneedlesize, pshortneedlesize);
                }
                else
                {
                    if (ImageStretch == false)      // if no stretching, the needle size is the length of the image
                        plongneedlesize = Image.Width;  // if stretching, its at plongneedlesize as calculated by control

                    // image is placed in centre of bar, at plongneedlesize
                    needlearea = new Rectangle(Math.Max(0, pbarcentre - plongneedlesize / 2), (int)(tickmarkarea.Top + (thumbvalue-minimum) * pixelscalar) - plongneedlesize / 2, plongneedlesize, plongneedlesize);
                }
            }
        }

        private void SetValues(int v, int max, int min, int lc, int sc)
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

            if (newthumbvalue != thumbvalue)        // if changed..
            {
                thumbvalue = newthumbvalue;
                iv = true;
            }

            if (iv)
            {
                Invalidate();
            }
        }

        private void MoveValue(int dir)
        {
            int newthumbvalue = Math.Min(Math.Max(thumbvalue+dir, minimum), maximum);
            if ( newthumbvalue != thumbvalue )
            {
                thumbvalue = newthumbvalue;
                OnValueChanged();
                Invalidate();
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseClick(GLMouseEventArgs)"/>
        protected override void OnMouseClick(GLMouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (!e.Handled)
            {
                if (!mousedrag)
                {
                    Areas(out Rectangle bararea, out Rectangle sliderarea, out Rectangle needlearea, out float pixelscalar);

                    int delta = horzmode ? (e.Location.X - needlearea.XCenter()) : (e.Location.Y - needlearea.YCenter());
                    int ticksize = horzmode ? needlearea.Width : needlearea.Height;

                    if (Math.Abs(delta) > ticksize / 2)
                    {
                        MoveValue(delta < 0 ? -LargeChange : LargeChange);
                    }
                }
            }
        }
        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseDown(GLMouseEventArgs)"/>
        protected override void OnMouseDown(GLMouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!e.Handled)
            {
                if (mouseovertick == true)
                {
                    mousedrag = true;
                }
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseMove(GLMouseEventArgs)"/>
        protected override void OnMouseMove(GLMouseEventArgs e)
        {
            base.OnMouseMove(e);

            Areas(out Rectangle bararea, out Rectangle sliderarea, out Rectangle needlearea, out float pixelscalar);

            if (mousedrag)
            {
                int offset = horzmode ? (e.Location.X - sliderarea.Left) : (e.Location.Y -sliderarea.Top);
                float scaledvalue = (float)offset / pixelscalar;
                MoveValue(minimum + (int)scaledvalue - thumbvalue); 
            }
            else
            {
                if (needlearea.Contains(e.Location))
                {
                    if (!mouseovertick)
                    {
                        mouseovertick = true;
                        Invalidate();
                    }
                }
                else
                {
                    if (mouseovertick)
                    {
                        mouseovertick = false;
                        Invalidate();
                    }
                }
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseUp(GLMouseEventArgs)"/>
        protected override void OnMouseUp(GLMouseEventArgs e)
        {
            base.OnMouseUp(e);
            if ( mousedrag )
            {
                mousedrag = false;
                Invalidate();
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnKeyDown(GLKeyEventArgs)"/>
        protected override void OnKeyDown(GLKeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!e.Handled)
            {
                //System.Diagnostics.Debug.WriteLine("KDown " + Name + " " + e.KeyCode);

                if ( horzmode ? e.KeyCode == System.Windows.Forms.Keys.Left : e.KeyCode == System.Windows.Forms.Keys.Up)
                {
                    MoveValue(-SmallChange);
                }
                else if (horzmode ? e.KeyCode == System.Windows.Forms.Keys.Right : e.KeyCode == System.Windows.Forms.Keys.Down)
                {
                    MoveValue(SmallChange);
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.PageUp)
                {
                    MoveValue(-LargeChange);
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.PageDown)
                {
                    MoveValue(LargeChange);
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.Home)
                {
                    MoveValue(minimum - thumbvalue);
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.End)
                {
                    MoveValue(maximum - thumbvalue);
                }

            }
        }

        /// <summary> Called when value changed, invokes call back </summary>
        protected void OnValueChanged()
        {
            ValueChanged?.Invoke(this, Value);
        }

        private float barposition = 0.25f;
        private float tickposition = 0.75f;
        private float barsize = 0.2f;
        private float ticksize = 0.20f;
        private float needlesize = 0.6f;

        private int maximum = 100;
        private int tickfrequency = 10;
        private int minimum = 0;
        private int largechange = 10;
        private int smallchange = 1;
        private int thumbvalue = 0;
        private Color baroutline;
        private Color tickcolor;
        private bool mouseovertick;
        private bool mousedrag;
        private bool horzmode = true;
    }
}

