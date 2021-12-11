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

using GLOFC;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;

namespace GLOFC.GL4.Controls
{
    public class GLDateTimePicker: GLForeDisplayBase
    {
        public Action<GLBaseControl> CheckChanged { get; set; } = null;   // not fired by programatic Checked  
        public Action<GLBaseControl> ValueChanged { get; set; } = null;   // Not fired by programatic Value
        public Action<GLBaseControl, bool> DropDownStateChanged { get; set; } = null;

        public DateTime Value { get { return datetimevalue; } set { datetimevalue = value; Invalidate(); } }
        public CultureInfo Culture { get { return culture; } set { culture = value; ParentInvalidateLayout(); } }

        public enum DateTimePickerFormat
        {
            Long = 1,
            Short = 2,
            Time = 4,
            Custom = 8
        }
        public DateTimePickerFormat Format { get { return format; } set { SetFormat(value); InvalidateLayout(); } }     // format control, primary

        // returns current format, or sets a custom format
        public string CustomFormat { get { return customformat; } set { customformat = value; format = DateTimePickerFormat.Custom;  ParentInvalidateLayout(); } }

        public bool ShowUpDown { get { return UpDown.Visible; } set { UpDown.Visible = value; InvalidateLayout(); } }
        public bool ShowCheckBox { get { return CheckBox.Visible; } set { CheckBox.Visible = value; InvalidateLayout(); } }
        public bool ShowCalendar { get { return CalendarSelect.Visible; } set { CalendarSelect.Visible = value; InvalidateLayout(); } }

        public bool Checked { get { return CheckBox.Checked; } set { CheckBox.Checked = value; } }

        public Color SelectedColor { get { return selectedColor; } set { selectedColor = value; Invalidate(); } }

        public GLCheckBox CheckBox { get; set; } = new GLCheckBox();              // access for setting colours
        public GLUpDownControl UpDown { get; set; } = new GLUpDownControl();
        public GLButton CalendarSelect { get; set; } = new GLButton();
        public GLCalendar Calendar { get; set; } = new GLCalendar();

        public bool InCalendar { get { return Calendar.Visible; } }

        public GLDateTimePicker(string name, Rectangle location, DateTime t) : base(name, location)
        {
            SuspendLayout();

            foreColor = DefaultDTPForeColor;
            BackColorNI = DefaultDTPBackColor;

            CheckBox.BackColor = Color.Transparent;
            CheckBox.CheckOnClick = true;
            CheckBox.KeyDown += OnKeyFromChild;
            CheckBox.CheckChanged += checkboxchanged;
            CheckBox.MouseDown += (o1, e1) => { selectedpart = -1; Invalidate(); };
            Add(CheckBox);

            UpDown.Clicked += updownchanged;
            UpDown.BackColor = Color.Transparent;
            UpDown.ShowFocusBox = false;
            UpDown.KeyDown += OnKeyFromChild;
            UpDown.GiveFocusToParent = true;
            Add(UpDown);

            CalendarSelect.Image = Properties.Resources.Calendar;
            CalendarSelect.BackColor = Color.Transparent;
            CalendarSelect.ImageStretch = true;
            CalendarSelect.MouseClick += calclicked;
            CalendarSelect.GiveFocusToParent = true;
            Add(CalendarSelect);

            Calendar.Visible = false;
            Calendar.ValueChanged += calselected;
            Calendar.OtherKeyPressed += calotherkey;
            Calendar.AutoSize = true;

            Focusable = true;
            InvalidateOnFocusChange = true;

            ResumeLayout();
        }

        public GLDateTimePicker() : this("DTP?", DefaultWindowRectangle, DateTime.Now)
        {
        }

        protected override void OnControlRemove(GLBaseControl parent, GLBaseControl child)     // ensure calendar is removed, since its not directly attached
        {
            if (child == this && InCalendar)
                Remove(Calendar);
            base.OnControlRemove(parent, child);
        }

        protected override void OnFontChanged()
        {
            base.OnFontChanged();
            ParentInvalidateLayout();
        }

        #region Layout paint

        const int borderoffset = 2;

        protected override void SizeControl(Size parentsize)
        {
            base.SizeControl(parentsize);
            if (AutoSize)
            {
                RecalculatePartsList();
                if ( partlist.Count>0)          // must see if we have parts
                {
                    int endx = partlist.Last().endx + borderoffset;     // get the end of the parts, in x
                    int size = ClientHeight - borderoffset * 2;
                    if (UpDown.Visible)
                    {
                        endx += size + borderoffset;
                    }
                    if (ShowCalendar)
                    {
                        int calwidth = size * 20 / 12;      // synchronise with below
                        endx += calwidth + borderoffset; 
                    }
                    SetNI(size: new Size(endx, ClientHeight));
                }
            }
        }


