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
using System.Drawing;
using System.Linq;

namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// Multi line text box control
    /// </summary>
    /// 
    public partial class GLMultiLineTextBox : GLForeDisplayTextBase
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
        /// <summary>Colour of the text area only. Normally transparent so the normal BackColor is the back of the box
        /// Useful if you are using the ExtraPadding or TextBoundary feature </summary>
        public Color TextAreaBackColor { get { return textAreabackcolor; } set { textAreabackcolor = value; Invalidate(); } }
        /// <summary> Text area alternate colour when text gradient is on </summary>
        public Color TextAreaBackColorAlt { get { return textAreagradientalt; } set { textAreagradientalt = value; Invalidate(); } }
        /// <summary> Text area alternate colour direction. Must be set to turn it on. </summary>
        public int TextAreaColorGradientDir { get { return textAreagradientdir; } set { if (textAreagradientdir != value) { textAreagradientdir = value; Invalidate(); } } }
        /// <summary> Reserved space around scroll bars/text area. Different to padding as derived controls can draw into it </summary>
        public PaddingType ExtraPadding { get { return extrapadding; } set { extrapadding = value; vertscroller.DockingMargin = new MarginType(0, value.Top, value.Right, value.Bottom);
                horzscroller.DockingMargin = new MarginType(value.Left, 0, 0, value.Bottom); Finish(true, false, false); } }
        /// <summary> Reserve space around the text area within the reserved boundary created by extra padding</summary>
        public MarginType TextMargin { get { return textboundary; } set { textboundary = value; Finish(true, false, false); } }
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
        public int LineHeight { get { return lineheight; } set { lineheight = value; forcedlineheight = true; Invalidate(); } }     // Line height

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


        /// <summary>
        /// Find index in document of cursor, starting line index and linenumber, if within range of text.
        /// </summary>
        /// <param name="position">X/Y in client rectangle</param>
        /// <param name="linestartcpos">line start position, or -1 if not found</param>
        /// <param name="lineno">lineumber, or -1 if not found</param>
        /// <returns>cursor character position, -1 not found</returns>

        public int FindCursorPositionFromPoint(Point position, out int linestartcpos, out int lineno)
        {
            //System.Diagnostics.Debug.WriteLine($"Find MLTB Position {click}");

            Rectangle usablearea = UsableAreaForText();
            usablearea.Width = 7000;        // set so chars don't clip and extend across

            lineno = linestartcpos = -1;

            if (position.Y < usablearea.Y)     // if above usable area.. no return
                return -1;

            // calculate line and limit to number of lines
            // if clicking below last line its the same as clicking the last one
            int lineclicked = Math.Min(firstline + (position.Y - usablearea.Y) / LineHeight, linelengths.Count - 1);

            lineno = 0;
            linestartcpos = 0;
            while (lineno < lineclicked)            // setting cpos and lineno
                linestartcpos += linelengths[lineno++];

            //  System.Diagnostics.Debug.WriteLine($"lc {lineclicked} cpos {cpos} lineno {lineno}");

            string s = GetLineWithoutCRLF(linestartcpos, lineno, displaystartx);

            if (s.Length == 0)  // no text, means its to the left, so click on end
                return linestartcpos + linelengths[lineno] - lineendlengths[lineno];

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

                            int mid = ((int)rect.Left + (int)rect.Right) / 2;

                            //System.Diagnostics.Debug.WriteLine("Region " + rect.Left + ".." + (rect.Right-1) + " mid " + mid + " char " + i + " " + s[i] + " vs " + click);
                            if (position.X >= rect.Left && position.X < rect.Right)
                            {
                                int pos = linestartcpos + displaystartx + i + ((position.X >= mid) ? 1 : 0);     // pick nearest edge
                                                                                                                 //      System.Diagnostics.Debug.WriteLine($"Cursor found {pos} cpos {cpos} ds {displaystartx} i {i}");
                                return pos;
                            }
                            else if (i == 0 && position.X < rect.Left)       // if before first char
                            {
                                int pos = linestartcpos + displaystartx;
                                //    System.Diagnostics.Debug.WriteLine($"Cursor found before {pos}");
                                return pos;
                            }
                        }
                    }
                }
            }

            //   System.Diagnostics.Debug.WriteLine($"Cursor not found return end of len");
            return linestartcpos + displaystartx + s.Length;
        }


        #endregion

        #region Estimation

        /// <summary>
        /// Estimate size of bitmap needed for this text
        /// This needs the font set up and TextBoundary/ExtraPadding 
        /// </summary>
        /// <param name="min">Minimum size of box</param>
        /// <param name="max">Maximum size of box</param>
        /// <returns>Tuple with estimated width and height, and indicate if horizonal scroll needed</returns>
        public Tuple<Size, bool> CalculateTextArea(Size min, Size max)
        {
            bool horzscroll = false;

            SizeF area = GLOFC.Utils.BitMapHelpers.MeasureStringInBitmap(Text, Font);

            int width = Math.Max(min.Width, (int)(area.Width + 0.99999 + LineHeight / 2));       // add a nerf based on font height
            width += TextMargin.TotalWidth + ExtraPadding.TotalWidth;

            if (width > max.Width)
            {
                width = max.Width;
                horzscroll = true;
            }

            int height = LineHeight * NumberOfLines ;
            height += TextMargin.TotalHeight + ExtraPadding.TotalHeight;

            if (height > max.Height)
            {
                height = max.Height;
                width += ScrollBarWidth;
            }

            return new Tuple<Size, bool>(new Size(width, height), horzscroll);
        }

        #endregion

    }
}

