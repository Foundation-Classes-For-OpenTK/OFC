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
 * 
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;


namespace GLOFC.GL4.Controls
{
    public class GLFormConfigurable : GLForm
    {
        // Trigger returns GLformConfiguratble, Entry (or null) actioning, logical string action, callertag
        // logical string action:
        // GLButton,GLCheckBox: control name is returned when clicked or return is pressed
        // GLComboBox : control name is returned when selection made
        // GLNumberBox: "Return" if return is pressed, or "Validity:true/false" when validity changes. Entry can give you the name
        // GLMultiLineTextBox: "Return" if return is pressed

        public event Action<GLFormConfigurable, Entry, string, Object> Trigger;

        // you must turn off autosize if you want it resizable.  Do this AFTER adding to displaycontrol etc.
        // if you want it resizable, set Resizable=true AFTER adding to displaycontrol etc.
        // if you want it moveable, set Moveable=true AFTER adding to displaycontrol etc.

        // You give an array of Entries describing the controls
        // either added programatically by Add(entry) 
        // Directly Supported Types (string name/base type)
        //      "button" ButtonExt, "textbox" TextBoxBorder, "checkbox" CheckBoxCustom, 
        //      "label" Label, "datetime" CustomDateTimePicker, 
        //      "numberboxdouble" NumberBoxDouble, "numberboxlong" NumberBoxLong, 
        //      "combobox" ComboBoxCustom
        // Or any type if you set controltype=null and set control field directly.
        // Set controlname, text,pos,size, tooltip
        // for specific type, set the other fields.

        // if the item has AnchorType == DialogButtonLine, they are auto arranged in add order right to left along a line below all other items

        public class Entry
        {
            public string Name;                 // logical name of control
            public Type ControlType;            // if non null, activate this type.  Else if null, control should be filled up with your specific type
            public int TabOrder = -1;           // tab order
            public Object Tag;                  // for use by caller
            public Point Location;
            public Size Size;
            public string ToolTip;              // can be null.

            // properties applied if this control makes it

            public AnchorType Anchor = AnchorType.None;
            public string Text;                 // for certain types, the text
            public bool Checked;                // fill in for checkbox
            public bool ClearOnFirstChar;       // fill in for textbox
            public string ComboBoxItems;        // fill in for combobox. comma separ list.
            public string CustomDateFormat;     // fill in for datetimepicker
            public double NumberBoxDoubleMinimum = double.MinValue;   // for double box
            public double NumberBoxDoubleMaximum = double.MaxValue;
            public long NumberBoxLongMinimum = long.MinValue;   // for long box
            public long NumberBoxLongMaximum = long.MaxValue;
            public string NumberBoxFormat;      // for both number boxes
            public ContentAlignment? TextAlign;  // label,button. nominal not applied
            public bool ReadOnly = false;       // text box
            public bool AutoSize = false;       // all

            public GLBaseControl Control; // if controltype is set, don't set.  If contrDaveoltype=null, pass your control type.

            // normal ones, by type
            public Entry(string name, Type c, string t, System.Drawing.Point p, System.Drawing.Size s, string tt = null, Object tag = null)
            {
                ControlType = c; Text = t; Location = p; Size = s; ToolTip = tt; Name = name; CustomDateFormat = "long"; this.Tag = tag;
            }

            // direct control
            public Entry( string name, GLBaseControl ctrl, string tt = null, Object tag = null)
            {
                Control = ctrl; Name = name; ToolTip = tt; this.Tag = tag;
            }

            // ComboBox
            public Entry(string nam, string t, System.Drawing.Point p, System.Drawing.Size s, string tt, List<string> comboitems, Object tag = null)
            {
                ControlType = typeof(GLComboBox); Text = t; Location = p; Size = s; ToolTip = tt; Name = nam; this.Tag = tag;
                ComboBoxItems = string.Join(",", comboitems);
            }

        }

        #region Public interface

        public GLFormConfigurable(string name) : base(name, "TitleConfigDefault",DefaultWindowRectangle)     // title changed on Init
        {
            entries = new List<Entry>();
            Moveable = Resizeable = false;
            AutoSize = true;
            AutoSizeToTitle = true;
        }

