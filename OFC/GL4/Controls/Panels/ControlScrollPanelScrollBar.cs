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
using System.Collections.Generic;
using System.Drawing;

namespace GLOFC.GL4.Controls
{
    public class GLScrollPanelScrollBar : GLBaseControl
    {
        public Color ScrollBackColor { get { return scrollpanel.BackColor; } set { scrollpanel.BackColor = value; } }

        public Color ArrowColor { get { return vertscrollbar.ArrowColor; } set { horzscrollbar.ArrowColor = vertscrollbar.ArrowColor = value;  } }       // of text
        public Color SliderColor { get { return vertscrollbar.SliderColor; } set { horzscrollbar.SliderColor = vertscrollbar.SliderColor = value;  } }

        public Color ArrowButtonColor { get { return vertscrollbar.ArrowButtonColor; } set { horzscrollbar.ArrowButtonColor = vertscrollbar.ArrowButtonColor = value; } }
        public Color ArrowBorderColor { get { return vertscrollbar.ArrowBorderColor; } set { horzscrollbar.ArrowBorderColor = vertscrollbar.ArrowBorderColor = value;  } }
        public float ArrowUpDrawAngle { get { return vertscrollbar.ArrowDecreaseDrawAngle; } set { horzscrollbar.ArrowDecreaseDrawAngle = vertscrollbar.ArrowDecreaseDrawAngle = value;  } }
        public float ArrowDownDrawAngle { get { return vertscrollbar.ArrowIncreaseDrawAngle; } set { horzscrollbar.ArrowIncreaseDrawAngle = vertscrollbar.ArrowIncreaseDrawAngle = value;  } }
        public float ArrowColorScaling { get { return vertscrollbar.ArrowColorScaling; } set { horzscrollbar.ArrowColorScaling = vertscrollbar.ArrowColorScaling = value;  } }

        public Color MouseOverButtonColor { get { return vertscrollbar.MouseOverButtonColor; } set { horzscrollbar.MouseOverButtonColor = vertscrollbar.MouseOverButtonColor = value;  } }
        public Color MousePressedButtonColor { get { return vertscrollbar.MousePressedButtonColor; } set { horzscrollbar.MousePressedButtonColor = vertscrollbar.MousePressedButtonColor = value;  } }
        public Color ThumbButtonColor { get { return vertscrollbar.ThumbButtonColor; } set { horzscrollbar.ThumbButtonColor = vertscrollbar.ThumbButtonColor = value;  } }
        public Color ThumbBorderColor { get { return vertscrollbar.ThumbBorderColor; } set { horzscrollbar.ThumbBorderColor = vertscrollbar.ThumbBorderColor = value;  } }
        public float ThumbColorScaling { get { return vertscrollbar.ThumbColorScaling; } set { horzscrollbar.ThumbColorScaling = vertscrollbar.ThumbColorScaling = value;  } }
        public float ThumbDrawAngle { get { return vertscrollbar.ThumbDrawAngle; } set { horzscrollbar.ThumbDrawAngle = vertscrollbar.ThumbDrawAngle = value;  } }

        public override IList<GLBaseControl> ControlsZ { get { return scrollpanel.ControlsZ; } }      // read only
        public override IList<GLBaseControl> ControlsIZ { get { return scrollpanel.ControlsIZ; } }      // read only

        public int ScrollBarWidth { get { return Font?.ScalePixels(20) ?? 20; } }

        public bool EnableHorzScrolling { get { return horzscrollbar.Visible; } set { horzscrollbar.Visible = value; } }
        public bool EnableVertScrolling { get { return vertscrollbar.Visible; } set { vertscrollbar.Visible = value; } }

        public new bool AutoSize { get { return false; } set { throw new NotImplementedException(); } }

        public GLScrollPanelScrollBar(string name, Rectangle location) : base(name,location)
        {
            BorderColorNI = DefaultVerticalScrollPanelBorderColor;
            BackColorGradientAltNI = BackColorNI = DefaultVerticalScrollPanelBackColor;

            scrollpanel = new GLScrollPanel(name+"_VSP");
            scrollpanel.Dock = DockingType.Fill;
            scrollpanel.BackColor = BackColor;
            base.Add(scrollpanel);  // base because we don't want to use the overrides

            vertscrollbar = new GLVerticalScrollBar(name + "_SVert");
            vertscrollbar.Dock = DockingType.Right;
            vertscrollbar.Width = ScrollBarWidth;
            base.Add(vertscrollbar);     // last added always goes to top of z-order
            vertscrollbar.Scroll += VScrolled;

            horzscrollbar = new GLHorizontalScrollBar(name + "_SHorz");
            horzscrollbar.Dock = DockingType.Bottom;
            horzscrollbar.Height =ScrollBarWidth;
            base.Add(horzscrollbar);     // last added always goes to top of z-order
            horzscrollbar.Scroll += HScrolled;
        }

        public GLScrollPanelScrollBar(string name = "SPSB?") : this(name, DefaultWindowRectangle)
        {
        }

        public override void Add(GLBaseControl other, bool atback = false)           // we need to override, since we want controls added to the scroll panel not us
        {
            scrollpanel.Add(other, atback);
            InvalidateLayout();
        }

        private GLVerticalScrollBar vertscrollbar;
        private GLHorizontalScrollBar horzscrollbar;
        private GLScrollPanel scrollpanel;

        private void VScrolled(GLBaseControl c, ScrollEventArgs e)
        {
            scrollpanel.VertScrollPos = vertscrollbar.Value;
        }
        private void HScrolled(GLBaseControl c, ScrollEventArgs e)
        {
            scrollpanel.HorzScrollPos = horzscrollbar.Value;
        }

        protected override void PerformRecursiveLayout()
        {
            vertscrollbar.Width = ScrollBarWidth;
            horzscrollbar.Height = ScrollBarWidth;
            horzscrollbar.DockingMargin = new Margin(0,0,vertscrollbar.Visible ? ScrollBarWidth : 0,0);

            base.PerformRecursiveLayout();   // the docking sorts out the positioning of the controls

            vertscrollbar.Maximum = scrollpanel.VertScrollRange + vertscrollbar.LargeChange;
            horzscrollbar.Maximum = scrollpanel.HorzScrollRange + horzscrollbar.LargeChange;
         //   System.Diagnostics.Debug.WriteLine($"Scroll panel range {horzscrollbar.Maximum} {vertscrollbar.Maximum}");
        }
    }
}

