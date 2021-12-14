/*
 * Copyright 2015 - 2021 EDDiscovery development team + Robbyxp1 @ github.com
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

using OpenTK;
using System;
using System.Diagnostics;

namespace GLOFC.Controller
{
    // class brings together keyboard, mouse, posdir, zoom to provide a means to move thru the playfield and zoom.
    // handles keyboard actions and mouse actions to provide a nice method of controlling the 3d playfield
    // Attaches to a GLWindowControl and hooks its events to provide control

    public class Controller3D : Controller3DBase
    {
        public float ZoomDistance { get { return PosCamera.Zoom1Distance; } set { PosCamera.Zoom1Distance = value; } }

        public Action<Controller3D, ulong> PaintObjects;                        // Mandatory. ulong is time in ms

        public PositionCamera PosCamera { get; private set; } = new PositionCamera();

        // Start with externs MC/PC 
        public void Start(GLMatrixCalc mc, PositionCamera pc, GLWindowControl win, Vector3 lookat, Vector3 cameradirdegrees, float zoomn)
        {
            MatrixCalc = mc;
            PosCamera = pc;
            Start(win,lookat, cameradirdegrees, zoomn);
        }

        // Start with external camera

        public void Start(GLMatrixCalc mc, GLWindowControl win, Vector3 lookat, Vector3 cameradirdegrees, float zoomn,
                        bool registermouseui = true, bool registerkeyui = true)
        {
            MatrixCalc = mc;
            Start(win,lookat, cameradirdegrees, zoomn, registermouseui, registerkeyui);
        }

        // set up starting conditions. If registerui = false, you handle the direct window mouse/keyboard actions

        public void Start(GLWindowControl win, Vector3 lookat, Vector3 cameradirdegrees, float zoomn, bool registermouseui = true, bool registerkeyui = true)
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
            MatrixCalc.CalculateModelMatrix(PosCamera.LookAt, PosCamera.EyePosition, PosCamera.CameraDirection, PosCamera.CameraRotation);
            MatrixCalc.CalculateProjectionMatrix();
        }

        // Control YHold for keyboard

        // Pos Direction interface - all of these will cause movement, which will be detected by the PosCamera different tracker. Use RecalcMatrixIfMoved
        public void SetPositionCamera(Vector3 lookat, Vector3 eyepos, float camerarot) { PosCamera.SetPositionCamera(lookat, eyepos, camerarot); }
        public void MoveLookAt(Vector3 pos, bool killslew = true) { PosCamera.MoveLookAt(pos, killslew); }
        public void TranslatePosition(Vector3 posx, bool killslew = true) { PosCamera.Translate(posx, killslew); }
        public void SlewToPosition(Vector3 normpos, float timeslewsec = 0, float unitspersecond = 10000F) { PosCamera.GoTo(normpos, timeslewsec, unitspersecond); }
        public void SlewToPositionZoom(Vector3 normpos, float zoom, float timeslewsec = 0, float unitspersecond = 10000F) { PosCamera.GoToZoom(normpos, zoom,timeslewsec, unitspersecond); }

        public void SetCameraDir(Vector2 pos) { PosCamera.CameraDirection = pos; }
        public void Pan(Vector2 pos, float timeslewsec = 0) { PosCamera.Pan(pos, timeslewsec); }
        public void PanTo(Vector3 normtarget, float timeslewsec = 0) { PosCamera.PanTo(normtarget, timeslewsec); }
        public void PanZoomTo(Vector3 normtarget, float zoom, float time = 0)  {  PosCamera.PanZoomTo(normtarget, zoom, time); }
        public bool SetPositionCamera(string s)     // String holds pos/eye
        {
            if (PosCamera.SetPositionCamera(s))
            {
                MatrixCalc.CalculateModelMatrix(PosCamera.LookAt, PosCamera.EyePosition, PosCamera.CameraDirection, PosCamera.CameraRotation);
                return true;
            }
            else
                return false;
        }

        // perspective.. use this don't just change the matrixcalc.
        public void ChangePerspectiveMode(bool on)
        {
            MatrixCalc.InPerspectiveMode = on;
            MatrixCalc.CalculateModelMatrix(PosCamera.LookAt, PosCamera.EyePosition, PosCamera.CameraDirection, PosCamera.CameraRotation);
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
        // Does not call any GL functions - only affects Matrixcalc

        public void HandleKeyboardSlews(bool activated, Action<KeyboardMonitor> handleotherkeys = null)
        {
            int interval = base.HandleKeyboardSlews(glwin.ElapsedTimems, glwin.Focused, activated, handleotherkeys);
            PosCamera.DoSlew(interval);     // changes here will be picked up by AnythingChanged
        }

        // Polls for keyboard movement
        // and with Invalidate on movement
        public bool HandleKeyboardSlewsAndInvalidateIfMoved(bool activated, Action<KeyboardMonitor> handleotherkeys = null, float minmove = 0.01f, float mincamera = 1.0f)
        {
            HandleKeyboardSlews(activated, handleotherkeys);
            bool moved = RecalcMatrixIfMoved(minmove, mincamera);
            if (moved)
                glwin.Invalidate();
            return moved;
        }

        public bool RecalcMatrixIfMoved(float minmove = 0.01f, float mincamera = 1.0f)
        {
            bool moved = PosCamera.IsMoved(minmove, mincamera);

            if (moved)
            {
                MatrixCalc.CalculateModelMatrix(PosCamera.LookAt, PosCamera.EyePosition, PosCamera.CameraDirection, PosCamera.CameraRotation);
            }

            return moved;
        }


        #region Implementation

        // from the window, a resize event. Must have the correct context, if multiple, set glwin.EnsureCurrentPaintResize
        private void glControl_Resize(object sender)         
        {
            //System.Diagnostics.Debug.WriteLine("Controller3d Resize" + glwin.Size);
            MatrixCalc.ResizeViewPort(this,glwin.Size);
            MatrixCalc.CalculateModelMatrix(PosCamera.LookAt, PosCamera.EyePosition, PosCamera.CameraDirection, PosCamera.CameraRotation); // non perspective viewport changes also can affect model matrix
            MatrixCalc.CalculateProjectionMatrix();
            glwin.Invalidate();
        }

        // Paint the scene - just pass the call down to the installed PaintObjects
        // gl paint hook, invoke paint objects for 3d 
        private void glControl_Paint(Object obj,ulong ts)
        {
            PaintObjects?.Invoke(this, ts);
        }

        protected override void KillSlew()
        {
            PosCamera.KillSlew();
        }
        protected override void RotateCamera(Vector2 dir, float addzrot, bool changelookat)
        {
            PosCamera.RotateCamera(dir, addzrot, changelookat);
        }

        protected override void Translate(Vector3 dir)
        {
            PosCamera.Translate(dir);
        }

        protected override void ZoomScale(bool dir)
        {
            PosCamera.ZoomScale(dir);
        }

        protected override float ZoomFactor => PosCamera.ZoomFactor;
        protected override float ZoomMin => PosCamera.ZoomMin;
        protected override float ZoomMax => PosCamera.ZoomMax;
        protected override void ZoomBy(float v)
        {
            PosCamera.Zoom(PosCamera.ZoomFactor * v);
        }
        protected override void GoToZoom(float v, float time)
        {
            PosCamera.GoToZoom(v, time);
        }
        protected override Vector2 CameraDirection => PosCamera.CameraDirection;
        protected override float CameraRotation => PosCamera.CameraRotation; protected override void Invalidate()
        {
            glwin.Invalidate();
        }


        private GLWindowControl glwin;
        #endregion
    }
}