        public void Add(Entry e)               // add an entry..
        {
            entries.Add(e);
        }

        public void AddButton(string ctrlname, string text, Point p, string tooltip = null, Size? sz = null, AnchorType ac = AnchorType.None)
        {
            if (sz == null)
                sz = new Size(80, 24);
            Add(new Entry(ctrlname, typeof(GLButton), text, p, sz.Value, tooltip) {Anchor = ac});
        }

        public void AddOK(string text, string tooltip = null, Size? sz = null, AnchorType ac = AnchorType.AutoPlacement)
        {
            AddButton("OK", text, new Point(0, 0), tooltip, sz,ac);
        }
        public void AddCancel(string text, string tooltip = null, Size? sz = null, AnchorType ac = AnchorType.AutoPlacement)
        {
            AddButton("Cancel", text, new Point(0, 0), tooltip, sz, ac);
        }

        public void InstallStandardTriggers()
        {
            Trigger += (cfg, en, controlname, args) =>
            {
                if (controlname == "OK")
                {
                    cfg.DialogResult = DialogResult.OK;
                    Close();
                }
                else if (controlname == "Close" || controlname == "Escape" || controlname == "Cancel")
                {
                    cfg.DialogResult = DialogResult.Cancel;
                    Close();
                }
            };
        }

        public Entry Last { get { return entries.Last(); } }

        public void Init(Point pos, string caption, Object callertag = null)
        {
            location = pos;
            InitInt(caption, callertag);
        }

        public void InitCentered(string caption, Object callertag = null)
        {
            centred = true;
            InitInt(caption, callertag);
        }

        public T GetControl<T>(string controlname) where T : GLBaseControl      // return value of dialog control
        {
            Entry t = entries.Find(x => x.Name.Equals(controlname, StringComparison.InvariantCultureIgnoreCase));
            if (t != null)
                return (T)t.Control;
            else
                return null;
        }

