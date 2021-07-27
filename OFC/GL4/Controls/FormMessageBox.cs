/*
 * Copyright 2019-2020 Robbyxp1 @ github.com
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

namespace OFC.GL4.Controls
{
    public class GLMessageBox
    {
        public enum MessageBoxButtons
        {
            OK = 0,
            OKCancel = 1,
            AbortRetryIgnore = 2,
            YesNoCancel = 3,
            YesNo = 4,
            RetryCancel = 5
        }

        public GLMessageBox( string logicalname, 
                            GLBaseControl attachto, Point offsetinparent, 
                            Action<GLMessageBox, DialogResult> callback, 
                            string text, string caption, 
                            MessageBoxButtons buttons = MessageBoxButtons.OK, Font fnt = null , 
                            Color? backcolor = null, Color? forecolor = null, bool resizeable = false, bool moveable = true,
                            bool readonlymarked = true )
        {
            callbackfunc = callback;

            if (fnt == null)
                fnt = new Font("Ms Sans Serif", 12);

            GLFormConfigurable cf = new GLFormConfigurable(logicalname);
            cf.TopMost = true;
            cf.Font = fnt;
            if (backcolor != null)
                cf.BackColor = backcolor.Value;
            if (forecolor != null)
                cf.ForeColor = forecolor.Value;
            cf.Resizeable = resizeable;
            cf.Moveable = moveable;

            const int butwidth = 80;
            const int butheight = 20;
            const int textoffsettop = 10;
            const int butspacingundertext = 10;
            const int butxspacing = 20;
            const int textmargin = 10;
            const int windowmargin = 10;
            int horzscrollbarheight = 0;
            bool horzscrollon = false;
            Rectangle textboxpos;

            Size availablespace = attachto.Size;

            GLMultiLineTextBox tb = new GLMultiLineTextBox("MLT", new Rectangle(0,0,100,100), text);

            using (var fmt = new StringFormat())
            {
                fmt.Alignment = StringAlignment.Near;
                fmt.LineAlignment = StringAlignment.Near;

                var textsize = BitMapHelpers.MeasureStringInBitmap(text + (text.EndsWith(Environment.NewLine) ? "AAAA" : ""), fnt, fmt);

                int buts = (buttons == MessageBoxButtons.AbortRetryIgnore || buttons == MessageBoxButtons.YesNoCancel) ? 3 : 2;     // guess of how many, just to set min but width

                int contentwidth = Math.Max((butwidth + butxspacing) * buts + butxspacing, (int)textsize.Width + fnt.Height/4);       // add on a little nerf
                int windowextrawidth =  textmargin + tb.Margin.TotalWidth + tb.Padding.TotalWidth + cf.BorderWidth + cf.ExtraClientMargin.Width;
                int estwidth = contentwidth + windowextrawidth;

                if ( estwidth > availablespace.Width - windowmargin * 2)
                {
                    contentwidth = availablespace.Width - windowmargin * 2 - windowextrawidth;
                    estwidth = contentwidth + windowextrawidth;
                    horzscrollon = true;
                    horzscrollbarheight = fnt.Height;
                }

                int tbheight = tb.NumberOfLines * fnt.Height + fnt.Height / 4 + horzscrollbarheight;   // font is added to nerf up a little to account for rounding

                int estclientarea = textoffsettop + tbheight + butspacingundertext + butheight;     // as measured by FormConfigurable line 442
                int estheight = cf.Margin.TotalHeight + cf.Padding.TotalHeight + cf.BorderWidth + cf.ExtraClientMargin.Height;      // see FormConfigurable line 443 Size=
                estheight += estclientarea ;     // estimate

                if (estheight > availablespace.Height - windowmargin * 2)       // too big
                {
                    estheight -= tbheight;
                    tbheight = (availablespace.Height - windowmargin * 2) - estheight;
                    estheight += tbheight;
                }

                if (offsetinparent.Y + estheight > availablespace.Height)      // make sure not off the bottom
                {
                    offsetinparent.Y = Math.Max(0, availablespace.Height - estheight - windowmargin);
                }

                if (offsetinparent.X + estwidth > availablespace.Width)      // make sure not off the right
                {
                    offsetinparent.X = Math.Max(0, availablespace.Width - estwidth - windowmargin);
                }

                textboxpos = new Rectangle(textmargin, textoffsettop, contentwidth, tbheight);
            }

            int butright = textboxpos.Right - butwidth;
            int butline = textboxpos.Bottom + butspacingundertext;

            if (buttons == MessageBoxButtons.AbortRetryIgnore)
            {
                cf.Add(new GLFormConfigurable.Entry("Ignore", typeof(GLButton), "Ignore", new Point(butright, butline), new Size(butwidth, butheight), null, DialogResult.Ignore));
                cf.Add(new GLFormConfigurable.Entry("Retry", typeof(GLButton), "Retry", new Point(butright-butwidth-butxspacing, butline), new Size(butwidth, butheight), null, DialogResult.Retry));
                cf.Add(new GLFormConfigurable.Entry("OK", typeof(GLButton), "OK", new Point(butright-(butwidth+butxspacing)*2, butline), new Size(butwidth, butheight), null, DialogResult.OK) { taborder = 0 });
            }
            else if (buttons == MessageBoxButtons.OKCancel)
            {
                cf.Add(new GLFormConfigurable.Entry("Cancel", typeof(GLButton), "Cancel", new Point(butright, butline), new Size(butwidth, butheight), null, DialogResult.Cancel) { taborder = 1 });
                cf.Add(new GLFormConfigurable.Entry("OK", typeof(GLButton), "OK", new Point(butright-butwidth-butxspacing, butline), new Size(butwidth, butheight), null, DialogResult.OK) { taborder = 0 });
            }
            else if (buttons == MessageBoxButtons.RetryCancel)
            {
                cf.Add(new GLFormConfigurable.Entry("Retry", typeof(GLButton), "Retry", new Point(butright, butline), new Size(butwidth, butheight), null, DialogResult.Retry));
                cf.Add(new GLFormConfigurable.Entry("OK", typeof(GLButton), "OK", new Point(butright-butwidth-butxspacing, butline), new Size(butwidth, butheight), null, DialogResult.OK) { taborder = 0 });
            }
            else if (buttons == MessageBoxButtons.YesNo)
            {
                cf.Add(new GLFormConfigurable.Entry("No", typeof(GLButton), "No", new Point(butright, butline), new Size(butwidth, butheight), null, DialogResult.No));
                cf.Add(new GLFormConfigurable.Entry("Yes", typeof(GLButton), "Yes", new Point(butright-butwidth-butxspacing, butline), new Size(butwidth, butheight), null, DialogResult.Yes) { taborder = 0 });
            }
            else if (buttons == MessageBoxButtons.YesNoCancel)
            {
                cf.Add(new GLFormConfigurable.Entry("Cancel", typeof(GLButton), "Cancel", new Point(butright, butline), new Size(butwidth, butheight), null, DialogResult.Cancel));
                cf.Add(new GLFormConfigurable.Entry("No", typeof(GLButton), "No", new Point(butright-butwidth-butxspacing, butline), new Size(butwidth, butheight), null, DialogResult.No));
                cf.Add(new GLFormConfigurable.Entry("Yes", typeof(GLButton), "Yes", new Point(butright-(butwidth+butxspacing)*2, butline), new Size(butwidth, butheight), null, DialogResult.Yes) { taborder = 0 });
            }
            else 
            {
                cf.Add(new GLFormConfigurable.Entry("OK", typeof(GLButton), "OK", new Point(butright, butline), new Size(butwidth, butheight), null, DialogResult.OK));
            }

            tb.Bounds = textboxpos;
            tb.BackColor = Color.Transparent;
            tb.ForeColor = cf.ForeColor;
            tb.ScrollBarWidth = horzscrollbarheight;
            tb.ReadOnly = readonlymarked;
            tb.EnableHorizontalScrollBar = horzscrollon;
            tb.EnableVerticalScrollBar = true;
            tb.CursorToTop();
            cf.Add(new GLFormConfigurable.Entry(tb));

            cf.Init(offsetinparent, caption);
            cf.DialogCallback = DialogCallback;
            cf.Tag = this;
            cf.Trigger += (cfg, en, ctrlname, args) =>
            {
                if (ctrlname == "Escape")
                {
                    cf.DialogResult = DialogResult.Abort;
                    cf.Close();
                }
                else
                {
                    cf.DialogResult = (DialogResult)en.tag;
                    cf.Close();
                }
            };

            attachto.AddToDesktop(cf);
        }

        private void DialogCallback(GLForm p, DialogResult r)
        {
            GLMessageBox m = p.Tag as GLMessageBox;
            callbackfunc?.Invoke(m, r);
        }

        private Action<GLMessageBox, DialogResult> callbackfunc;
    }
}

