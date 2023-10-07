/*
 * Copyright 2019-2023 Robbyxp1 @ github.com
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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// Multi line text box control
    /// </summary>
    /// 
    public partial class GLMultiLineTextBox
    {
        #region Text Changing and cursor movement helpers
        private void CalculateTextParameters()        // recalc all the text parameters and arrays
        {
            linelengths.Clear();
            lineendlengths.Clear();

            if (cursorpos < 0 || cursorpos > Text.Length)
            {
                startpos = cursorpos = Text.Length;
            }

            if (startpos < 0 || startpos > Text.Length)
            {
                startpos = cursorpos;
            }

            int cpos = 0;
            int lineno = 0;

            MaxLineLength = 0;

            while (true)
            {
                if (cpos < text.Length)
                {
                    int nextlf = text.IndexOfAny(new char[] { '\r', '\n' }, cpos);

                    if (nextlf == -1)       // no /r/n on last line
                    {
                        nextlf = text.Length;
                        if (cursorpos >= cpos && cursorpos <= nextlf)
                        {
                            cursorlinecpos = cpos;
                            cursorlineno = lineno;
                        }
                        if (startpos >= cpos && startpos <= nextlf)
                        {
                            startlinecpos = cpos;
                        }

                        int len = nextlf - cpos;
                        MaxLineLength = Math.Max(MaxLineLength, len);
                        linelengths.Add(len);
                        lineendlengths.Add(0);
                        break;
                    }
                    else
                    {
                        int el = 1;

                        if (text[nextlf] == '\r')
                        {
                            nextlf++;
                            el = 2;
                        }

                        if (nextlf < text.Length && text[nextlf] == '\n')
                            nextlf++;

                        if (cursorpos >= cpos && cursorpos < nextlf)
                        {
                            cursorlinecpos = cpos;
                            cursorlineno = lineno;
                        }
                        if (startpos >= cpos && startpos < nextlf)
                        {
                            startlinecpos = cpos;
                        }

                        int len = nextlf - cpos;
                        MaxLineLength = Math.Max(MaxLineLength, len - el);
                        linelengths.Add(len);
                        lineendlengths.Add(el);
                        cpos = nextlf;
                    }
                }
                else
                {
                    if (cursorpos == cpos)
                    {
                        cursorlinecpos = cpos;
                        cursorlineno = lineno;
                    }

                    if (startpos == cpos)
                    {
                        startlinecpos = cpos;
                    }

                    linelengths.Add(0);
                    lineendlengths.Add(0);
                    break;
                }

                lineno++;
            }

            //            System.Diagnostics.Debug.WriteLine("RC Max line length " + MaxLineLength);
        }

        // Called at the end of all active moves and edits, ensure area is showing the cursorlineno, vert/horz scroll bar, etc
        private void Finish(bool invalidate, bool clearselection, bool restarttimer)
        {
            if (MaxLineLength < 0)
                CalcMaxLineLengths();

            // ensure cursor line visible

            if (cursorlineno < firstline)
            {
                firstline = cursorlineno;
                invalidate = true;
            }
            else if (cursorlineno >= firstline + CurrentDisplayableLines)
            {
                firstline = cursorlineno - CurrentDisplayableLines + 1;
                invalidate = true;
            }

            // calc startx

            if (displaystartx < 0)      // we start at -1, to indicate not calculated in case paint is called first, calc
                displaystartx = 0;

            int cursoroffset = cursorpos - cursorlinecpos;  // offset into line

            if (displaystartx > cursoroffset)           // if > cursor offset, go back to cursor offset so it shows
            {
                displaystartx = Math.Max(0, cursoroffset - 1);
            }
            else
            {
                Rectangle usablearea = UsableAreaForText();
                Rectangle measurearea = usablearea;
                measurearea.Width = 7000;        // big rectangle so we get all the text in

                using (Bitmap bmp = new Bitmap(1, 1))           // make sure cursor is visible across the line
                {
                    using (Graphics gr = Graphics.FromImage(bmp))
                    {
                        gr.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit; // normal we use in OFC

                        using (var sfmt = new StringFormat())       // measure where the cursor will be and move startx to make it visible
                        {
                            sfmt.Alignment = StringAlignment.Near;
                            sfmt.LineAlignment = StringAlignment.Near;

                            while (true)        // this part sets startx so cursor is visible
                            {
                                string cursorline = GetLineWithoutCRLF(cursorlinecpos, cursorlineno, displaystartx);
                                if (cursorline.IsEmpty())
                                    break;

                                CharacterRange[] characterRanges = { new CharacterRange(0, cursoroffset - displaystartx) };   // measure where the cursor is..
                                sfmt.SetMeasurableCharacterRanges(characterRanges);
                                var rect = gr.MeasureCharacterRanges(cursorline + "@", Font, measurearea, sfmt)[0].GetBounds(gr);    // ensure at least 1 char
                                                                                                                                     //System.Diagnostics.Debug.WriteLine("{0} {1} {2}", startx, cursoroffset, rect);
                                if ((int)(rect.Width + 1) > usablearea.Width - LineHeight)      // Font.Height is to allow for an overlap  
                                {
                                    //System.Diagnostics.Debug.WriteLine("Display start move right");
                                    displaystartx++;
                                }
                                else
                                    break;
                            }
                        }
                    }
                }
            }

            if (vertscroller.Visible)
            {
                //System.Diagnostics.Debug.WriteLine("No lines {0} Max {1}", NumberOfLines, CurrentDisplayableLines);
                bool newstate = NumberOfLines > CurrentDisplayableLines;
                if (newstate != vertscroller.Visible)
                {
                    vertscroller.Visible = newstate;
                    //System.Diagnostics.Debug.WriteLine("Change state to {0}", newstate);
                    invalidate = true;
                }

                if (vertscroller.Visible)
                    vertscroller.SetValueMaximumLargeChange(firstline, NumberOfLines - 1, CurrentDisplayableLines);
            }

            if (horzscroller?.Visible ?? false)
            {
                horzscroller.SetValueMaximumLargeChange(displaystartx, MaxLineLength, 1);
            }

            if (clearselection)
                ClearSelection();

            if (restarttimer)
                CursorTimerRestart();

            if (invalidate)
                Invalidate();

            //System.Diagnostics.Debug.WriteLine("Cpos Line {0} cpos {1} cur {2} off {3} len {4} maxline {5} line '{6}'", cursorlineno, cursorlinecpos, cursorpos, cursorpos - cursorlinecpos, linelengths[cursorlineno], MaxLineLength, GetLineWithoutCRLF(cursorlinecpos,cursorlineno,displaystartx));
        }

        private void CalcMaxLineLengths()       // recalc this when its hard to do it 
        {
            MaxLineLength = 0;
            for (int i = 0; i < linelengths.Count; i++)
                MaxLineLength = Math.Max(MaxLineLength, linelengths[i] - lineendlengths[i]);
            //System.Diagnostics.Debug.WriteLine("Max line length " + MaxLineLength);
        }

        private string GetLineWithoutCRLF(int startpos, int lineno, int offsetin = 0)
        {
            if (startpos < Text.Length)
            {
                int avtext = linelengths[lineno] - lineendlengths[lineno];
                if (offsetin > avtext)
                    return string.Empty;
                else
                    return text.Substring(startpos + offsetin, avtext - offsetin);
            }
            else
                return string.Empty;
        }

        private void InsertTextIntoLineInt(string t, bool insertinplace = false)        // no lf in text, insert text
        {
            int offsetin = cursorpos - cursorlinecpos;
            text = text.Substring(0, cursorpos) + t + text.Substring(cursorpos);
            linelengths[cursorlineno] += t.Length;
            MaxLineLength = Math.Max(MaxLineLength, linelengths[cursorlineno] - lineendlengths[cursorlineno]);  // we made a bigger, line, see if its max
            if (!insertinplace)
                cursorpos += t.Length;
        }

        private void InsertCRLFInt()        // insert cr/lf
        {
            int offsetin = cursorpos - cursorlinecpos;
            int lineleft = linelengths[cursorlineno] - offsetin;
            string s = CRLF ? "\r\n" : "\n";
            text = text.Substring(0, cursorpos) + s + text.Substring(cursorpos);
            linelengths[cursorlineno] = offsetin + s.Length;
            linelengths.Insert(cursorlineno + 1, lineleft);
            lineendlengths.Insert(cursorlineno + 1, lineendlengths[cursorlineno]);  // copy end down
            lineendlengths[cursorlineno] = CRLF ? 2 : 1;    // and set ours to CR type
            cursorpos = cursorlinecpos += linelengths[cursorlineno++];
            MaxLineLength = -1;         // no idea now, recalc thru Ensure.
        }

        private bool DeleteInt()            // delete to right, true if we did it
        {
            int offsetin = cursorpos - cursorlinecpos;

            if (offsetin < linelengths[cursorlineno] - lineendlengths[cursorlineno])   // simple delete
            {
                //System.Diagnostics.Debug.WriteLine("Text '" + text.EscapeControlChars() + "' cursor text '" + text.Substring(cursorpos).EscapeControlChars() + "'");
                text = text.Substring(0, cursorpos) + text.Substring(cursorpos + 1);
                linelengths[cursorlineno]--;
                MaxLineLength = -1;     // we don't know if its the max anymore
                return true;
            }
            else if (cursorpos < Text.Length) // not at end of text
            {
                text = text.Substring(0, cursorpos) + text.Substring(cursorpos + lineendlengths[cursorlineno]); // remove lf/cr from out line
                linelengths[cursorlineno] += linelengths[cursorlineno + 1] - lineendlengths[cursorlineno];   // our line is whole of next less our lf/cr
                linelengths.RemoveAt(cursorlineno + 1);     // next line disappears
                lineendlengths.RemoveAt(cursorlineno);  // and we remove our line ends and keep the next one

                MaxLineLength = Math.Max(MaxLineLength, linelengths[cursorlineno] - lineendlengths[cursorlineno]);  // we made a bigger, line, see if its max
                return true;
            }
            else
                return false;
        }

        private bool DeleteSelectionClearInt()      // true if cleared a selection..
        {
            if (ClearOnFirstChar)
            {
                ClearOnFirstChar = false;           // not true if this is activated
                Clear();
            }
            else if (startpos > cursorpos)
            {
                text = text.Substring(0, cursorpos) + text.Substring(startpos);
                System.Diagnostics.Debug.WriteLine("Delete {0} to {1} text '{2}'", startpos, cursorpos, text.EscapeControlChars());
                startpos = cursorpos;
                CalculateTextParameters();
                return true;
            }
            else if (startpos < cursorpos)
            {
                text = text.Substring(0, startpos) + text.Substring(cursorpos);
                System.Diagnostics.Debug.WriteLine("Delete {0} to {1} text '{2}'", startpos, cursorpos, text.EscapeControlChars());
                cursorpos = startpos;
                CalculateTextParameters();
                return true;
            }

            return false;
        }

        private void CalcLineHeight()
        {
            // so MS sand serif at 8.25 or 12 if you just rely on Font.Height cuts the bottom off. So use a bit of text to find the mininum. Seems ok with Arial/MS Sans Serif
            var area = BitMapHelpers.MeasureStringInBitmap("AAjjqqyyy", Font);
            lineheight = Math.Max(Font.Height + 1, (int)(area.Height + 0.4999));
            //System.Diagnostics.Debug.WriteLine($"Calc Line height {area} {Font.Height} {Font.ToString()} = {LineHeight}");
        }

        private int CurrentDisplayableLines { get 
            { return Math.Max(1, ((ClientRectangle.Height - extrapadding.TotalHeight - textboundary.TotalHeight - ((horzscroller?.Visible ?? false) ? ScrollBarWidth : 0)) / LineHeight)); } 
        }

        #endregion

        #region Cursor Helpers

        private void CursorTimerRestart()
        {
            if (FlashingCursor && Focused)
                cursortimer.Start(1000, 500);

            cursorshowing = true;
        }

        private void CursorTick(PolledTimer t, long tick)
        {
            cursorshowing = !cursorshowing;
            Invalidate();
        }

        #endregion

        #region Overrides

        // called by upper class to say i've changed the Text=value
        private protected override void TextValueChanged()      
        {
            displaystartx = 0;
            SetCursorPos(Text.Length);          // will set to end, cause Calculate and FInish
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnControlAdd(GLBaseControl, GLBaseControl)"/>
        protected override void OnControlAdd(GLBaseControl parent, GLBaseControl child)
        {
            base.OnControlAdd(parent, child);

            if (child == this)
            {
                // font may have changed, we have better do a recalc
                CalcLineHeight();
                Finish(true, false, false);
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnFontChanged"/>
        protected override void OnFontChanged()
        {
            base.OnFontChanged();
            //System.Diagnostics.Debug.WriteLine($"On font changed in MLTB {Font.Name} {Font.Height}");
            CalcLineHeight();
            Finish(invalidate: false, clearselection: false, restarttimer: false);        // no need to invalidate again, it will
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnFocusChanged(FocusEvent, GLBaseControl)"/>
        protected override void OnFocusChanged(FocusEvent evt, GLBaseControl fromto)
        {
            base.OnFocusChanged(evt, fromto);

            if (evt == FocusEvent.Focused)
            {
                CursorTimerRestart();
                Invalidate();
            }
            else
            {
                cursortimer.Stop();
                Invalidate();
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnResize"/>
        protected override void OnResize()
        {
            base.OnResize();
            Finish(invalidate: false, clearselection: false, restarttimer: false);        // no need to invalidate again, it will
        }

        /// <summary>
        /// Override this function in derived classes to see the event without going thru the TextChanged call back
        /// Default is to call OnTextChanged
        /// </summary>
        protected virtual void TextChangedEvent()
        {
            OnTextChanged();
        }

        /// <summary>
        /// Override this function in derived classes for the event of text changing in the box. Default implementation calls TextChanged
        /// </summary>
        protected virtual void OnTextChanged()
        {
            TextChanged?.Invoke(this);
        }

        /// <summary>
        /// Override this to see return pressed events on single line boxes. Default implementation calls ReturnPressed
        /// </summary>
        protected virtual void OnReturnPressed()
        {
            ReturnPressed?.Invoke(this);
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.DrawBack(Rectangle, Graphics, Color, Color, int)"/>
        protected override void DrawBack(Rectangle bounds, Graphics gr, Color bc, Color bcgradientalt, int bcgradient)
        {
            if (InErrorCondition)       // override colour for error condition, so much easier in this scheme than winforms
            {
                bc = BackErrorColor;
                bcgradientalt = BackErrorColor.Multiply(0.9f);
            }

            base.DrawBack(bounds, gr, bc, bcgradientalt, bcgradient);
        }

        #endregion
        
        #region Painting

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.Paint(Graphics)"/>
        protected override void Paint(Graphics gr)
        {
            if (displaystartx < 0)      // no paint if not set up - checking for race conditions
                return;

            //System.Diagnostics.Debug.WriteLine($"Paint MLTB {startpos} {cursorpos} {firstline} {CurrentDisplayableLines}");
            Rectangle usablearea = UsableAreaForText();      // area inside out client rectangle where text is

            if (textAreabackcolor != Color.Transparent)
            {
                if (textAreagradientdir != int.MinValue)
                {
                    using (var b = new System.Drawing.Drawing2D.LinearGradientBrush(usablearea, textAreabackcolor, textAreagradientalt, textAreagradientdir))
                        gr.FillRectangle(b, usablearea);       // linear grad brushes do not respect smoothing mode, btw
                }
                else
                {
                    //System.Diagnostics.Debug.WriteLine("Background " + Name + " " + bounds + " " + backcolor);
                    using (Brush b = new SolidBrush(textAreabackcolor))     // always fill, so we get back to start
                        gr.FillRectangle(b, usablearea);
                }
            }

            var clipregion = gr.Clip.GetBounds(gr);     // get the region which was set up by our caller, this is where we have permission to paint (0,0 = client origin)

            // work out the area, which is useable top/left, with width limited by the usablewidth or the clip left
            Rectangle areatoclip = new Rectangle(usablearea.Left, usablearea.Top, Math.Min((int)clipregion.Width - usablearea.Left, usablearea.Width), Math.Min((int)clipregion.Height - usablearea.Top, usablearea.Height));

            //            System.Diagnostics.Debug.WriteLine($"{Name} Clip reset to {usablearea} {ClientRectangle} current clip {clipregion} -> {areatoclip}");

            gr.SetClip(areatoclip);     // so we don't paint outside of this

            //using (Pen p = new Pen(this.ForeColor))   {  gr.DrawLine(p, new Point(0, 0), new Point(100,100));   }


            using (Brush textb = new SolidBrush(Enabled ? this.ForeColor : this.ForeColor.Multiply(ForeDisabledScaling)))
            {
                int lineno = 0;
                int cpos = 0;
                while (lineno < firstline)
                    cpos += linelengths[lineno++];

                using (var pfmt = new StringFormat())   // for some reasons, using set measurable characters above on fmt screws it up when it comes to paint, vs not using it
                {
                    pfmt.Alignment = StringAlignment.Near;
                    pfmt.LineAlignment = StringAlignment.Near;
                    pfmt.FormatFlags = StringFormatFlags.NoWrap;

                    int bottom = usablearea.Bottom;
                    usablearea.Height = LineHeight;        // move the area down the screen progressively

                    while (usablearea.Top < bottom)       // paint each line
                    {
                        if (!lineColor.IsFullyTransparent())        // lined paper
                        {
                            using (Pen p = new Pen(Color.Green))
                            {
                                gr.DrawLine(p, new Point(usablearea.Left, usablearea.Bottom - 1), new Point(usablearea.Right - 1, usablearea.Bottom - 1));
                            }
                        }

                        int highlightstart = 0, highlightend = 0;

                        if (startpos < cursorpos)       // start less than cursor, so estimate s/e this way. 
                        {
                            highlightstart = cpos == startlinecpos ? startpos - startlinecpos : 0;
                            highlightend = (cpos >= startlinecpos && cpos < cursorlinecpos) ? (linelengths[lineno] - lineendlengths[lineno]) : cpos == cursorlinecpos ? (cursorpos - cursorlinecpos) : 0;
                        }
                        else if (startpos > cursorpos)    // other way
                        {
                            highlightstart = cpos == cursorlinecpos ? cursorpos - cursorlinecpos : 0;
                            highlightend = (cpos >= cursorlinecpos && cpos < startlinecpos) ? (linelengths[lineno] - lineendlengths[lineno]) : cpos == startlinecpos ? (startpos - startlinecpos) : 0;
                        }

                        if (highlightstart != 0 || highlightend != 0)       // if set, we need to offset by startx. this may result in 0,0, turning the highlight off
                        {
                            highlightstart = Math.Max(highlightstart - displaystartx, 0);        // offset by startx, min 0.
                            highlightend = Math.Max(highlightend - displaystartx, 0);          // and the end points
                        }

                        string s = GetLineWithoutCRLF(cpos, lineno, displaystartx);   // text without cr/lf, empty if none

                        if (highlightstart != 0 || highlightend != 0)       // and highlight if on
                        {
                            //System.Diagnostics.Debug.WriteLine("{0} {1}-{2}", cpos, highlightstart, highlightend);

                            using (var sfmt = new StringFormat())   // new measurer, don't trust reuse
                            {
                                sfmt.Alignment = StringAlignment.Near;
                                sfmt.LineAlignment = StringAlignment.Near;
                                sfmt.FormatFlags = StringFormatFlags.NoWrap;

                                CharacterRange[] characterRanges = { new CharacterRange(highlightstart, highlightend - highlightstart) };
                                sfmt.SetMeasurableCharacterRanges(characterRanges);
                                var rect = gr.MeasureCharacterRanges(s + "@", Font, usablearea, sfmt)[0].GetBounds(gr);    // ensure at least 1 char, need to do it in area otherwise it does not works:

                                using (Brush b1 = new SolidBrush(HighlightColor))
                                {
                                    gr.FillRectangle(b1, rect);
                                }
                            }
                        }

                        if (s.Length > 0)
                        {
                            //System.Diagnostics.Debug.WriteLine($"Draw '{s}' into text box {usablearea} cr {ClientRectangle} b { Bounds} font {Font.ToString()} {Font.Height}");

                            gr.DrawString(s, Font, textb, usablearea, pfmt);        // need to paint to pos not in an area
                            // useful for debug

                            //Pen pr = new Pen(Color.Red);
                            //Pen pg = new Pen(Color.Green);
                            //using (Bitmap b = new Bitmap(1000, 100))
                            //{
                            //    using (Graphics grx = Graphics.FromImage(b))
                            //    {
                            //        grx.TextRenderingHint = gr.TextRenderingHint;     // recommended default for measuring text

                            //        for (int i = 0; i < s.Length; i++)    // we have to do it one by one, as the query is limited to 32 char ranges
                            //        {
                            //            using (var fmt = new StringFormat())
                            //            {
                            //                fmt.Alignment = StringAlignment.Near;
                            //                fmt.LineAlignment = StringAlignment.Near;
                            //                fmt.FormatFlags = StringFormatFlags.NoWrap;

                            //                CharacterRange[] characterRanges = { new CharacterRange(i, 1) };
                            //                fmt.SetMeasurableCharacterRanges(characterRanges);

                            //                var rect = grx.MeasureCharacterRanges(s, Font, usablearea, fmt)[0].GetBounds(grx);
                            //                if (rect.Width == 0)
                            //                    break;
                            //                if ( !pone )
                            //                    System.Diagnostics.Debug.WriteLine("Paint Region " + rect + " char " + i + " " + s[i]);
                            //                gr.DrawLine(pr, new Point(usablearea.X + (int)rect.Left, usablearea.Y + 15), new Point(usablearea.X + (int)rect.Left, usablearea.Y + 30));
                            //                gr.DrawLine(pg, new Point(usablearea.X + (int)rect.Right - 1, usablearea.Y + 15), new Point(usablearea.X + (int)rect.Right - 1, usablearea.Y + 30));
                            //            }
                            //        }
                            //    }
                            //    pone = true;
                            //    pr.Dispose();
                            //    pg.Dispose();
                            //}
                        }

                        int cursoroffset = cursorpos - cursorlinecpos - displaystartx;

                        if (cursorlineno == lineno && Enabled && Focused && cursorshowing && cursoroffset >= 0)
                        {
                            using (var sfmt = new StringFormat())
                            {
                                sfmt.Alignment = StringAlignment.Near;
                                sfmt.LineAlignment = StringAlignment.Near;
                                sfmt.FormatFlags = StringFormatFlags.NoWrap;

                                CharacterRange[] characterRanges = { new CharacterRange(cursoroffset, 1) };   // find character at cursor offset left and width

                                string t = GetLineWithoutCRLF(cursorlinecpos, cursorlineno, displaystartx) + "i";       // add an extra character for the end, small

                                sfmt.SetMeasurableCharacterRanges(characterRanges);
                                var rect = gr.MeasureCharacterRanges(t, Font, usablearea, sfmt)[0].GetBounds(gr);    // ensure at least 1 char, need to do it in area otherwise it does not works:

                                //System.Diagnostics.Debug.WriteLine(" Offset '{0}' {1} {2} {3} {4}", t.Substring(cursoroffset), cursoroffset, characterRanges[0].First, characterRanges[0].Length, rect);
                                //using (Pen p = new Pen(this.ForeColor)) { gr.DrawRectangle(p, new Rectangle((int)rect.Left, (int)rect.Top, (int)rect.Width, (int)rect.Height)); }

                                int charwidth = (int)rect.Width;
                                int cursorxpos = (int)rect.Left;

                                if (insert == false)
                                {
                                    using (Brush b = new SolidBrush(Color.FromArgb(64, this.ForeColor)))        // overwrite mode paints forecolor over the char with low alpha
                                    {
                                        gr.FillRectangle(b, new Rectangle(cursorxpos, usablearea.Y, charwidth, LineHeight - 2));
                                    }
                                }
                                else
                                {
                                    using (Pen p = new Pen(this.ForeColor))     // a solid bar
                                    {
                                        gr.DrawLine(p, new Point(cursorxpos, usablearea.Y), new Point(cursorxpos, usablearea.Y + LineHeight - 2));
                                    }
                                }
                            }
                        }

                        if (lineno == linelengths.Count() - 1)   // valid line, last entry in lines is the terminating pos
                            break;

                        cpos += linelengths[lineno];
                        usablearea.Y += LineHeight;
                        lineno++;
                    }
                }
            }
        }

        private Rectangle UsableAreaForText()
        {
            Rectangle clientarea = ClientRectangle;

            Rectangle usablearea = new Rectangle(clientarea.Left + TextMargin.Left + ExtraPadding.Left,
                                                clientarea.Top + TextMargin.Top + ExtraPadding.Top,
                                                clientarea.Width - TextMargin.TotalWidth - ExtraPadding.TotalWidth,
                                                clientarea.Height - TextMargin.TotalHeight - ExtraPadding.TotalHeight);

            //  System.Diagnostics.Debug.WriteLine($"Usable area {clientarea} -> {TextBoundary} + {reservedboundary} => {usablearea}");

            if (vertscroller?.Visible ?? false)         // take into account text boundary, scroll bars
                usablearea.Width -= ScrollBarWidth;
            if (horzscroller?.Visible ?? false)
                usablearea.Height -= ScrollBarWidth;

            if (!MultiLineMode)     // if we are in no LF mode (ie. Textbox) then use TextAlign to format area
            {
                usablearea = TextAlign.ImagePositionFromContentAlignment(usablearea, new Size(usablearea.Width, LineHeight));
            }

            return usablearea;
        }

        #endregion

        #region UI

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnKeyDown(GLKeyEventArgs)"/>
        protected override void OnKeyDown(GLKeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!e.Handled)
            {
                //System.Diagnostics.Debug.WriteLine("KDown " + Name + " " + e.KeyCode);

                if (e.KeyCode == System.Windows.Forms.Keys.Left)
                    CursorLeft(!e.Shift);
                else if (e.KeyCode == System.Windows.Forms.Keys.Right)
                    CursorRight(!e.Shift);
                else if (e.KeyCode == System.Windows.Forms.Keys.Down)
                    CursorDown(!e.Shift);
                else if (e.KeyCode == System.Windows.Forms.Keys.Up)
                    CursorUp(!e.Shift);
                else if (e.KeyCode == System.Windows.Forms.Keys.PageUp)
                    CursorUp(!e.Shift, CurrentDisplayableLines - 1);
                else if (e.KeyCode == System.Windows.Forms.Keys.PageDown)
                    CursorDown(!e.Shift, CurrentDisplayableLines - 1);
                else if (e.KeyCode == System.Windows.Forms.Keys.Delete)
                {
                    if (e.Shift)
                        Cut();
                    else
                        Delete();
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.Home)
                    Home(!e.Shift);
                else if (e.KeyCode == System.Windows.Forms.Keys.End)
                    End(!e.Shift);
                else if (e.KeyCode == System.Windows.Forms.Keys.Insert)
                {
                    if (e.Control)
                        Copy();
                    else if (e.Shift)
                        Paste();
                    else
                    {
                        insert = !insert;
                        Invalidate();
                    }
                }

                else if (e.KeyCode == System.Windows.Forms.Keys.F1)
                    InsertTextWithCRLF("Hello\rThere\rFred");
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnKeyPress(GLKeyEventArgs)"/>
        protected override void OnKeyPress(GLKeyEventArgs e)
        {
            base.OnKeyPress(e);
            if (!e.Handled)
            {
                if (e.KeyChar == 8)
                {
                    Backspace();
                }
                else if (e.KeyChar == 3)    // ctrl-c
                {
                    Copy();
                }
                else if (e.KeyChar == 22)    // ctrl-v
                {
                    Paste();
                }
                else if (e.KeyChar == 24)    // ctrl-x
                {
                    Cut();
                }
                else if (e.KeyChar == 13)
                {
                    if (MultiLineMode)
                        InsertCRLF();
                    else
                        OnReturnPressed();
                }
                else if (!char.IsControl(e.KeyChar) || AllowControlChars)
                {
                    if (insert)
                    {
                        InsertText(new string(e.KeyChar, 1));
                    }
                    else
                    {
                        OverwriteText(new string(e.KeyChar, 1));
                    }
                }
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseDown(GLMouseEventArgs)"/>
        protected override void OnMouseDown(GLMouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!e.Handled && e.Button == GLMouseEventArgs.MouseButtons.Left)
            {
                int xcursorpos = FindCursorPositionFromPoint(e.Location, out int xlinecpos, out int xlineno);

                if (xcursorpos >= 0)
                {
                    cursorlinecpos = xlinecpos;
                    cursorlineno = xlineno;
                    cursorpos = xcursorpos;
                    Finish(invalidate: true, clearselection: !e.Shift, restarttimer: true);
                }
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseMove(GLMouseEventArgs)"/>
        protected override void OnMouseMove(GLMouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!e.Handled && e.Button == GLMouseEventArgs.MouseButtons.Left)
            {
                int xcursorpos = FindCursorPositionFromPoint(e.Location, out int xlinecpos, out int xlineno);

                if (xcursorpos >= 0)
                {
                    cursorlinecpos = xlinecpos;
                    cursorlineno = xlineno;
                    cursorpos = xcursorpos;
                    Invalidate();
                }
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseClick(GLMouseEventArgs)"/>
        protected override void OnMouseClick(GLMouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (!e.Handled && e.Button == GLMouseEventArgs.MouseButtons.Right)
            {
                RightClickMenu.Font = RightClickMenuFont ?? Font;
                RightClickMenu.ApplyToControlOfName("MTLBEdit*", a => a.Enabled = !ReadOnly);
                RightClickMenu.Show(this.FindDisplay(), e.ScreenCoord);
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseWheel(GLMouseEventArgs)"/>
        protected override void OnMouseWheel(GLMouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (e.Delta < 0)
            {
                FirstDisplayedLine += 1;
                vertscroller.Value = FirstDisplayedLine;
            }
            else
            {
                FirstDisplayedLine -= 1;
                vertscroller.Value = FirstDisplayedLine;
            }
        }


        #endregion

        #region Scroll Bars

        private void ScrollBars(bool vert, bool horz)
        {
            vertscroller.Visible = vert;
            vertscroller.Padding = new PaddingType(0, 0, 0, horz ? ScrollBarWidth : 0);
            horzscroller.Visible = horz;

            //remove for now
            //      if (horzscroller?.Visible ?? false)
            //       horzscroller.Padding = new Padding(0, 0, vert ? ScrollBarWidth : 0, 0);

            Invalidate();
        }

        #endregion

        #region Vars

        // cursor and marker info

        private int cursorpos = int.MaxValue; // set on text set if invalid
        private int startpos = int.MaxValue; // set on text set if invalid
        private int firstline = 0;  // first line to display
        private int displaystartx = -1; // first character to display

        // Calculated by CalculateTextParameters - these are independent of font/area size etc.
        // Includes MaxLineLength

        private List<int> linelengths = new List<int>(); // computed on text set, includes space for cr/lf
        private List<int> lineendlengths = new List<int>(); // computed on text set, 0 = none, 1 = lf, 2 = cr/lf

        private int cursorlineno;   // computed on text set, updated by all moves/inserts
        private int cursorlinecpos;   // computed on text set, updated by all moves/inserts, start of current line
        private int startlinecpos;   // computed on text set, updated by all moves/inserts, start of current line

        // Display

        private Color highlightColor { get; set; } = DefaultTextBoxHighlightColor;
        private Color lineColor { get; set; } = Color.Transparent;
        private Color backerrorcolor { get; set; } = DefaultTextBoxErrorColor;
        private bool inerror = false;

        private int lineheight;

        private int scrollbarwidth = 20;

        private bool cursorshowing = true;  // is cursor currently showing

        private PolledTimer cursortimer = new PolledTimer();

        private GLScrollBar vertscroller;
        private GLScrollBar horzscroller;

        private bool insert = true;

        private MarginType textboundary = new MarginType(0);
        private PaddingType extrapadding = new PaddingType(0);

        private Color textAreabackcolor = Color.Transparent;
        private Color textAreagradientalt = Color.Red;
        private int textAreagradientdir = int.MinValue;           // in degrees

        //bool pone = false;      // debugging only

        #endregion

    }
}

