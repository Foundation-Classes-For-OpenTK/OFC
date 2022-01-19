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
*
*
*/

using GLOFC.Utils;
using OpenTK;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace GLOFC.Controller
{
    /// <summary>
    /// Holds position (lookat, eye) and camera direction
    /// </summary>
    public class PositionCamerad        // holds lookat and eyepositions and camera
    {
        #region Positions
        /// <summary> Lookat position </summary>
        public Vector3d LookAt { get { return lookat; }  }
        /// <summary> Eye position </summary>
        public Vector3d EyePosition { get { return eyeposition; }  }

        /// <summary> Eye distance </summary>
        public double EyeDistance { get { return (lookat - EyePosition).Length; } }

        /// <summary> Translate eye and lookout </summary>
        public void Translate(Vector3d pos, bool killslew = true)
        {
            if ( killslew) KillSlew();  lookat += pos;  eyeposition += pos;
        }
        
        /// <summary> Move look at to value, with optional kill slew </summary>
        public void MoveLookAt(Vector3d value, bool killslew = true)
        {
            if ( killslew) KillSlew(); var eyeoffset = eyeposition - lookat; lookat = value; eyeposition = lookat + eyeoffset;
        }

        /// <summary> Slew to lookat position. Timeslewsec is 0 for immediate, less than 0 for automatic calc, else seconds. unitspersecond determines speed for automatic </summary>
        public void GoTo(Vector3d gotopos, double timeslewsec = 0, double unitspersecond = 10000F)       // may pass a Nan Position - no action. Y is normal sense
        {
            if (!double.IsNaN(gotopos.X))
            {
                //System.Diagnostics.Debug.WriteLine("Goto " + gotopos + " in " + timeslewsec + " at " + unitspersecond);

                double dist = Math.Sqrt((lookat.X - gotopos.X) * (lookat.X - gotopos.X) + (lookat.Y - gotopos.Y) * (lookat.Y - gotopos.Y) + (lookat.Z - gotopos.Z) * (lookat.Z - gotopos.Z));
                Debug.Assert(!double.IsNaN(dist));      // had a bug due to incorrect signs!

                if (dist >= 1)
                {
                    Vector3d eyeoffset = eyeposition - lookat;

                    if (timeslewsec == 0)
                    {
                        lookat = gotopos;
                        eyeposition = gotopos + eyeoffset;
                        //System.Diagnostics.Debug.WriteLine("{0} Immediate Slew to {1}", Environment.TickCount % 10000, targetposSlewTarget);
                    }
                    else
                    {
                        targetposSlewTarget = gotopos;
                        targetposSlewProgress = 0.0f;
                        targetposSlewTime = (timeslewsec < 0) ? ((double)Math.Max(1.0, dist / unitspersecond)) : timeslewsec;            //10000 ly/sec, with a minimum slew
                        //System.Diagnostics.Debug.WriteLine($"{Environment.TickCount % 10000} Slew start to {gotopos} {targetposSlewTime}  eye {eyeposition} look {lookat} dir {CameraDirection} rot {CameraRotation}");
                    }
                }
            }
        }

        /// <summary> Go to and zoom in. Timeslewsec is 0 for immediate, less than 0 for automatic calc, else seconds. unitspersecond determines speed for automatic</summary>
        public void GoToZoom(Vector3d gotopos, double zoom, double timeslewsec = 0, double unitspersecond = 10000F)       // may pass a Nan Position - no action. Y is normal sense
        {
            GoTo(gotopos, timeslewsec, unitspersecond);
            GoToZoom(zoom, Math.Max(targetposSlewTime, 1));
        }

        /// <summary> Go to and zoom in and pan. Timeslewsec is 0 for immediate, less than 0 for automatic calc, else seconds. unitspersecond determines speed for automatic</summary>
        public void GoToZoomPan(Vector3d gotopos, Vector2d cameradir, double zoom, double timeslewsec = 0, double unitspersecond = 10000F)       // may pass a Nan Position - no action. Y is normal sense
        {
            GoTo(gotopos, timeslewsec, unitspersecond);
            double time = Math.Max(targetposSlewTime, 1);
            GoToZoom(zoom, time);
            Pan(cameradir, time);
        }

        #endregion

        #region Camera

        /// <summary> 
        /// Camera position in degrees
        /// camera.x rotates around X, counterclockwise, = 0 (up), 90 = (forward), 180 (down)
        /// camera.y rotates around Y, counterclockwise, = 0 (forward), 90 = (to left), 180 (back), -90 (to right)
        /// </summary>
        public Vector2d CameraDirection { get { return cameradir; } set { KillSlew(); cameradir = value; SetLookatPositionFromEye(value, EyeDistance); } }

        /// <summary> Camera Rotation around its axis</summary>
        public double CameraRotation { get { return camerarot; } set { KillSlew(); camerarot = value; } }       // rotation around Z

        /// <summary> Rotate camera by an amount.  If changelookat = true, we move the lookat, else we move the eye</summary>
        public bool RotateCamera(Vector2d addazel, double addzrot, bool changelookat)
        {
            KillSlew();
            //System.Diagnostics.Debug.WriteLine($"{Environment.TickCount % 1000} Rotate camera {addazel} z {addzrot} look {lookat} eye {eyeposition} camera dir {cameradir}");

            System.Diagnostics.Debug.Assert(!double.IsNaN(addazel.X) && !double.IsNaN(addazel.Y));
            Vector2d newdir = CameraDirection;

            newdir.X = newdir.X.AddBoundedAngle(addazel.X);
            newdir.Y = newdir.Y.AddBoundedAngle(addazel.Y);

            if (newdir.X < 0 && newdir.X > -90)                   // Limit camera pitch
                newdir.X = 0;
            if (newdir.X > 180 || newdir.X <= -90)
                newdir.X = 180;

            if (newdir.X == 0 || newdir.X == 180)                // we can't rotate camera at these positions, reject
                return false;

            camerarot = camerarot.AddBoundedAngle(addzrot);

            if (changelookat)
                SetLookatPositionFromEye(newdir, EyeDistance);
            else
                SetEyePositionFromLookat(newdir, EyeDistance);

            return true;
            // System.Diagnostics.Debug.WriteLine("{0} Camera moved to {1} Eye {2} Zoom Fact {3} Eye dist {4}", Environment.TickCount % 10000, lookat, eyeposition, ZoomFactor, EyeDistance);
        }

        /// <summary> Pan to camera position, time = 0 immediate, less than 0 estimate, else time to slew in seconds</summary>
        public void Pan(Vector2d newcamerapos, double timeslewsec = 0)
        {
            if (timeslewsec == 0)
            {
                SetLookatPositionFromEye(newcamerapos, EyeDistance);
            }
            else
            {
                if (timeslewsec < 0)       // auto estimate on log distance between them
                {
                    Vector2d diff = newcamerapos - cameradir;
                    timeslewsec = (double)(diff.Length / 60);
                    // System.Diagnostics.Debug.WriteLine($"camera diff {diff} {timeslewsec}");
                }

                cameraDirSlewStart = CameraDirection;
                cameraDirSlewTarget = newcamerapos;
                cameraDirSlewProgress = 0.0f;
                cameraDirSlewTime = (timeslewsec == 0) ? (1.0F) : timeslewsec;
            }
        }

        /// <summary> Pan to target, time = 0 immediate, else time to slew</summary>
        public void PanTo(Vector3d target, double timeslewsec = 0)
        {
            Vector2d camera = EyePosition.AzEl(target, true);
            Pan(camera, timeslewsec);
        }

        /// <summary> Pan to and zoom to target, time = 0 immediate, else time to slew in seconds</summary>
        public void PanZoomTo(Vector3d target, double zoom, double timeslewsec = 0)
        {
            Vector2d camera = EyePosition.AzEl(target, true);
            Pan(camera, timeslewsec);
            GoToZoom(zoom, timeslewsec);
        }

        #endregion

        #region Zoom

        /// <summary> Zoom factor. At zoom factor 1, distance is Zoom1Distance </summary>
        public double ZoomFactor { get { return Zoom1Distance / EyeDistance; } set { KillSlew(); Zoom(value); } }
        /// <summary> Distance at zoom 1</summary>
        public double Zoom1Distance { get; set; } = 1000F;                     
        /// <summary> Maximum zoom </summary>
        public double ZoomMax = 300F;           
        /// <summary> Minimum zoom </summary>
        public double ZoomMin = 0.01F;           
        /// <summary> Scaling for using zoom direction scaling</summary>
        public double ZoomScaling = 1.258925F;

        /// <summary> Scale zoom in a direction (true = increase) </summary>
        public void ZoomScale(bool direction)
        {
            KillSlew();
            double newzoomfactor = ZoomFactor;
            if (direction)
            {
                newzoomfactor *= (double)ZoomScaling;
                if (newzoomfactor > ZoomMax)
                    newzoomfactor = (double)ZoomMax;
            }
            else
            {
                newzoomfactor /= (double)ZoomScaling;
                if (newzoomfactor < ZoomMin)
                    newzoomfactor = (double)ZoomMin;
            }

            Zoom(newzoomfactor);
        }

        /// <summary> Goto to zoom. Time = 0 immediate, less than 0 estimate, else seconds </summary>
        public void GoToZoom(double z, double timetozoom = 0)        // <0 means auto estimate
        {
            z = Math.Max(Math.Min(z, ZoomMax), ZoomMin);

            if (timetozoom == 0)
            {
                Zoom(z);
            }
            else if (Math.Abs(z - ZoomFactor) > 0.01)
            {
                zoomSlewTarget = z;
                zoomSlewStart = ZoomFactor;

                if (timetozoom < 0)       // auto estimate on log distance between them
                {
                    var log = Math.Log10(zoomSlewTarget / ZoomFactor);
                    timetozoom = (double)(Math.Abs(log) * 0.75);
                }

                zoomSlewTime = timetozoom;
                zoomSlewProgress = 0;
                System.Diagnostics.Debug.WriteLine($"gotozoom to {zoomSlewTarget} from {ZoomFactor} {zoomSlewTime}");
            }
        }

        /// <summary> Goto Zoom </summary>
        public void Zoom(double newzoomfactor)
        {
            newzoomfactor = Math.Max(Math.Min(newzoomfactor, ZoomMax), ZoomMin);
            SetEyePositionFromLookat(CameraDirection, Zoom1Distance / newzoomfactor);
        }

        #endregion

        #region More Position functions

        /// <summary> Return a string representation of lookat, eyeposition, camera rotation </summary>
        public string StringPositionCamera { get { return $"{lookat.X},{lookat.Y},{lookat.Z},{eyeposition.X},{eyeposition.Y},{eyeposition.Z},{camerarot}"; } }

        /// <summary> Set camera lookat, eyeposition, camera rotation from string </summary>
        public bool SetPositionCamera(string s)     
        {
            string[] sparts = s.Split(',');
            if (sparts.Length == 7)
            {
                double[] dparts = sparts.Select(x => x.InvariantParseDouble(0)).ToArray();
                SetPositionCamera(new Vector3d(dparts[0], dparts[1], dparts[2]), new Vector3d(dparts[3], dparts[4], dparts[5]), dparts[6]);
                return true;
            }
            else
                return false;
        }

        /// <summary> Set camera by lookat, eyeposition, and camera rotation</summary>
        public void SetPositionCamera(Vector3d lookp, Vector3d eyeposp, double camerarotp = 0)     
        {
            lookat = lookp;
            eyeposition = eyeposp;
            camerarot = camerarotp;
            cameradir = eyeposition.AzEl(lookat, true);
        }

        /// <summary> Set camera by lookat, camera direction, distance and camera rotation</summary>
        public void SetPositionDistance(Vector3d lookp, Vector2d cameradirdegreesp, double distance, double camerarotp = 0)    
        {
            lookat = lookp;
            cameradir = cameradirdegreesp;
            camerarot = camerarotp;
            SetEyePositionFromLookat(cameradir, distance);
        }

        /// <summary> Set camera by lookat, camera direction, zoom and camera rotation</summary>
        public void SetPositionZoom(Vector3d lookp, Vector2d cameradirdegreesp, double zoom, double camerarotp = 0)    
        {
            lookat = lookp;
            cameradir = cameradirdegreesp;
            camerarot = camerarotp;
            SetEyePositionFromLookat(cameradir, Zoom1Distance / zoom);
        }

        /// <summary> Set eye position from lookat given camera direction and distance</summary>
        public void SetEyePositionFromLookat(Vector2d cameradirdegreesp, double distance)          
        {
            eyeposition = lookat.CalculateEyePositionFromLookat(cameradirdegreesp, distance);
            cameradir = cameradirdegreesp;
        }

        /// <summary> Set lookat position from eye given camera direction and distance </summary>
        public void SetLookatPositionFromEye(Vector2d cameradirdegreesp, double distance)           
        {
            lookat = eyeposition.CalculateLookatPositionFromEye(cameradirdegreesp, distance);
            //  System.Diagnostics.Debug.WriteLine($"setlookat {cameradirdegreesp} distance {distance} resulting distance {(lookat - eyeposition).Length}");
            cameradir = cameradirdegreesp;
        }


        #endregion

        #region Slew

        /// <summary> Are we slewing? </summary>
        public bool InSlew { get { return (targetposSlewProgress < 1.0f || zoomSlewTarget > 0 || cameraDirSlewProgress < 1.0f); } }

        /// <summary> Kill all slews </summary>
        public void KillSlew()
        {
            if (targetposSlewProgress < 1)
            {
                //System.Diagnostics.Debug.WriteLine($"Kill target pos slew at {targetposSlewProgress}");
                targetposSlewProgress = 1.0f;
            }
            zoomSlewProgress = 1.0f;
            cameraDirSlewProgress = 1.0f;
        }

        /// <summary> Execute slews. </summary>
        public void DoSlew(int msticks)
        {
            if (targetposSlewProgress < 1.0f)
            {
                Debug.Assert(targetposSlewTime > 0);
                var newprogress = targetposSlewProgress + msticks / (targetposSlewTime * 1000);

                if (newprogress >= 1.0f)        // limit
                    newprogress = 1.0f;

                var slewstart = Math.Sin((targetposSlewProgress - 0.5) * Math.PI);
                var slewend = Math.Sin((newprogress - 0.5) * Math.PI);
                Debug.Assert((1 - 0 - slewstart) != 0);
                var slewfact = (slewend - slewstart) / (1.0 - slewstart);

                var totvector = new Vector3d((double)(targetposSlewTarget.X - lookat.X), (double)(targetposSlewTarget.Y - lookat.Y), (double)(targetposSlewTarget.Z - lookat.Z));

                var move = Vector3d.Multiply(totvector, (double)slewfact);
                lookat += move;
                eyeposition += move;

                if (newprogress >= 1.0f)
                {
                    //System.Diagnostics.Debug.WriteLine($"{Environment.TickCount%1000} Slew complete at {lookat} {eyeposition}");
                }
                else
                {
                    //System.Diagnostics.Debug.WriteLine($"{Environment.TickCount % 1000} Slew to {lookat} eye {eyeposition} dist {EyeDistance} prog {newprogress}");
                }

                targetposSlewProgress = (double)newprogress;
            }

            if (zoomSlewProgress < 1.0f)
            {
                var newprogress = zoomSlewProgress + msticks / (zoomSlewTime * 1000);

                if (newprogress >= 1.0f)
                {
                    SetEyePositionFromLookat(CameraDirection, Zoom1Distance / zoomSlewTarget);
                    System.Diagnostics.Debug.WriteLine($"{Environment.TickCount % 10000} Zoom {zoomSlewTarget} over {ZoomFactor}");
                }
                else
                {
                    double newzoom = zoomSlewStart + (zoomSlewTarget - zoomSlewStart) * newprogress;
                    System.Diagnostics.Debug.WriteLine($"{Environment.TickCount % 10000} Zoom {newprogress:N2} targ {zoomSlewTarget} zoomfactor {ZoomFactor} -> set new {newzoom}");
                    SetEyePositionFromLookat(CameraDirection, Zoom1Distance / newzoom);
                }

                zoomSlewProgress = newprogress;
            }

            if (cameraDirSlewProgress < 1.0f)
            {
                var newprogress = cameraDirSlewProgress + msticks / (cameraDirSlewTime * 1000);

                if (newprogress >= 1.0f)
                {
                    SetLookatPositionFromEye(cameraDirSlewTarget, EyeDistance);
                    //      System.Diagnostics.Debug.WriteLine($"Slew stop at {cameraDirSlewTarget}");
                }
                else
                {
                    Vector2d newpos = new Vector2d(cameraDirSlewStart.X + (cameraDirSlewTarget.X - cameraDirSlewStart.X) * newprogress,
                                             cameraDirSlewStart.Y + (cameraDirSlewTarget.Y - cameraDirSlewStart.Y) * newprogress);
                    SetLookatPositionFromEye(newpos, EyeDistance);
                    //       System.Diagnostics.Debug.WriteLine($"Slewing {cameraDirSlewProgress} to {newpos}");
                }
                cameraDirSlewProgress = newprogress;
            }
        }

        #endregion

        #region Different tracker

        private Vector3d lastlookat;
        private Vector3d lasteyepos;
        private double lastcamerarotation;

        /// <summary> Reset difference tracker</summary>
        public void ResetDifferenceTracker()
        {
            lasteyepos = EyePosition;
            lastlookat = Vector3d.Zero;
            lastcamerarotation = 0;
        }

        /// <summary> Has distance tracker detected movement </summary>
        public bool IsMoved(double minmovement = 0.1f, double cameramove = 1.0f)
        {
            bool moved = Vector3d.Subtract(lastlookat, LookAt).Length >= minmovement;
            if (moved)
                lastlookat = LookAt;
            bool eyemoved = Vector3d.Subtract(lasteyepos, EyePosition).Length >= minmovement;
            if (eyemoved)
                lasteyepos = EyePosition;
            bool rotated = Math.Abs(CameraRotation - lastcamerarotation) >= cameramove;
            if (rotated)
                lastcamerarotation = CameraRotation;
            return moved | eyemoved | rotated;
        }

        #endregion


        #region Privates

        private Vector3d lookat = Vector3d.Zero;                // point where we are viewing.
        private Vector3d eyeposition = new Vector3d(10, 10, 10);  // and the eye position
        private Vector2d cameradir = Vector2d.Zero;               // camera dir, kept in track
        private double camerarot = 0;                            // and rotation

        private Vector3d targetposSlewTarget;                    // where to slew to.
        private double targetposSlewProgress = 1.0f;             // 0 -> 1 slew progress
        private double targetposSlewTime;                        // how long to take to do the slew

        private double zoomSlewTarget = 0;
        private double zoomSlewStart = 0;
        private double zoomSlewProgress = 1.0f;
        private double zoomSlewTime = 0;

        private Vector2d cameraDirSlewTarget;                    // where to slew to.
        private Vector2d cameraDirSlewStart;                     // where it started
        private double cameraDirSlewProgress = 1.0f;             // 0 -> 1 slew progress
        private double cameraDirSlewTime;                        // how long to take to do the slew

        #endregion
    }
}