        protected override void PerformRecursiveLayout()
        {
            base.PerformRecursiveLayout();

            int size = ClientHeight - borderoffset*2;

            // NI versions stops repeated invalidates/layouts
            CheckBox.SetNI(location: new Point(borderoffset, borderoffset), size: new Size(size, size));

            RecalculatePartsList();   // cause anything might have changed

            int calwidth = size * 20 / 12;
            int calpos = ClientRectangle.Width - calwidth - borderoffset;
            CalendarSelect.SetNI(location: new Point(calpos, (ClientHeight / 2 - size / 2)), size: new Size(calwidth, size));

            int updownpos = CalendarSelect.Visible ? CalendarSelect.Left - size - borderoffset : ClientRectangle.Width - size - borderoffset;
            UpDown.SetNI(location: new Point(updownpos, borderoffset), size: new Size(size, size));
        }

        // called after the background of the panel has been drawn - so it will be clear to write.

        protected override void Paint(Graphics gr)
        {
            using (var fmt = new StringFormat() { Alignment = StringAlignment.Center })
            {
                using (Brush textb = new SolidBrush(this.ForeColor))
                {
                    for (int i = 0; i < partlist.Count; i++)
                    {
                        Parts p = partlist[i];

                        string t = (p.ptype == PartsTypes.Text) ? p.text : datetimevalue.ToString(p.format, Culture);

                        if (i == selectedpart && IsThisOrChildrenFocused() )
                        {
                            using (Brush br = new SolidBrush(this.SelectedColor))
                                gr.FillRectangle(br, new Rectangle(p.xpos, 0, p.endx - p.xpos, ClientHeight));
                        }

                        int ymarg = (ClientHeight - Font.Height) / 2;
                        Rectangle r = new Rectangle(p.xpos, ymarg, p.endx - p.xpos, ClientHeight - ymarg);
                        gr.DrawString(t, this.Font, textb, r, fmt);
                    }
                }
            }
        }

        #endregion

        #region UI

        protected override void OnMouseClick(GLMouseEventArgs e)
        {
            base.OnMouseDown(e);

            if ( !e.Handled )
            {
                for (int i = 0; i < partlist.Count; i++)
                {
                    if (partlist[i].ptype >= PartsTypes.DayName && e.Location.X >= partlist[i].xpos && e.Location.X <= partlist[i].endx)
                    {
                        if (selectedpart == i )      // click again, increment
                        {
                            if ( e.WasFocusedAtClick )
                                ProcessUpDown((e.Button == GLMouseEventArgs.MouseButtons.Right) ? -1 : 1);
                        }
                        else
                        {
                            selectedpart = i;
                            Invalidate();
                        }

                        SetFocus();     // we were clicked on, the OnFocus below may have forced it to go back to checkbox, make sure we have focus
                        break;
                    }
                }
            }
        }

        protected override void OnMouseWheel(GLMouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (!e.Handled)
            {
                if (e.Delta < 0)
                    ProcessUpDown(1);
                else
                    ProcessUpDown(-1);
            }
        }

        private void OnKeyFromChild(object c, GLKeyEventArgs e)
        {
            OnKeyDown(e);
        }

        protected override void OnKeyDown(GLKeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!e.Handled)
            {
                //System.Diagnostics.Debug.WriteLine("Key down" + e.KeyCode);

                if (e.KeyCode == System.Windows.Forms.Keys.Up)
                    ProcessUpDown(1);
                else if (e.KeyCode == System.Windows.Forms.Keys.Down)
                    ProcessUpDown(-1);
                else if (e.KeyCode == System.Windows.Forms.Keys.Left && selectedpart >= 0)
                {
                    int findprev = selectedpart - 1; // back 1
                    while (findprev >= 0 && partlist[findprev].ptype < PartsTypes.DayName)       // back until valid or -1
                        findprev--;

                    if ( findprev == -1 && CheckBox.Visible)
                    {
                        selectedpart = -1;
                        CheckBox.SetFocus();
                    }
                    else if ( findprev >= 0 )
                    {                       
                        selectedpart = findprev;
                        Invalidate();
                    }
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.Right && selectedpart < partlist.Count - 1)
                {
                    int findnext = selectedpart + 1; // fwd 1
                    while (findnext < partlist.Count && partlist[findnext].ptype < PartsTypes.DayName)       // fwd until valid
                        findnext++;

                    if (findnext < partlist.Count)
                    {
                        selectedpart = findnext;
                        SetFocus();
                        Invalidate();
                    }
                }
                else if (e.KeyCode >= System.Windows.Forms.Keys.D0 && e.KeyCode <= System.Windows.Forms.Keys.D9)
                {
                    keybuffer += (char)((e.KeyCode - System.Windows.Forms.Keys.D0) + '0');
                    if (!TryConvertString(keybuffer))
                    {
                        keybuffer = "";
                        keybuffer += (char)((e.KeyCode - System.Windows.Forms.Keys.D0) + '0');
                        TryConvertString(keybuffer);
                    }
                }
                else if ( e.KeyCode == System.Windows.Forms.Keys.C)
                {
                    if (!InCalendar)
                        Activate();
                }
            }
        }

