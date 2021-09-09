/*
 * Copyright 2015 - 2019 EDDiscovery development team + Robbyxp1 @ github.com
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
 * 
 */

using OpenTK;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace GLOFC.Controller
{
    // class brings together keyboard, mouse, posdir, zoom to provide a means to move thru the playfield and zoom.
    // handles keyboard actions and mouse actions to provide a nice method of controlling the 3d playfield
    // Attaches to a GLWindowControl and hooks its events to provide control

    public class Controller3D
    {
        public float ZoomDistance { get { return PosCamera.Zoom1Distance; } set { PosCamera.Zoom1Distance = value; } }

        private GLWindowControl glwin;

        public Func<int, float, float> KeyboardTravelSpeed;                     // optional set to scale travel key commands given this time interval and camera distance
        public Func<int, float> KeyboardRotateSpeed;                            // optional set to scale camera key rotation commands given this time interval
        public Func<int, float> KeyboardZoomSpeed;                              // optional set to scale zoom speed commands given this time interval

        public float MouseRotateAmountPerPixel { get; set; } = 0.25f;           // mouse speeds, degrees/pixel
        public float MouseUpDownAmountAtZoom1PerPixel { get; set; } = 0.5f;     // per pixel movement at zoom 1 (zoom scaled)
        public float MouseTranslateAmountAtZoom1PerPixel { get; set; } = 2.0f;  // per pixel movement at zoom 1

        public Action<Controller3D, ulong> PaintObjects;                        // Mandatory. ulong is time in ms

        public GLMatrixCalc MatrixCalc { get; set; } = new GLMatrixCalc();
        public PositionCamera PosCamera { get; private set; } = new PositionCamera();

        public void Start(GLMatrixCalc mc, PositionCamera pc, GLWindowControl win, Vector3 lookat, Vector3 cameradirdegrees, float zoomn)
        {
            MatrixCalc = mc;
            PosCamera = pc;
            Start(win, lookat, cameradirdegrees, zoomn);
        }

        public void Start(GLMatrixCalc mc, GLWindowControl win, Vector3 lookat, Vector3 cameradirdegrees, float zoomn,
                        bool registermouseui = true, bool registerkeyui = true)
        {
            MatrixCalc = mc;
            Start(win, lookat, cameradirdegrees, zoomn, registermouseui, registerkeyui);
        }

        // set up starting conditions. If registerui = false, you handle the direct window mouse/keyboard actions

        public void Start(GLWindowControl win, Vector3 lookat, Vector3 cameradirdegrees, float zoomn,
                            bool registermouseui = true, bool registerkeyui = true)
        {
            glwin = win;

            win.Resize += glControl_Resize;
            win.Paint += glControl_Paint;

            if (registermouseui)
            {
                win.MouseDown += MouseDown;
                win.MouseUp += MouseUp;
                win.MouseMove += MouseMove;
                win.MouseWheel += MouseWheel;
            }

            if ( registerkeyui )
            { 
                win.KeyDown += KeyDown;
                win.KeyUp += KeyUp;
            }

            MatrixCalc.ResizeViewPort(this,win.Size);               // inform matrix calc of window size

            PosCamera.SetPositionZoom(lookat, new Vector2(cameradirdegrees.X, cameradirdegrees.Y), zoomn, cameradirdegrees.Z);
            MatrixCalc.CalculateModelMatrix(PosCamera.Lookat, PosCamera.EyePosition, PosCamera.CameraDirection, PosCamera.CameraRotation);
            MatrixCalc.CalculateProjectionMatrix();
        }

        // Pos Direction interface

        public bool YHoldMovement { get { return PosCamera.YHoldMovement; } set { PosCamera.YHoldMovement = value; } }      // hold Y steady when moving, whatever the camera direction
        public void TranslatePosition(Vector3 posx) { PosCamera.Translate(posx); }
        public void SlewToPosition(Vector3 normpos, float timeslewsec = 0, float unitspersecond = 10000F) { PosCamera.GoTo(normpos, timeslewsec, unitspersecond); }
        public void SlewToPositionZoom(Vector3 normpos, float zoom, float timeslewsec = 0, float unitspersecond = 10000F) { PosCamera.GoToZoom(normpos, zoom,timeslewsec, unitspersecond); }

        public void SetCameraDir(Vector2 pos) { PosCamera.CameraDirection = pos; }
        public void Pan(Vector2 pos, float timeslewsec = 0) { PosCamera.Pan(pos, timeslewsec); }
        public void PanTo(Vector3 normtarget, float timeslewsec = 0) { PosCamera.PanTo(normtarget, timeslewsec); }
        public void PanZoomTo(Vector3 normtarget, float zoom, float time = 0)  {  PosCamera.PanZoomTo(normtarget, zoom, time); }

        public void KillSlew()
        {
            PosCamera.KillSlew();
        }

        // perspective.. use this don't just change the matrixcalc.
        public void ChangePerspectiveMode(bool on)
        {
            MatrixCalc.InPerspectiveMode = on;
            MatrixCalc.CalculateModelMatrix(PosCamera.Lookat, PosCamera.EyePosition, PosCamera.CameraDirection, PosCamera.CameraRotation);
            MatrixCalc.CalculateProjectionMatrix();
            glwin.Invalidate();
        }

        // Redraw scene, something has changed

        public void Redraw() { glwin.Invalidate(); }            // invalidations causes a glControl_Paint

        public long Redraw(int times)                               // for testing, redraw the scene N times and give ms 
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < times; i++)
                glControl_Paint(null,(ulong)sw.ElapsedMilliseconds);
            long time = sw.ElapsedMilliseconds;
            sw.Stop();
            return time;
        }

        // Owner should call this at regular intervals.
        // handle keyboard, handle other keys if required

        public void HandleKeyboardSlews(bool activated, Action<KeyboardMonitor> handleotherkeys = null)
        {
            ulong curtime = glwin.ElapsedTimems;
            int LastHandleInterval = lastkeyintervalcount.HasValue ? (int)(curtime - lastkeyintervalcount) : 1;
            lastkeyintervalcount = curtime;

            if (activated && glwin.Focused)                      // if we can accept keys
            {
                if (MatrixCalc.InPerspectiveMode)               // camera rotations are only in perspective mode
                {
                    PosCamera.CameraKeyboard(keyboard, KeyboardRotateSpeed?.Invoke(LastHandleInterval) ?? (0.02f * LastHandleInterval));
                }

                PosCamera.PositionKeyboard(keyboard, MatrixCalc.InPerspectiveMode, KeyboardTravelSpeed?.Invoke(LastHandleInterval, MatrixCalc.EyeDistance) ?? (0.1f * LastHandleInterval));
                PosCamera.ZoomKeyboard(keyboard, KeyboardZoomSpeed?.Invoke(LastHandleInterval) ?? (1.0f + ((float)LastHandleInterval * 0.002f)));      // zoom slew is not affected by the above

                if (keyboard.HasBeenPressed(Keys.P, KeyboardMonitor.ShiftState.Ctrl))
                    ChangePerspectiveMode(!MatrixCalc.InPerspectiveMode);

                handleotherkeys?.Invoke(keyboard);

                keyboard.ClearHasBeenPressed();
            }
            else
            {
                keyboard.Reset();
            }

            PosCamera.DoSlew(LastHandleInterval);     // changes here will be picked up by AnythingChanged
        }

        // Polls for keyboard movement
        // and with Invalidate on movement

        public bool HandleKeyboardSlewsAndInvalidateIfMoved(bool activated, Action<KeyboardMonitor> handleotherkeys = null, float minmove = 0.01f, float mincamera = 1.0f)
        {
            HandleKeyboardSlews(activated, handleotherkeys);
            return InvalidateIfMoved(minmove,mincamera);
        }

        // Invalidate on movement
        public bool InvalidateIfMoved(float minmove = 0.01f, float mincamera = 1.0f)
        { 
            bool moved = PosCamera.IsMoved(minmove,mincamera);

            if (moved )
            {
                //System.Diagnostics.Debug.WriteLine("Changed");
                MatrixCalc.CalculateModelMatrix(PosCamera.Lookat, PosCamera.EyePosition, PosCamera.CameraDirection, PosCamera.CameraRotation);
                glwin.Invalidate();
            }

            return moved;
        }

        public void MouseDown(object sender, GLMouseEventArgs e)
        {
          //  System.Diagnostics.Debug.WriteLine("3dcontroller mouse down");

            mouseDownPos = MatrixCalc.AdjustWindowCoordToViewPortCoord(e.WindowLocation);

            if (e.Button.HasFlag(GLMouseEventArgs.MouseButtons.Left))
            {
                mouseStartRotate = mouseDownPos;
               // System.Diagnostics.Debug.WriteLine($"..start rotate {mouseStartRotate} {e.WindowLocation}");
            }

            if (e.Button.HasFlag(GLMouseEventArgs.MouseButtons.Right))
            {
                mouseStartTranslateXY = mouseDownPos;
                mouseStartTranslateXZ = mouseDownPos;
            }
        }

        public void MouseUp(object sender, GLMouseEventArgs e)
        {
           // System.Diagnostics.Debug.WriteLine("3dcontroller mouse up");

            var mousepos = MatrixCalc.AdjustWindowCoordToViewPortCoord(e.WindowLocation);

            bool notmovedmouse = Math.Abs(mousepos.X - mouseDownPos.X) + Math.Abs(mousepos.Y - mouseDownPos.Y) < 4;

            if (!notmovedmouse)     // if we moved it, its not a stationary click, ignore
                return;

            if (e.Button == GLMouseEventArgs.MouseButtons.Right)                    // right clicks are about bookmarks.
            {
                mouseStartTranslateXY = new Point(int.MinValue, int.MinValue);         // indicate rotation is finished.
                mouseStartTranslateXZ = new Point(int.MinValue, int.MinValue);
            }
        }

        public void MouseMove(object sender, GLMouseEventArgs e)
        {
            var mousepos = MatrixCalc.AdjustWindowCoordToViewPortCoord(e.WindowLocation);

            if (e.Button == GLMouseEventArgs.MouseButtons.Left)
            {
                if (MatrixCalc.InPerspectiveMode && mouseStartRotate.X != int.MinValue) // on resize double click resize, we get a stray mousemove with left, so we need to make sure we actually had a down event
                {
                    int dx = mousepos.X - mouseStartRotate.X;
                    int dy = mousepos.Y - mouseStartRotate.Y;
                   // System.Diagnostics.Debug.WriteLine($"3dcontroller Mouse move left {mouseStartRotate} {mousepos} {e.WindowLocation} {dx} {dy}");

                    KillSlew();    // all slews

                    mouseStartTranslateXZ = mouseStartRotate = mousepos;

                    PosCamera.RotateCamera(new Vector2((float)(dy * MouseRotateAmountPerPixel), (float)(dx * MouseRotateAmountPerPixel)), 0, true);
                }
            }
            else if (e.Button == GLMouseEventArgs.MouseButtons.Right)
            {
                if (mouseStartTranslateXY.X != int.MinValue)
                {
                    KillSlew();

                    int dx = mousepos.X - mouseStartTranslateXY.X;
                    int dy = mousepos.Y - mouseStartTranslateXY.Y;

                    mouseStartTranslateXZ = mouseStartRotate = mousepos;

                    //System.Diagnostics.Trace.WriteLine("dx" + dx.ToString() + " dy " + dy.ToString() + " Button " + e.Button.ToString());

                    PosCamera.Translate(new Vector3(0, -dy * (1.0f / PosCamera.ZoomFactor) * MouseUpDownAmountAtZoom1PerPixel, 0));
                }
            }
            else if (e.Button == (GLMouseEventArgs.MouseButtons.Left | GLMouseEventArgs.MouseButtons.Right))
            {
                if (mouseStartTranslateXZ.X != int.MinValue)
                {
                    KillSlew();

                    int dx = mousepos.X - mouseStartTranslateXZ.X;
                    int dy = mousepos.Y - mouseStartTranslateXZ.Y;

                    mouseStartTranslateXZ = mouseStartRotate = mouseStartTranslateXY = mousepos;

                    Vector3 translation = new Vector3(dx * (1.0f / PosCamera.ZoomFactor) * MouseTranslateAmountAtZoom1PerPixel, -dy * (1.0f / PosCamera.ZoomFactor) * MouseTranslateAmountAtZoom1PerPixel, 0.0f);

                    if (MatrixCalc.InPerspectiveMode)
                    {
                        //System.Diagnostics.Trace.WriteLine("dx" + dx.ToString() + " dy " + dy.ToString() + " Button " + e.Button.ToString());

                        Matrix3 transform = Matrix3.CreateRotationZ((float)(-PosCamera.CameraDirection.Y * Math.PI / 180.0f));
                        translation = Vector3.Transform(translation, transform);

                        PosCamera.Translate(new Vector3(translation.X, 0, translation.Y));
                    }
                    else
                        PosCamera.Translate(new Vector3(translation.X, 0, translation.Y));
                }
            }

        }

        public void MouseWheel(object sender, GLMouseEventArgs e)
        {
            if (e.Delta != 0)
            {
                if (keyboard.Ctrl)
                {
                    if (MatrixCalc.FovScale(e.Delta < 0))
                    {
                        MatrixCalc.CalculateProjectionMatrix();
                        glwin.Invalidate();
                    }
                }
                else
                {
                    PosCamera.ZoomScale(e.Delta > 0);
                }
            }
        }

        public void KeyDown(object sender, GLKeyEventArgs e)
        {
            keyboard.KeyDown(e.Control, e.Shift, e.Alt, e.KeyCode);
        }

        public void KeyUp(object sender, GLKeyEventArgs e)
        {
            keyboard.KeyUp(e.Control, e.Shift, e.Alt, e.KeyCode);
        }

        #region Implementation

        private void glControl_Resize(object sender)          // from the window, a resize event
        {
            System.Diagnostics.Debug.WriteLine("Controller3d Resize" + glwin.Size);
            MatrixCalc.ResizeViewPort(this,glwin.Size);
            MatrixCalc.CalculateModelMatrix(PosCamera.Lookat, PosCamera.EyePosition, PosCamera.CameraDirection, PosCamera.CameraRotation); // non perspective viewport changes also can affect model matrix
            MatrixCalc.CalculateProjectionMatrix();
            glwin.Invalidate();
        }

        // Paint the scene - just pass the call down to the installed PaintObjects

        // gl paint hook, invoke paint objects for 3d 
        private void glControl_Paint(Object obj,ulong ts)
        {
            PaintObjects?.Invoke(this, ts);
        }

        private KeyboardMonitor keyboard = new KeyboardMonitor();        // needed to be held because it remembers key downs
        private ulong? lastkeyintervalcount = null;

        private Point mouseDownPos;
        private Point mouseStartRotate = new Point(int.MinValue, int.MinValue);        // used to indicate not started for these using mousemove
        private Point mouseStartTranslateXZ = new Point(int.MinValue, int.MinValue);
        private Point mouseStartTranslateXY = new Point(int.MinValue, int.MinValue);

        #endregion
    }
}
