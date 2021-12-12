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
using System.Linq;

namespace GLOFC.GL4.Controls
{
    // its a vertical scrollable list. Adding elements to it adds it to the 
    public class GLListBox : GLForeDisplayBase
    {
        public Action<GLBaseControl, int> SelectedIndexChanged { get; set; } = null;     // not fired by programatically 
        public Action<GLBaseControl, GLKeyEventArgs> OtherKeyPressed { get; set; } = null;     

        public List<string> Items { get { return items; } set { items = value; focusindex = -1; firstindex = 0; ParentInvalidateLayout(); } }
        public List<Image> ImageItems { get { return images; } set { images = value; ParentInvalidateLayout(); } }
        public int[] ItemSeperators { get { return itemSeperators; } set { itemSeperators = value; ParentInvalidateLayout(); } }

        public int SelectedIndex { get { return selectedIndex; } set { setSelectedIndex(value,false); } }       // does not fire SelectedIndexChanged
        public string SelectedItem { get { return selectedIndex >= 0 ? Items[selectedIndex] : null; } set { setSelectedItem(value); } }     // does not fire SelectedIndexChanged
        public string Text { get { return (items != null && selectedIndex >= 0) ? items[selectedIndex] : null; } set { setSelectedItem(value); } } // does not fire SelectedIndexChanged

        public int FocusIndex { get { return focusindex; } set { if (items != null) focusindex = Math.Min(value, items.Count - 1); Invalidate(); } }        // -1 turns off focus

        // if set, no half lines shown
        public bool FitToItemsHeight { get { return fitToItemsHeight; } set { fitToItemsHeight = value; Invalidate(); } }

        // if set, images fit to items
        public bool FitImagesToItemHeight { get { return fitImagesToItemHeight; } set { fitImagesToItemHeight = value; Invalidate(); } }

        public int DisplayableItems { get { return displayableitems; } }            // not valid until first layout

        public int DropDownHeightMaximum { get { return dropDownHeightMaximum; } set { System.Diagnostics.Debug.WriteLine("DDH Set"); dropDownHeightMaximum = value; ParentInvalidateLayout(); } }

        public Color SelectedItemBackColor { get { return selectedItemBackColor; } set { selectedItemBackColor = value; Invalidate(); } }
        public Color MouseOverColor { get { return mouseOverColor; } set { mouseOverColor = value; Invalidate(); } }
        public Color ItemSeperatorColor { get { return itemSeperatorColor; } set { itemSeperatorColor = value; Invalidate(); } }

        // normal behaviour is a dotted focus box following the keystrokes/hover, A hover over box, plus a solid highlight on the selected item. These allow change of mode

        public bool ShowFocusBox { get { return showfocusbox; } set { showfocusbox = value; Invalidate(); } }       // normal focus dotted box, 
        public bool HighlightSelectedItem { get { return highlightSelectedItem; } set { highlightSelectedItem = value; Invalidate(); } }    // SelectedItem is highlighted
        public bool ShowFocusHighlight { get { return showfocushighlight; } set { showfocushighlight = value; Invalidate(); } }            // if set, focus is a dotted area, no selecteditem shown

        // scroll bar
        public Color ArrowColor { get { return scrollbar.ArrowColor; } set { scrollbar.ArrowColor = value; } }       // of text
        public Color SliderColor { get { return scrollbar.SliderColor; } set { scrollbar.SliderColor = value; } }
        public Color ArrowButtonColor { get { return scrollbar.ArrowButtonColor; } set { scrollbar.ArrowButtonColor = value; } }
        public Color ArrowBorderColor { get { return scrollbar.ArrowBorderColor; } set { scrollbar.ArrowBorderColor = value; } }
        public float ArrowUpDrawAngle { get { return scrollbar.ArrowDecreaseDrawAngle; } set { scrollbar.ArrowDecreaseDrawAngle = value; } }
        public float ArrowDownDrawAngle { get { return scrollbar.ArrowIncreaseDrawAngle; } set { scrollbar.ArrowIncreaseDrawAngle = value; } }
        public float ArrowColorScaling { get { return scrollbar.ArrowColorScaling; } set { scrollbar.ArrowColorScaling = value; } }
        public Color MouseOverButtonColor { get { return scrollbar.MouseOverButtonColor; } set { scrollbar.MouseOverButtonColor = value; } }
        public Color MousePressedButtonColor { get { return scrollbar.MousePressedButtonColor; } set { scrollbar.MousePressedButtonColor = value; } }
        public Color ThumbButtonColor { get { return scrollbar.ThumbButtonColor; } set { scrollbar.ThumbButtonColor = value; } }
        public Color ThumbBorderColor { get { return scrollbar.ThumbBorderColor; } set { scrollbar.ThumbBorderColor = value; } }
        public float ThumbColorScaling { get { return scrollbar.ThumbColorScaling; } set { scrollbar.ThumbColorScaling = value; } }
        public float ThumbDrawAngle { get { return scrollbar.ThumbDrawAngle; } set { scrollbar.ThumbDrawAngle = value; } }