        public string Get(string controlname)      // return value of dialog control
        {
            Entry t = entries.Find(x => x.Name.Equals(controlname, StringComparison.InvariantCultureIgnoreCase));
            if (t != null)
            {
                GLBaseControl c = t.Control;
                if (c is GLMultiLineTextBox)
                    return (c as GLMultiLineTextBox).Text;
                else if (c is GLCheckBox)
                    return (c as GLCheckBox).Checked ? "1" : "0";
                else if (c is GLDateTimePicker)
                    return (c as GLDateTimePicker).Value.ToString("yyyy/dd/MM HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                else if (c is GLNumberBoxDouble)
                {
                    var cn = c as GLNumberBoxDouble;
                    return cn.IsValid ? cn.Value.ToStringInvariant() : "INVALID";
                }
                else if (c is GLNumberBoxLong)
                {
                    var cn = c as GLNumberBoxLong;
                    return cn.IsValid ? cn.Value.ToStringInvariant() : "INVALID";
                }
                else if (c is GLComboBox)
                {
                    GLComboBox cb = c as GLComboBox;
                    return (cb.SelectedIndex != -1) ? cb.Text : "";
                }
            }

            return null;
        }

        public double? GetDouble(string controlname)     // Null if not valid
        {
            Entry t = entries.Find(x => x.Name.Equals(controlname, StringComparison.InvariantCultureIgnoreCase));
            if (t != null)
            {
                var cn = t.Control as GLNumberBoxDouble;
                if (cn.IsValid)
                    return cn.Value;
            }
            return null;
        }

        public long? GetLong(string controlname)     // Null if not valid
        {
            Entry t = entries.Find(x => x.Name.Equals(controlname, StringComparison.InvariantCultureIgnoreCase));
            if (t != null)
            {
                var cn = t.Control as GLNumberBoxLong;
                if (cn.IsValid)
                    return cn.Value;
            }
            return null;
        }

        public DateTime? GetDateTime(string controlname)
        {
            Entry t = entries.Find(x => x.Name.Equals(controlname, StringComparison.InvariantCultureIgnoreCase));
            if (t != null)
            {
                GLDateTimePicker c = t.Control as GLDateTimePicker;
                if (c != null)
                    return c.Value;
            }

            return null;
        }

        public bool Set(string controlname, string value)      // set value of dialog control
        {
            Entry t = entries.Find(x => x.Name.Equals(controlname, StringComparison.InvariantCultureIgnoreCase));
            if (t != null)
            {
                GLBaseControl c = t.Control;
                if (c is GLTextBox)
                {
                    (c as GLTextBox).Text = value;
                    return true;
                }
                else if (c is GLMultiLineTextBox)
                {
                    (c as GLMultiLineTextBox).Text = value;
                    return true;
                }
                else if (c is GLCheckBox)
                {
                    (c as GLCheckBox).Checked = !value.Equals("0");
                    return true;
                }
                else if (c is GLComboBox)
                {
                    GLComboBox cb = c as GLComboBox;
                    if (cb.Items.Contains(value))
                    {
                        cb.Enabled = false;
                        cb.SelectedItem = value;
                        cb.Enabled = true;
                        return true;
                    }
                }
                else if (c is GLNumberBoxDouble)
                {
                    var cn = c as GLNumberBoxDouble;
                    double? v = value.InvariantParseDoubleNull();
                    if (v.HasValue)
                    {
                        cn.Value = v.Value;
                        return true;
                    }
                }
                else if (c is GLNumberBoxLong)
                {
                    var cn = c as GLNumberBoxLong;
                    long? v = value.InvariantParseLongNull();
                    if (v.HasValue)
                    {
                        cn.Value = v.Value;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool SetEnabled(string controlname, bool state)      // set enable state of dialog control
        {
            Entry t = entries.Find(x => x.Name.Equals(controlname, StringComparison.InvariantCultureIgnoreCase));
            if (t != null)
            {
                var cn = t.Control as GLBaseControl;
                cn.Enabled = state;
                return true;
            }
            else
                return false;
        }


        #endregion

        #region Implementation

        private void InitInt(string caption, Object callertag)
        {
            this.callertag = callertag;      // passed back to caller via trigger
            this.Text = caption;

            SuspendLayout();

            for (int i = 0; i < entries.Count; i++)
            {
                Entry ent = entries[i];

                bool oursmade = ent.Control == null;

                if (oursmade)
                {
                    ent.Control = (GLBaseControl)Activator.CreateInstance(ent.ControlType);
                    ent.Control.Name = ent.Name;
                    ent.Control.SetNI(ent.Location,ent.Size);
                    ent.Control.Anchor = ent.Anchor;
                    ent.Control.AutoSize = ent.AutoSize;
                }

                GLBaseControl c = ent.Control;
                c.Tag = ent;     // point control tag at ent structure
                c.TabOrder = ent.TabOrder;

                if ( c is GLLabel)
                {
                    var l = c as GLLabel;
                    if (oursmade)
                        l.Text = ent.Text;

                    if (ent.TextAlign.HasValue)
                        l.TextAlign = ent.TextAlign.Value;
                }
                else if ( c is GLMultiLineTextBox ) // also TextBox as its inherited
                {
                    GLMultiLineTextBox tb = c as GLMultiLineTextBox;

                    if (oursmade)
                    {
                        tb.Text = ent.Text;
                        tb.ClearOnFirstChar = ent.ClearOnFirstChar;
                        tb.ReadOnly = ent.ReadOnly;
                    }

                    tb.ReturnPressed += (box) =>        // only works for text box
                    {
                        Entry en = (Entry)(box.Tag);
                        Trigger?.Invoke(this, en, "Return", this.callertag);       // pass back the logical name of dialog, the name of the control, the caller tag
                    };

                }
                else if ( c is GLButton )
                { 
                    GLButton b = c as GLButton;
                    if (oursmade)
                        b.Text = ent.Text;
                    
                    if (ent.TextAlign.HasValue)
                        b.TextAlign = ent.TextAlign.Value;

                    b.Click += (sender, ev) =>
                    {
                        Entry en = (Entry)(((GLBaseControl)sender).Tag);
                        Trigger?.Invoke(this, en, en.Name, this.callertag);       // pass back the logical name of dialog, the name of the control, the caller tag
                    };

                    b.Return += (sender) =>
                    {
                        Entry en = (Entry)(((GLBaseControl)sender).Tag);
                        Trigger?.Invoke(this, en, en.Name, this.callertag);       // pass back the logical name of dialog, the name of the control, the caller tag
                    };
                }
                else if (c is GLCheckBox)
                {
                    GLCheckBox cb = c as GLCheckBox;
                    if (oursmade)
                        cb.Checked = ent.Checked;
                    cb.CheckChanged = (sender) =>
                    {
                        Entry en = (Entry)(((GLBaseControl)sender).Tag);
                        Trigger?.Invoke(this, en, en.Name, this.callertag);       // pass back the logical name of dialog, the name of the control, the caller tag
                    };
                }
                else if (c is GLDateTimePicker)
                {
                    GLDateTimePicker dt = c as GLDateTimePicker;
                    if (oursmade)
                    {
                        DateTime t;
                        if (DateTime.TryParse(ent.Text, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeLocal, out t))     // assume local, so no conversion
                            dt.Value = t;
                    }

                    switch (ent.CustomDateFormat.ToLowerInvariant())
                    {
                        case "short":
                            dt.Format = GLDateTimePicker.DateTimePickerFormat.Short;
                            break;
                        case "long":
                            dt.Format = GLDateTimePicker.DateTimePickerFormat.Long;
                            break;
                        case "time":
                            dt.Format = GLDateTimePicker.DateTimePickerFormat.Time;
                            break;
                        default:
                            dt.CustomFormat = ent.CustomDateFormat;
                            break;
                    }
                }
                else if (c is GLComboBox)
                {
                    GLComboBox cb = c as GLComboBox;

                    if (oursmade)
                    {
                        cb.Items.AddRange(ent.ComboBoxItems.Split(','));
                        if (cb.Items.Contains(ent.Text))
                            cb.SelectedItem = ent.Text;
                    }

                    cb.SelectedIndexChanged += (sender) =>
                    {
                        GLBaseControl ctr = (GLBaseControl)sender;
                        if (ctr.Enabled)
                        {
                            Entry en = (Entry)(ctr.Tag);
                            Trigger?.Invoke(this, en, en.Name, this.callertag);       // pass back the logical name of dialog, the name of the control, the caller tag
                        }
                    };

                }
                else if (c is GLNumberBoxDouble)
                {
                    GLNumberBoxDouble cb = c as GLNumberBoxDouble;

                    if (oursmade)
                    {
                        cb.Minimum = ent.NumberBoxDoubleMinimum;
                        cb.Maximum = ent.NumberBoxDoubleMaximum;
                        double? v = ent.Text.InvariantParseDoubleNull();
                        cb.Value = v.HasValue ? v.Value : cb.Minimum;
                        if (ent.NumberBoxFormat != null)
                            cb.Format = ent.NumberBoxFormat;
                    }

                    cb.ReturnPressed += (box) =>
                    {
                        Entry en = (Entry)(box.Tag);
                        Trigger?.Invoke(this, en, ":Return", this.callertag);       // pass back the logical name of dialog, the name of the control, the caller tag
                    };
                    cb.ValidityChanged += (box, b) =>
                    {
                        Entry en = (Entry)(box.Tag);
                        Trigger?.Invoke(this, en, "Validity:" + b.ToString(), this.callertag);       // pass back the logical name of dialog, the name of the control, the caller tag
                    };
                }
                else if (c is GLNumberBoxLong)
                {
                    GLNumberBoxLong cb = c as GLNumberBoxLong;
                    if (oursmade)
                    {
                        cb.Minimum = ent.NumberBoxLongMinimum;
                        cb.Maximum = ent.NumberBoxLongMaximum;
                        long? v = ent.Text.InvariantParseLongNull();
                        cb.Value = v.HasValue ? v.Value : cb.Minimum;
                        if (ent.NumberBoxFormat != null)
                            cb.Format = ent.NumberBoxFormat;
                    }

                    cb.ReturnPressed += (box) =>
                    {
                        Entry en = (Entry)(box.Tag);
                        Trigger?.Invoke(this, en, "Return", this.callertag);       // pass back the logical name of dialog, the name of the control, the caller tag
                    };
                    cb.ValidityChanged += (box, s) =>
                    {
                        Entry en = (Entry)(box.Tag);
                        Trigger?.Invoke(this, en, "Validity:" + s.ToString(), this.callertag);       // pass back the logical name of dialog, the name of the control, the caller tag
                    };
                }

                Add(c);
            }

            ResumeLayout();
        }

        private const int butspacing = 8;

        protected override void SizeControl(Size parentsize)
        {
            if (ControlsIZ.Count > 0 && Parent != null)       // if not resizable, and we have stuff
            {
                if (AutoSize && !Resizeable)        // if autosizable, and not resizable (turned off and on AFTER added into the display control etc) we can set the size
                {
                    //System.Diagnostics.Debug.WriteLine($"conf {Name} Attempt resize");

                    base.SizeControl(parentsize);           // first perform the Form Autosize - taking into consideration title and objects other than the autoplacement items

                    Rectangle area = ChildArea(x => (x.Anchor & AnchorType.AutoPlacement) == 0);   // get the clients area , ignoring anchor buttons

                    int buttonsmaxh = ControlsIZ.Where(x => (x.Anchor & AnchorType.AutoPlacement) != 0).Select(x => x.Height + butspacing).DefaultIfEmpty(0).Max() + AutoSizeClientMargin.Height;
                    int buttonswidth = ControlsIZ.Where(x => (x.Anchor & AnchorType.AutoPlacement) != 0).Select(y => y.Width + butspacing).DefaultIfEmpty(0).Sum() + AutoSizeClientMargin.Width;

                    if (ClientWidth < buttonswidth || ClientHeight < area.Top + area.Height + buttonsmaxh)
                    {
                        //System.Diagnostics.Debug.WriteLine($"Conf {Name} Need to make it wider/height for buttons {buttonswidth} {buttonsmaxh} ");
                        Size news = new Size(Math.Max(ClientWidth, buttonswidth), Math.Max(ClientHeight, area.Top + area.Height + buttonsmaxh));
                        SetNI(clientsize: news);

                        if (SetMinimumSizeOnAutoSize)
                            MinimumSize = Size;
                    }
                }

                if (!Moveable)          // as long as not movable (turned on AFTER added into the display control etc) we can set the position
                {
                    Size psize = Parent.Size;

                    if (centred)
                    {
                        int left = psize.Width / 2 - Size.Width / 2;
                        int top = psize.Height / 2 - Size.Height / 2;
                        SetNI(location: new Point(left, top));
                    }
                    else
                    {
                        int left = location.X;
                        if (left + Width > psize.Width)
                            left = psize.Width - Width;
                        int top = location.Y;
                        if (top + Height > psize.Height)
                            top = psize.Height - Height;

                        SetNI(location: new Point(left, top));
                    }
                }
            }
        }

        // layout any anchor line buttons
        protected override void PerformRecursiveLayout()
        {
            base.PerformRecursiveLayout();      // do normal layout on children

            int buttonsmaxh = ControlsIZ.Where(x => (x.Anchor & AnchorType.AutoPlacement) != 0).Select(x => x.Height).DefaultIfEmpty(0).Max();
            int buttonline = ClientHeight - AutoSizeClientMargin.Height - buttonsmaxh;
            int buttonright = ClientWidth - AutoSizeClientMargin.Width;

            foreach (var control in ControlsIZ)
            {
                if ((control.Anchor & AnchorType.AutoPlacement) != 0)        // if dialog line anchor
                {
                    buttonright -= control.Width;
                    var pos = new Point(buttonright, buttonline);
                    System.Diagnostics.Debug.WriteLine($"{control.Name} {control.Size} to {pos}");
                    control.SetNI(location: pos);
                    buttonright -= butspacing;
                }
            }
        }

        protected override void OnKeyPress(GLKeyEventArgs e)       // forms gets first dibs at keys of children
        {
            base.OnKeyPress(e);
            if ( !e.Handled && e.KeyChar == 27 )
            {
                Trigger?.Invoke(this, null, "Escape", callertag);
                e.Handled = true;
            }
        }

        #endregion

        private List<Entry> entries;
        private Object callertag;
        private bool centred;
        private Point location;
    }
}

