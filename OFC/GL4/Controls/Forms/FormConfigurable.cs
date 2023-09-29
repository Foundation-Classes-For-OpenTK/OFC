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

using GLOFC.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// Configurable Form, allowing simple setup of a forms content.
    /// Forms are autosized to content. 
    /// They default to not being resizable. For resizable forms, turn off AutoSize after adding the form to the parent, and set Resizable or Movable properties.
    /// They can have a entries with AnchorType=DialogButtonLine, which autoarranges the items along a dialog line at the bottom of all other content
    /// </summary>

    public class GLFormConfigurable : GLForm
    {
        /// <summary>
        /// Callback, called when the user interacts with the form.
        /// Trigger passes: GLformConfiguratble, Entry (or null) actioning, string action, callertag 
        /// The string action is:
        /// * GLButton, GLCheckBox: control name is returned when clicked or return is pressed
        /// * GLComboBox : control name is returned when selection made
        /// * GLNumberBox: "Return" if return is pressed, or "Validity:true/false" when validity changes. Entry can give you the number using Get
        /// * GLMultiLineTextBox: "Return" if return is pressed
        /// * "Escape" is the escape key is pressed
        /// * "Close" is the close button pressed
        /// </summary>
        public event Action<GLFormConfigurable, Entry, string, Object> Trigger;

        /// <summary> For entries marked with AnchorType == DialogButtonLine, spacing between items on line</summary>
        public int AnchorDialogButtonLineSpacing { get; set; } = 8;

        /// <summary>
        /// This is an entry, descripting one visual element on the form. These are added to the form by the Add function before Init is called
        /// ControlTypes supported are GLMultiLineTextBox, GLNumberBoxLong, GLNumberBoxDouble, GLNumberBoxFloat, GLCheckBox, GLLabel, GLDateTimePicker, GLComboBox
        /// Any other type may be added by setting the ControlType=null and setting Control manually.
        /// You always set Name, ControlType, Location, Size
        /// Other fields are specific to certain types and are set for them
        /// Tab order is in order added, unless the entry specifically overrides it.  If it does, next autotab follows on from this value. 
        /// You can get feedback from the form using Trigger or DialogResultChanged (if you InstalledStandardTriggers or your triggers set DialogResult) 
        /// </summary>

        public class Entry
        {
            /// <summary> Control Name </summary>
            public string Name;      
            /// <summary> Control Type, or null to indicate Control is set</summary>
            public Type ControlType; 
            /// <summary> Tab order </summary>
            public int TabOrder = -1;
            /// <summary> User Tag</summary>
            public Object Tag;    
            /// <summary> Location </summary>
            public Point Location;
            /// <summary>Size  </summary>
            public Size Size;
            /// <summary> Tooltip text, can be null</summary>
            public string ToolTip;

            // properties applied if this control makes it

            /// <summary> Anchor type. If the entry has AnchorType == DialogButtonLine, they are auto arranged in add order right to left along a line below all other items</summary>
            public AnchorType Anchor = AnchorType.None;
            /// <summary> Text of control, GLButton, GLComboBox, GLMultiLineTextBox (+derivates), GLLabel, GLDateTimePicker </summary>
            public string Text;                 
            /// <summary> Check state for checkbox</summary>
            public bool Checked;                
            /// <summary> For textbox's, clear on first character</summary>
            public bool ClearOnFirstChar;       
            /// <summary> For combobox, item list </summary>
            public string[] ComboBoxItems;        
            /// <summary> For DateTimePicker, date format </summary>
            public string CustomDateFormat;
            /// <summary> For GLNumberBoxDouble/Float, minimum value</summary>
            public double NumberBoxDoubleMinimum = double.MinValue;   // for double box
            /// <summary> For GLNumberBoxDouble/Float, maximum value</summary>
            public double NumberBoxDoubleMaximum = double.MaxValue;
            /// <summary> For GLNumberBoxLong, minimum value</summary>
            public long NumberBoxLongMinimum = long.MinValue;   // for long box
            /// <summary> For GLNumberBoxLong, maximum value</summary>
            public long NumberBoxLongMaximum = long.MaxValue;
            /// <summary> For GLNumberBox.. , number format</summary>
            public string NumberBoxFormat;      
            /// <summary> For GLLabel, GLButton, GLComboBox: text alignment</summary>
            public ContentAlignment? TextAlign;  
            /// <summary> For GLMultiLineTextBox (+derivates) read only state </summary>
            public bool ReadOnly = false;     
            /// <summary> Autosize the entry </summary>
            public bool AutoSize = false;     

            /// <summary> Control for entry. Normally null, as ControlType describes entry, but can be set for custom control types 
            /// If set, properties not applied are Name, Location, Size, Anchor and Autosize.
            /// </summary>
            public GLBaseControl Control; 

            /// <summary> Create an entry by type. </summary>
            /// <param name="name">Name of control</param>
            /// <param name="typeofcontrol">Control type</param>
            /// <param name="text">Value of control</param>
            /// <param name="location">Location</param>
            /// <param name="size">Size</param>
            /// <param name="tooltip">Tooltip, may be null</param>
            /// <param name="tag">User tag</param>
            public Entry(string name, Type typeofcontrol, string text, Point location, Size size, string tooltip = null, Object tag = null)
            {
                ControlType = typeofcontrol; Text = text; Location = location; Size = size; ToolTip = tooltip; Name = name; CustomDateFormat = "long"; this.Tag = tag;
            }

            /// <summary> Create an entry by giving control </summary>
            /// <param name="name">Name</param>
            /// <param name="control">Control, already made</param>
            /// <param name="tooltip">Tooltip, may be null</param>
            /// <param name="tag">User tag</param>
            public Entry( string name, GLBaseControl control, string tooltip = null, Object tag = null)
            {
                Control = control; Name = name; ToolTip = tooltip; this.Tag = tag;
            }

            /// <summary> Create a combo box entry </summary>
            /// <param name="name">Name of control</param>
            /// <param name="text">Value of control</param>
            /// <param name="location">Location</param>
            /// <param name="size">Size</param>
            /// <param name="tooltip">Tooltip, may be null</param>
            /// <param name="comboitems">Array of combo box items</param>
            /// <param name="tag">User tag</param>
            public Entry(string name, string text, Point location, Size size, string tooltip, string[] comboitems, Object tag = null)
            {
                ControlType = typeof(GLComboBox); Text = text; Location = location; Size = size; ToolTip = tooltip; Name = name; this.Tag = tag;
                ComboBoxItems = comboitems;
            }

        }

        #region Public interface

        /// <summary> Construct with name </summary>
        public GLFormConfigurable(string name) : base(name, "TitleConfigDefault", DefaultWindowRectangle)     // title changed on Init
        {
            entries = new List<Entry>();
            Moveable = Resizeable = false;
            AutoSize = true;
            AutoSizeToTitle = true;
            tabnumber = 0;
        }

        /// <summary> Default constructor </summary>
        public GLFormConfigurable() : base("CF", "TitleConfigDefault", DefaultWindowRectangle)     // title changed on Init
        {
        }

        /// <summary> Add entry  </summary>
        public void Add(Entry e) 
        {
            if (e.TabOrder == -1)
                e.TabOrder = tabnumber++;
            else
                tabnumber = e.TabOrder + 1;

            entries.Add(e);
        }

        /// <summary> Add an entry with a pre-made control </summary>
        /// <param name="name">Name of control</param>
        /// <param name="control">Control, pre-made</param>
        /// <param name="tooltiptext">Tool tip text, may be null</param>
        /// <param name="tag">User tag, may be null</param>
        public void Add(string name, GLBaseControl control, string tooltiptext = null, Object tag = null)   // add a previously made ctrl
        {
            Add(new Entry(name, control, tooltiptext, tag) { TabOrder = tabnumber++ });
        }

        /// <summary> Add a button </summary>
        /// <param name="name">Name of control</param>
        /// <param name="text">Text for button</param>
        /// <param name="location">Location </param>
        /// <param name="tooltiptext">Tool tip text, may be null</param>
        /// <param name="size">Size, may be null, if so 80x24</param>
        /// <param name="anchor">Anchor.  If its DialogButtonLine, the button is auto arranged in add order right to left along a line below all other items</param>
        /// <param name="tag">User tag, may be null</param>
        public void AddButton(string name, string text, Point location, string tooltiptext = null, Size? size = null, AnchorType anchor = AnchorType.None, Object tag = null)
        {
            if (size == null)
                size = new Size(80, 24);
            Add(new Entry(name, typeof(GLButton), text, location, size.Value, tooltiptext, tag) {Anchor = anchor});
        }

        /// <summary> Add a button, name of OK </summary>
        /// <param name="text">Text for button</param>
        /// <param name="tooltiptext">Tool tip text, may be null</param>
        /// <param name="size">Size, may be null, if so 80x24</param>
        /// <param name="anchor">Anchor.  If its DialogButtonLine, the button is auto arranged in add order right to left along a line below all other items</param>
        public void AddOK(string text, string tooltiptext = null, Size? size = null, AnchorType anchor = AnchorType.AutoPlacement)
        {
            AddButton("OK", text, new Point(0, 0), tooltiptext, size,anchor);
        }

        /// <summary> Add a button, name of Cancel </summary>
        /// <param name="text">Text for button</param>
        /// <param name="tooltiptext">Tool tip text, may be null</param>
        /// <param name="size">Size, may be null, if so 80x24</param>
        /// <param name="anchor">Anchor.  If its DialogButtonLine, the button is auto arranged in add order right to left along a line below all other items</param>
        public void AddCancel(string text, string tooltiptext = null, Size? size = null, AnchorType anchor = AnchorType.AutoPlacement)
        {
            AddButton("Cancel", text, new Point(0, 0), tooltiptext, size, anchor);
        }

        /// <summary> Install standard trigger handlers for "OK" (Close, DialogResult=OK) and "Close","Escape","Cancel" (Close, DialogResult=Cancel)</summary>
        public void InstallStandardTriggers()
        {
            Trigger += (cfg, en, controlname, args) =>
            {
                if (controlname == "OK")
                {
                    cfg.DialogResult = DialogResultEnum.OK;
                    Close();
                }
                else if (controlname == "Close")        // close can get called due to Close() being called, or by X.  if by X, dialogresult won't be set so its must be indicated
                {
                    if ( cfg.DialogResult == DialogResultEnum.None )
                        cfg.DialogResult = DialogResultEnum.Cancel;
                }
                else if( controlname == "Escape" || controlname == "Cancel")
                {
                    cfg.DialogResult = DialogResultEnum.Cancel;
                    Close();
                }
            };
        }

        /// <summary> Get last entry </summary>
        public Entry Last { get { return entries.Last(); } }

        /// <summary> Initialise form, with position, caption and callertag (passed back in Trigger)</summary>
        public void Init(Point pos, string caption, Object callertag = null)
        {
            location = pos;
            InitInt(caption, callertag);
        }

        /// <summary> Initialise form to centre of screen, with caption and callertag (passed back in Trigger)</summary>
        public void InitCentered(string caption, Object callertag = null)
        {
            centred = true;
            InitInt(caption, callertag);
        }

        /// <summary> Get control of name, as type T. If name not found, return null</summary>
        public T GetControl<T>(string name) where T : GLBaseControl      
        {
            Entry t = entries.Find(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (t != null)
                return (T)t.Control;
            else
                return null;
        }

        #endregion

        #region Implementation

        private void InitInt(string caption, Object callertag)
        {
            this.callertag = callertag;      // passed back to caller via trigger
            this.Text = caption;
            this.FormClosed += (a) => {
                Trigger?.Invoke(this, null, "Close", this.callertag);       // pass back the logical name of dialog, the name of the control, the caller tag
            };

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

                GLBaseControl control = ent.Control;
                control.Tag = ent;     // point control tag at ent structure
                control.TabOrder = ent.TabOrder;

                if ( control is GLLabel)
                {
                    var l = control as GLLabel;
                    if (oursmade)
                        l.Text = ent.Text;

                    if (ent.TextAlign.HasValue)
                        l.TextAlign = ent.TextAlign.Value;
                }
                else if (control is GLNumberBoxFloat)      // must be before MLTB
                {
                    GLNumberBoxFloat cb = control as GLNumberBoxFloat;

                    if (oursmade)
                    {
                        cb.Minimum = (float)ent.NumberBoxDoubleMinimum;
                        cb.Maximum = (float)ent.NumberBoxDoubleMaximum;
                        float? v = ent.Text.InvariantParseFloatNull();
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
                else if (control is GLNumberBoxDouble)      // must be before MLTB
                {
                    GLNumberBoxDouble cb = control as GLNumberBoxDouble;

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
                else if (control is GLNumberBoxLong)        // must be before MLTB
                {
                    GLNumberBoxLong cb = control as GLNumberBoxLong;
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

                else if ( control is GLMultiLineTextBox ) // also TextBox as its inherited
                {
                    GLMultiLineTextBox tb = control as GLMultiLineTextBox;

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
                else if ( control is GLButton )
                { 
                    GLButton b = control as GLButton;
                    if (oursmade)
                        b.Text = ent.Text;
                    
                    if (ent.TextAlign.HasValue)
                        b.TextAlign = ent.TextAlign.Value;

                    b.Click += (sender, ev) =>
                    {
                        Entry en = (Entry)(((GLBaseControl)sender).Tag);
                        Trigger?.Invoke(this, en, en.Name, this.callertag);       // pass back the logical name of dialog, the name of the control, the caller tag
                    };

                }
                else if (control is GLCheckBox)
                {
                    GLCheckBox cb = control as GLCheckBox;
                    if (oursmade)
                        cb.Checked = ent.Checked;
                    cb.CheckChanged = (sender) =>
                    {
                        Entry en = (Entry)(((GLBaseControl)sender).Tag);
                        Trigger?.Invoke(this, en, en.Name, this.callertag);       // pass back the logical name of dialog, the name of the control, the caller tag
                    };
                }
                else if (control is GLDateTimePicker)
                {
                    GLDateTimePicker dt = control as GLDateTimePicker;
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
                else if (control is GLComboBox)
                {
                    GLComboBox cb = control as GLComboBox;

                    if (oursmade)
                    {
                        cb.Items.AddRange(ent.ComboBoxItems);
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


                Add(control);
            }

            ResumeLayout();
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.SizeControl(Size)"/>
        protected override void SizeControl(Size parentsize)
        {
            if (ControlsIZ.Count > 0 && Parent != null)       // if not resizable, and we have stuff
            {
                if (AutoSize && !Resizeable)        // if autosizable, and not resizable (turned off and on AFTER added into the display control etc) we can set the size
                {
                    //System.Diagnostics.Debug.WriteLine($"conf {Name} Attempt resize");

                    base.SizeControl(parentsize);           // first perform the Form Autosize - taking into consideration title and objects other than the autoplacement items

                    Rectangle area = VisibleChildArea(x => (x.Anchor & AnchorType.AutoPlacement) == 0);   // get the clients area , ignoring anchor buttons

                    int buttonsmaxh = ControlsIZ.Where(x => (x.Anchor & AnchorType.AutoPlacement) != 0).Select(x => x.Height + AnchorDialogButtonLineSpacing).DefaultIfEmpty(0).Max() + AutoSizeClientMargin.Height;
                    int buttonswidth = ControlsIZ.Where(x => (x.Anchor & AnchorType.AutoPlacement) != 0).Select(y => y.Width + AnchorDialogButtonLineSpacing).DefaultIfEmpty(0).Sum() + AutoSizeClientMargin.Width;

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

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.PerformRecursiveLayout"/>
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
                    //System.Diagnostics.Debug.WriteLine($"{control.Name} {control.Size} to {pos}");
                    control.SetNI(location: pos);
                    buttonright -= AnchorDialogButtonLineSpacing;
                }
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnKeyPress(GLKeyEventArgs)"/>
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
        private int tabnumber;
    }
}