        public int ScrollBarWidth { get { return Font?.ScalePixels(20) ?? 20; } }

        public float GradientColorScaling
        {
            get { return gradientColorScaling; }
            set
            {
                if (float.IsNaN(value) || float.IsInfinity(value))
                    return;
                else if (gradientColorScaling != value)
                {
                    gradientColorScaling = value;
                    Invalidate();
                }
            }
        }

        public GLListBox(string n, Rectangle pos, List<string> texts) : base(n,pos)
        {
            items = texts;
            Focusable = true;
            BackColorNI = BackColorGradientAltNI = DefaultListBoxBackColor;
            BorderColorNI = DefaultListBoxBorderColor;
            foreColor = DefaultListBoxForeColor;
            SetNI(margin: new Margin(0), borderwidth: 1, padding: new Padding(1));
            InvalidateOnFocusChange = true;
            InvalidateOnEnterLeave = true;
            scrollbar = new GLScrollBar();
            scrollbar.Name = "GLLSB";
            scrollbar.Dock = DockingType.Right;
            scrollbar.SmallChange = 1;
            scrollbar.LargeChange = 1;
            scrollbar.Width = 20;
            scrollbar.Visible = false;
            scrollbar.Scroll += (s, e) => { if (firstindex != e.NewValue) { firstindex = e.NewValue; Invalidate(); } };
            Add(scrollbar);
        }

        public GLListBox() : this("LB?", DefaultWindowRectangle, null)
        {
        }

        public bool FocusUp(int count = 1)
        {
            count = Math.Min(focusindex, count);

            if (Items != null && count > 0)
            {
                focusindex -= count;
                showfocusindex = true;
                Invalidate();
                return true;
            }
            else
                return false;
        }

        public bool FocusDown(int count = 1)
        {
            if (Items != null)
            {
                count = Math.Min(count, Items.Count - focusindex - 1);

                if (count > 0)
                {
                    focusindex += count;
                    showfocusindex = true;
                    Invalidate();
                    return true;
                }
            }

            return false;
        }
        public void SelectCurrentFocus()            // does fire selectedindexchange, no action if focusindex is not set
        {
            setSelectedIndex(focusindex,true);
        }

        #region Implementation;

        protected override void OnFontChanged()
        {
            InvalidateLayout();
        }

