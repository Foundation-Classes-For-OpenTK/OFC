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

using GLOFC.Utils;
using System;
using System.Drawing;
using System.Globalization;

namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// A calendar control, single date selection
    /// </summary>

    public class GLCalendar : GLButtonBase
    {
        /// <summary> Callback when value has changed </summary>
        public Action<GLBaseControl> ValueChanged { get; set; } = null;   // Not fired by programatic Value

        /// <summary> Callback when an unrecognised key is pressed</summary>
        public Action<GLBaseControl, GLKeyEventArgs> OtherKeyPressed { get; set; } = null;     // not fired by programatically

        /// <summary> Date value selected. Default is local date </summary>
        public DateTime Value { get { return datetimevalue; } set { datetimevalue = value; Invalidate(); } }

        /// <summary> The left calendar button, for theming purposes </summary>
        public GLButton ButLeft { get; set; } = new GLButton();
        /// <summary> The right calendar button, for theming purposes </summary>
        public GLButton ButRight { get; set; } = new GLButton();

        /// <summary> Culture of calendar. Default is CurrentCulture </summary>
        public CultureInfo Culture { get { return culture; } set { culture = value; Invalidate(); } }

        /// <summary> Construct with name and bounds </summary>
        public GLCalendar(string name, Rectangle location) : base(name, location)
        {
            Focusable = true;
            InvalidateOnFocusChange = true;
            foreColor = DefaultCalendarForeColor;
            BackColorNI = BackColorGradientAltNI = DefaultCalendarBackColor;

            ButLeft.Symbol = GLButtonTextBase.SymbolType.LeftTriangle;
            ButLeft.Click += GoLeft;
            ButLeft.Dock = DockingType.TopLeft;
            ButLeft.Size = new Size(24, 24);
            ButLeft.Name = "CalLeft";
            ButLeft.GiveFocusToParent = true;
            ButRight.Symbol = GLButtonTextBase.SymbolType.RightTriangle;
            ButRight.Click += GoRight;
            ButRight.Dock = DockingType.TopRight;
            ButRight.Size = new Size(24, 24);
            ButRight.Name = "CalRight";
            ButRight.GiveFocusToParent= true;

            Add(ButLeft);
            Add(ButRight);

            datetimecursor = datetimevalue = DateTime.Now;
        }

        /// <summary> Default Constructor </summary>
        public GLCalendar() : this("But?", DefaultWindowRectangle)
        {
        }

        #region Implementation

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.SizeControl(Size)"/>
        protected override void SizeControl(Size parentsize)
        {
            base.SizeControl(parentsize);
            if (AutoSize)
            {
                string daynames = string.Join("xx", Culture.DateTimeFormat.AbbreviatedDayNames);        // by far the largest horz width, two xx allow for extra space around each name
                int width = (int)GLOFC.Utils.BitMapHelpers.MeasureStringInBitmap(daynames, Font).Width;
                int height = 9 * Font.Height;       // 9 seems to give the right proportions
                SetNI(size: new Size(width,height));
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.Paint(Graphics)"/>
        protected override void Paint(Graphics gr)
        {
            string titletext = "";
            string[] titles = null;
            int curdateoffset = 0;
            int curselectedoffset = 0;
            int focuspos = 0;
            DateTime timenow = DateTime.Now;

            if (mode == Mode.Day)
            {
                titletext = datetimecursor.ToString("MMMM yyyy", Culture);
                int monthdays = DateTime.DaysInMonth(datetimecursor.Year, datetimecursor.Month);
                titles = new string[7];
                for (int i = 0; i < titles.Length; i++)
                    titles[i] = Culture.DateTimeFormat.GetAbbreviatedDayName((DayOfWeek)((i + 1) % 7));
                selectiontext = new string[monthdays];
                for (int i = 0; i < selectiontext.Length; i++)
                    selectiontext[i] = (i+1).ToStringInvariant();
                gridstartoffset = (int)new DateTime(datetimecursor.Year, datetimecursor.Month, 1).DayOfWeek;      // sunday = 0
                gridstartoffset = (gridstartoffset + 6) % 7;        // shift so monday = 0
                gridxacross = 7;
                gridydown = (gridstartoffset + monthdays - 1) / 7 + 1;
                curdateoffset = datetimecursor.Month == timenow.Month && datetimecursor.Year == timenow.Year ? timenow.Day - 1 : -1;
                curselectedoffset = datetimevalue.Month == datetimecursor.Month && datetimevalue.Year == datetimecursor.Year ? datetimevalue.Day - 1 : -1;
                focuspos = datetimecursor.Day - 1;
            }
            else if (mode == Mode.Month)
            {
                titletext = datetimecursor.Year.ToStringInvariant();
                selectiontext = new string[12];
                for (int i = 0; i < selectiontext.Length; i++)
                    selectiontext[i] = Culture.DateTimeFormat.GetAbbreviatedMonthName(i + 1);
                gridstartoffset = 0;
                gridxacross = 4;
                gridydown = 3;
                curdateoffset = datetimecursor.Year == timenow.Year ? timenow.Month - 1 : -1;
                curselectedoffset = datetimecursor.Year == datetimevalue.Year ? datetimevalue.Month - 1 : -1;
                focuspos = datetimecursor.Month - 1;
            }
            else if ( mode == Mode.Decade )
            {
                int startdecade = (datetimecursor.Year / 10)*10;
                titletext = startdecade.ToStringInvariant() + " - " + (startdecade + 9).ToStringInvariant();
                selectiontext = new string[12];
                for (int i = 0; i < selectiontext.Length; i++)
                    selectiontext[i] = (startdecade -1 + i).ToStringInvariant();
                gridstartoffset = 0;
                gridxacross = 4;
                gridydown = 3;
                curdateoffset = timenow.Year >= startdecade - 1 && timenow.Year <= startdecade + 10 ? timenow.Year - startdecade + 1: -1;
                curselectedoffset = datetimecursor.Year >= startdecade - 1 && datetimecursor.Year <= startdecade + 10 ? datetimevalue.Year - startdecade + 1 : -1;
                focuspos = datetimecursor.Year - startdecade + 1;
            }

            using (Brush textb = new SolidBrush((Enabled) ? this.ForeColor : this.ForeColor.Multiply(ForeDisabledScaling)))
            {
                using (var fmt = ControlHelpersStaticFunc.StringFormatFromContentAlignment(ContentAlignment.MiddleCenter))
                {
                    int margin = Font.Height / 4;

                    Rectangle titlearea = new Rectangle(0, margin, ClientWidth, Font.Height);
                    gr.DrawString(titletext, this.Font, textb, titlearea, fmt);

                    int vpos = ButLeft.Height + margin*2;

                    int cellwidth = (Width - margin * 2) / gridxacross;
                    gridxleft = (Width - cellwidth * gridxacross) / 2;

                    if (titles != null)
                    {
                        for (int i = 0; i < titles.Length; i++)
                            gr.DrawString(titles[i], this.Font, textb, new Rectangle(i * cellwidth + gridxleft, vpos, cellwidth, Font.Height), fmt);

                        vpos+= Font.Height;
                    }

                    gridystart = vpos;       // offset pixels
                    int cellheight = (ClientHeight- vpos) / gridydown;

                    if (hoveredpos >= 0)
                        hoveredpos = HoveringOver(hoverpoint);

                    int xoff = gridstartoffset;
                    for (int i = 0; i < selectiontext.Length; i++)
                    {
                        if ( xoff == gridxacross )
                        {
                            vpos += cellheight;
                            xoff = 0;
                        }

                        Rectangle butarea = new Rectangle(xoff++ * cellwidth + gridxleft , vpos, cellwidth, cellheight);
                        Rectangle focusrect = butarea;

                        if (i == focuspos)
                            butarea.Inflate(-2, -2);

                        if (i == curselectedoffset && Enabled)
                        {
                            using (Brush mouseover = new SolidBrush(MouseDownColor))
                            {
                                gr.FillRectangle(mouseover, butarea);
                            }
                        }
                        else if (i == hoveredpos && Enabled)
                        {
                            using (Brush mouseover = new SolidBrush(MouseOverColor))
                            {
                                gr.FillRectangle(mouseover, butarea);
                            }
                        }

                        if ( i == curdateoffset )
                        {
                            using (Pen outline = new Pen(MouseDownColor))
                            {
                                gr.DrawRectangle(outline, butarea);
                            }
                        }

                        if ( i == focuspos && ShowFocusBox && Focused)
                        {
                            using (var p = new Pen(MouseDownColor) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                            {
                                gr.DrawRectangle(p, focusrect);
                            }
                        }


                        gr.DrawString(selectiontext[i], this.Font, textb, butarea, fmt);
                    }
                }
            }
        }

        private void GoLeft(GLBaseControl c, GLMouseEventArgs e)
        {
            if (mode == Mode.Day)
                datetimecursor = datetimecursor.SafeAddMonths(-1);
            else if (mode == Mode.Month)
                datetimecursor = datetimecursor.SafeAddYears(-1);
            else if (mode == Mode.Decade)
                datetimecursor = datetimecursor.SafeAddYears(-10);
            Invalidate();
        }

        private void GoRight(GLBaseControl c, GLMouseEventArgs e)
        {
            if (mode == Mode.Day)
                datetimecursor = datetimecursor.SafeAddMonths(1);
            else if (mode == Mode.Month)
                datetimecursor = datetimecursor.SafeAddYears(1);
            else if (mode == Mode.Decade)
                datetimecursor = datetimecursor.SafeAddYears(10);
            Invalidate();
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseClick(GLMouseEventArgs)"/>
        protected override void OnMouseClick(GLMouseEventArgs e)
        {
            base.OnMouseClick(e);
            int hover = HoveringOver(e.Location);
            if (hover >= 0)
            {
                ClickOn(hover+1);
            }
            else
            {
                Rectangle p = new Rectangle(ButLeft.Width, 0, Width - ButLeft.Width - ButRight.Width, Font.Height);
                if ( p.Contains(e.Location))
                {
                    if (mode != Mode.Decade)
                        mode = mode + 1;
                    Invalidate();
                }
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseMove(GLMouseEventArgs)"/>
        protected override void OnMouseMove(GLMouseEventArgs e)
        {
            base.OnMouseMove(e);
            int hover = HoveringOver(e.Location);
            if (hover >= 0)
            {
                if (hoveredpos != hover)
                {
                    hoveredpos = hover;
                    hoverpoint = e.Location;
                    Invalidate();
                }
            }
            else if (hoveredpos >= 0)
            {
                hoveredpos = -1;
                Invalidate();
            }

        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseLeave(GLMouseEventArgs)"/>
        protected override void OnMouseLeave(GLMouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (hoveredpos >= 0)
            {
                hoveredpos = -1;
                Invalidate();
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseWheel(GLMouseEventArgs)"/>
        protected override void OnMouseWheel(GLMouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (!e.Handled)
            {
                if (e.Delta < 0)
                    GoRight(null, null);
                else
                    GoLeft(null, null);
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnKeyDown(GLKeyEventArgs)"/>
        protected override void OnKeyDown(GLKeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == System.Windows.Forms.Keys.Down)
            {
                if (e.Control)
                {
                    if (mode != Mode.Day)
                        mode = mode -1;
                }
                else if (mode == Mode.Day)
                    datetimecursor = datetimecursor.SafeAddDays(7);
                else if (mode == Mode.Month)
                    datetimecursor = datetimecursor.SafeAddMonths(4);
                else if (mode == Mode.Decade)
                    datetimecursor = datetimecursor.SafeAddYears(4);
                Invalidate();
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.Up)
            {
                if (e.Control)
                {
                    if (mode != Mode.Decade)
                        mode = mode + 1;
                }
                else if (mode == Mode.Day)
                    datetimecursor = datetimecursor.SafeAddDays(-7);
                else if (mode == Mode.Month)
                    datetimecursor = datetimecursor.SafeAddMonths(-4);
                else if (mode == Mode.Decade)
                    datetimecursor = datetimecursor.SafeAddYears(-4);
                Invalidate();
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.Left)
            {
                if (mode == Mode.Day)
                    datetimecursor = datetimecursor.SafeAddDays(-1);
                else if (mode == Mode.Month)
                    datetimecursor = datetimecursor.SafeAddMonths(-1);
                else if (mode == Mode.Decade)
                    datetimecursor = datetimecursor.SafeAddYears(-1);
                Invalidate();
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.Right)
            {
                if (mode == Mode.Day)
                    datetimecursor = datetimecursor.SafeAddDays(1);
                else if (mode == Mode.Month)
                    datetimecursor = datetimecursor.SafeAddMonths(1);
                else if (mode == Mode.Decade)
                    datetimecursor = datetimecursor.SafeAddYears(1);
                Invalidate();
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.PageUp)
            {
                GoLeft(null, null);
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.PageDown)
            {
                GoRight(null, null);
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.Return)
            {
                ClickOn(-1);
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.Delete || e.KeyCode == System.Windows.Forms.Keys.Escape || e.KeyCode == System.Windows.Forms.Keys.Back)
            {
                OnOtherKeyPressed(e);
            }

            System.Diagnostics.Debug.WriteLine("Date time now " + datetimecursor.ToLongDateString());
        }

        private void OnOtherKeyPressed(GLKeyEventArgs e)
        {
            OtherKeyPressed?.Invoke(this, e);
        }

        // we have selected and entry, either the datetimecursor time (index=-1) or one of the index ones (mouse click)
        private void ClickOn(int index)
        {
            if (index >= 0)
            {
                if (mode == Mode.Day)
                {
                    datetimecursor = new DateTime(datetimecursor.Year, datetimecursor.Month, index);
                }
                else if (mode == Mode.Month)
                {
                    datetimecursor = new DateTime(datetimecursor.Year, index, datetimecursor.Day);
                }
                else if (mode == Mode.Decade)
                {
                    datetimecursor = new DateTime((datetimecursor.Year / 10) * 10 - 1 + index - 1, datetimecursor.Month, datetimecursor.Day);
                }
            }

            datetimevalue = datetimecursor;
            System.Diagnostics.Debug.WriteLine("Date value now " + datetimevalue.ToLongDateString());

            if (mode != Mode.Day)
                mode = mode - 1;
            else
                OnValueChanged();

            Invalidate();
        }


        /// <summary> Called when value changed, invokes call back </summary>
        protected virtual void OnValueChanged()
        {
            ValueChanged?.Invoke(this);
        }


        private int HoveringOver(Point p)
        {
            if (gridxacross <= 0)       // called before paint, ignore
                return -1;

            // System.Diagnostics.Debug.WriteLine("{0} {1}  {2}", p, gridystart, gridxleft);
            int margin = Font.Height / 4;
            int cellwidth = (Width - margin * 2) / gridxacross;     // x 
            int cellheight = (Height - gridystart) / gridydown;

            if ( gridystart>0 && p.Y >= gridystart && p.X >= gridxleft && p.X < gridxleft + cellwidth*gridxacross)
            {
                int voffset = (p.Y - gridystart) / cellheight;
                int hoffset = (p.X - margin) / cellwidth;
                int hover = voffset * gridxacross + hoffset - gridstartoffset;
              //  System.Diagnostics.Debug.WriteLine("{0} {1} = {2}", voffset, hoffset, hover);
                if (hover >= 0 && hover < selectiontext.Length)
                    return hover;
            }
            return -1;
        }

        private enum Mode
        {
            Day,Month,Decade
        }

        private Mode mode = Mode.Day;
        private DateTime datetimevalue = DateTime.Now;
        private DateTime datetimecursor = DateTime.Now;
        private string[] selectiontext = null;
        private int gridstartoffset = 0;
        private int gridxacross = 0;        // no of items across
        private int gridydown = 0;        // no of items down
        private int gridxleft = 0;     // offset pixels on left
        private int gridystart = 0;     // offset pixels in Y
        private Point hoverpoint = Point.Empty;     // save position to recalc
        private int hoveredpos = -1;    // save index to prevent too many updates
        private CultureInfo culture = CultureInfo.CurrentCulture;

        #endregion
    }
}
