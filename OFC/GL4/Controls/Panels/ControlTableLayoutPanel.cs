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

namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// Table layout panel, using the Row and Column properties of controls to assign them to cells
    /// </summary>
    public class GLTableLayoutPanel : GLPanel
    {
        /// <summary> Default Constructor </summary>
        public GLTableLayoutPanel() : this("TLP?", DefaultWindowRectangle)
        {
        }

        /// <summary> Construtor with name, bounds, and optional back color, enable theme</summary>
        public GLTableLayoutPanel(string name, Rectangle location, Color? backcolour = null, bool enablethemer = true) : base(name, location)
        {
            BackColorGradientAltNI = BackColorNI = backcolour.HasValue ? backcolour.Value : DefaultTableLayoutBackColor;
            BorderColorNI = DefaultTableLayoutBorderColor;
            EnableThemer = enablethemer;
        }

        /// <summary> Constructor with name, docking type, docking percent, and optional backcolour</summary>
        public GLTableLayoutPanel(string name, DockingType type, float dockpercent, Color? backcolour = null, bool enablethemer = true) : this(name, DefaultWindowRectangle, backcolour, enablethemer)
        {
            Dock = type;
            DockPercent = dockpercent;
        }

        /// <summary> Constructor with name, size, docking type, docking percent, and optional backcolour</summary>
        public GLTableLayoutPanel(string name, Size sizep, DockingType type, float dockpercentage, Color? backcolour = null, bool enablethemer = true) : this(name, DefaultWindowRectangle, backcolour, enablethemer)
        {
            Dock = type;
            DockPercent = dockpercentage;
            SetNI(size: sizep);
        }

        /// <summary>
        /// Row and Column style. Set up in table the Rows and Columns list with definitions on each row and column and 
        /// how you want it to size. Size can be set absolute pixels, by weighting, or autosized to maximum item in cell
        /// </summary>
        public struct Style
        {
            /// <summary> Size Type </summary>
            public enum SizeTypeEnum {
                /// <summary> Size is set by Value in pixels </summary>
                Absolute,
                /// <summary> Size is determined by weight of Value against other columns </summary>
                Weight,
                /// <summary> Size is autosized</summary>
                Autosize
            };
            /// <summary> Select the column or row sizing mode, Absolute, Weight or Autosize </summary>
            public SizeTypeEnum SizeType { get; set; }
            /// <summary> Either pixel width (Absolute mode) or Weight (Weight mode)</summary>
            public int Value { get; set; }

            /// <summary> Constructor </summary>
            public Style(SizeTypeEnum ste, int v) { SizeType = ste; Value = v; }
        }

        /// <summary> Styles for each row </summary>
        public List<Style> Rows { get { return rows; } set { rows = value; InvalidateLayout(); } }
        /// <summary> Styles for each column </summary>
        public List<Style> Columns { get { return columns; } set { columns = value; InvalidateLayout(); } }
        /// <summary> Padding around each cell </summary>
        public PaddingType CellPadding { get { return cellPadding; } set { cellPadding = value; InvalidateLayout(); } }
        
        /// <summary> Autosize is not supported </summary>
        public new bool AutoSize { get { return false; } set { throw new NotImplementedException(); } }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.PerformRecursiveLayout"/>
        protected override void PerformRecursiveLayout()
        {
            bool okay = true;

            if (Columns != null && Rows != null)
            {
                int[] maxcolsize = new int[Columns.Count];      // maximum column width of all items in this column
                int[] maxrowsize = new int[Rows.Count];         // maximum row Height of all items in this row
                Dictionary<Tuple<int, int>, List<GLBaseControl>> sortedbycell = new Dictionary<Tuple<int, int>, List<GLBaseControl>>();

                // go thru all controls, and find out the maximum width/height of each row/column
                // and assign to sortebycell the control into a list.

                foreach (var c in ControlsZ) 
                {
                    if (c.Column < maxcolsize.Length && c.Row < maxrowsize.Length)
                    {
                        maxcolsize[c.Column] = Math.Max(maxcolsize[c.Column], c.Width + CellPadding.TotalWidth);
                        maxrowsize[c.Row] = Math.Max(maxrowsize[c.Row], c.Height + CellPadding.TotalHeight);

                        Tuple<int, int> ad = new Tuple<int, int>(c.Column, c.Row);
                        if (!sortedbycell.ContainsKey(ad))
                            sortedbycell[ad] = new List<GLBaseControl>();
                        sortedbycell[ad].Add(c);
                    }
                    else
                        okay = false;       // column or row out of range of styles
                }

                if (okay)
                {
                    Rectangle panelarea = ClientRectangle;      // in terms of our client area

                    // using the column/row styles, the width, and the max sizes, calculate the positions of each row/column boundary

                    var cols = CalcPos(Columns, panelarea.Width, maxcolsize);    
                    var rows = CalcPos(Rows, panelarea.Height, maxrowsize);

                    // if okay 
                    if (cols.Count > 0 && rows.Count > 0)
                    {
                        foreach (var k in sortedbycell)     // position items
                        {
                            var col = k.Key.Item1;
                            var row = k.Key.Item2;
                            var clist = k.Value;

                            Rectangle cellarea = new Rectangle(cols[col], rows[row], cols[col + 1] - cols[col], rows[row + 1] - rows[row]);
                            Rectangle flowarea = new Rectangle(cellarea.Left, cellarea.Top,0,0);
                            cellarea.X += CellPadding.Left;
                            cellarea.Width -= CellPadding.TotalWidth;
                            cellarea.Y += CellPadding.Top;
                            cellarea.Height -= CellPadding.TotalHeight;

                            foreach (GLBaseControl c in clist)
                            {
                                //  System.Diagnostics.Debug.WriteLine("Table layout " + c.Name + " " + cellarea);

                                if (c.Dock != DockingType.None)        // if docking,
                                {
                                    c.Layout(ref cellarea);     // allow docking to work in the cell area, it uses the area to set position
                                }
                                else
                                {
                                    //   System.Diagnostics.Debug.WriteLine("Top Left layout " + c.Name + " " + cellarea);
                                    c.SetNI(location: cellarea.Location, size: cellarea.Size, clipsizetobounds: true);
                                }

                                c.CallPerformRecursiveLayout();
                            }
                        }

                        ClearLayoutFlags();
                    }
                    else
                        okay = false;
                }
            }
            else
                okay = false;

            if ( !okay )
                base.PerformRecursiveLayout();      // default
        }

        // return a list of position of the col/row, given this list of styles, the overall size available, and the list of max sizes of each row/col
        private List<int> CalcPos(List<Style> cr, int sizeavailable, int[] maxsizes)
        {
            // go thru all and work out weight and absolute size
            
            int cabs = 0;           // for all absolute sizes, total pixels
            int cweight = 0;        // for all weight sizes, total weight
            
            for (int c = 0; c < cr.Count; c++)          // total columns
            {
                if (cr[c].SizeType == Style.SizeTypeEnum.Absolute)      // absolute adds onto cabs
                    cabs += cr[c].Value;
                else if (cr[c].SizeType == Style.SizeTypeEnum.Autosize) // autosize just uses the maxsize of each item
                    cabs += maxsizes[c];
                else
                    cweight += cr[c].Value;                            // weight sum weights
            }

            List<int> xpos = new List<int>();       // positions//

            int cweightpixelsleft = sizeavailable - cabs;    // pixels left after absolute

            if (cabs <= sizeavailable && (cweight == 0 || cweightpixelsleft > 0))   // if enough size
            {
                int x = 0;
                for (int c = 0; c < cr.Count; c++)      // over columns
                {
                    xpos.Add(x);                        // add position

                    if (cr[c].SizeType == Style.SizeTypeEnum.Absolute)      //if absolute column, we move on by Value, which is pixel width 
                        x += cr[c].Value;
                    else if (cr[c].SizeType == Style.SizeTypeEnum.Autosize) //if autosize, we move on by column size
                        x += maxsizes[c];
                    else
                        x += cr[c].Value * cweightpixelsleft / cweight;     // else we move on by weight
                }

                xpos.Add(x);        // add last end point
            }

            return xpos;
        }

        private List<Style> rows  = null;
        private List<Style> columns  = null;
        private PaddingType cellPadding = new PaddingType(1);
    }
}