        private void checkboxchanged(GLBaseControl b)
        {
            OnCheckBoxChanged();
        }

        private void updownchanged(GLBaseControl b, int dir)
        {
            if (dir > 0)
                ProcessUpDown(1);
            else
                ProcessUpDown(-1);
        }

        protected virtual void OnCheckBoxChanged()
        {
            CheckChanged?.Invoke(this);
        }

        protected virtual void OnValueChanged()
        {
            ValueChanged?.Invoke(this);
        }

        #region Calendar

        private void calclicked(Object b, GLMouseEventArgs e)       // clicked button
        {
            Activate();
        }

        private void calselected(GLBaseControl c)                   // cal date selected
        {
            Deactivate();
            datetimevalue = Calendar.Value;
            Invalidate();
            OnValueChanged();
        }

        private void calotherkey(GLBaseControl c, GLKeyEventArgs e) // another cal key hit
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Escape)
            {
                Deactivate();
            }
        }

        protected override void OnGlobalMouseClick(GLBaseControl ctrl, GLMouseEventArgs e)
        {
            base.OnGlobalMouseClick(ctrl, e);   // do heirachy before we mess with it

            if (InCalendar && (ctrl == null || !IsThisOrChildOf(ctrl)))        // if its not part of us, close
                Deactivate();
        }

        public override bool IsThisOrChildOf(GLBaseControl ctrl)         // override, and make the Calendar one of us
        {
            if (base.IsThisOrChildOf(ctrl))
                return true;
            else if (InCalendar && Calendar.IsThisOrChildOf(ctrl))
                return true;
            else
                return false;
        }

        private void Activate()     // turn on
        {
            if (!InCalendar)
            {
                Calendar.SuspendLayout();
                var p = FindScreenCoords(new Point(ClientLeftMargin, Height + 1));
                Calendar.Bounds = new Rectangle(p.X, p.Y, 200, 200);     // autosize 
                Calendar.Culture = Culture;
                                                                        
                Calendar.ScaleWindow = FindScaler();
                Calendar.Name = Name + "-Cal";
                Calendar.TopMost = true;
                Calendar.Font = Font;
                Calendar.Visible = true;
                Calendar.Value = datetimevalue;
                Calendar.ResumeLayout();
                AddToDesktop(Calendar);             // attach to display, not us, so it shows over everything
                Calendar.SetFocus();
                DropDownStateChanged?.Invoke(this, true);
            }
        }

        private void Deactivate(bool takefocus = true)  // turn off
        {
            if (InCalendar)
            {
                Remove(Calendar);
                Calendar.Visible = false;
                if ( takefocus)
                    SetFocus();
                Invalidate();
                DropDownStateChanged?.Invoke(this, false);
            }
        }

        #endregion

        #region Parts

        private void SetFormat(DateTimePickerFormat f)
        {
            format = f;
            if (format == DateTimePickerFormat.Long)
                customformat = Culture.DateTimeFormat.LongDatePattern.Trim();
            else if (format == DateTimePickerFormat.Short)
                customformat = Culture.DateTimeFormat.ShortDatePattern.Trim();
            else if (format == DateTimePickerFormat.Time)
                customformat = Culture.DateTimeFormat.LongTimePattern.Trim();

            RecalculatePartsList();
        }

        private void RecalculatePartsList()
        {
            if (Font == null)
                return;

            //System.Diagnostics.Debug.WriteLine($"DTP Recalc {Name} {customformat}");

            partlist.Clear();

            int size = ClientHeight - borderoffset * 2;
            int xpos = (CheckBox.Visible ? (borderoffset + size + borderoffset) : borderoffset);

            using (Bitmap b = new Bitmap(1, 1))
            {
                using (Graphics e = Graphics.FromImage(b))
                {
                    string fmt = customformat;

                    while (fmt.Length > 0)
                    {
                        Parts p = FromString(ref fmt);      // is it a part?
                        if (p == null)
                        {
                            if (fmt[0] == '\'')
                            {
                                int index = fmt.IndexOf('\'', 1);
                                if (index == -1)
                                    index = fmt.Length;

                                p = new Parts() { text = fmt.Substring(1, index - 1), ptype = PartsTypes.Text };
                                fmt = (index < fmt.Length) ? fmt.Substring(index + 1) : "";
                            }
                            else
                            {
                                string s = "";
                                while (fmt[0] != '\'' && FromString(fmt) == null)       // collect all together until we find another format or quote esacpe
                                {
                                    s += fmt[0];
                                    fmt = fmt.Substring(1);
                                }

                                p = new Parts() { text = s, ptype = PartsTypes.Text };
                            }
                        }

                        p.xpos = xpos;
                        SizeF sz = e.MeasureString(p.text, this.Font);
                        int width = (int)(sz.Width + 1);
                        p.endx = p.xpos + width;
                        xpos = p.endx;// + (p.ptype != PartsTypes.Text ?  borderoffset :0);
                        //System.Diagnostics.Debug.WriteLine($"Part {p.ptype} {p.xpos}..{p.endx} '{p.text}'");
                        partlist.Add(p);
                    }
                }
            }
        }

        private Parts FromString(string fmt)
        {
            string s = fmt;
            return FromString(ref s);
        }

        private Parts FromString(ref string fmt)        // find part, or null
        {
            if (fmt.StartsWith("dddd"))
                return Make(ref fmt, 4, PartsTypes.DayName, Maxlengthof(Culture.DateTimeFormat.DayNames));
            else if (fmt.StartsWith("ddd"))
                return Make(ref fmt, 3, PartsTypes.DayName, Maxlengthof(Culture.DateTimeFormat.AbbreviatedDayNames));
            else if (fmt.StartsWith("dd"))
                return Make(ref fmt, 2, PartsTypes.Day, "99");
            else if (fmt.StartsWith("d"))
                return Make(ref fmt, 1, PartsTypes.Day, "99");
            else if (fmt.StartsWith("MMMM"))
                return Make(ref fmt, 4, PartsTypes.Month, Maxlengthof(Culture.DateTimeFormat.MonthNames));
            else if (fmt.StartsWith("MMM"))
                return Make(ref fmt, 3, PartsTypes.Month, Maxlengthof(Culture.DateTimeFormat.AbbreviatedMonthNames));
            else if (fmt.StartsWith("MM"))
                return Make(ref fmt, 2, PartsTypes.Month, "99");
            else if (fmt.StartsWith("M"))
                return Make(ref fmt, 1, PartsTypes.Month, "99");
            else if (fmt.StartsWith("HH", StringComparison.InvariantCultureIgnoreCase))
                return Make(ref fmt, 2, PartsTypes.Hours, "99");
            else if (fmt.StartsWith("H", StringComparison.InvariantCultureIgnoreCase))
                return Make(ref fmt, 1, PartsTypes.Hours, "99");
            else if (fmt.StartsWith("mm"))
                return Make(ref fmt, 2, PartsTypes.Mins, "99");
            else if (fmt.StartsWith("m"))
                return Make(ref fmt, 1, PartsTypes.Mins, "99");
            else if (fmt.StartsWith("ss"))
                return Make(ref fmt, 2, PartsTypes.Seconds, "99");
            else if (fmt.StartsWith("s"))
                return Make(ref fmt, 1, PartsTypes.Seconds, "99");
            else if (fmt.StartsWith("tt"))
                return Make(ref fmt, 2, PartsTypes.AmPm, "AM");
            else if (fmt.StartsWith("t"))
                return Make(ref fmt, 1, PartsTypes.AmPm, "AM");
            else if (fmt.StartsWith("yyyyy"))
                return Make(ref fmt, 5, PartsTypes.Year, "99999");
            else if (fmt.StartsWith("yyyy"))
                return Make(ref fmt, 4, PartsTypes.Year, "9999");
            else if (fmt.StartsWith("yyy"))
                return Make(ref fmt, 3, PartsTypes.Year, "9999");
            else if (fmt.StartsWith("yy"))
                return Make(ref fmt, 2, PartsTypes.Year, "99");
            else if (fmt.StartsWith("y"))
                return Make(ref fmt, 1, PartsTypes.Year, "99");
            else
                return null;
        }

        private Parts Make(ref string c, int len, PartsTypes t, string maxs)
        {
            Parts p = new Parts() { format = c.Substring(0, len) + " ", ptype = t, text = maxs }; // space at end seems to make multi ones work
            c = c.Substring(len);
            return p;
        }

        private string Maxlengthof(string[] a)
        {
            return a.Aggregate("", (max, cur) => max.Length > cur.Length ? max : cur);
        }

        private void ProcessUpDown(int dir)
        {
            if (selectedpart != -1)
            {
                Parts p = partlist[selectedpart];
                if (p.ptype == PartsTypes.DayName)
                    datetimevalue = datetimevalue.SafeAddDays(dir);
                else if (p.ptype == PartsTypes.Day)
                    datetimevalue = datetimevalue.SafeAddDays(dir);
                else if (p.ptype == PartsTypes.Month)
                    datetimevalue = datetimevalue.SafeAddMonths(dir);
                else if (p.ptype == PartsTypes.Year)
                    datetimevalue = datetimevalue.SafeAddYears(dir);
                else if (p.ptype == PartsTypes.Hours)
                    datetimevalue = datetimevalue.SafeAddHours(dir);
                else if (p.ptype == PartsTypes.Mins)
                    datetimevalue = datetimevalue.SafeAddMinutes(dir);
                else if (p.ptype == PartsTypes.Seconds)
                    datetimevalue = datetimevalue.SafeAddSeconds(dir);
                else if (p.ptype == PartsTypes.AmPm)
                    datetimevalue = datetimevalue.SafeAddHours((datetimevalue.Hour >= 12) ? -12 : 12);
                else
                    return;

                OnValueChanged();
                Invalidate();
            }
        }

        private bool TryConvertString(string s)
        {
            int newvalue;
            int.TryParse(s, out newvalue);
            DateTime nv = DateTime.Now;

            Parts p = partlist[selectedpart];

            try
            {
                if (p.ptype == PartsTypes.DayName)
                    return false;
                else if (p.ptype == PartsTypes.Day)
                        nv = new DateTime(datetimevalue.Year, datetimevalue.Month, newvalue, datetimevalue.Hour, datetimevalue.Minute, datetimevalue.Second, datetimevalue.Kind);
                else if (p.ptype == PartsTypes.Month)
                    nv = new DateTime(datetimevalue.Year, newvalue, datetimevalue.Day, datetimevalue.Hour, datetimevalue.Minute, datetimevalue.Second, datetimevalue.Kind);
                else if (p.ptype == PartsTypes.Year)
                    nv = new DateTime(newvalue, datetimevalue.Month, datetimevalue.Day, datetimevalue.Hour, datetimevalue.Minute, datetimevalue.Second, datetimevalue.Kind);
                else if (p.ptype == PartsTypes.Hours)
                    nv = new DateTime(datetimevalue.Year, datetimevalue.Month, datetimevalue.Day, newvalue, datetimevalue.Minute, datetimevalue.Second, datetimevalue.Kind);
                else if (p.ptype == PartsTypes.Mins)
                    nv = new DateTime(datetimevalue.Year, datetimevalue.Month, datetimevalue.Day, datetimevalue.Hour, newvalue, datetimevalue.Second, datetimevalue.Kind);
                else if (p.ptype == PartsTypes.Seconds)
                    nv = new DateTime(datetimevalue.Year, datetimevalue.Month, datetimevalue.Day, datetimevalue.Hour, datetimevalue.Minute, newvalue, datetimevalue.Kind);

                datetimevalue = nv;

                OnValueChanged();
                Invalidate();
                return true;
            }
            catch
            {
                return false;
            }
        }


        #endregion

        private DateTime datetimevalue = DateTime.Now;
        private DateTimePickerFormat format = DateTimePickerFormat.Long;
        private string customformat = CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern;
        private CultureInfo culture = CultureInfo.CurrentCulture;

        enum PartsTypes { Text, DayName, Day, Month, Year, Hours, Mins, Seconds, AmPm }
        class Parts
        {
            public PartsTypes ptype;
            public string text;
            public string format;
            public int xpos;
            public int endx;
        };

        private List<Parts> partlist = new List<Parts>();
        private int selectedpart = 0;                            // always select first part as default.  -1 means checkbox

        private string keybuffer;
        private Color selectedColor = DefaultDTPSelectedColor;


        #endregion

    }
}
