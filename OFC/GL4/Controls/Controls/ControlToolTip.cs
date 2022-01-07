﻿/*
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

using System.Drawing;

namespace GLOFC.GL4.Controls
{
    // tooltip, if added to GLControlDisplay, and AutomaticDelay>0 it acts as a global tooltip and displays the tooltip of the current mouseover control
    // if added to another form or AutomaticDelay = 0, its manual and you need to call Show() to display it and Hide() to remove it.

    public class GLToolTip : GLForeDisplayBase
    {
        public int AutomaticDelay { get; set; } = 500;
        public ulong FadeInTime { get; set; } = 250;
        public ulong FadeOutTime { get; set; } = 250;
        public StringFormat StringFormat { get; set; } = null;
        public Point AutoPlacementOffset { get; set; } = new Point(10, 0);
        public new bool AutoSize { get { return false; } set { throw new System.NotImplementedException(); } }
        public float ShownOpacity { get; set; } = 1.0f;

        public GLToolTip(string name, Color? backcolour = null) : base(name, DefaultWindowRectangle)
        {
            BackColor = backcolour.HasValue ? backcolour.Value : DefaultToolTipBackColor;
            foreColor = DefaultToolTipForeColor;
            SetNI(padding: new Padding(3));
            VisibleNI = false;
            timer.Tick += TimeOut;
        }

        public GLToolTip() : this("TT?", null)
        {
        }

        protected override void Paint(Graphics gr)
        {
            using (Brush br = new SolidBrush(ForeColor))
            {
                System.Diagnostics.Debug.WriteLine("Tooltip paint " + tiptext);
                if (StringFormat != null)
                    gr.DrawString(tiptext, Font, br, ClientRectangle,StringFormat);
                else
                    gr.DrawString(tiptext, Font, br, ClientRectangle);
            }
        }


        protected override void OnControlAdd(GLBaseControl parent, GLBaseControl child)
        {
            base.OnControlAdd(parent, child);

            var p = parent as GLControlDisplay;     // if attached to control display, its an automatic tool tip
            if (p != null && AutomaticDelay>0)      // only if auto delay is on
            {
                p.GlobalMouseMove += MouseMoved;
            }
        }

        protected override void OnControlRemove(GLBaseControl parent, GLBaseControl child)
        {
            var p = parent as GLControlDisplay;     // if attached to control display, its an automatic tool tip
            if (p != null )            // unsubscribe, and we can do this even if we did not subsribe in the first place
            {
                p.GlobalMouseMove -= MouseMoved;
            }

            base.OnControlRemove(parent, child);
        }

        public void Show(Point pos, string text)
        {
            if (Visible == false)
            {
                var size = BitMapHelpers.MeasureStringInBitmap(text, Font, StringFormat);
                Location = new Point(pos.X+AutoPlacementOffset.X, pos.Y + AutoPlacementOffset.Y);
                ClientSize = new Size((int)size.Width + 1, (int)size.Height + 1);
                TopMost = true;
                tiptext = text;
                Visible = true;
                Invalidate();       // must invalidate as paint uses tiptext.

                if (Parent is GLControlDisplay)
                {
                    if (FadeInTime > 0)     // if we are attached to control display, and we are fading in, do it
                    {
                        Opacity = 0;
                        Animators.Add(new AnimateOpacity(0, FadeInTime, true, ShownOpacity, 0.0f, true));   // note delta time
                    }
                    else
                        Opacity = ShownOpacity;
                }
            }
        }

        public void Hide()
        {
            if (Visible)
            {
                if (Parent is GLControlDisplay && FadeOutTime > 0)
                {
                    var animate = new AnimateOpacity(0, FadeOutTime, true, 0.0f, Opacity, true);
                    animate.FinishAction = (an, ctrl, time) => 
                    { 
                        ctrl.Visible = false;
                        // animators are removed at this point, find out what the positional mouse args would be and call again to give the next control the chance
                        var me = FindDisplay().MouseEventArgsFromPoint(FindDisplay().MouseWindowPosition);      
                        MouseMoved(me);
                    };
                    Animators.Add(animate);
                }
                else
                    Visible = false;
            }
        }

        private void MouseMoved(GLMouseEventArgs e)
        {
            // System.Diagnostics.Debug.WriteLine($"GLOBAL: Pos {e.WindowLocation} VP {e.ViewportLocation} SC {e.ScreenCoord} BL {e.BoundsLocation} loc {e.Location} {e.Area} {control.Name}");

            if (Animators.Count > 0)        // if we are animating, can't do anything
                return;

            var control = e.Control as GLBaseControl;

            if ( mouseover != control)
            {
                Hide();

                if (control == null)       // out
                {
                    timer.Stop();
                }
                else if (mouseover != null )    // into
                {
                    if (control.ToolTipText.HasChars() && control.Enabled)
                    {
                     //   System.Diagnostics.Debug.WriteLine("Tooltip Found " + ctrl.Name + " " + e.ScreenCoord);
                        timer.Start(AutomaticDelay);     // start timer
                        showloc = entryloc = e.ScreenCoord;
                    }
                    else
                    {
                      //  System.Diagnostics.Debug.WriteLine("No Tooltip Found " + ctrl.Name + " " + e.ScreenCoord);
                        timer.Stop();
                    }
                }

                mouseover = control;       // set control mouse is over
            }
            else
            {       // in same control
                if (timer.Running)
                {
                    int delta2 = (e.ScreenCoord.X - entryloc.X) * (e.ScreenCoord.X - entryloc.X) + (e.ScreenCoord.Y + entryloc.Y) * (e.ScreenCoord.Y + entryloc.Y);

                    if (delta2 > 16)
                    {
                        entryloc = e.ScreenCoord;
                        timer.Start(AutomaticDelay);        // moved within control, restart
                       // System.Diagnostics.Debug.WriteLine("Restart " + mouseover.Name);
                    }

                    showloc = e.ScreenCoord;
                }
            }
        }

        private void TimeOut(Timers.Timer t, long timeout)
        {
            if (!Visible && mouseover != null )
            {
                //System.Diagnostics.Debug.WriteLine("Show " + mouseover.Name + " " + showloc +  " " + mouseover.ToolTipText);
                Show(showloc, mouseover.ToolTipText);
            }
        }

        private GLOFC.Timers.Timer timer = new Timers.Timer();
        private Point entryloc;
        private Point showloc;
        private GLBaseControl mouseover = null;
        private string tiptext;

    }
}