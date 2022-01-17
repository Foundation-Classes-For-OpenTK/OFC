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


namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// A Form control
    /// Forms are usually placed below DisplayControl, but can act as movable controls inside other controls
    /// </summary>

    public class GLForm : GLForeDisplayTextBase
    {
        /// <summary> Is from shown? </summary>
        public bool FormShown { get; set; } = false;        // only applies to top level forms
        /// <summary> Tab will change focus of controls on form </summary>
        public bool TabChangesFocus { get; set; } = true;   // tab works
        /// <summary> Show close symbol</summary>
        public bool ShowClose { get; set; } = true;         // show close symbol
        /// <summary> Is Resizeable by user </summary>
        public bool Resizeable { get; set; } = true;        // resize works
        /// <summary> Is Movable by user </summary>
        public bool Moveable { get; set; } = true;          // move window works

        /// <summary> Callback when form shown </summary>
        public Action<GLForm> Shown;
        /// <summary> Callback when user asks to close it. Set Handled = true to stop close</summary>
        public Action<GLForm,GLHandledArgs> FormClosing;
        /// <summary> Form has closed </summary>
        public Action<GLForm> FormClosed;

        /// <summary> Dialog result when form is a dilaog </summary>
        public enum DialogResultEnum
        {
            /// <summary> No result </summary>
            None = 0,
            /// <summary> OK </summary>
            OK = 1,
            /// <summary> Cancel </summary>
            Cancel = 2,
            /// <summary> Abort </summary>
            Abort = 3,
            /// <summary> Retry </summary>
            Retry = 4,
            /// <summary> Ignore </summary>
            Ignore = 5,
            /// <summary> Yes </summary>
            Yes = 6,
            /// <summary> No </summary>
            No = 7
        }

        /// <summary> Dialog result from form </summary>
        public DialogResultEnum DialogResult { get { return dialogResult; } set { SetDialogResult(value); }  }
        /// <summary> Callback saying dialog result changed</summary>
        public Action<GLForm, DialogResultEnum> DialogResultChanged { get; set; } 

        // Form can AutoSize to client content. 
        /// <summary> Autosize left/bottom client margin. Extra space to add left/bottom when autosizing </summary>
        public Size AutoSizeClientMargin { get; set; } = new Size(10, 10);         
        /// <summary> Autosize should take account of the title length </summary>
        public bool AutoSizeToTitle { get; set; } = false;             

        /// <summary> When set, Autosize should set MinimumSize to this on autosize, so the form can't be sized below this </summary>
        public bool SetMinimumSizeOnAutoSize { get; set; } = false;                
        /// <summary> Title bar height computed</summary>
        public int TitleBarHeight { get { return (Font?.ScalePixels(20) ?? 20) + FormMargins * 2; } }


        /// <summary> Constructor with name, title and bounds</summary>
        public GLForm(string name, string title, Rectangle location) : base(name, location)
        {
            minimumsize = new Size(32, 32);
            text = title;
            ForeColor = DefaultFormTextColor;
            BackColor = DefaultFormBackColor;
            SetNI(padding: new PaddingType(FormPadding), margin: new MarginType(FormMargins,TitleBarHeight ,FormMargins,FormMargins), borderwidth: FormBorderWidth);
            BorderColorNI = DefaultFormBorderColor;
            Focusable = true;           // we can focus, but we always pass on the focus to the first child focus
        }

        /// <summary> Default constructor </summary>
        public GLForm() : this("F?", "", DefaultWindowRectangle)
        {
        }

        /// <summary> Close form. May not close if FormClosing denies it </summary>
        public void Close()
        {
            GLHandledArgs e = new GLHandledArgs();
            OnClose(e);
            if ( !e.Handled )
            {
                OnClosed();
                Remove(this);
            }
        }

        /// <summary> Force a close </summary>
        public void ForceClose()    
        {
            OnClosed();
            Remove(this);
        }

        #region For inheritors

        /// <summary> Called on shown. Override if required in derived classes </summary>
        public virtual void OnShown()   // only called if top level form
        {
            lastchildfocus = FindNextTabChild(-1);      // try the tab order
            if (lastchildfocus != null)     // if found a focusable child, set it
                lastchildfocus.SetFocus();
            Shown?.Invoke(this);
        }

        /// <summary> Called on close request. Override if required in derived classes </summary>
        public virtual void OnClose(GLHandledArgs e)   
        {
            FormClosing?.Invoke(this, e);
        }

        /// <summary> Called on closed. Override if required in derived classes </summary>
        public virtual void OnClosed()   
        {
            FormClosed?.Invoke(this);
        }

        #endregion

        #region Implementation

        private void SetDialogResult(DialogResultEnum v)
        {
            dialogResult = v;
            DialogResultChanged?.Invoke(this, dialogResult);
        }

        private protected override void TextValueChanged()      // called by upper class to say i've changed the text.
        {
            Invalidate();
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnFontChanged"/>
        protected override void OnFontChanged()
        {
            base.OnFontChanged();
            Margin = new MarginType(Margin.Left, TitleBarHeight, Margin.Right, Margin.Bottom);
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.SizeControl(Size)"/>
        protected override void SizeControl(Size parentsize)
        {
            // Form autosizer, taking into consideration all objects without autoplacement
            // and optionally the title text size

            base.SizeControl(parentsize);
            if (AutoSize)
            {
                Rectangle area = VisibleChildArea(x=>(x.Anchor & AnchorType.AutoPlacement) == 0);   // get the clients area (ignore autoplaced items and autosize to it)

                if (AutoSizeToTitle)        // if required, take into considering the title text
                {
                    using (var fmt = ControlHelpersStaticFunc.StringFormatFromContentAlignment(TextAlign))
                    {
                        var titlearea = GLOFC.Utils.BitMapHelpers.MeasureStringInBitmap(Text, Font, fmt);
                        area.Width = Math.Max(area.Width, (int)titlearea.Width + (ShowClose ? TitleBarHeight : 0) - ClientLeftMargin);
                    }
                }

                Size s = new Size(area.Left + area.Width + AutoSizeClientMargin.Width, area.Top + area.Height + AutoSizeClientMargin.Height);
                //System.Diagnostics.Debug.WriteLine($"Form {Name} Clients {area} -> {s}");
                SetNI(clientsize: s);

                if (SetMinimumSizeOnAutoSize)
                    MinimumSize = Size;
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.DrawBorder(Graphics, Color, float)"/>
        protected override void DrawBorder(Graphics gr, Color bc, float bw)     
        {
            base.DrawBorder(gr, bc, bw);    // draw basic border

            Color c = (Enabled) ? this.ForeColor : this.ForeColor.Multiply(ForeDisabledScaling);

            if (Text.HasChars())
            {
                using (var fmt = ControlHelpersStaticFunc.StringFormatFromContentAlignment(TextAlign))
                {
                    using (Brush textb = new SolidBrush(c))
                    {
                        Rectangle titlearea = new Rectangle(0,0, Width-(ShowClose?TitleBarHeight:0), TitleBarHeight);
                        gr.DrawString(this.Text, this.Font, textb, titlearea, fmt);
                    }
                }

            }

            if ( ShowClose )
            {
                Rectangle closearea = new Rectangle(Width- TitleBarHeight, 0, TitleBarHeight, TitleBarHeight);
                closearea.Inflate(new Size(-6,-6));

                using (Pen p = new Pen(c))
                {
                    gr.DrawLine(p, new Point(closearea.Left, closearea.Top), new Point(closearea.Right, closearea.Bottom));
                    gr.DrawLine(p, new Point(closearea.Left, closearea.Bottom), new Point(closearea.Right, closearea.Top));
                }
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseDown(GLMouseEventArgs)"/>
        protected override void OnMouseDown(GLMouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (!e.Handled && e.Area != GLMouseEventArgs.AreaType.Client )
            {
                if (!OverClose(e))
                {
                    capturelocation = e.ScreenCoord;
                    originalwindow = Bounds;
                    captured = e.Area;
                }
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseMove(GLMouseEventArgs)"/>
        protected override void OnMouseMove(GLMouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.Handled == false )
            {
                //System.Diagnostics.Debug.WriteLine("Form Mouse move " + e.Location + " " +  e.Area);
                if (captured != GLMouseEventArgs.AreaType.Client)       // Client meaning none
                {
                    Point curscrlocation = e.ScreenCoord; 
                    Point capturedelta = new Point(curscrlocation.X - capturelocation.X, curscrlocation.Y - capturelocation.Y);
                    //System.Diagnostics.Debug.WriteLine("Form " + captured + " " + e.Location + " " + capturelocation + " " + capturedelta);

                    if (Resizeable)
                    {
                        if (captured == GLMouseEventArgs.AreaType.Right)
                        {
                            int right = originalwindow.Right + capturedelta.X;
                            Width = right - originalwindow.Left;
                        }
                        else if (captured == GLMouseEventArgs.AreaType.Bottom)
                        {
                            int bottom = originalwindow.Bottom + capturedelta.Y;
                            Height = bottom - originalwindow.Top;
                        }
                        else if (captured == GLMouseEventArgs.AreaType.NWSE)
                        {
                            int right = originalwindow.Right + capturedelta.X;
                            int bottom = originalwindow.Bottom + capturedelta.Y;
                            int width = right - originalwindow.Left;
                            int height = bottom - originalwindow.Top;
                            Size = new Size(width, height);
                        }
                        else if (captured == GLMouseEventArgs.AreaType.Left && Moveable)
                        {
                            int left = originalwindow.Left + capturedelta.X;
                            int width = originalwindow.Right - left;
                            Bounds = new Rectangle(left, originalwindow.Top, width, originalwindow.Height);
                        }
                    }

                    if (Moveable)
                    {
                        if (captured == GLMouseEventArgs.AreaType.Top)
                        {
                            if (originalwindow.Top + capturedelta.Y >= 0 &&
                                originalwindow.Left + capturedelta.X + 16 < FindDisplay().Width &&
                                originalwindow.Left + capturedelta.X + Width - 40 >= 0)        // limit so can't go off screen
                            {
                                Location = new Point(originalwindow.Left + capturedelta.X, originalwindow.Top + capturedelta.Y);
                            }

                         //   System.Diagnostics.Debug.WriteLine("Drag to {0}", Location);
                        }
                    }
                }
                else
                {
                    // look at where we are pointing, and change cursor appropriately

                    if ( OverClose(e))      // we could animate close, but that requires invalidating the whole form, too much just for this
                    {
                        //System.Diagnostics.Debug.WriteLine("Over form close");
                    }
                    else if ((e.Area == GLMouseEventArgs.AreaType.Left && Moveable && Resizeable) || (e.Area == GLMouseEventArgs.AreaType.Right && Resizeable))
                    {
                        Cursor = GLWindowControl.GLCursorType.EW;
                    }
                    else if (Moveable && e.Area == GLMouseEventArgs.AreaType.Top )
                    {
                        Cursor = GLWindowControl.GLCursorType.Move;
                    }
                    else if (Resizeable && e.Area == GLMouseEventArgs.AreaType.Bottom)
                    {
                        Cursor = GLWindowControl.GLCursorType.NS;
                    }
                    else if (Resizeable && e.Area == GLMouseEventArgs.AreaType.NWSE)
                    {
                        Cursor = GLWindowControl.GLCursorType.NWSE;
                    }
                    else 
                    {
                        Cursor = GLWindowControl.GLCursorType.Normal;
                    }
                }
            }
        }


        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseUp(GLMouseEventArgs)"/>
        protected override void OnMouseUp(GLMouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (captured != GLMouseEventArgs.AreaType.Client)
            {
                Cursor = GLWindowControl.GLCursorType.Normal;
                captured = GLMouseEventArgs.AreaType.Client;
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseClick(GLMouseEventArgs)"/>
        protected override void OnMouseClick(GLMouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (!e.Handled)
            {
                if (OverClose(e))
                {
                    Close();
                }
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnKeyDown(GLKeyEventArgs)"/>
        protected override void OnKeyDown(GLKeyEventArgs e)       // forms gets first dibs at keys of children
        {
            base.OnKeyDown(e);
            //System.Diagnostics.Debug.WriteLine("Form key " + e.KeyCode);
            if (!e.Handled && TabChangesFocus && e.KeyCode == System.Windows.Forms.Keys.Tab)
            {
                bool forward = e.Shift == false;
                GLBaseControl next = FindNextTabChild(lastchildfocus?.TabOrder??-1,forward);
                if (next == null)
                    next = FindNextTabChild(forward ?-1 : int.MaxValue,forward);
                if (next != null)
                {
                    lastchildfocus = next;
                    next.SetFocus();
                }

                e.Handled = true;
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnFocusChanged(FocusEvent, GLBaseControl)"/>
        protected override void OnFocusChanged(FocusEvent evt, GLBaseControl fromto) // called if we get focus (focused=true) or if child gets focused (focused=false)
        {
            if (evt == FocusEvent.ChildFocused)     // need to take a note
            {
                if (ControlsZ.Contains(fromto))
                {
                    //System.Diagnostics.Debug.WriteLine("Form saw child focused {0} '{1}'", evt, fromto?.Name);
                    lastchildfocus = fromto;
                }
            }
            else if (evt == FocusEvent.Focused)     // we got focus, hand off to child
            {
                if (lastchildfocus != null)
                {
                    lastchildfocus.SetFocus();
                    //System.Diagnostics.Debug.WriteLine("Form focus, focus on child");
                }
            }
        }

        private bool OverClose(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("Over close {0} {1} {2} {3}", e.Area == GLMouseEventArgs.AreaType.Top && e.Location.X >= Width - TitleBarHeight, e.Area, e.Location.X , Width - TitleBarHeight);
            return ShowClose && e.Area == GLMouseEventArgs.AreaType.Top && e.Location.X >= Width - TitleBarHeight;
        }

        private GLMouseEventArgs.AreaType captured = GLMouseEventArgs.AreaType.Client;  // meaning none
        private Point capturelocation;
        private Rectangle originalwindow;
        private DialogResultEnum dialogResult = DialogResultEnum.None;
        private GLBaseControl lastchildfocus = null;

        private const int FormMargins = 2;
        private const int FormPadding = 2;
        private const int FormBorderWidth = 1;

        #endregion
    }
}


