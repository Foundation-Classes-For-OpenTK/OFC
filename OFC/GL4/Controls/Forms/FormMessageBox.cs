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
using System.Drawing;
using static GLOFC.GL4.Controls.GLBaseControl;
using static GLOFC.GL4.Controls.GLForm;

namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// Message box form
    /// </summary>
    public class GLMessageBox
    {
        /// <summary> Message box type </summary>
        public enum MessageBoxButtons
        {
            /// <summary> OK only</summary>
            OK = 0,
            /// <summary> OK and Cancel</summary>
            OKCancel = 1,
            /// <summary> Abort, Retry and Ignore</summary>
            AbortRetryIgnore = 2,
            /// <summary> Yes, No and Cancel </summary>
            YesNoCancel = 3,
            /// <summary> Yes or No</summary>
            YesNo = 4,
            /// <summary> Retry or Cancel</summary>
            RetryCancel = 5
        }

        /// <summary> Display a modal message box </summary>
        /// <param name="name">Name of message box</param>
        /// <param name="parent">Give a control to use to find the display control</param>
        /// <param name="location">Location relative to parent. Set to X=int.MinValue for center</param>
        /// <param name="text">Text for message box</param>
        /// <param name="caption">Caption for message box </param>
        /// <param name="buttons">What buttons to display (OK is the default)</param>
        /// <param name="font">What font to display the text in (null = parent font)</param>
        /// <param name="backcolor">Back color of box (null = default)</param>
        /// <param name="forecolor">Fore color (null = default)</param>
        /// <param name="callback">Callback function, called when user has made a selection or hit close (result Cancel). May be null</param>
        /// <param name="moveable">Indicate if dialog should be moveable</param>
        /// <param name="readonlymarked">Indicate if text is editable</param>

        public static void Show(string name,
                            GLBaseControl parent, Point location,
                            string text, string caption,
                            MessageBoxButtons buttons = MessageBoxButtons.OK, Font font = null,
                            Color? backcolor = null, Color? forecolor = null,
                            Action<GLMessageBox, DialogResultEnum> callback = null,
                            bool moveable = true,
                            bool readonlymarked = true)
        {
            new GLMessageBox(name, parent, location, text, caption, buttons, font, backcolor, forecolor, callback, moveable, readonlymarked,true);
        }

        /// <summary> Construct and display a message box </summary>
        /// <param name="name">Name of message box</param>
        /// <param name="parent">Give a control to use to find the display control</param>
        /// <param name="location">Location relative to parent. Set to X=int.MinValue for center</param>
        /// <param name="text">Text for message box</param>
        /// <param name="caption">Caption for message box </param>
        /// <param name="buttons">What buttons to display (OK is the default)</param>
        /// <param name="font">What font to display the text in (null = parent font)</param>
        /// <param name="backcolor">Back color of box (null = default)</param>
        /// <param name="forecolor">Fore color (null = default)</param>
        /// <param name="callback">Callback function, called when user has made a selection or hit close (result Cancel). May be null</param>
        /// <param name="moveable">Indicate if dialog should be moveable</param>
        /// <param name="readonlymarked">Indicate if text is editable</param>
        /// <param name="modal">Is the form modal</param>

        public GLMessageBox( string name, 
                            GLBaseControl parent, Point location, 
                            string text, string caption, 
                            MessageBoxButtons buttons = MessageBoxButtons.OK, Font font = null , 
                            Color? backcolor = null, Color? forecolor = null,
                            Action<GLMessageBox, DialogResultEnum> callback = null,
                            bool moveable = true,
                            bool readonlymarked = true,
                            bool modal = false)
        {
            System.Diagnostics.Trace.Assert(parent != null, "Must give parent in FormMessageBox");

            callbackfunc = callback;

            if (font == null)
                font = new Font("Ms Sans Serif", 12);

            GLFormConfigurable cf = new GLFormConfigurable(name);
            cf.TopMost = true;
            cf.Font = font;
            if (backcolor != null)
                cf.BackColor = backcolor.Value;
            if (forecolor != null)
                cf.ForeColor = forecolor.Value;

            GLMultiLineTextBox tb = new GLMultiLineTextBox("MLT", new Rectangle(0,0,100,100), text);
            tb.Font = font;
            tb.BackColor = Color.Transparent;
            tb.ForeColor = cf.ForeColor;
            tb.ReadOnly = readonlymarked;
            tb.EnableVerticalScrollBar = true;
            tb.CursorToTop();

            const int butwidth = 80;
            const int butheight = 28;
            const int textoffsettop = 10;
            const int butspacingundertext = 8;
            const int butxspacing = 20;
            const int textmargin = 4;
            const int windowmargin = 10;
            Size availablespace = parent.Size;

            int windowextrawidth = textmargin + tb.ClientWidthMargin + cf.AutoSizeClientMargin.Width;
            int availablewidthforclient = availablespace.Width - windowextrawidth - windowmargin * 2;

            int windowsextraheight = textoffsettop + butspacingundertext + butheight + cf.ClientHeightMargin + cf.AutoSizeClientMargin.Height;
            int availableheightforclient = availablespace.Height - windowsextraheight - windowmargin * 2;

            var estsize = tb.CalculateTextArea(new Size(20, 24), new Size(availablewidthforclient, availableheightforclient));
            tb.Bounds = new Rectangle(textmargin, textoffsettop, estsize.Item1.Width, estsize.Item1.Height);
            tb.EnableHorizontalScrollBar = estsize.Item2;

            int butright = tb.Bounds.Right - butwidth;
            int butline = tb.Bounds.Bottom + butspacingundertext;

            if (buttons == MessageBoxButtons.AbortRetryIgnore)
            {
                cf.Add(new GLFormConfigurable.Entry("Ignore", typeof(GLButton), "Ignore", new Point(butright, butline), new Size(butwidth, butheight), null, DialogResultEnum.Ignore) { TabOrder = 0, Anchor = AnchorType.AutoPlacement });
                cf.Add(new GLFormConfigurable.Entry("Retry", typeof(GLButton), "Retry", new Point(butright - butwidth - butxspacing, butline), new Size(butwidth, butheight), null, DialogResultEnum.Retry) { TabOrder = 1, Anchor = AnchorType.AutoPlacement });
                cf.Add(new GLFormConfigurable.Entry("OK", typeof(GLButton), "OK", new Point(butright - (butwidth + butxspacing) * 2, butline), new Size(butwidth, butheight), null, DialogResultEnum.OK) { TabOrder = 2, Anchor = AnchorType.AutoPlacement });
            }
            else if (buttons == MessageBoxButtons.OKCancel)
            {
                cf.Add(new GLFormConfigurable.Entry("Cancel", typeof(GLButton), "Cancel", new Point(butright, butline), new Size(butwidth, butheight), null, DialogResultEnum.Cancel) { TabOrder = 0, Anchor = AnchorType.AutoPlacement });
                cf.Add(new GLFormConfigurable.Entry("OK", typeof(GLButton), "OK", new Point(butright - butwidth - butxspacing, butline), new Size(butwidth, butheight), null, DialogResultEnum.OK) { TabOrder = 1, Anchor = AnchorType.AutoPlacement });
            }
            else if (buttons == MessageBoxButtons.RetryCancel)
            {
                cf.Add(new GLFormConfigurable.Entry("Retry", typeof(GLButton), "Retry", new Point(butright, butline), new Size(butwidth, butheight), null, DialogResultEnum.Retry) { TabOrder = 0, Anchor = AnchorType.AutoPlacement });
                cf.Add(new GLFormConfigurable.Entry("OK", typeof(GLButton), "OK", new Point(butright - butwidth - butxspacing, butline), new Size(butwidth, butheight), null, DialogResultEnum.OK) { TabOrder = 1, Anchor = AnchorType.AutoPlacement });
            }
            else if (buttons == MessageBoxButtons.YesNo)
            {
                cf.Add(new GLFormConfigurable.Entry("No", typeof(GLButton), "No", new Point(butright, butline), new Size(butwidth, butheight), null, DialogResultEnum.No) { TabOrder = 0, Anchor = AnchorType.AutoPlacement });
                cf.Add(new GLFormConfigurable.Entry("Yes", typeof(GLButton), "Yes", new Point(butright - butwidth - butxspacing, butline), new Size(butwidth, butheight), null, DialogResultEnum.Yes) { TabOrder = 1, Anchor = AnchorType.AutoPlacement });
            }
            else if (buttons == MessageBoxButtons.YesNoCancel)
            {
                cf.Add(new GLFormConfigurable.Entry("Cancel", typeof(GLButton), "Cancel", new Point(butright, butline), new Size(butwidth, butheight), null, DialogResultEnum.Cancel) { TabOrder = 0, Anchor = AnchorType.AutoPlacement });
                cf.Add(new GLFormConfigurable.Entry("No", typeof(GLButton), "No", new Point(butright - butwidth - butxspacing, butline), new Size(butwidth, butheight), null, DialogResultEnum.No) { TabOrder = 1, Anchor = AnchorType.AutoPlacement });
                cf.Add(new GLFormConfigurable.Entry("Yes", typeof(GLButton), "Yes", new Point(butright - (butwidth + butxspacing) * 2, butline), new Size(butwidth, butheight), null, DialogResultEnum.Yes) { TabOrder = 2, Anchor = AnchorType.AutoPlacement });
            }
            else
            {
                cf.Add(new GLFormConfigurable.Entry("OK", typeof(GLButton), "OK", new Point(butright, butline), new Size(butwidth, butheight), null, DialogResultEnum.OK));
            }

            cf.Add(new GLFormConfigurable.Entry("MTL-MB",tb));

            if ( location.X == int.MinValue )
                cf.InitCentered(caption);
            else
                cf.Init(location, caption);

            cf.Tag = this;
            cf.Trigger += (cfg, en, ctrlname, args) =>
            {
                if ( ctrlname == "Close")       
                {
                    if ( !sentcallback)         // close gets called if form is closing, but we may have already below sent a callbackfunc
                        callbackfunc?.Invoke(this, DialogResultEnum.Cancel);
                }
                else if (ctrlname == "Escape" )
                {
                    cf.DialogResult = DialogResultEnum.Abort;
                    callbackfunc?.Invoke(this, cf.DialogResult);
                    sentcallback = true;
                    cf.Close();
                }
                else
                {
                    cf.DialogResult = (DialogResultEnum)en.Tag;
                    callbackfunc?.Invoke(this, cf.DialogResult);
                    sentcallback = true;
                    cf.Close();
                }
            };

            cf.Owner = parent;              // associate with parent
            
            GLControlDisplay cd = parent.FindDisplay();
            System.Diagnostics.Trace.Assert(cd != null, "Can't find display control in FormMessageBox");
            
            if (modal)
                cd.AddModalForm(cf);
            else
                cd.Add(cf);

            cf.AutoSize = false;            // now we turn autosize off, and allow it to move
            cf.Moveable = moveable;
            tb.Width = cf.ClientWidth - textmargin*2;
        }

        private Action<GLMessageBox, DialogResultEnum> callbackfunc;
        private bool sentcallback = false;
    }
}

