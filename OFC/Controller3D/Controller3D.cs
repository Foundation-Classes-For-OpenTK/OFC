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
    /// <summary>
    /// This namespace contains the controller 3D classes which allow the lookat and eyeposition to be manipulated efficiently by keyboard and mouse
    /// * Controller3D (and the double version) are the top level 3D controller class and handles keyboard and mouse interactions
    /// * Controller3DBase is common between the float and double versions of the controller.
    /// * KeyboardMonitor remembers key presses.
    /// * PositionCamera (and the double version) holds the lookat position, camera position and camera direction, the zoom, and has slew functions to move around the world space
    /// </summary>
    internal static class NamespaceDoc { } // just for documentation purposes


    /// <summary>
    /// Class brings together keyboard, mouse, posdir, zoom to provide a means to move thru the playfield and zoom.
    /// Handles keyboard actions and mouse actions to provide a nice method of controlling the 3d playfield 
    /// Attaches to a GLWindowControl and hooks its events to provide control 
    /// </summary>

    public class Controller3D : Controller3DBase
    {
        /// <summary> Get or set zoom distance </summary>
        public float ZoomDistance { get { return PosCamera.Zoom1Distance; } set { PosCamera.Zoom1Distance = value; } }

        /// <summary> Callback to paint your objects at invalidation of window. Ulong is time in milliseconds </summary>
        public Action<Controller3D, ulong> PaintObjects { get; set; }

        /// <summary> Position camera object for this controller </summary>
        public PositionCamera PosCamera { get; private set; } = new PositionCamera();

        /// <summary> Start the class with this matrixcalc and position camera. Pass the GL window control, and the initial lookat/cameradirection and zoom </summary>
        public void Start(GLMatrixCalc mc, PositionCamera pc, GLWindowControl win, Vector3 lookat, Vector3 cameradirdegrees, float zoomn)
        {
            MatrixCalc = mc;
            PosCamera = pc;
            Start(win,lookat, cameradirdegrees, zoomn);
        }

        /// <summary> Start the class with this matrixcalc. Pass the GL window control, and the initial lookat/cameradirection and zoom.
        /// Control if registration of mouse and keyboard UI is performed with GL window control
        /// </summary>
        public void Start(GLMatrixCalc mc, GLWindowControl win, Vector3 lookat, Vector3 cameradirdegrees, float zoomn,
                        bool registermouseui = true, bool registerkeyui = true)
        {
            MatrixCalc = mc;
            Start(win,lookat, cameradirdegrees, zoomn, registermouseui, registerkeyui);
        }

        /// <summary> Start the class. Pass the GL window control, and the initial lookat/cameradirection and zoom.
        /// Control if registration of mouse and keyboard UI is performed with GL window control
        /// </summary>
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
            MatrixCalc.CalculateModelMatrix(PosCamera.LookAt, PosCamera.EyePosition,  PosCamera.CameraRotation);
            MatrixCalc.CalculateProjectionMatrix();
        }

        // Pos Direction interface - all of these will cause movement, which will be detected by the PosCamera different tracker. Use RecalcMatrixIfMoved
        /// <summary>Set the position, lookat, eyepos, camerarotation </summary>
        public void SetPositionCamera(Vector3 lookat, Vector3 eyepos, float camerarot) { PosCamera.SetPositionCamera(lookat, eyepos, camerarot); }
        /// <summary> Move the look at position, eye position tracks to it</summary>
        public void MoveLookAt(Vector3 pos, bool killslew = true) { PosCamera.MoveLookAt(pos, killslew); }
        /// <summary> Translate the eye and look. </summary>
        public void TranslatePosition(Vector3 posx, bool killslew = true) { PosCamera.Translate(posx, killslew); }

        /// <summary> Slew to lookat position. Timeslewsec is 0 for immediate, less than 0 for automatic calc, else seconds. unitspersecond determines speed for automatic </summary>
        public void SlewToPosition(Vector3 normpos, float timeslewsec = 0, float unitspersecond = 10000F) { PosCamera.GoTo(normpos, timeslewsec, unitspersecond); }
        /// <summary> Slew to lookat position and zoom. Timeslewsec is 0 for immediate, less than 0 for automatic calc, else seconds. unitspersecond determines speed for automatic </summary>
        public void SlewToPositionZoom(Vector3 normpos, float zoom, float timeslewsec = 0, float unitspersecond = 10000F) { PosCamera.GoToZoom(normpos, zoom,timeslewsec, unitspersecond); }

        /// <summary> Set the camera direction </summary>
        public void SetCameraDir(Vector2 pos) { PosCamera.CameraDirection = pos; }
        /// <summary> Pan to this camera position. timeslewsec is 0 for immediate, less than 0 estimate, greater than 0 seconds </summary>
        public void Pan(Vector2 pos, float timeslewsec = 0) { PosCamera.Pan(pos, timeslewsec); }
        /// <summary> Pan to look at this position. timeslewsec is 0 for immediate, less than 0 estimate, greater than 0 seconds </summary>
        public void PanTo(Vector3 normtarget, float timeslewsec = 0) { PosCamera.PanTo(normtarget, timeslewsec); }
        /// <summary> Pan and zoom to look at this position. timeslewsec is 0 for immediate, less than 0 estimate, greater than 0 seconds </summary>
        public void PanZoomTo(Vector3 normtarget, float zoom, float time = 0)  {  PosCamera.PanZoomTo(normtarget, zoom, time); }
        /// <summary> Set position camera (lookat and eye) from this setting string</summary>
        public bool SetPositionCamera(string s)     // String holds pos/eye
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
            MatrixCalc.CalculateModelMatrix(PosCamera.LookAt, PosCamera.EyePosition, PosCamera.CameraRotation);
            MatrixCalc.CalculateProjectionMatrix();
            glwin.Invalidate();
        }

        /// <summary> Redraw scene </summary>
        public void Redraw() { glwin.Invalidate(); }          

        /// <summary> Debug only - redraw these number of times and return time in ms</summary>
        public long Redraw(int times)                            
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < times; i++)
                glControl_Paint(null,(ulong)sw.ElapsedMilliseconds);
            long time = sw.ElapsedMilliseconds;
            sw.Stop();
            return time;
        }

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
        public bool HandleKeyboardSlewsAndInvalidateIfMoved(bool activated, Action<KeyboardMonitor> handleotherkeys = null, float minmove = 0.01f, float mincamera = 1.0f)
        {
            HandleKeyboardSlews(activated, handleotherkeys);
            bool moved = RecalcMatrixIfMoved(minmove, mincamera);
            if (moved)
                glwin.Invalidate();
            return moved;
        }

        /// <summary>Recalc matrix if moved </summary>
        public bool RecalcMatrixIfMoved(float minmove = 0.01f, float mincamera = 1.0f)
        {
            bool moved = PosCamera.IsMoved(minmove, mincamera);

            if (moved)
            {
                MatrixCalc.CalculateModelMatrix(PosCamera.LookAt, PosCamera.EyePosition, PosCamera.CameraRotation);
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
            MatrixCalc.ResizeViewPort(this,glwin.Size);
            MatrixCalc.CalculateModelMatrix(PosCamera.LookAt, PosCamera.EyePosition, PosCamera.CameraRotation); // non perspective viewport changes also can affect model matrix
            MatrixCalc.CalculateProjectionMatrix();
            glwin.Invalidate();
        }

        // Paint the scene - just pass the call down to the installed PaintObjects
        // gl paint hook, invoke paint objects for 3d 
        private void glControl_Paint(Object obj,ulong ts)
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
                PosCamera.RotateCamera(dir, addzrot, changelookat);
            else
                PosCamera.RotateCamera(new Vector2(dir.X,-dir.Y), addzrot, changelookat); // if we are operating in gl mode (+Z to viewer), the axis is turned, so rotation needs inverting
        }

        private protected override void Translate(Vector3 dir)
        {
            if (MatrixCalc.ModelAxisPositiveZAwayFromViewer)
                PosCamera.Translate(dir);
            else
                PosCamera.Translate(new Vector3(-dir.X,dir.Y,dir.Z));   // if we are operating in gl mode, the axis is turned, so X translation needs inverting
        }

        private protected override void ZoomScale(bool dir)
        {
            PosCamera.ZoomScale(dir);
        }

        private protected override float ZoomFactor => PosCamera.ZoomFactor;
        private protected override float ZoomMin => PosCamera.ZoomMin;
        private protected override float ZoomMax => PosCamera.ZoomMax;
        private protected override void ZoomBy(float v)
        {
            PosCamera.Zoom(PosCamera.ZoomFactor * v);
        }
        private protected override void GoToZoom(float v, float time)
        {
            PosCamera.GoToZoom(v, time);
        }
        private protected override Vector2 CameraDirection => PosCamera.CameraDirection;
        private protected override float CameraRotation => PosCamera.CameraRotation; 
        private protected override void Invalidate()
        {
            glwin.Invalidate();
        }


        private GLWindowControl glwin;
        #endregion
    }
}
