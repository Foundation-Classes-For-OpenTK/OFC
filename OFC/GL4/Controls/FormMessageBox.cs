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

namespace GLOFC.GL4.Controls
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
                            Color? backcolor = null, Color? forecolor = null, bool moveable = true,
                            bool readonlymarked = true )
        {
            callbackfunc = callback;

            if (fnt == null)
                fnt = new Font("Ms Sans Serif", 12);

            GLFormConfigurable cf = new GLFormConfigurable(logicalname);
            cf.SuspendLayout();
            cf.TopMost = true;
            cf.Font = fnt;
            if (backcolor != null)
                cf.BackColor = backcolor.Value;
            if (forecolor != null)
                cf.ForeColor = forecolor.Value;


            GLMultiLineTextBox tb = new GLMultiLineTextBox("MLT", new Rectangle(0,0,100,100), text);
            tb.SuspendLayout();
            tb.Font = fnt;
            tb.BackColor = Color.Transparent;
            tb.ForeColor = cf.ForeColor;
            tb.ReadOnly = readonlymarked;
            tb.EnableVerticalScrollBar = true;
            tb.CursorToTop();

            const int butwidth = 80;
            const int butheight = 20;
            const int textoffsettop = 10;
            const int butspacingundertext = 8;
            const int butxspacing = 20;
            const int textmargin = 10;
            const int windowmargin = 10;
            Size availablespace = attachto.Size;

            int buts = (buttons == MessageBoxButtons.AbortRetryIgnore || buttons == MessageBoxButtons.YesNoCancel) ? 3 : 2;     // guess of how many, just to set min but width
            int buttonswidth = (butwidth + butxspacing) * buts + butxspacing;
            int windowextrawidth = textmargin + tb.ClientWidthMargin + cf.AutoSizeClientMargin.Width;
            int availablewidthforclient = availablespace.Width - windowextrawidth - windowmargin * 2;

            int windowsextraheight = textoffsettop + butspacingundertext + butheight + cf.ClientHeightMargin + cf.AutoSizeClientMargin.Height;
            int availableheightforclient = availablespace.Height - windowsextraheight - windowmargin * 2;

            var estsize = tb.CalculateTextArea(new Size(buttonswidth, 24), new Size(availablewidthforclient, availableheightforclient));

            tb.Bounds = new Rectangle(textmargin, textoffsettop, estsize.Item1.Width, estsize.Item1.Height);
            tb.EnableHorizontalScrollBar = estsize.Item2;
            tb.ResumeLayout();

            int butright = tb.Bounds.Right - butwidth;
            int butline = tb.Bounds.Bottom + butspacingundertext;

            if (buttons == MessageBoxButtons.AbortRetryIgnore)
            {
                cf.Add(new GLFormConfigurable.Entry("Ignore", typeof(GLButton), "Ignore", new Point(butright, butline), new Size(butwidth, butheight), null, DialogResult.Ignore) { TabOrder = 2, Anchor = AnchorType.DialogButtonLine });
                cf.Add(new GLFormConfigurable.Entry("Retry", typeof(GLButton), "Retry", new Point(butright - butwidth - butxspacing, butline), new Size(butwidth, butheight), null, DialogResult.Retry) { TabOrder = 1, Anchor = AnchorType.DialogButtonLine });
                cf.Add(new GLFormConfigurable.Entry("OK", typeof(GLButton), "OK", new Point(butright - (butwidth + butxspacing) * 2, butline), new Size(butwidth, butheight), null, DialogResult.OK) { TabOrder = 0, Anchor = AnchorType.DialogButtonLine });
            }
            else if (buttons == MessageBoxButtons.OKCancel)
            {
                cf.Add(new GLFormConfigurable.Entry("Cancel", typeof(GLButton), "Cancel", new Point(butright, butline), new Size(butwidth, butheight), null, DialogResult.Cancel) { TabOrder = 1, Anchor = AnchorType.DialogButtonLine });
                cf.Add(new GLFormConfigurable.Entry("OK", typeof(GLButton), "OK", new Point(butright - butwidth - butxspacing, butline), new Size(butwidth, butheight), null, DialogResult.OK) { TabOrder = 0, Anchor = AnchorType.DialogButtonLine });
            }
            else if (buttons == MessageBoxButtons.RetryCancel)
            {
                cf.Add(new GLFormConfigurable.Entry("Retry", typeof(GLButton), "Retry", new Point(butright, butline), new Size(butwidth, butheight), null, DialogResult.Retry) { TabOrder = 1, Anchor = AnchorType.DialogButtonLine });
                cf.Add(new GLFormConfigurable.Entry("OK", typeof(GLButton), "OK", new Point(butright - butwidth - butxspacing, butline), new Size(butwidth, butheight), null, DialogResult.OK) { TabOrder = 0, Anchor = AnchorType.DialogButtonLine });
            }
            else if (buttons == MessageBoxButtons.YesNo)
            {
                cf.Add(new GLFormConfigurable.Entry("No", typeof(GLButton), "No", new Point(butright, butline), new Size(butwidth, butheight), null, DialogResult.No) { TabOrder = 1, Anchor = AnchorType.DialogButtonLine });
                cf.Add(new GLFormConfigurable.Entry("Yes", typeof(GLButton), "Yes", new Point(butright - butwidth - butxspacing, butline), new Size(butwidth, butheight), null, DialogResult.Yes) { TabOrder = 0, Anchor = AnchorType.DialogButtonLine });
            }
            else if (buttons == MessageBoxButtons.YesNoCancel)
            {
                cf.Add(new GLFormConfigurable.Entry("Cancel", typeof(GLButton), "Cancel", new Point(butright, butline), new Size(butwidth, butheight), null, DialogResult.Cancel) { TabOrder = 2, Anchor = AnchorType.DialogButtonLine });
                cf.Add(new GLFormConfigurable.Entry("No", typeof(GLButton), "No", new Point(butright - butwidth - butxspacing, butline), new Size(butwidth, butheight), null, DialogResult.No) { TabOrder = 1, Anchor = AnchorType.DialogButtonLine });
                cf.Add(new GLFormConfigurable.Entry("Yes", typeof(GLButton), "Yes", new Point(butright - (butwidth + butxspacing) * 2, butline), new Size(butwidth, butheight), null, DialogResult.Yes) { TabOrder = 0, Anchor = AnchorType.DialogButtonLine });
            }
            else
            {
                cf.Add(new GLFormConfigurable.Entry("OK", typeof(GLButton), "OK", new Point(butright, butline), new Size(butwidth, butheight), null, DialogResult.OK));
            }

            cf.Add(new GLFormConfigurable.Entry("MTL-MB",tb));

            cf.Init(offsetinparent, caption);

            cf.Tag = this;
            cf.DialogCallback = DialogCallback;
            cf.Trigger += (cfg, en, ctrlname, args) =>
            {
                if (ctrlname == "Escape")
                {
                    cf.DialogResult = DialogResult.Abort;
                    cf.Close();
                }
                else
                {
                    cf.DialogResult = (DialogResult)en.Tag;
                    cf.Close();
                }
            };

            cf.ResumeLayout();
            attachto.AddToDesktop(cf);
            cf.Moveable = moveable;
        }

        private void DialogCallback(GLForm p, DialogResult r)
        {
            GLMessageBox m = p.Tag as GLMessageBox;
            callbackfunc?.Invoke(m, r);
        }

        private Action<GLMessageBox, DialogResult> callbackfunc;
    }
}

