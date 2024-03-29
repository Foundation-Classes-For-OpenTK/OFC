﻿/*
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
    /// <summary>
    /// Class brings together keyboard, mouse, posdir, zoom to provide a means to move thru the playfield and zoom.
    /// Handles keyboard actions and mouse actions to provide a nice method of controlling the 3d playfield 
    /// Attaches to a GLWindowControl and hooks its events to provide control 
    /// </summary>

    public class Controller3Dd : Controller3DBase
    {
        /// <summary> Get attached GLWindowControl </summary>
        public GLWindowControl Window { get { return glwin; } }

        /// <summary> Get or set zoom distance </summary>
        public double ZoomDistance { get { return PosCamera.Zoom1Distance; } set { PosCamera.Zoom1Distance = value; } }

        /// <summary> Callback to paint your objects at invalidation of window. Ulong is time in milliseconds </summary>
        public Action<Controller3Dd, ulong> PaintObjects { get; set; }                        // Mandatory. ulong is time in ms

        /// <summary> Position camera object for this controller </summary>
        public PositionCamerad PosCamera { get; private set; } = new PositionCamerad();

        /// <summary> Start the class with this matrixcalc and position camera. Pass the GL window control, and the initial lookat/cameradirection and zoom </summary>
        public void Start(GLMatrixCalc mc, PositionCamerad pc, GLWindowControl win, Vector3d lookat, Vector3d cameradirdegrees, float zoomn)
        {
            MatrixCalc = mc;
            PosCamera = pc;
            Start(win, lookat, cameradirdegrees, zoomn);
        }

        /// <summary> Start the class with this matrixcalc. Pass the GL window control, and the initial lookat/cameradirection and zoom.
        /// Control if registration of mouse and keyboard UI is performed with GL window control
        /// </summary>
        public void Start(GLMatrixCalc mc, GLWindowControl win, Vector3d lookat, Vector3d cameradirdegrees, float zoomn,
                        bool registermouseui = true, bool registerkeyui = true)
        {
            MatrixCalc = mc;
            Start(win, lookat, cameradirdegrees, zoomn, registermouseui, registerkeyui);
        }

        /// <summary> Start the class. Pass the GL window control, and the initial lookat/cameradirection and zoom.
        /// Control if registration of mouse and keyboard UI is performed with GL window control
        /// </summary>
        public void Start(GLWindowControl win, Vector3d lookat, Vector3d cameradirdegrees, float zoomn,
                            bool registermouseui = true, bool registerkeyui = true)
        {
            glwin = win;

            win.Resize += glControl_Resize;
            win.Paint += glControl_Paint;

            Hook(glwin, registermouseui, registerkeyui);

            MatrixCalc.ResizeViewPort(this, win.Size);               // inform matrix calc of window size

            PosCamera.SetPositionZoom(lookat, new Vector2d(cameradirdegrees.X, cameradirdegrees.Y), zoomn, cameradirdegrees.Z);
            MatrixCalc.CalculateModelMatrix(PosCamera.LookAt, PosCamera.EyePosition, PosCamera.CameraRotation);
            MatrixCalc.CalculateProjectionMatrix();
        }

        // Pos Direction interface - all of these will cause movement, which will be detected by the PosCamera different tracker. Use RecalcMatrixIfMoved

        /// <summary>Set the position, lookat, eyepos, camerarotation </summary>
        public void SetPositionCamera(Vector3d lookat, Vector3d eyepos, float camerarot) { PosCamera.SetPositionCamera(lookat, eyepos, camerarot); }
        /// <summary> Move the look at position, eye position tracks to it</summary>
        public void MoveLookAt(Vector3d pos, bool killslew = true) { PosCamera.MoveLookAt(pos, killslew); }
        /// <summary> Translate the eye and look. </summary>
        public void TranslatePosition(Vector3d posx, bool killslew = true) { PosCamera.Translate(posx, killslew); }

        /// <summary> Slew to lookat position. Timeslewsec is 0 for immediate, less than 0 for automatic calc, else seconds. unitspersecond determines speed for automatic </summary>
        public void SlewToPosition(Vector3d normpos, float timeslewsec = 0, float unitspersecond = 10000F) { PosCamera.GoTo(normpos, timeslewsec, unitspersecond); }
        /// <summary> Slew to lookat position and zoom. Timeslewsec is 0 for immediate, less than 0 for automatic calc, else seconds. unitspersecond determines speed for automatic </summary>
        public void SlewToPositionZoom(Vector3d normpos, float zoom, float timeslewsec = 0, float unitspersecond = 10000F) { PosCamera.GoToZoom(normpos, zoom, timeslewsec, unitspersecond); }

        /// <summary> Set the camera direction </summary>
        public void SetCameraDir(Vector2d pos) { PosCamera.CameraDirection = pos; }
        /// <summary> Pan to this camera position. timeslewsec is 0 for immediate, less than 0 estimate, greater than 0 seconds </summary>
        public void Pan(Vector2d pos, float timeslewsec = 0) { PosCamera.Pan(pos, timeslewsec); }
        /// <summary> Pan to look at this position. timeslewsec is 0 for immediate, less than 0 estimate, greater than 0 seconds </summary>
        public void PanTo(Vector3d normtarget, float timeslewsec = 0) { PosCamera.PanTo(normtarget, timeslewsec); }
        /// <summary> Pan and zoom to look at this position. timeslewsec is 0 for immediate, less than 0 estimate, greater than 0 seconds </summary>
        public void PanZoomTo(Vector3d normtarget, float zoom, float time = 0) { PosCamera.PanZoomTo(normtarget, zoom, time); }
        /// <summary> Set position camera (lookat and eye) from this setting string</summary>
        public bool SetPositionCamera(string s)
        {
            if (PosCamera.SetPositionCamera(s))
            {
                MatrixCalc.CalculateModelMatrix(PosCamera.LookAt, PosCamera.EyePosition,  PosCamera.CameraRotation);
                return true;
            }
            else
                return false;
        }

        /// <summary> Set Perpective mode (true) or Othographic mode (false) </summary>
        public void ChangePerspectiveMode(bool on)
        {
            MatrixCalc.InPerspectiveMode = on;
            MatrixCalc.CalculateModelMatrix(PosCamera.LookAt, PosCamera.EyePosition,  PosCamera.CameraRotation);
            MatrixCalc.CalculateProjectionMatrix();
            glwin.Invalidate();
        }

        /// <summary> Redraw scene </summary>
        public void Redraw() { glwin.Invalidate(); }          

        /// <summary>
        /// Owner should call this at regular intervals.
        /// Handle keyboard, handle other keys if required
        /// Does not call any GL functions - only affects Matrixcalc
        /// </summary>
        public void HandleKeyboardSlews(bool activated, Action<KeyboardMonitor> handleotherkeys = null)
        {
            int interval = base.HandleKeyboardSlews(glwin.ElapsedTimems, glwin.Focused, activated, handleotherkeys);
            PosCamera.DoSlew(interval);     // changes here will be picked up by AnythingChanged
        }

        /// <summary>
        /// Owner should call this at regular intervals.
        /// Handle keyboard, handle other keys if required, and invalidates if moved.
        /// Does not call any GL functions - only affects Matrixcalc
        /// </summary>
        public bool HandleKeyboardSlewsAndInvalidateIfMoved(bool activated, Action<KeyboardMonitor> handleotherkeys = null, double minmove = 0.01f, double mincamera = 1.0f)
        {
            HandleKeyboardSlews(activated, handleotherkeys);
            bool moved = RecalcMatrixIfMoved(minmove, mincamera);
            if (moved)
                glwin.Invalidate();
            return moved;
        }

        /// <summary>Recalc matrix if moved </summary>
        public bool RecalcMatrixIfMoved(double minmove = 0.01f, double mincamera = 1.0f)
        {
            bool moved = PosCamera.IsMoved(minmove, mincamera);

            if (moved)
            {
                //System.Diagnostics.Debug.WriteLine("Changed");
                MatrixCalc.CalculateModelMatrix(PosCamera.LookAt, PosCamera.EyePosition,  PosCamera.CameraRotation);
            }

            return moved;
        }

        /// <summary> Recalculate matrix - only use if changed a fundamental in matrixcalc </summary>
        public void RecalcMatrices()
        {
            MatrixCalc.CalculateModelMatrix(PosCamera.LookAt, PosCamera.EyePosition, PosCamera.CameraRotation);
            MatrixCalc.CalculateProjectionMatrix();
        }

        #region Implementation

        // from the window, a resize event. Must have the correct context, if multiple, set glwin.EnsureCurrentPaintResize
        private void glControl_Resize(object sender)
        {
            //System.Diagnostics.Debug.WriteLine("Controller3d Resize" + glwin.Size);
            MatrixCalc.ResizeViewPort(this, glwin.Size);
            MatrixCalc.CalculateModelMatrix(PosCamera.LookAt, PosCamera.EyePosition,  PosCamera.CameraRotation); // non perspective viewport changes also can affect model matrix
            MatrixCalc.CalculateProjectionMatrix();
            glwin.Invalidate();
        }

        // Paint the scene - just pass the call down to the installed PaintObjects
        // gl paint hook, invoke paint objects for 3d
        private void glControl_Paint(ulong ts)
        {
            PaintObjects?.Invoke(this, ts);
        }

        private protected override void KillSlew()
        {
            PosCamera.KillSlew();
        }
        private protected override void RotateCamera(Vector2 dir, float addzrot, bool changelookat)
        {
            if (MatrixCalc.ModelAxisPositiveZAwayFromViewer)
                PosCamera.RotateCamera(new Vector2d(dir.X,dir.Y), addzrot, changelookat);
            else
                PosCamera.RotateCamera(new Vector2d(dir.X, -dir.Y), addzrot, changelookat); // if we are operating in gl mode (+Z to viewer), the axis is turned, so rotation needs inverting
        }
        private protected override void Translate(Vector3 dir)
        {
            if (MatrixCalc.ModelAxisPositiveZAwayFromViewer)
                PosCamera.Translate(new Vector3d(dir.X, dir.Y, dir.Z));
            else
                PosCamera.Translate(new Vector3d(-dir.X, dir.Y, dir.Z));   // if we are operating in gl mode, the axis is turned, so X translation needs inverting
        }
        private protected override void ZoomScale(bool dir)
        {
            PosCamera.ZoomScale(dir);
        }

        private protected override float ZoomFactor => (float)PosCamera.ZoomFactor;
        private protected override Vector2 CameraDirection => new Vector2((float)PosCamera.CameraDirection.X, (float)PosCamera.CameraDirection.Y);
        private protected override float CameraRotation => (float)PosCamera.CameraRotation;
        private protected override float ZoomMin => (float)PosCamera.ZoomMin;
        private protected override float ZoomMax => (float)PosCamera.ZoomMax;
        private protected override void Invalidate()
        {
            glwin.Invalidate();
        }
        private protected override void ZoomBy(float v)
        {
            PosCamera.Zoom(PosCamera.ZoomFactor * v);
        }
        private protected override void GoToZoom(float v, float time)
        {
            PosCamera.GoToZoom(v, time);
        }

        private GLWindowControl glwin;
        #endregion
    }
}
