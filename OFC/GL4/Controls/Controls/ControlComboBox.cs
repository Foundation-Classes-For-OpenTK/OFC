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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace GLOFC.GL4.Controls
{
    public class GLComboBox : GLForeDisplayBase
    {
        public Action<GLBaseControl> SelectedIndexChanged { get; set; } = null;     // not fired by programatically changing CheckState
        public Action<GLBaseControl, bool> DropDownStateChanged { get; set; } = null;

        public string Text { get { return dropdownbox.Text; } }

        public List<string> Items { get { return dropdownbox.Items; } set { dropdownbox.Items = value; } }
        public List<Image> ImageItems { get { return dropdownbox.ImageItems; } set { dropdownbox.ImageItems = value; } }
        public int[] ItemSeperators { get { return dropdownbox.ItemSeperators; } set { dropdownbox.ItemSeperators = value;  } }

        public int SelectedIndex { get { return dropdownbox.SelectedIndex; } set { if (value != dropdownbox.SelectedIndex) { dropdownbox.SelectedIndex = value; OnSelectedIndexChanged(); Invalidate(); } } }
        public string SelectedItem { get { return dropdownbox.SelectedItem; } set { dropdownbox.SelectedItem = value; OnSelectedIndexChanged(); Invalidate(); } }

        public int DropDownHeightMaximum { get { return dropdownbox.DropDownHeightMaximum; } set { dropdownbox.DropDownHeightMaximum = value; } }

        // ForeColor for text, BackColor for background
        public Color FaceColor { get { return comboboxFaceColor; } set { comboboxFaceColor = value; Invalidate(); } }
        public float FaceColorScaling { get { return faceColorScaling; } set { faceColorScaling = value; Invalidate(); } }
        
        public Color MouseOverColor { get { return dropdownbox.MouseOverColor; } set { dropdownbox.MouseOverColor = value; } }

        // dropdown colour
        public Color DropDownBackgroundColor { get { return dropdownbox.BackColor; } set { dropdownbox.BackColor = value; } }
        public Color DropDownForeColor { get { return dropdownbox.ForeColor; } set { dropdownbox.ForeColor = value; } }
        public Color DropDownItemSeperatorColor { get { return dropdownbox.ItemSeperatorColor; } set { dropdownbox.ItemSeperatorColor = value; } }
        public Color DropDownSelectedItemBackColor { get { return dropdownbox.SelectedItemBackColor; } set { dropdownbox.SelectedItemBackColor = value; Invalidate(); } }

        public bool InDropDown { get { return dropdownbox.Visible; } }

        public bool DisableChangeKeys { get; set; } = false;            // stop responding to up/down/left/right directly. Return still works

        // scroll bar
        public Color ArrowColor { get { return dropdownbox.ArrowColor; } set { dropdownbox.ArrowColor = value; } }       // of text
        public Color SliderColor { get { return dropdownbox.SliderColor; } set { dropdownbox.SliderColor = value; } }
        public Color ArrowButtonColor { get { return dropdownbox.ArrowButtonColor; } set { dropdownbox.ArrowButtonColor = value; } }
        public Color ArrowBorderColor { get { return dropdownbox.ArrowBorderColor; } set { dropdownbox.ArrowBorderColor = value; } }
        public float ArrowUpDrawAngle { get { return dropdownbox.ArrowUpDrawAngle; } set { dropdownbox.ArrowUpDrawAngle = value; } }
        public float ArrowDownDrawAngle { get { return dropdownbox.ArrowDownDrawAngle; } set { dropdownbox.ArrowDownDrawAngle = value; } }
        public float ArrowColorScaling { get { return dropdownbox.ArrowColorScaling; } set { dropdownbox.ArrowColorScaling = value; } }
        public Color MouseOverButtonColor { get { return dropdownbox.MouseOverButtonColor; } set { dropdownbox.MouseOverButtonColor = value; } }
        public Color MousePressedButtonColor { get { return dropdownbox.MousePressedButtonColor; } set { dropdownbox.MousePressedButtonColor = value; } }
        public Color ThumbButtonColor { get { return dropdownbox.ThumbButtonColor; } set { dropdownbox.ThumbButtonColor = value; } }
        public Color ThumbBorderColor { get { return dropdownbox.ThumbBorderColor; } set { dropdownbox.ThumbBorderColor = value; } }
        public float ThumbColorScaling { get { return dropdownbox.ThumbColorScaling; } set { dropdownbox.ThumbColorScaling = value; } }
        public float ThumbDrawAngle { get { return dropdownbox.ThumbDrawAngle; } set { dropdownbox.ThumbDrawAngle = value; } }

        public GLComboBox(string name, Rectangle location, List<string> itms) : base(name, location)
        {
            Items = itms;
            InvalidateOnEnterLeave = true;
            Focusable = true;
            InvalidateOnFocusChange = true;
            dropdownbox.Visible = false;
            dropdownbox.SelectedIndexChanged += dropdownchanged;
            dropdownbox.OtherKeyPressed += dropdownotherkey;
            BorderColorNI = DefaultComboBoxBorderColor;
            BackColorGradientAltNI = BackColorNI = DefaultComboBoxBackColor;
            foreColor = DefaultComboBoxForeColor;
            SetNI(padding: new Padding(1), borderwidth: 1);
        }

        public GLComboBox(string name, Rectangle location) : this(name, location, new List<string>())
        {
        }

        public GLComboBox() : this("Combo?", DefaultWindowRectangle)
        {
        }

        protected override void OnControlRemove(GLBaseControl parent, GLBaseControl child)
        {
            if (child == this && InDropDown)        // if its dropped, it need removing
                Remove(dropdownbox);
            base.OnControlRemove(parent, child);
        }

        protected override void SizeControl(Size parentsize)
        {
            base.SizeControl(parentsize);
            if (AutoSize)
            {
                SizeF size = new Size(80, 24);

                if ( Items != null )
                {
                    string longest = Items.Aggregate("", (max, cur) => max.Length > cur.Length ? max : cur);
                    if (longest.HasChars())
                    {
                        size = BitMapHelpers.MeasureStringInBitmap(longest, Font, ControlHelpersStaticFunc.StringFormatFromContentAlignment(ContentAlignment.MiddleLeft));
                        int arrowwidth = Font.ScalePixels(20);
                        size.Width += arrowwidth + textspacing*2;
                        size.Height += textspacing*2;
                    }
                }
                SetNI(clientsize: new Size((int)size.Width,(int)size.Height));
            }
        }

        const int textspacing = 2;

        protected override void Paint(Graphics gr)
        {
            bool enabled = Enabled && Items.Count > 0;
            Color bc = enabled && Hover ? MouseOverColor : comboboxFaceColor;

            using (var b = new LinearGradientBrush(new Rectangle(0, -1, ClientWidth, ClientHeight + 1), bc, bc.Multiply(FaceColorScaling), 90))
                gr.FillRectangle(b, ClientRectangle);       // linear grad brushes do not respect smoothing mode, btw

            int arrowwidth = Font.ScalePixels(20);
            Rectangle arrowbox = new Rectangle(ClientWidth - arrowwidth, 0, arrowwidth, ClientHeight);

            Rectangle textbox = new Rectangle(0, 0, ClientWidth - arrowwidth - textspacing, ClientHeight);

            if ( Focused )
            {
                using (Pen p1 = new Pen(MouseOverColor) { DashStyle = DashStyle.Dash })
                {
                    Rectangle fr = textbox;
                    fr.Inflate(-1, -1);
                    gr.DrawRectangle(p1, fr);
                }
            }

            if (Text.HasChars())
            {
                using (var fmt = new StringFormat())
                {
                    fmt.Alignment = StringAlignment.Near;
                    fmt.LineAlignment = StringAlignment.Center;
                    using (Brush textb = new SolidBrush(enabled ? this.ForeColor : this.ForeColor.Multiply(ForeDisabledScaling)))
                    {
                        gr.DrawString(Text, Font, textb, textbox, fmt);
                    }
                }
            }

            if (enabled)
            {
                int hoffset = arrowbox.Width / 12 + 2;
                int voffset = arrowbox.Height / 4;
                Point arrowpt1 = new Point(arrowbox.Left + hoffset, arrowbox.Y + voffset);
                Point arrowpt2 = new Point(arrowbox.XCenter(), arrowbox.Bottom - voffset);
                Point arrowpt3 = new Point(arrowbox.Right - hoffset, arrowpt1.Y);

                Point arrowpt1c = new Point(arrowpt1.X, arrowpt2.Y);
                Point arrowpt2c = new Point(arrowpt2.X, arrowpt1.Y);
                Point arrowpt3c = new Point(arrowpt3.X, arrowpt2.Y);

                using (Pen p2 = new Pen(ForeColor))
                {
                    if (dropdownbox.Visible)
                    {
                        gr.DrawLine(p2, arrowpt1c, arrowpt2c);            // the arrow!
                        gr.DrawLine(p2, arrowpt2c, arrowpt3c);
                    }
                    else
                    {
                        gr.DrawLine(p2, arrowpt1, arrowpt2);            // the arrow!
                        gr.DrawLine(p2, arrowpt2, arrowpt3);
                    }
                }
            }
        }

        protected override void OnMouseClick(GLMouseEventArgs e)
        {
            base.OnMouseClick(e);
            if ( e.Button == GLMouseEventArgs.MouseButtons.Left )
            {
                if (!dropdownbox.Visible)
                    Activate();
                else
                    Deactivate();
            }
        }

        protected override void OnKeyDown(GLKeyEventArgs e)
        {
            base.OnKeyDown(e);

            if ( !e.Handled && Items.Count>0)
            { 
                if (!DisableChangeKeys && (e.KeyCode == System.Windows.Forms.Keys.Up || e.KeyCode == System.Windows.Forms.Keys.Left))
                {
                    if (SelectedIndex == -1)
                        SelectedIndex = 0;
                    else if (SelectedIndex > 0)
                        SelectedIndex = SelectedIndex - 1;
                }
                else if (!DisableChangeKeys && (e.KeyCode == System.Windows.Forms.Keys.Down || e.KeyCode == System.Windows.Forms.Keys.Right))
                {
                    if (SelectedIndex == -1)
                        SelectedIndex = 0;
                    else if (SelectedIndex < Items.Count - 1)
                        SelectedIndex = SelectedIndex + 1;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.Return)
                {
                    Activate();
                }
            }
        }

        protected override void OnGlobalMouseClick(GLBaseControl ctrl, GLMouseEventArgs e)
        {
            base.OnGlobalMouseClick(ctrl, e);   // do heirarchy before we mess with it

            if (InDropDown && (ctrl == null || !IsThisOrChildOf(ctrl)))        // if its not part of us, close
                Deactivate();
        }

        public override bool IsThisOrChildOf(GLBaseControl ctrl)         // override, and make the DropDown one of us - important for some checks
        {
            if (base.IsThisOrChildOf(ctrl))
                return true;
            else if (InDropDown && dropdownbox.IsThisOrChildOf(ctrl))
                return true;
            else
                return false;
        }

        private void Activate()
        {
            bool activatable = Enabled && Items.Count > 0 && !InDropDown;

            if (activatable)
            {
                dropdownbox.SuspendLayout();
                var p = FindScreenCoords(new Point(0, Height));
                dropdownbox.Bounds = new Rectangle(p.X, p.Y, Width - ClientLeftMargin - ClientRightMargin, Height);
                dropdownbox.ScaleWindow = FindScaler();
                dropdownbox.Name = Name + "-Dropdown";
                dropdownbox.TopMost = true;
                dropdownbox.AutoSize = true;
                dropdownbox.Font = Font;
                dropdownbox.Visible = true;
                dropdownbox.ShowFocusBox = true;
                dropdownbox.HighlightSelectedItem = true;
                dropdownbox.ResumeLayout();
                AddToDesktop(dropdownbox);             // attach to display, not us, so it shows over everything
                DropDownStateChanged?.Invoke(this, true);
                dropdownbox.SetFocus();
            }
        }

        private void Deactivate()
        {
            if (InDropDown)
            {
                Remove(dropdownbox);
                dropdownbox.Visible = false;
                SetFocus();
                Invalidate();
                DropDownStateChanged?.Invoke(this, false);
            }
        }

        private void dropdownchanged(GLBaseControl c, int v)
        {
            Deactivate();
            OnSelectedIndexChanged();       // order here important, called after action taken
        }

        private void dropdownotherkey(GLBaseControl c, GLKeyEventArgs e)
        {
            if ( e.KeyCode == System.Windows.Forms.Keys.Escape)
            {
                Deactivate();
            }
        }

        protected virtual void OnSelectedIndexChanged()
        {
            SelectedIndexChanged?.Invoke(this);
        }


        private GLListBox dropdownbox = new GLListBox();
        private Color comboboxFaceColor = DefaultComboBoxFaceColor;
        private float faceColorScaling = 1.0F;

    }
}