        protected override void SizeControl(Size parentsize)
        {
            base.SizeControl(parentsize);

            if (AutoSize)       // measure text size and number of items to get idea of space required. Allow for scroll bar
            {
                int items = (Items != null) ? Items.Count() : 0;
                //System.Diagnostics.Debug.WriteLine("AS Items" + items);
                SizeF max = new SizeF(ScrollBarWidth*2,0);
                if ( items>0)
                {
                    using (StringFormat f = new StringFormat() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
                    {
                        foreach (var s in Items)
                        {
                            SizeF cur = BitMapHelpers.MeasureStringInBitmap(s, Font, f);
                            if (cur.Width > max.Width)
                                max.Width = cur.Width;
                        }
                    }
                }
                int fh = (int)Font.GetHeight() + 2;
                Size sz = new Size((int)max.Width+ScrollBarWidth+8,Math.Min(items*fh+4,DropDownHeightMaximum));
                SetNI(size: sz);
                //System.Diagnostics.Debug.WriteLine("Autosize list box " + Size);
            }
        }


        protected override void PerformRecursiveLayout()
        {
            if (scrollbar != null)  
                scrollbar.Width = ScrollBarWidth;       // set width 

            base.PerformRecursiveLayout();              // layout, scroll bar autodocks right

            if (Font != null)
            {
                int items = (Items != null) ? Items.Count() : 0;

                itemheight = (int)Font.GetHeight() + 2;

                displayableitems = ClientRectangle.Height / itemheight;            // number of items to display

                if (!FitToItemsHeight && (ClientRectangle.Height % itemheight) > 4) // if we have space for a partial row, and are allowed, increase lines
                    displayableitems++;

                if (items > 0 && displayableitems > items)
                    displayableitems = items;

                //System.Diagnostics.Debug.WriteLine("List box" + mainarea + " " + items + "  " + displayableitems);

                if (items > displayableitems)
                {
                    scrollbar.Maximum = Items.Count - displayableitems;
                    scrollbar.Visible = true;
                }
                else
                    scrollbar.Visible = false;
            }
        }

        protected override void Paint(Graphics gr)
        {
            if (itemheight < 1)     // can't paint yet
                return;

            Rectangle itemarea = new Rectangle(0, 0, ClientRectangle.Width - (scrollbar.Visible ? scrollbar.Width : 0), ClientRectangle.Height);     // total width area
            itemarea.Height = itemheight;

            // System.Diagnostics.Debug.WriteLine("Paint List box");
            if (items != null && items.Count > 0)
            {
                Rectangle textarea = itemarea;      // where we draw text
                Rectangle imagearea = itemarea;     // where we draw the images

                if (images != null)           // if we have images, allocate space between the 
                {
                    if (FitImagesToItemHeight)
                    {
                        imagearea = new Rectangle(imagearea.X, imagearea.Y, itemheight - 1, itemheight - 1);
                        textarea.X += imagearea.Width + 1;
                    }
                    else
                    {
                        int maxwidth = images.Max(x => x.Width);
                        textarea.X += maxwidth;
                        imagearea.Width = maxwidth;
                    }
                }

                if ( showfocusindex && focusindex >= 0 )              // if we have been told to ensure focus index is displayed..
                {
                    //System.Diagnostics.Debug.WriteLine($"Focus Index reset {firstindex} to fi {focusindex} sel {selectedIndex}");
                    System.Diagnostics.Debug.Assert(displayableitems >= 1);
                    if (firstindex > focusindex)    // must display focusindex
                    {
                        firstindex = focusindex;
                        scrollbar.Value = firstindex;       // ensure the scroll bar is informed
                    }
                    else if (focusindex >= firstindex + displayableitems) // if too far back..
                    {
                        firstindex = focusindex - displayableitems + 1;     // +1 to shift it on one up
                        scrollbar.Value = firstindex;       // ensure the scroll bar is informed
                    }
                    System.Diagnostics.Debug.Assert(firstindex >= 0,$"first index {focusindex} {firstindex} {displayableitems}");
                    showfocusindex = false;
                }

                //System.Diagnostics.Debug.WriteLine($"Paint fi {firstindex} focus {focusindex} sel {selectedIndex}");

                using (StringFormat f = new StringFormat() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
                using (Brush textb = new SolidBrush(this.ForeColor))
                {
                    int offset = 0;
                    int indextodrawfocusbox = focusindex < 0 ? firstindex : focusindex;

                    foreach (string s in items)
                    {
                        if (offset >= firstindex && offset < firstindex + displayableitems) // + (FitToItemsHeight ? 0 : 1))
                        {
                            if (ShowFocusHighlight)
                            {
                                if (offset == focusindex)                                  // default is if hovering, show hover box
                                {
                                    using (Brush highlight = new SolidBrush(Hover ? MouseOverColor : SelectedItemBackColor))
                                        gr.FillRectangle(highlight, itemarea);
                                }
                            }
                            else
                            {
                                if (offset == focusindex && Hover)                                  // default is if hovering, show hover box
                                {
                                    using (Brush highlight = new SolidBrush(MouseOverColor))
                                        gr.FillRectangle(highlight, itemarea);
                                }
                                else if (offset == selectedIndex && HighlightSelectedItem)          // else if on selected item, show
                                {
                                    using (Brush highlight = new SolidBrush(SelectedItemBackColor))
                                        gr.FillRectangle(highlight, itemarea);
                                }

                                if (ShowFocusBox && Focused && offset == indextodrawfocusbox)       // if showing focus box, and focused, draw
                                {
                                    Color b = selectedIndex == offset ? MouseOverColor : SelectedItemBackColor;
                                    using (Pen p1 = new Pen(b) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                                        gr.DrawRectangle(p1, itemarea);
                                }
                            }

                            if (images != null && offset < images.Count)
                            {
                                gr.DrawImage(images[offset], imagearea);
                                //System.Diagnostics.Debug.WriteLine(offset + " Image is " + imagearea);
                            }

                            gr.DrawString(s, this.Font, textb, textarea, f);

                            if (itemSeperators != null && Array.IndexOf(itemSeperators, offset) >= 0)
                            {
                                using (Pen p = new Pen(ItemSeperatorColor))
                                {
                                    gr.DrawLine(p, new Point(textarea.Left, textarea.Top), new Point(textarea.Right, textarea.Top));
                                }
                            }

                            itemarea.Y += itemheight;
                            textarea.Y = imagearea.Y = itemarea.Y;
                        }

                        offset++;
                    }
                }
            }
            else
            {
                if (ShowFocusBox && Focused )
                {
                    using (Pen p1 = new Pen(MouseOverColor) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                        gr.DrawRectangle(p1, itemarea);
                }
            }
       
        }

        private bool setSelectedItem(string v, StringComparison c = StringComparison.InvariantCultureIgnoreCase)
        {
            if (items != null)
            {
                int i = items.FindIndex((x) => x.Equals(v, c));
                if (i >= 0 && i < items.Count)
                {
                    setSelectedIndex(i, false);
                    return true;
                }
            }
            return false;
        }

        private void setSelectedIndex(int i, bool firechange)
        {
            if (items != null)
            {
                if (i >= 0 && i < items.Count)
                {
                    focusindex = selectedIndex = i;             // selecting it moves the sel and focus to this point
                    showfocusindex = true;
                    if ( firechange)
                        OnSelectedIndexChanged();
                    Invalidate();
                }
            }
        }

        protected override void OnMouseClick(GLMouseEventArgs e)
        {
            base.OnMouseClick(e);
            if ( !e.Handled)
            {
                if (items != null && itemheight > 0)       // if any items and we have done a calc layout.. just to check
                {
                    setSelectedIndex(firstindex + e.Location.Y / itemheight, true);
                }
            }
        }

        protected override void OnMouseWheel(GLMouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (!e.Handled)
            {
                if (e.Delta > 0)
                    FocusUp();
                else
                    FocusDown();
            }
        }

        protected override void OnMouseMove(GLMouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (!e.Handled)
            {
                if (items != null && itemheight > 0)  // may not have been set yet
                {
                    int y = e.Location.Y;
                    int index = (y / itemheight);
                    if (index < displayableitems)
                    {
                        index += firstindex;
                        if (index < items.Count)
                        {
                            focusindex = index;
                            Invalidate();
                        }
                    }
                }
            }
        }

        protected override void OnKeyDown(GLKeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!e.Handled)
            {
                //System.Diagnostics.Debug.WriteLine("LB KDown " + Name + " " + e.KeyCode);

                if (e.KeyCode == System.Windows.Forms.Keys.Up)
                {
                    FocusUp();
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.Down)
                {
                    FocusDown();
                }

                if ((e.KeyCode == System.Windows.Forms.Keys.Enter || e.KeyCode == System.Windows.Forms.Keys.Return) || (e.Alt && (e.KeyCode == System.Windows.Forms.Keys.Up || e.KeyCode == System.Windows.Forms.Keys.Down)))
                {
                    SelectCurrentFocus();
                }

                if (e.KeyCode == System.Windows.Forms.Keys.Delete || e.KeyCode == System.Windows.Forms.Keys.Escape || e.KeyCode == System.Windows.Forms.Keys.Back)
                {
                    OnOtherKeyPressed(e);
                }
            }
        }

        protected virtual void OnSelectedIndexChanged()
        {
            SelectedIndexChanged?.Invoke(this, SelectedIndex);
        }

        protected virtual void OnOtherKeyPressed(GLKeyEventArgs e)
        {
            OtherKeyPressed?.Invoke(this, e);
        }

        #endregion

        private bool fitToItemsHeight { get; set; } = true;              // if set, move the border to integer of item height.
        private bool fitImagesToItemHeight { get; set; } = false;        // if set images scaled to fit within item height
        private float gradientColorScaling = 0.5F;
        private Color selectedItemBackColor { get; set; } = DefaultListBoxSelectedItemColor;
        private Color mouseOverColor { get; set; } = DefaultListBoxMouseOverColor;
        private Color itemSeperatorColor { get; set; } = DefaultListBoxLineSeparColor;

        private GLScrollBar scrollbar;
        private List<string> items;
        private List<Image> images;
        private int[] itemSeperators { get; set; } = null;     // set to array giving index of each separator
        private int itemheight;
        private int displayableitems;

        private int firstindex = 0;                 // top line of box
        private int selectedIndex { get; set; } = -1;   // which is selected
        private int focusindex = -1;                // where the focus is at

        private int dropDownHeightMaximum = 400;

        private bool showfocusbox = true;           // normal focus dotted box
        private bool showfocushighlight = false;    // focus selection is highlighted, no highlight selected shown
        private bool highlightSelectedItem = true;  // highlight selected

        private bool showfocusindex = false;        // set on selection or move if we need ensure focus index is showing

    }
}
