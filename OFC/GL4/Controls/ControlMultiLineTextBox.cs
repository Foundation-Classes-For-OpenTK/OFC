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
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OFC.GL4.Controls
{
    public class GLMultiLineTextBox : GLForeDisplayTextBase
    {
        public Action<GLBaseControl> TextChanged { get; set; } = null;      // not fired by programatically changing Text
        public Action<GLBaseControl> ReturnPressed { get; set; } = null;    // not fired by programatically changing Text, only if AllowLF = false

        public bool CRLF { get; set; } = true;                              // set to determine CRLF or LF is used
        public bool MultiLineMode { get; set; } = true;                     // clear to prevent multiline, TextAlign then determines alignment in box (vert only)
        public bool ClearOnFirstChar { get; set; } = false;                 // clear on first char
        public bool AllowControlChars { get; set; } = false;                // other controls chars allowed
        public bool ReadOnly { get; set; } = false;                         // can edit it
        public Margin TextBoundary { get; set; } = new Margin(0);           // limit text area
        public Color HighlightColor { get { return highlightColor; } set { highlightColor = value; Invalidate(); } }       // of text
        public Color LineColor { get { return lineColor; } set { lineColor = value; Invalidate(); } }       // lined text, default off
        public bool FlashingCursor { get; set; } = true;

        public bool IsSelectionSet { get { return startpos != cursorpos; } }
        public int SelectionStart { get { return Math.Min(startpos, cursorpos); } }
        public int SelectionEnd { get { return Math.Max(startpos, cursorpos); } }

        public bool InErrorCondition { get { return inerror; } set { inerror = value; Invalidate(); } }
        public Color BackErrorColor { get { return backerrorcolor; } set { backerrorcolor = value; Invalidate(); } }
        public int NumberOfLines { get { return linelengths.Count(); } }

        public int FirstDisplayedLine { get { return firstline; } set { firstline = Math.Max(0, Math.Min(value, NumberOfLines - CurrentDisplayableLines)); Invalidate(); } }
        public int FirstDisplayedCharacter { get { return displaystartx; } set { displaystartx = Math.Max(0, Math.Min(value, MaxLineLength)); Invalidate(); } }

        public int MaxLineLength { get; private set; } = 0;

        public bool EnableVerticalScrollBar { get { return vertscroller != null; } set { ScrollBars(value, horzscroller != null); } }
        public bool EnableHorizontalScrollBar { get { return horzscroller != null; } set { ScrollBars(vertscroller != null, value); } }
        public int ScrollBarWidth { get { return scrollbarwidth; } set { scrollbarwidth = value; Finish(true, true, true); } }

        public GLMultiLineTextBox(string name, Rectangle pos, string text) : base(name, pos)
        {
            Focusable = true;
            this.text = text;
            cursortimer.Tick += CursorTick;
            CalculateTextParameters();
            Finish(false, false, false);
        }

        public GLMultiLineTextBox() : this("TBML?", DefaultWindowRectangle, "")
        {
        }

        #region Public Interface

        public void SetCursorPos(int p)
        {
            if (p >= 0 && p <= Text.Length)
            {
                startpos = cursorpos = p;
                if (cursorpos < Text.Length && text[cursorpos] == '\n' && cursorpos > 0 && text[cursorpos - 1] == '\r') // if on a \r\n at \n, need to move back 1 more to disallow
                    cursorpos--;

                CalculateTextParameters();      // reset text paras
                Finish(invalidate: true, clearmarkers: false, restarttimer: true);
            }
        }

        public void CursorLeft(bool clearmarkers = false, int count = 1)
        {
            while (count > 0 && cursorpos>0)
            {
                int backupby = Math.Min(count, cursorpos - cursorlinecpos + 1);     // compute max to back up on this line. The +1 allow you to back 1 beyond the left
                count -= backupby;
                cursorpos -= backupby;

                if (cursorpos < cursorlinecpos)
                {
                    cursorlinecpos -= linelengths[--cursorlineno];      // go back 1 line
                    if (lineendlengths[cursorlineno] == 2)              // and back another one if double length marker
                        cursorpos--;
                }
            }

            Finish(invalidate: true, clearmarkers: clearmarkers, restarttimer: true);
        }

        public void CursorRight(bool clearmarkers = false, int count = 1)
        {
            int nextlinecpos = cursorlinecpos + linelengths[cursorlineno];      // end of line, including any /r/n
            int nextmarkerpos = nextlinecpos - lineendlengths[cursorlineno];

            while (count > 0 && cursorpos < nextlinecpos )                    // will only occur if nextlinecpos has no /r/n, i.e., end of text
            {
                int goforwardby = Math.Min(count, nextmarkerpos - cursorpos + 1);   // go forward maximum..
                count -= goforwardby;
                cursorpos += goforwardby;

                if (cursorpos > nextmarkerpos)        // if past and on /r/n etc
                {
                    cursorlinecpos = cursorpos = nextlinecpos;
                    cursorlineno++;
                    nextlinecpos = cursorlinecpos + linelengths[cursorlineno];
                    nextmarkerpos = nextlinecpos - lineendlengths[cursorlineno];
                }
            }

            Finish(invalidate: true, clearmarkers: clearmarkers, restarttimer: true);
        }

        public void CursorDown(bool clearmarkers = false, int count = 1)
        {
            while(count-- > 0 && cursorlineno < linelengths.Count() - 1)
            {
                int offsetin = cursorpos - cursorlinecpos;
                cursorlinecpos += linelengths[cursorlineno++];
                cursorpos = cursorlinecpos + Math.Min(offsetin, linelengths[cursorlineno] - lineendlengths[cursorlineno]);
            }

            Finish(invalidate: true, clearmarkers: clearmarkers, restarttimer: true);
        }

        public void CursorUp(bool clearmarkers = false, int count = 1)
        {
            while (count-- > 0 && cursorlineno > 0)
            {
                int offsetin = cursorpos - cursorlinecpos;
                cursorlinecpos -= linelengths[--cursorlineno];
                cursorpos = cursorlinecpos + Math.Min(offsetin, linelengths[cursorlineno] - lineendlengths[cursorlineno]);
            }

            Finish(invalidate: true, clearmarkers: clearmarkers, restarttimer: true);
        }

        public void Home(bool clearmarkers = false)
        {
            cursorpos = cursorlinecpos;
            Finish(invalidate: true, clearmarkers: clearmarkers, restarttimer: true);
        }

        public void End(bool clearmarkers = false)
        {
            cursorpos = cursorlinecpos + linelengths[cursorlineno] - lineendlengths[cursorlineno];
            Finish(invalidate: true, clearmarkers: clearmarkers, restarttimer: true);
        }

        public void InsertTextWithCRLF(string str, bool insertinplace = false)        // any type of lf/cr combo, replaced by selected combo
        {
            if (!ReadOnly)
            {
                DeleteSelectionClearInt();         

                int cpos = 0;
                while (true)
                {
                    if (cpos < str.Length)
                    {
                        int nextlf = str.IndexOfAny(new char[] { '\r', '\n' }, cpos);

                        if (nextlf >= 0)
                        {
                            InsertTextIntoLineInt(str.Substring(cpos, nextlf - cpos), insertinplace);
                            InsertCRLFInt();

                            if (str[nextlf] == '\r')
                            {
                                nextlf++;
                            }

                            if (nextlf < str.Length && str[nextlf] == '\n')
                                nextlf++;

                            cpos = nextlf;
                        }
                        else
                        {
                            InsertTextIntoLineInt(str.Substring(cpos), insertinplace);
                            break;
                        }
                    }
                }

                Finish(invalidate: true, clearmarkers: true, restarttimer: true);
                OnTextChanged();
            }
        }

        public void InsertText(string t, bool insertinplace = false)        // no lf in text
        {
            if (!ReadOnly)
            {
                DeleteSelectionClearInt();
                InsertTextIntoLineInt(t, insertinplace);
                Finish(invalidate: true, clearmarkers: true, restarttimer: true);
                OnTextChanged();
            }
        }

        public void InsertCRLF()        // insert the selected cr/lf pattern
        {
            if (!ReadOnly)
            {
                DeleteSelectionClearInt();
                InsertCRLFInt();
                Finish(invalidate: true, clearmarkers: true, restarttimer: true);
                OnTextChanged();
            }
        }

        public void Backspace()
        {
            if (!ReadOnly)
            {
                if (!DeleteSelection())     // if we deleted a selection, no other action
                {
                    int offsetin = cursorpos - cursorlinecpos;

                    if (offsetin > 0)   // simple backspace
                    {
                        //System.Diagnostics.Debug.WriteLine("Text '" + text.EscapeControlChars() + "' cursor text '" + text.Substring(cursorpos).EscapeControlChars() + "'");
                        text = text.Substring(0, cursorpos - 1) + text.Substring(cursorpos);
                        linelengths[cursorlineno]--;
                        cursorpos--;
                        MaxLineLength = -1;     // we don't know if this is the maximum any more, need to recalc
                        Finish(invalidate: true, clearmarkers: true, restarttimer: true);
                        OnTextChanged();
                    }
                    else if (cursorlinecpos > 0)    // not at start of text
                    {
                        cursorlinecpos -= linelengths[--cursorlineno];      // back 1 line
                        int textlen = linelengths[cursorlineno] - lineendlengths[cursorlineno];
                        text = text.Substring(0, cursorpos - lineendlengths[cursorlineno]) + text.Substring(cursorpos); // remove lf/cr from previous line
                        linelengths[cursorlineno] = textlen + linelengths[cursorlineno + 1];
                        lineendlengths[cursorlineno] = lineendlengths[cursorlineno + 1];        // copy end type
                        cursorpos = cursorlinecpos + textlen;
                        linelengths.RemoveAt(cursorlineno + 1);
                        lineendlengths.RemoveAt(cursorlineno + 1);
                        MaxLineLength = Math.Max(MaxLineLength, linelengths[cursorlineno] - lineendlengths[cursorlineno]);  // we made a bigger, line, see if its max
                        Finish(invalidate: true, clearmarkers: true, restarttimer: true);
                        OnTextChanged();
                    }
                }
            }
        }

        public void Delete()
        {
            if (!ReadOnly)
            {
                if (!DeleteSelection())      // if we deleted a selection, no other action
                {
                    int offsetin = cursorpos - cursorlinecpos;

                    if (offsetin < linelengths[cursorlineno] - lineendlengths[cursorlineno])   // simple delete
                    {
                        //System.Diagnostics.Debug.WriteLine("Text '" + text.EscapeControlChars() + "' cursor text '" + text.Substring(cursorpos).EscapeControlChars() + "'");
                        text = text.Substring(0, cursorpos) + text.Substring(cursorpos + 1);
                        linelengths[cursorlineno]--;
                        MaxLineLength = -1;     // we don't know if its the max anymore

                        Finish(invalidate: true, clearmarkers: true, restarttimer: true);
                        OnTextChanged();
                    }
                    else if (cursorpos < Text.Length) // not at end of text
                    {
                        text = text.Substring(0, cursorpos) + text.Substring(cursorpos + lineendlengths[cursorlineno]); // remove lf/cr from out line
                        linelengths[cursorlineno] += linelengths[cursorlineno + 1] - lineendlengths[cursorlineno];   // our line is whole of next less our lf/cr
                        linelengths.RemoveAt(cursorlineno + 1);     // next line disappears
                        lineendlengths.RemoveAt(cursorlineno);  // and we remove our line ends and keep the next one

                        MaxLineLength = Math.Max(MaxLineLength, linelengths[cursorlineno] - lineendlengths[cursorlineno]);  // we made a bigger, line, see if its max

                        Finish(invalidate: true, clearmarkers: true, restarttimer: true);
                        OnTextChanged();
                    }
                }
            }
        }

        public void CursorToTop()
        {
            startpos = firstline = cursorpos = 0;
            CalculateTextParameters();                      // will correct for out of range start/cursor pos
            Finish(invalidate: true, clearmarkers: false, restarttimer: true);
        }

        public void SetSelection(int start, int end)        // set equal to cancel, else set start/end pos
        {
            startpos = Math.Min(start, end);
            cursorpos = Math.Max(start, end);
            CalculateTextParameters();                      // will correct for out of range start/cursor pos
            Finish(invalidate: true, clearmarkers: false, restarttimer: true);
        }

        public void ClearSelection()
        {
            startpos = cursorpos;
            CalculateTextParameters();
            Finish(invalidate: true, clearmarkers: false, restarttimer: true);
        }

        public bool DeleteSelection()
        {
            if (!ReadOnly && DeleteSelectionClearInt())
            {
                Finish(invalidate: true, clearmarkers: false, restarttimer: true);
                OnTextChanged();
                return true;
            }
            else
                return false;
        }

        public string SelectedText
        {
            get
            {
                if (IsSelectionSet)
                {
                    int min = Math.Min(startpos, cursorpos);
                    int max = Math.Max(startpos, cursorpos);
                    return text.Substring(min, max - min);
                }
                else
                    return null;
            }
        }

        public void Copy()
        {
            string sel = SelectedText;
            if (sel != null)
                System.Windows.Forms.Clipboard.SetText(sel);
        }

        public void Cut()
        {
            if (!ReadOnly)
            {
                string sel = SelectedText;
                if (sel != null)
                {
                    System.Windows.Forms.Clipboard.SetText(sel);
                    DeleteSelection();
                }
            }
        }

        public void Paste()
        {
            if (!ReadOnly)
            {
                string s = System.Windows.Forms.Clipboard.GetText(System.Windows.Forms.TextDataFormat.UnicodeText);
                if (!s.IsEmpty())
                    InsertTextWithCRLF(s);
            }
        }

        public void Clear()
        {
            if (!ReadOnly)
            {
                text = string.Empty;
                cursorpos = startpos = 0;
                MaxLineLength = 0;
                CalculateTextParameters();
                Finish(invalidate: true, clearmarkers: false, restarttimer: true);
            }
        }

        #endregion

        #region Text Changing and cursor movement helpers

        protected void CalculateTextParameters()        // recalc all the text parameters and arrays
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
                            startlineno = lineno;
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
                            startlineno = lineno;
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
                        startlineno = lineno;
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

        private void Finish(bool invalidate, bool clearmarkers, bool restarttimer)   
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
                Rectangle usablearea = UsableAreaForText(ClientRectangle);
                Rectangle measurearea = usablearea;
                measurearea.Width = 7000;        // big rectangle so we get all the text in

                using (Bitmap bmp = new Bitmap(1, 1))
                {
                    using (Graphics gr = Graphics.FromImage(bmp))
                    {
                        gr.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;     // recommended default for measuring text

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
                                if ((int)(rect.Width + 1) > usablearea.Width - Font.Height)      // Font.Height is to allow for an overlap  
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

            if (vertscroller != null)
            {
                //System.Diagnostics.Debug.WriteLine("No lines {0} Max {1}", NumberOfLines, CurrentDisplayableLines);
                bool newstate = NumberOfLines > CurrentDisplayableLines;
                if (newstate != vertscroller.Visible)
                {
                    vertscroller.Visible = newstate;
                    //System.Diagnostics.Debug.WriteLine("Change state to {0}", newstate);
                    invalidate = true;
                }

                //if (horzscroller != null)
                //{
                //    //horzscroller.Padding = new Padding(0, 0, newstate ? ScrollBarWidth : 0, 0);
                //}

                if (vertscroller.Visible)
                    vertscroller.SetValueMaximumLargeChange(firstline, NumberOfLines-1 , CurrentDisplayableLines);
            }

            if ( horzscroller?.Visible??false )
            {
                horzscroller.SetValueMaximumLargeChange(displaystartx, MaxLineLength, 1);
            }
            
            if (clearmarkers)
                ClearMarkers();

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

        protected override void TextValueChanged()      // called by upper class to say i've changed the text.
        {
            displaystartx = 0;
            SetCursorPos(Text.Length);          // will set to end, cause Calculate and FInish
        }

        private void ClearMarkers()
        {
            startlinecpos = cursorlinecpos;
            startlineno = cursorlineno;
            startpos = cursorpos;
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

        #endregion

        #region Other implementation

        protected override void OnControlAdd(GLBaseControl parent, GLBaseControl child)    
        {
            base.OnControlAdd(parent, child);
            if (child == this && !IsFontDefined)    // if we have not defined a font, on attach we need to finish again, because the font has changed from the default
                Finish(true, false, false);
        }

        protected override void OnFontChanged()
        {
            base.OnFontChanged();
            Finish(invalidate: false, clearmarkers: false, restarttimer: false);        // no need to invalidate again, it will
        }

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

        protected override void OnResize()
        {
            base.OnResize();
            Finish(invalidate: false, clearmarkers: false, restarttimer: false);        // no need to invalidate again, it will
        }

        private void CursorTimerRestart()
        {
            if (FlashingCursor && Focused)
                cursortimer.Start(1000, 500);

            cursorshowing = true;
        }

        private void CursorTick(OFC.Timers.Timer t, long tick)
        {
            cursorshowing = !cursorshowing;
            Invalidate();
        }

        // override the draw back for error conditions

        protected override void DrawBack(Rectangle bounds, Graphics gr, Color bc, Color bcgradientalt, int bcgradient)
        {
            if (InErrorCondition)       // override colour for error condition, so much easier in this scheme than winforms
            {
                bc = BackErrorColor;
                bcgradientalt = BackErrorColor.Multiply(0.9f);
            }

            base.DrawBack(bounds, gr, bc, bcgradientalt, bcgradient);
        }

        private Rectangle UsableAreaForText(Rectangle clientarea)
        {
            Rectangle usablearea = new Rectangle(clientarea.Left + TextBoundary.Left, clientarea.Top + TextBoundary.Top, clientarea.Width - TextBoundary.TotalWidth, clientarea.Height - TextBoundary.TotalHeight);

            if (vertscroller?.Visible ?? false)         // take into account text boundary, scroll bars
                usablearea.Width -= ScrollBarWidth;
            if (horzscroller?.Visible ?? false)
                usablearea.Height -= ScrollBarWidth;

            if (!MultiLineMode)     // if we are in no LF mode (ie. Textbox) then use TextAlign to format area
            {
                usablearea = TextAlign.ImagePositionFromContentAlignment(usablearea, new Size(usablearea.Width, Font.Height));
            }

            return usablearea;
        }

        protected override void Paint(Graphics gr)
        {
            if (displaystartx < 0)      // no paint if not set up - checking for race conditions
                return;

            System.Diagnostics.Debug.WriteLine($"{startpos} {cursorpos} {firstline} {CurrentDisplayableLines}");
            Rectangle usablearea = UsableAreaForText(ClientRectangle);
            gr.SetClip(usablearea);     // so we don't paint outside of this

            using (Brush textb = new SolidBrush(Enabled ? this.ForeColor : this.ForeColor.Multiply(DisabledScaling)))
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
                    usablearea.Height = Font.Height;        // move the area down the screen progressively

                    //gr.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;     // all rendering and measurement uses this

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
                            System.Diagnostics.Debug.WriteLine("Paint with " + Font);
                            gr.DrawString(s, Font, textb, usablearea, pfmt);        // need to paint to pos not in an area
                        }

                        int cursoroffset = cursorpos - cursorlinecpos - displaystartx;

                        if (cursorlineno == lineno && Enabled && Focused && cursorshowing && cursoroffset >= 0 )
                        {
                            int cursorxpos = -1;

                            using (var sfmt = new StringFormat())
                            {
                                sfmt.Alignment = StringAlignment.Near;
                                sfmt.LineAlignment = StringAlignment.Near;
                                sfmt.FormatFlags = StringFormatFlags.NoWrap;

                                CharacterRange[] characterRanges = { new CharacterRange(0, Math.Max(cursoroffset, 1)) };   // if offset=0, 1 char and we use the left pos

                                string t = GetLineWithoutCRLF(cursorlinecpos, cursorlineno, displaystartx) + "a";
                                //System.Diagnostics.Debug.WriteLine(" Offset '{0}' {1} {2} {3}", t.Substring(cursoroffset), cursoroffset, characterRanges[0].First, characterRanges[0].Length);

                                sfmt.SetMeasurableCharacterRanges(characterRanges);
                                var rect = gr.MeasureCharacterRanges(t, Font, usablearea, sfmt)[0].GetBounds(gr);    // ensure at least 1 char, need to do it in area otherwise it does not works:

                                //using (Pen p = new Pen(this.ForeColor)) { gr.DrawRectangle(p, new Rectangle((int)rect.Left, (int)rect.Top, (int)rect.Width, (int)rect.Height)); }

                                if (cursoroffset == 0)
                                    cursorxpos = (int)rect.Left;
                                else if (rect.Right < usablearea.Width)
                                    cursorxpos = (int)rect.Right-1;

                                //System.Diagnostics.Debug.WriteLine(" Measured {0} -> {1}", rect, cursorxpos);
                            }

                            if (cursorxpos >= 0)
                            {
                                using (Pen p = new Pen(this.ForeColor))
                                {
                                    gr.DrawLine(p, new Point(cursorxpos, usablearea.Y), new Point(cursorxpos, usablearea.Y + Font.Height - 2));
                                }
                            }
                        }

                        if (lineno == linelengths.Count() - 1)   // valid line, last entry in lines is the terminating pos
                            break;

                        cpos += linelengths[lineno];
                        usablearea.Y += Font.Height;
                        lineno++;
                    }

                    //gr.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;     // back to system default
                }
            }
        }

        #endregion

        #region UI

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
                }

                else if (e.KeyCode == System.Windows.Forms.Keys.F1)
                    InsertTextWithCRLF("Hello\rThere\rFred");
            }
        }

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
                    InsertText(new string(e.KeyChar, 1));
                }
            }
        }

        protected virtual void OnTextChanged()
        {
            TextChanged?.Invoke(this);
        }

        protected virtual void OnReturnPressed()
        {
            ReturnPressed?.Invoke(this);
        }

        private int FindCursorPos(Point click, out int cpos, out int lineno)
        {
            Rectangle usablearea = UsableAreaForText(ClientRectangle);
            usablearea.Width = 7000;        // set so chars don't clip and extend across

            lineno = cpos = -1;

            if (click.Y < usablearea.Y)
                return -1;

            int lineoffset = (click.Y - usablearea.Y) / Font.Height;

            int lineclicked = Math.Min(firstline + lineoffset, linelengths.Count - 1);

            lineno = 0;
            cpos = 0;
            while (lineno < lineclicked)            // setting cpos and lineno
                cpos += linelengths[lineno++];

            string s = GetLineWithoutCRLF(cpos, lineno, displaystartx);

            if (s.Length == 0)  // no text, means its to the left, so click on end
                return cpos + linelengths[lineno] - lineendlengths[lineno];

            using (Bitmap b = new Bitmap(1, 1))
            {
                using (Graphics gr = Graphics.FromImage(b))
                {
                    gr.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;     // recommended default for measuring text
                                                                                                
                    for (int i = 0; i < s.Length; i++)    // we have to do it one by one, as the query is limited to 32 char ranges
                    {
                        using (var fmt = new StringFormat())
                        {
                            fmt.Alignment = StringAlignment.Near;
                            fmt.LineAlignment = StringAlignment.Near;

                            CharacterRange[] characterRanges = { new CharacterRange(i, 1) };
                            fmt.SetMeasurableCharacterRanges(characterRanges);

                            var rect = gr.MeasureCharacterRanges(s, Font, usablearea, fmt)[0].GetBounds(gr);
                            //System.Diagnostics.Debug.WriteLine("Region " + rect + " char " + i + " " + s[i] + " vs " + click);
                            if (click.X >= rect.Left && click.X < rect.Right)
                            {
                                //System.Diagnostics.Debug.WriteLine("Return " + (cpos + displaystartx + i));
                                return cpos + displaystartx + i;
                            }
                        }
                    }
                }
            }

            //System.Diagnostics.Debug.WriteLine("Failed to find ");

            return cpos + displaystartx+ s.Length;
        }

        protected override void OnMouseDown(GLMouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!e.Handled)
            {
                int xcursorpos = FindCursorPos(e.Location, out int xlinecpos, out int xlineno);

                if (xcursorpos >= 0)
                {
                    cursorlinecpos = xlinecpos;
                    cursorlineno = xlineno;
                    cursorpos = xcursorpos;
                    Finish(invalidate: true, clearmarkers: !e.Shift, restarttimer: true);
                }
            }
        }

        protected override void OnMouseMove(GLMouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!e.Handled && e.Button == GLMouseEventArgs.MouseButtons.Left)
            {
                int xcursorpos = FindCursorPos(e.Location, out int xlinecpos, out int xlineno);

                if (xcursorpos >= 0)
                {
                    cursorlinecpos = xlinecpos;
                    cursorlineno = xlineno;
                    cursorpos = xcursorpos;
                    Invalidate();
                }
            }
        }

        protected override void OnMouseWheel(GLMouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (e.Delta < 0)
            {
                FirstDisplayedLine += 1;
                if (vertscroller != null)
                    vertscroller.Value = FirstDisplayedLine;
            }
            else
            {
                FirstDisplayedLine -= 1;
                if (vertscroller != null)
                    vertscroller.Value = FirstDisplayedLine;
            }
        }


        #endregion

        #region Scroll Bars

        private void ScrollBars(bool vert, bool horz)
        {
            if (horz)
            {
                if (horzscroller == null)
                {
                    horzscroller = new GLScrollBar();
                    horzscroller.Name = "MLTB-Horz";
                    horzscroller.HorizontalScroll = true;
                    horzscroller.Height = ScrollBarWidth;
                    horzscroller.Dock = DockingType.Bottom;
                    horzscroller.Visible = true;
                    horzscroller.RejectFocus = true;
                    horzscroller.Scroll += (bc2,sa2) => { FirstDisplayedCharacter = sa2.NewValue; };
                    Add(horzscroller);
                    Invalidate();
                }
            }
            else
            {
                if (horzscroller != null)
                {
                    Remove(horzscroller);
                    horzscroller = null;
                    if (vertscroller != null)
                        vertscroller.Padding = new Padding(0, 0, 0, 0);
                    Invalidate();
                }
            }

            if (vert)
            {
                if (vertscroller == null)
                {
                    vertscroller = new GLScrollBar();
                    vertscroller.Name = "MLTB-Vert";
                    vertscroller.Width = ScrollBarWidth;
                    vertscroller.Dock = DockingType.Right;
                    vertscroller.Visible = NumberOfLines > CurrentDisplayableLines;
                    vertscroller.RejectFocus = true;
                    vertscroller.Scroll += (bc1,sa1) => { FirstDisplayedLine = sa1.NewValue; };
                    Add(vertscroller);
                    Invalidate();
                }
            }
            else
            {
                if (vertscroller != null)
                {
                    Remove(vertscroller);
                    vertscroller = null;
                    Invalidate();
                }
            }

            //remove for now
      //      if (horzscroller?.Visible ?? false)
         //       horzscroller.Padding = new Padding(0, 0, vert ? ScrollBarWidth : 0, 0);
            if (vertscroller?.Visible ?? false)
                vertscroller.Padding = new Padding(0, 0, 0, horz ? ScrollBarWidth : 0);
        }

        #endregion

        private int CurrentDisplayableLines { get { return ((ClientRectangle.Height - ((horzscroller?.Visible ?? false) ? ScrollBarWidth : 0)) / Font.Height); } }
        private int MaxDisplayableLines { get { return ClientRectangle.Height / Font.Height; } }

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
        private int startlineno;   // computed on text set, updated by all moves/inserts
        private int startlinecpos;   // computed on text set, updated by all moves/inserts, start of current line

        // Display

        private Color highlightColor { get; set; } = DefaultHighlightColor;
        private Color lineColor { get; set; } = Color.Transparent;
        private Color backerrorcolor { get; set; } = DefaultErrorColor;
        private bool inerror = false;

        private int scrollbarwidth = 20;

        private bool cursorshowing = true;  // is cursor currently showing

        private OFC.Timers.Timer cursortimer = new Timers.Timer();

        private GLScrollBar vertscroller;
        private GLScrollBar horzscroller;
    }
}

