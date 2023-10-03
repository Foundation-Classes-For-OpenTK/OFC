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
    public class GLMultiLineTextBox : GLForeDisplayTextBase
    {
        /// <summary> Callback when text changed. Not fired programatically when changing Text </summary>
        public Action<GLBaseControl> TextChanged { get; set; } = null;
        /// <summary> Callback when return pressed, and only if MultiLineMode = false. Not fired programatically when changing Text </summary>
        public Action<GLBaseControl> ReturnPressed { get; set; } = null;

        /// <summary> Set to determine CRLF or LF is used</summary>
        public bool CRLF { get; set; } = true;
        /// <summary> Set true to enable multiline text box. 
        /// Set false for a single line text box, in which case TextAlign then determines alignment in box (vert only)
        /// Do this before control becomes active.
        /// </summary>
        public bool MultiLineMode { get; set; } = true;
        /// <summary> Insert mode </summary>
        public bool Insert { get { return insert; } set { insert = value; Invalidate(); } }
        /// <summary> Set true so when first character is types the text box is cleared </summary>
        public bool ClearOnFirstChar { get; set; } = false;
        /// <summary> Allow control character in text </summary>
        public bool AllowControlChars { get; set; } = false;
        /// <summary> Set to make read only. No user editing is allowed, and editing commands will not work </summary>
        public bool ReadOnly { get; set; } = false;
        /// <summary>Colour of the text area only. Normally transparent </summary>
        public Color TextAreaBackColor { get { return textAreabackcolor; } set { textAreabackcolor = value; Invalidate(); } }
        /// <summary> Text area alternate colour when text gradient is on </summary>
        public Color TextAreaBackColorAlt { get { return textAreagradientalt; } set { textAreagradientalt = value; Invalidate(); } }
        /// <summary> Text area alternate colour direction. Must be set to turn it on. </summary>
        public int TextAreaColorGradientDir { get { return textAreagradientdir; } set { if (textAreagradientdir != value) { textAreagradientdir = value; Invalidate(); } } }
        /// <summary> Reserved space around scroll bars/text area. Different to padding as derived controls can draw into it </summary>
        public PaddingType ExtraPadding { get { return extrapadding; } set { extrapadding = value; vertscroller.DockingMargin = new MarginType(0, value.Top, value.Right, value.Bottom);
                horzscroller.DockingMargin = new MarginType(value.Left, 0, 0, value.Bottom); Finish(true, false, false); } }

        /// <summary> Reserve space around the text area within the reserved boundary </summary>
        public MarginType TextBoundary { get { return textboundary; } set { textboundary = value; Finish(true, false, false); } }
        /// <summary> Highlight back color for selection </summary>
        public Color HighlightColor { get { return highlightColor; } set { highlightColor = value; Invalidate(); } }
        /// <summary> Line separator color, default is line separator is off (Color.Transparent) </summary>
        public Color LineColor { get { return lineColor; } set { lineColor = value; Invalidate(); } }
        /// <summary> Set to enable a flashing cursor </summary>
        public bool FlashingCursor { get; set; } = true;

        /// <summary> Is selection active </summary>
        public bool IsSelectionActive { get { return startpos != cursorpos; } }
        /// <summary> Selection start index </summary>
        public int SelectionStart { get { return Math.Min(startpos, cursorpos); } }
        /// <summary> Selection end index </summary>
        public int SelectionEnd { get { return Math.Max(startpos, cursorpos); } }

        /// <summary> Error condition enable or disable </summary>
        public bool InErrorCondition { get { return inerror; } set { inerror = value; Invalidate(); } }
        /// <summary> Error back color</summary>
        public Color BackErrorColor { get { return backerrorcolor; } set { backerrorcolor = value; Invalidate(); } }

        /// <summary> Number of lines in text</summary>
        public int NumberOfLines { get { return linelengths.Count(); } }

        /// <summary> First displayed line on screen (0 onwards)</summary>
        public int FirstDisplayedLine { get { return firstline; } set { firstline = Math.Max(0, Math.Min(value, NumberOfLines - CurrentDisplayableLines)); Invalidate(); } }
        /// <summary> First displayed character index </summary>
        public int FirstDisplayedCharacter { get { return displaystartx; } set { displaystartx = Math.Max(0, Math.Min(value, MaxLineLength)); Invalidate(); } }

        /// <summary> Maximum line length found (not including line termination characters)</summary>
        public int MaxLineLength { get; private set; } = 0;

        /// <summary> Enable vertical scroll bar</summary>
        public bool EnableVerticalScrollBar { get { return vertscroller.Visible; } set { ScrollBars(value, horzscroller.Visible); } }
        /// <summary> Enable horizontal scroll bar </summary>
        public bool EnableHorizontalScrollBar { get { return horzscroller.Visible; } set { ScrollBars(vertscroller.Visible, value); } }
        /// <summary> Scroll bar theme, to configure scroll bar look</summary>
        public GLScrollBarTheme ScrollBarTheme { get { return vertscroller.Theme; } set { vertscroller.Theme = horzscroller.Theme = value; } }
        /// <summary> Scroll bar width </summary>
        public int ScrollBarWidth { get { return scrollbarwidth; } set { scrollbarwidth = value; Finish(true, true, true); } }

        /// <summary> Font for right click menu. If null, use Font for this control </summary>
        public Font RightClickMenuFont { get; set; } = null;

        /// <summary> Disable autosize, not supported. See CalculateTextArea for a method for external auto sizing </summary>
        public new bool AutoSize { get { return false; } set { throw new NotImplementedException(); } }

        /// <summary> Height of lines. Note changing Font changes this</summary>
        public int LineHeight { get { return lineheight; } set { lineheight = value; Invalidate(); } }     // Line height

        /// <summary> Right click menu, for adding options or themeing </summary>
        public GLContextMenu RightClickMenu { get; }

        /// <summary> Construct with name, bounds and text contents </summary>
        public GLMultiLineTextBox(string name, Rectangle pos, string text, bool enablethemer = true) : base(name, pos)
        {
            Focusable = true;
            BackColorGradientAltNI = BackColorNI = DefaultTextBoxBackColor;
            foreColor = DefaultTextBoxForeColor;

            this.text = text;
            this.EnableThemer = enablethemer;

            horzscroller = new GLScrollBar();
            horzscroller.Name = Name + "_SBHorz";
            horzscroller.HorizontalScroll = true;
            horzscroller.Height = ScrollBarWidth;
            horzscroller.Dock = DockingType.Bottom;
            horzscroller.Visible = false;
            horzscroller.RejectFocus = true;
            horzscroller.EnableThemer = false;
            Add(horzscroller);

            vertscroller = new GLScrollBar();
            vertscroller.Name = Name + "_SBVert";
            vertscroller.Width = ScrollBarWidth;
            vertscroller.Dock = DockingType.Right;
            vertscroller.Visible = false;
            vertscroller.RejectFocus = true;
            vertscroller.EnableThemer = false;
            Add(vertscroller);

            horzscroller.Theme = vertscroller.Theme;        // use one theme between them

            cursortimer.Tick += CursorTick;
            CalcLineHeight();
            CalculateTextParameters();
            Finish(false, false, false);

            vertscroller.Scroll += (bc1, sa1) => { FirstDisplayedLine = sa1.NewValue; };
            horzscroller.Scroll += (bc2, sa2) => { FirstDisplayedCharacter = sa2.NewValue; };

            RightClickMenu = new GLContextMenu(Name+"_RightClickMenu", false,
                        new GLMenuItem(Name+"_EditCut", "Cut")
                        {
                            MouseClick = (s, e) => {
                                Cut();
                                SetFocus();
                            }
                        },
                        new GLMenuItem(Name+"_Copy", "Copy")
                        {
                            MouseClick = (s1, e1) => {
                                Copy();
                                SetFocus();
                            }
                        },
                        new GLMenuItem(Name+"_EditPaste", "Paste")
                        {
                            MouseClick = (s1, e1) => {
                                Paste();
                                SetFocus();
                            }
                        }
                    );

        }

        /// <summary> Construct with name, bounds, text contents, colors </summary>
        public GLMultiLineTextBox(string name, Rectangle pos, string text, Color backcolor, Color forecolor, bool enablethemer = true) : this(name, pos,text, enablethemer)
        {
            BackColor = backcolor;
            ForeColor = forecolor;
        }

        /// <summary> Default Constructor </summary>
        public GLMultiLineTextBox() : this("TBML?", DefaultWindowRectangle, "")
        {
        }

        #region Public Interface

        /// <summary> Set cursor position to this index </summary>
        public void SetCursorPos(int p)
        {
            if (p >= 0 && p <= Text.Length)
            {
                startpos = cursorpos = p;
                if (cursorpos < Text.Length && text[cursorpos] == '\n' && cursorpos > 0 && text[cursorpos - 1] == '\r') // if on a \r\n at \n, need to move back 1 more to disallow
                    cursorpos--;

                CalculateTextParameters();      // reset text paras
                Finish(invalidate: true, clearselection: false, restarttimer: true);
            }
        }

        /// <summary> Cursor move left, by count, back up over lines if required. If clearselection is true, selection is cleared </summary>
        public void CursorLeft(bool clearselection = false, int count = 1)
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

            Finish(invalidate: true, clearselection: clearselection, restarttimer: true);
        }

        /// <summary> Cursor move right, by count, over line ends. If clearselection is true, selection is cleared </summary>
        public void CursorRight(bool clearselection = false, int count = 1)
        {
            int nextlinecpos = cursorlinecpos + linelengths[cursorlineno];      // end of line, including any /r/n
            int nextmarkerpos = nextlinecpos - lineendlengths[cursorlineno];

            while (count > 0 && cursorpos < nextlinecpos )                    // nextlinecpos is moved on below when end of line, so that will only occur if nextlinecpos has no /r/n, i.e., end of text
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

            Finish(invalidate: true, clearselection: clearselection, restarttimer: true);
        }

        /// <summary> Cursor move down, by count. If clearselection is true, selection is cleared </summary>
        public void CursorDown(bool clearselection = false, int count = 1)
        {
            while(count-- > 0 && cursorlineno < linelengths.Count() - 1)
            {
                int offsetin = cursorpos - cursorlinecpos;
                cursorlinecpos += linelengths[cursorlineno++];
                cursorpos = cursorlinecpos + Math.Min(offsetin, linelengths[cursorlineno] - lineendlengths[cursorlineno]);
            }

            Finish(invalidate: true, clearselection: clearselection, restarttimer: true);
        }

        /// <summary> Cursor move up, by count. If clearselection is true, selection is cleared </summary>
        public void CursorUp(bool clearselection = false, int count = 1)
        {
            while (count-- > 0 && cursorlineno > 0)
            {
                int offsetin = cursorpos - cursorlinecpos;
                cursorlinecpos -= linelengths[--cursorlineno];
                cursorpos = cursorlinecpos + Math.Min(offsetin, linelengths[cursorlineno] - lineendlengths[cursorlineno]);
            }

            Finish(invalidate: true, clearselection: clearselection, restarttimer: true);
        }

        /// <summary> Cursor move to home position (Start of line). If clearselection is true, selection is cleared </summary>
        public void Home(bool clearselection = false)
        {
            cursorpos = cursorlinecpos;
            Finish(invalidate: true, clearselection: clearselection, restarttimer: true);
        }

        /// <summary> Cursor move to end position (End of line). If clearselection is true, selection is cleared </summary>
        public void End(bool clearselection = false)
        {
            cursorpos = cursorlinecpos + linelengths[cursorlineno] - lineendlengths[cursorlineno];
            Finish(invalidate: true, clearselection: clearselection, restarttimer: true);
        }

        /// <summary> Insert text at cursor position, with LF/CR or LF or CR allowed in text.
        /// Any current selection is removed.
        /// Note the type of end of line inserted is determined by the CRLF state, not by the form in the text
        /// </summary>
        public void InsertTextWithCRLF(string text, bool insertinplace = false)       
        {
            if (!ReadOnly)
            {
                DeleteSelectionClearInt();         // clear any selection

                int cpos = 0;
                while (true)
                {
                    if (cpos < text.Length)         // if not at end of input text
                    {
                        int nextlf = text.IndexOfAny(new char[] { '\r', '\n' }, cpos);  // find crlf

                        if (nextlf >= 0)    // if found..
                        {
                            InsertTextIntoLineInt(text.Substring(cpos, nextlf - cpos), insertinplace);      // insert up to it
                            InsertCRLFInt();        // insert crlf

                            if (text[nextlf] == '\r')
                            {
                                nextlf++;
                            }

                            if (nextlf < text.Length && text[nextlf] == '\n')
                                nextlf++;

                            cpos = nextlf;  // move on, after removeing crlf
                        }
                        else
                        {
                            InsertTextIntoLineInt(text.Substring(cpos), insertinplace); // just insert and stop
                            break;
                        }
                    }
                    else
                        break;
                }

                Finish(invalidate: true, clearselection: true, restarttimer: true);
                TextChangedEvent();
            }
        }

        /// <summary> Insert text at cursor position. No CR/LF is allowed in text.
        /// Any current selection is removed.
        /// </summary>
        public void InsertText(string text, bool insertinplace = false)     
        {
            if (!ReadOnly)
            {
                DeleteSelectionClearInt();
                InsertTextIntoLineInt(text, insertinplace);
                Finish(invalidate: true, clearselection: true, restarttimer: true);
                TextChangedEvent();
            }
        }

        /// <summary> Overwrite text at cursor position. No CR/LF is allowed in text.
        /// Any current selection is removed.
        /// </summary>
        public void OverwriteText(string text)
        {
            if (!ReadOnly)
            {
                DeleteSelectionClearInt();      // clear any selection

                for (int i = 0 ; i < text.Length; i++)
                {
                    int offsetin = cursorpos - cursorlinecpos;
                    if (offsetin >= linelengths[cursorlineno] - lineendlengths[cursorlineno])
                    {
                        System.Diagnostics.Debug.WriteLine("At end, insert rest");
                        InsertTextIntoLineInt(text.Substring(i));   // insert the rest
                        break;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Delete right and insert");
                        DeleteInt();
                        InsertTextIntoLineInt(text.Substring(i, 1));
                    }
                }

                Finish(invalidate: true, clearselection: true, restarttimer: true);
                TextChangedEvent();
            }
        }

        /// <summary> Insert a line break (CR+LF or LF dependent on CRLF state) at cursor position. 
        /// Any current selection is removed.
        /// </summary>
        public void InsertCRLF()        // insert the selected cr/lf pattern
        {
            if (!ReadOnly)
            {
                DeleteSelectionClearInt();
                InsertCRLFInt();
                Finish(invalidate: true, clearselection: true, restarttimer: true);
                TextChangedEvent();
            }
        }

        /// <summary> Delete the character to the left, or delete the selection.
        /// </summary>
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
                        Finish(invalidate: true, clearselection: true, restarttimer: true);
                        TextChangedEvent();
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
                        Finish(invalidate: true, clearselection: true, restarttimer: true);
                        TextChangedEvent();
                    }
                }
            }
        }

        /// <summary> Delete the character to the right, or delete the selection.
        /// </summary>
        public void Delete()
        {
            if (!ReadOnly)
            {
                if (!DeleteSelection())      // if we deleted a selection, no other action
                {
                    if ( DeleteInt() )      // if we deleted something
                    {
                        Finish(invalidate: true, clearselection: true, restarttimer: true);
                        TextChangedEvent();
                    }
                }
            }
        }

        /// <summary> Cursor to start of text </summary>
        public void CursorToTop()
        {
            startpos = firstline = cursorpos = 0;
            CalculateTextParameters();                      // will correct for out of range start/cursor pos
            Finish(invalidate: true, clearselection: false, restarttimer: true);
        }

        /// <summary> Cursor to end of text </summary>
        public void CursorToEnd()
        {
            firstline = 0;
            startpos = cursorpos = Text.Length;
            CalculateTextParameters();                      // will correct for out of range start/cursor pos
            Finish(invalidate: true, clearselection: false, restarttimer: true);
        }

        /// <summary> Set the selection area to these values.  If start=end selection is cancelled </summary>
        public void SetSelection(int start, int end)        // set equal to cancel, else set start/end pos
        {
            startpos = Math.Min(start, end);
            cursorpos = Math.Max(start, end);
            CalculateTextParameters();                      // will correct for out of range start/cursor pos
            Finish(invalidate: true, clearselection: false, restarttimer: true);
        }

        /// <summary> Clear the selection </summary>
        public void ClearSelection()
        {
            startpos = cursorpos;
            CalculateTextParameters();
            Finish(invalidate: true, clearselection: false, restarttimer: true);
        }

        /// <summary> Delete the selection. Return true if selection was present </summary>
        public bool DeleteSelection()
        {
            if (!ReadOnly && DeleteSelectionClearInt())
            {
                Finish(invalidate: true, clearselection: false, restarttimer: true);
                TextChangedEvent();
                return true;
            }
            else
                return false;
        }

        /// <summary> Return the selected text, including any LF/CR characters within it. Null if no text is selected</summary>
        public string SelectedText
        {
            get
            {
                if (IsSelectionActive)
                {
                    int min = Math.Min(startpos, cursorpos);
                    int max = Math.Max(startpos, cursorpos);
                    return text.Substring(min, max - min);
                }
                else
                    return null;
            }
        }


        /// <summary> Copy selected text to clipboard. True if copied to clipboard </summary>
        public bool Copy()
        {
            string sel = SelectedText;
            if (sel != null)
            {
                try
                {
                    System.Windows.Forms.Clipboard.SetText(sel);
                    return true;
                }
                catch { }       // external reason, don't care
            }
            return false;
        }

        /// <summary> Cut selected text to clipboard. True if copied to clipboard </summary>
        public bool Cut()
        {
            if (!ReadOnly)
            {
                string sel = SelectedText;
                if (sel != null)
                {
                    try
                    {
                        System.Windows.Forms.Clipboard.SetText(sel);
                        DeleteSelection();
                        return true;
                    }
                    catch { }
                }
            }

            return false;
        }

        /// <summary> Paste the clipboard text to the cursor position. True if pasted </summary>
        public bool Paste()
        {
            if (!ReadOnly)
            {
                try
                {
                    string s = System.Windows.Forms.Clipboard.GetText(System.Windows.Forms.TextDataFormat.UnicodeText);
                    if (!s.IsEmpty())
                        InsertTextWithCRLF(s);

                    return true;
                }
                catch { }
            }

            return false;
        }

        /// <summary> Clear all text</summary>
        public void Clear()
        {
            if (!ReadOnly)
            {
                text = string.Empty;
                cursorpos = startpos = 0;
                MaxLineLength = 0;
                CalculateTextParameters();
                Finish(invalidate: true, clearselection: false, restarttimer: true);
            }
        }

        #endregion

        #region Estimation

        /// <summary>
        /// Estimate size of bitmap needed for this text
        /// This needs the font set up and TextBoundary
        /// </summary>
        /// <param name="min">Minimum size of box</param>
        /// <param name="max">Maximum size of box</param>
        /// <returns>Tuple with estimated width and height, and indicate if horizonal scroll needed</returns>
        public Tuple<Size, bool> CalculateTextArea(Size min, Size max)
        {
            bool horzscroll = false;

            SizeF area = GLOFC.Utils.BitMapHelpers.MeasureStringInBitmap(Text, Font);

            int width = Math.Max(min.Width, (int)(area.Width + 0.99999 + LineHeight / 2));       // add a nerf based on font height
            width += TextBoundary.TotalWidth + ExtraPadding.TotalWidth;

            if (width > max.Width)
            {
                width = max.Width;
                horzscroll = true;
            }

            int height = LineHeight * NumberOfLines ;
            height += TextBoundary.TotalHeight + ExtraPadding.TotalHeight;

            if (height > max.Height)
            {
                height = max.Height;
                width += ScrollBarWidth;
            }

            return new Tuple<Size, bool>(new Size(width, height), horzscroll);
        }

        #endregion

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
                    vertscroller.SetValueMaximumLargeChange(firstline, NumberOfLines-1 , CurrentDisplayableLines);
            }

            if ( horzscroller?.Visible??false )
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

        private protected override void TextValueChanged()      // called by upper class to say i've changed the text.
        {
            displaystartx = 0;
            SetCursorPos(Text.Length);          // will set to end, cause Calculate and FInish
        }

        private void clearselection()
        {
            startlinecpos = cursorlinecpos;
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

        #endregion

        #region Other implementation

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

        // override the draw back for error conditions

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

        private Rectangle UsableAreaForText()
        {
            Rectangle clientarea = ClientRectangle;

            Rectangle usablearea = new Rectangle(clientarea.Left + TextBoundary.Left + ExtraPadding.Left, 
                                                clientarea.Top + TextBoundary.Top + ExtraPadding.Top, 
                                                clientarea.Width - TextBoundary.TotalWidth - ExtraPadding.TotalWidth, 
                                                clientarea.Height - TextBoundary.TotalHeight - ExtraPadding.TotalHeight);

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

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.Paint(Graphics)"/>
        protected override void Paint(Graphics gr)
        {
            if (displaystartx < 0)      // no paint if not set up - checking for race conditions
                return;

            //System.Diagnostics.Debug.WriteLine($"Paint MLTB {startpos} {cursorpos} {firstline} {CurrentDisplayableLines}");
            Rectangle usablearea = UsableAreaForText();      // area inside out client rectangle where text is

            if ( textAreabackcolor != Color.Transparent)
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

                        if (cursorlineno == lineno && Enabled && Focused && cursorshowing && cursoroffset >= 0 )
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

        private protected virtual void TextChangedEvent()           // in derived classes, override to see the event without going thru the TextChanged call back
        {
            OnTextChanged();
        }

        private protected virtual void OnTextChanged()              // standard OnTextChanged call
        {
            TextChanged?.Invoke(this);
        }

        private void OnReturnPressed()
        {
            ReturnPressed?.Invoke(this);
        }

        // Find index in document of cursor, if within range.
        private int FindCursorPos(Point click, out int cpos, out int lineno)
        {
            //System.Diagnostics.Debug.WriteLine($"Find MLTB Position {click}");

            Rectangle usablearea = UsableAreaForText();
            usablearea.Width = 7000;        // set so chars don't clip and extend across

            lineno = cpos = -1;

            if (click.Y < usablearea.Y)     // if above usable area.. no return
                return -1;

            // calculate line and limit to number of lines
            // if clicking below last line its the same as clicking the last one
            int lineclicked = Math.Min(firstline + (click.Y - usablearea.Y) / LineHeight, linelengths.Count - 1); 

            lineno = 0;
            cpos = 0;
            while (lineno < lineclicked)            // setting cpos and lineno
                cpos += linelengths[lineno++];

          //  System.Diagnostics.Debug.WriteLine($"lc {lineclicked} cpos {cpos} lineno {lineno}");

            string s = GetLineWithoutCRLF(cpos, lineno, displaystartx);

            if (s.Length == 0)  // no text, means its to the left, so click on end
                return cpos + linelengths[lineno] - lineendlengths[lineno];

            using (Bitmap b = new Bitmap(1, 1))
            {
                using (Graphics gr = Graphics.FromImage(b))
                {
                    gr.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;     // default for OFC - MUST sync with the TRH used in DrawString
                                                                                                
                    for (int i = 0; i < s.Length; i++)    // we have to do it one by one, as the query is limited to 32 char ranges
                    {
                        using (var fmt = new StringFormat())
                        {
                            fmt.Alignment = StringAlignment.Near;
                            fmt.LineAlignment = StringAlignment.Near;
                            fmt.FormatFlags = StringFormatFlags.NoWrap;

                            CharacterRange[] characterRanges = { new CharacterRange(i, 1) };
                            fmt.SetMeasurableCharacterRanges(characterRanges);

                            var rect = gr.MeasureCharacterRanges(s, Font, usablearea, fmt)[0].GetBounds(gr);

                            int mid =((int)rect.Left + (int)rect.Right) / 2;

                            //System.Diagnostics.Debug.WriteLine("Region " + rect.Left + ".." + (rect.Right-1) + " mid " + mid + " char " + i + " " + s[i] + " vs " + click);
                            if ( click.X >= rect.Left && click.X < rect.Right)
                            {
                                int pos = cpos + displaystartx + i + ((click.X >= mid) ? 1 : 0);     // pick nearest edge
                          //      System.Diagnostics.Debug.WriteLine($"Cursor found {pos} cpos {cpos} ds {displaystartx} i {i}");
                                return pos;
                            }
                            else if ( i == 0 && click.X < rect.Left )       // if before first char
                            {
                                int pos = cpos + displaystartx;
                            //    System.Diagnostics.Debug.WriteLine($"Cursor found before {pos}");
                                return pos;
                            }
                        }
                    }
                }
            }

         //   System.Diagnostics.Debug.WriteLine($"Cursor not found return end of len");
            return cpos + displaystartx+ s.Length;
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseDown(GLMouseEventArgs)"/>
        protected override void OnMouseDown(GLMouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!e.Handled && e.Button == GLMouseEventArgs.MouseButtons.Left)
            {
                int xcursorpos = FindCursorPos(e.Location, out int xlinecpos, out int xlineno);

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

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseClick(GLMouseEventArgs)"/>
        protected override void OnMouseClick(GLMouseEventArgs e)
        {
            base.OnMouseClick(e);
            if ( !e.Handled && e.Button == GLMouseEventArgs.MouseButtons.Right)
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

        private void CalcLineHeight()
        {
            // so MS sand serif at 8.25 or 12 if you just rely on Font.Height cuts the bottom off. So use a bit of text to find the mininum. Seems ok with Arial/MS Sans Serif
            var area = BitMapHelpers.MeasureStringInBitmap("AAjjqqyyy", Font);
            lineheight = Math.Max(Font.Height+1,(int)(area.Height+0.4999));
            //System.Diagnostics.Debug.WriteLine($"Calc Line height {area} {Font.Height} {Font.ToString()} = {LineHeight}");
        }

        #endregion

        private int CurrentDisplayableLines { get { return Math.Max(1,((ClientRectangle.Height - extrapadding.TotalHeight - textboundary.TotalHeight- ((horzscroller?.Visible ?? false) ? ScrollBarWidth : 0)) / LineHeight)); } }

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

    }
}

