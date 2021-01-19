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
using System.Drawing;

namespace OFC.GL4.Controls
{
    public class GLFlowLayoutPanel : GLPanel
    {
        public GLFlowLayoutPanel(string name, Rectangle location) : base(name, location)
        {
        }

        public GLFlowLayoutPanel(string name, DockingType type, float dockpercent) : base(name, DefaultWindowRectangle)
        {
            Dock = type;
            DockPercent = dockpercent;

        }

        public GLFlowLayoutPanel(string name, Size sizep, DockingType type, float dockpercentage) : base(name, DefaultWindowRectangle)
        {
            Dock = type;
            DockPercent = dockpercentage;
            SetLocationSizeNI(size: sizep);
        }

        public GLFlowLayoutPanel() : this("TLP?",DefaultWindowRectangle)
        {
        }

        public enum ControlFlowDirection { Right, Down };

        public bool FlowInZOrder { get; set; } = true;      // if set, flown in Z order
        public bool AutoSizeBoth { get; set; } = true;      // if set, autosizes both width and height, else just only one of its width/height dependent on flow direction

        public ControlFlowDirection FlowDirection { get { return flowDirection; } set { flowDirection = value; InvalidateLayout(); } }
        public GL4.Controls.Padding FlowPadding { get { return flowPadding; } set { flowPadding = value; InvalidateLayout(); } }

        private GL4.Controls.Padding flowPadding { get; set; } = new Padding(1);
        private ControlFlowDirection flowDirection = ControlFlowDirection.Right;

        // Sizing has been recursively done for all children, now size us

        protected override void SizeControl(Size parentsize)
        {
            base.SizeControl(parentsize);
    
            if (AutoSize)       // width stays the same, height changes, width based on what parent says we can have (either our width, or docked width)
            {
                var flowsize = Flow(parentsize, false, (c, p) => { });
                if (flowsize.IsEmpty)
                    flowsize = DefaultWindowRectangle.Size;     // emergency min for no controls

                if (AutoSizeBoth)
                {
                    SetLocationSizeNI(size: flowsize);
                }
                else if (FlowDirection == ControlFlowDirection.Right)
                {
                    SetLocationSizeNI(size: new Size(Width, flowsize.Height + ClientBottomMargin + flowPadding.Bottom));
                }
                else
                {
                    SetLocationSizeNI(size: new Size(flowsize.Width + ClientRightMargin + flowPadding.Right, Height));
                }
            }
        }

        // now we are laying out from top down

        public override void PerformRecursiveLayout()
        {
            //System.Diagnostics.Debug.WriteLine("Flow Laying out " + Name + " In client size " + ClientSize);

            Flow(ClientSize, true, (c, p) => 
            {
                //System.Diagnostics.Debug.WriteLine("Control " + c.Name + " to " + p);
                c.SetLocationSizeNI(location:p);
                c.PerformRecursiveLayout();
            });
        }

        private Size Flow(Size area, bool usearea, Action<GLBaseControl, Point> action)
        {
            Point flowpos = ClientLocation;
            Size max = new Size(0, 0);

            foreach (GLBaseControl c in (FlowInZOrder ? ControlsZ: ControlsIZ))
            {
                //System.Diagnostics.Debug.WriteLine("flow layout " + c.Name + " " + flowpos + " h " + maxh);

                Point pos;

                int controlwidth = c.Width + c.FlowOffsetPosition.X;        // including any flow offsets
                int controlheight = c.Height + c.FlowOffsetPosition.Y;
                //System.Diagnostics.Debug.WriteLine("Flow {0} {1} {2} {3}", c.Name, controlwidth, controlheight, FlowOffsetPosition);

                if (FlowDirection == ControlFlowDirection.Right)
                {
                    if (usearea && flowpos.X + controlwidth + flowPadding.TotalWidth > area.Width)    // if beyond client right, more down
                    {
                        flowpos = new Point(ClientLeftMargin, max.Height);
                    }

                    pos = new Point(flowpos.X + flowPadding.Left + c.FlowOffsetPosition.X, flowpos.Y + flowPadding.Top + c.FlowOffsetPosition.Y);

                    flowpos.X += controlwidth + flowPadding.TotalWidth;                 // move x right
                    int y = flowpos.Y + controlheight + flowPadding.TotalHeight;        // calculate bottom of control

                    max = new Size(Math.Max(max.Width, flowpos.X), Math.Max(max.Height, y));
                }
                else
                {
                    if ( usearea && flowpos.Y + controlheight + flowPadding.TotalHeight > area.Height )
                    {
                        flowpos = new Point(max.Width, ClientTopMargin);
                    }

                    pos = new Point(flowpos.X + flowPadding.Left + c.FlowOffsetPosition.X, flowpos.Y + flowPadding.Top + c.FlowOffsetPosition.Y);

                    flowpos.Y += controlheight + flowPadding.TotalHeight;
                    int x = flowpos.X + controlwidth + flowPadding.TotalWidth;

                    max = new Size(Math.Max(max.Width, x),  Math.Max(max.Height, flowpos.Y));
                }

                action(c, pos);
            }

            return max;
        }
    }
}

