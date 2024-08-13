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
using System.Globalization;
using System.Linq;

namespace GLOFC.Controller
{
    /// <summary>
    /// Holds position (lookat, eye) and camera direction
    /// </summary>
    
    public class PositionCamera       // holds lookat and eyepositions and camera
    {
        #region Positions

        /// <summary> Lookat position </summary>
        public Vector3 LookAt { get { return lookat; }}
        /// <summary> Eye position </summary>
        public Vector3 EyePosition { get { return eyeposition; } }

        /// <summary> Eye distance </summary>
        public float EyeDistance { get { return (lookat - EyePosition).Length; } }

        /// <summary> Translate eye and lookout </summary>
        public void Translate(Vector3 pos, bool killslew = true)
        {
            if (killslew) KillSlew(); lookat += pos; eyeposition += pos;
        }

        /// <summary> Move look at to value, with optional kill slew </summary>
        public void MoveLookAt(Vector3 value, bool killslew = true)
        {
            if (killslew) KillSlew(); var eyeoffset = eyeposition - lookat; lookat = value; eyeposition = lookat + eyeoffset;
        }

        /// <summary> Slew to lookat position. Timeslewsec is 0 for immediate, less than 0 for automatic calc, else seconds. unitspersecond determines speed for automatic </summary>
        public void GoTo(Vector3 gotopos, float timeslewsec = 0, float unitspersecond = 10000F)       // may pass a Nan Position - no action. Y is normal sense
        {
            if (!float.IsNaN(gotopos.X))
            {
                //System.Diagnostics.Debug.WriteLine("Goto " + gotopos + " in " + timeslewsec + " at " + unitspersecond);

                double dist = Math.Sqrt((lookat.X - gotopos.X) * (lookat.X - gotopos.X) + (lookat.Y - gotopos.Y) * (lookat.Y - gotopos.Y) + (lookat.Z - gotopos.Z) * (lookat.Z - gotopos.Z));
                Debug.Assert(!double.IsNaN(dist));      // had a bug due to incorrect signs!

                if (dist >= 1)
                {
                    Vector3 eyeoffset = eyeposition - lookat;

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
                        targetposSlewTime = (timeslewsec < 0) ? ((float)Math.Max(1.0, dist / unitspersecond)) : timeslewsec;            //10000 ly/sec, with a minimum slew
                        //System.Diagnostics.Debug.WriteLine($"{Environment.TickCount % 10000} Slew start to {gotopos} {targetposSlewTime}  eye {eyeposition} look {lookat} dir {CameraDirection} rot {CameraRotation}");
                    }
                }
            }
        }

        /// <summary> Go to and zoom in. Timeslewsec is 0 for immediate, less than 0 for automatic calc, else seconds. unitspersecond determines speed for automatic</summary>
        public void GoToZoom(Vector3 gotopos, float zoom, float timeslewsec = 0, float unitspersecond = 10000F)       // may pass a Nan Position - no action. Y is normal sense
        {
            GoTo(gotopos, timeslewsec, unitspersecond);
            GoToZoom(zoom, Math.Max(targetposSlewTime,1));
        }

        /// <summary> Go to and zoom in and pan. Timeslewsec is 0 for immediate, less than 0 for automatic calc, else seconds. unitspersecond determines speed for automatic</summary>
        public void GoToZoomPan(Vector3 gotopos, Vector2 cameradir, float zoom, float timeslewsec = 0, float unitspersecond = 10000F)       // may pass a Nan Position - no action. Y is normal sense
        {
            GoTo(gotopos, timeslewsec, unitspersecond);
            float time = Math.Max(targetposSlewTime, 1);
            GoToZoom(zoom, time);
            Pan(cameradir, time);
        }

        #endregion

        #region Camera

        /// <summary> 
        /// Camera position in degrees
        /// * camera.x rotates around the x axis (elevation) and camera.y rotates around the y axis (aziumth)
        /// * Elevation: 0 is straignt up, 180 is straight down, 90 is level
        /// * Azimuth in opengl +Z towards the viewer: 180 is straight forward, 0 is straight back
        /// * Azimuth in +Z away from the viewer: 0 is straight forward, 180 is straight back
        /// </summary>
        public Vector2 CameraDirection { get { return cameradir; } set { KillSlew(); cameradir = value; SetLookatPositionFromEye(value, EyeDistance); } }

        /// <summary> Camera Rotation around its axis</summary>
        public float CameraRotation { get { return camerarot; } set { KillSlew(); camerarot = value; } }       // rotation around Z

        /// <summary> Rotate camera by an amount.  If changelookat = true, we move the lookat, else we move the eye</summary>
        public bool RotateCamera(Vector2 addazel, float addzrot, bool changelookat)
        {
            KillSlew();
            //System.Diagnostics.Debug.WriteLine($"{Environment.TickCount % 1000} Rotate camera {addazel} z {addzrot} look {lookat} eye {eyeposition} camera dir {cameradir}");

            System.Diagnostics.Debug.Assert(!float.IsNaN(addazel.X) && !float.IsNaN(addazel.Y));
            Vector2 newdir = CameraDirection;

            newdir.X = newdir.X.AddBoundedAngle(addazel.X);
            newdir.Y = newdir.Y.AddBoundedAngle(addazel.Y);

            if (newdir.X < 0 && newdir.X > -90)                   // Limit camera pitch
                newdir.X = 0;
            if (newdir.X > 180 || newdir.X <= -90)
                newdir.X = 180;

            if (newdir.X == 0 || newdir.X == 180)                // we can't rotate camera at these positions, reject
                return false;

            camerarot = camerarot.AddBoundedAngle(addzrot);
            
           // System.Diagnostics.Debug.WriteLine($".. Rotate {CameraDirection} -> {newdir} {camerarot}");

            if (changelookat)
                SetLookatPositionFromEye(newdir, EyeDistance);
            else
                SetEyePositionFromLookat(newdir, EyeDistance);

            return true;
           // System.Diagnostics.Debug.WriteLine("{0} Camera moved to {1} Eye {2} Zoom Fact {3} Eye dist {4}", Environment.TickCount % 10000, lookat, eyeposition, ZoomFactor, EyeDistance);
        }

        /// <summary> Pan to camera position, time = 0 immediate, less than 0 estimate, else time to slew in seconds</summary>
        public void Pan(Vector2 newcamerapos, float timeslewsec = 0) 
        {
            if (timeslewsec == 0)
            {
                SetLookatPositionFromEye(newcamerapos, EyeDistance);
            }
            else
            {
                if (timeslewsec < 0)       // auto estimate on log distance between them
                {
                    Vector2 diff = newcamerapos - cameradir;
                    timeslewsec = (float)(diff.Length / 60);
                   // System.Diagnostics.Debug.WriteLine($"camera diff {diff} {timeslewsec}");
                }

                cameraDirSlewStart = CameraDirection;
                cameraDirSlewTarget = newcamerapos;
                cameraDirSlewProgress = 0.0f;
                cameraDirSlewTime = (timeslewsec == 0) ? (1.0F) : timeslewsec;
            }
        }

        /// <summary> Pan to target, time = 0 immediate, else time to slew in seconds</summary>
        public void PanTo(Vector3 target, float timeslewsec = 0)            
        {
            Vector2 camera = EyePosition.AzEl(target, true);
            Pan(camera, timeslewsec);
        }

        /// <summary> Pan to and zoom to target, time = 0 immediate, else time to slew in seconds</summary>
        public void PanZoomTo(Vector3 target, float zoom, float timeslewsec = 0) 
        {
            Vector2 camera = EyePosition.AzEl(target, true);
            Pan(camera, timeslewsec);
            GoToZoom(zoom, timeslewsec);
        }

        #endregion

        #region Zoom

        /// <summary> Zoom factor. At zoom factor 1, distance is Zoom1Distance </summary>
        public float ZoomFactor { get { return Zoom1Distance / EyeDistance; } set { KillSlew(); Zoom(value); } }
        /// <summary> Distance at zoom 1</summary>
        public float Zoom1Distance { get; set; } = 1000F;     
        /// <summary> Maximum zoom </summary>
        public float ZoomMax = 300F;            
        /// <summary> Minimum zoom </summary>
        public float ZoomMin = 0.01F;           
        /// <summary> Scaling for using zoom direction scaling</summary>
        public float ZoomScaling = 1.258925F;

        /// <summary> Scale zoom in a direction (true = increase) </summary>
        public void ZoomScale(bool direction)
        {
            KillSlew();
            float newzoomfactor = ZoomFactor;
            if (direction)
            {
                newzoomfactor *= (float)ZoomScaling;
                if (newzoomfactor > ZoomMax)
                    newzoomfactor = (float)ZoomMax;
            }
            else
            {
                newzoomfactor /= (float)ZoomScaling;
                if (newzoomfactor < ZoomMin)
                    newzoomfactor = (float)ZoomMin;
            }

            Zoom(newzoomfactor);
        }

        /// <summary> Goto to zoom. Time = 0 immediate, less than 0 estimate, else seconds </summary>
        public void GoToZoom(float z, float timetozoom = 0)        // <0 means auto estimate
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
                    timetozoom = (float)(Math.Abs(log) * 0.75);
                }

                zoomSlewTime = timetozoom;
                zoomSlewProgress = 0;
                //System.Diagnostics.Debug.WriteLine($"gotozoom to {zoomSlewTarget} from {ZoomFactor} {zoomSlewTime}");
            }
        }

        /// <summary> Goto Zoom </summary>
        public void Zoom(float newzoomfactor)
        {
            newzoomfactor = Math.Max(Math.Min(newzoomfactor, ZoomMax), ZoomMin);
            SetEyePositionFromLookat(CameraDirection, Zoom1Distance / newzoomfactor);
        }

        #endregion

        #region More Position functions

        /// <summary> Return a string representation of lookat, eyeposition, camera rotation. Invariant culture </summary>
        public string StringPositionCamera { get { return string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5},{6}", lookat.X, lookat.Y, lookat.Z, eyeposition.X, eyeposition.Y, eyeposition.Z, camerarot); } }

        /// <summary> Set camera lookat, eyeposition, camera rotation from invariant culture string </summary>
        public bool SetPositionCamera(string s)     // from StringPositionCamera
        {
            string[] sparts = s.Split(',');
            if (sparts.Length == 7)
            {
                float[] dparts = sparts.Select(x => x.InvariantParseFloat(0)).ToArray();
                SetPositionCamera(new Vector3(dparts[0], dparts[1], dparts[2]), new Vector3(dparts[3], dparts[4], dparts[5]), dparts[6]);
                return true;
            }
            else
                return false;
        }

        /// <summary> Set camera by lookat, eyeposition, and camera rotation</summary>
        public void SetPositionCamera(Vector3 lookp, Vector3 eyeposp, float camerarotp = 0)    
        {
            lookat = lookp;
            eyeposition = eyeposp;
            camerarot = camerarotp;
            cameradir = eyeposition.AzEl(lookat, true);
        }

        /// <summary> Set camera by lookat, camera direction, distance and camera rotation</summary>
        public void SetPositionDistance(Vector3 lookp, Vector2 cameradirdegreesp, float distance, float camerarotp = 0)
        {
            lookat = lookp;
            cameradir = cameradirdegreesp;
            camerarot = camerarotp;
            SetEyePositionFromLookat(cameradir, distance);
        }

        /// <summary> Set camera by lookat, camera direction, zoom and camera rotation</summary>
        public void SetPositionZoom(Vector3 lookp, Vector2 cameradirdegreesp, float zoom, float camerarotp = 0)
        {
            lookat = lookp;
            cameradir = cameradirdegreesp;
            camerarot = camerarotp;
            SetEyePositionFromLookat(cameradir, Zoom1Distance / zoom);
        }

        /// <summary> Set eye position from lookat given camera direction and distance</summary>
        /// <summary> </summary>
        public void SetEyePositionFromLookat(Vector2 cameradirdegreesp, float distance)             
        {
            eyeposition = lookat.CalculateEyePositionFromLookat(cameradirdegreesp, distance);
            cameradir = cameradirdegreesp;
        }

        /// <summary> Set lookat position from eye given camera direction and distance </summary>
        public void SetLookatPositionFromEye(Vector2 cameradirdegreesp, float distance)             
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

                var totvector = new Vector3((float)(targetposSlewTarget.X - lookat.X), (float)(targetposSlewTarget.Y - lookat.Y), (float)(targetposSlewTarget.Z - lookat.Z));

                var move = Vector3.Multiply(totvector, (float)slewfact);
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

                targetposSlewProgress = (float)newprogress;
            }

            if ( zoomSlewProgress < 1.0f )
            {
                var newprogress = zoomSlewProgress + msticks / (zoomSlewTime * 1000);

                if ( newprogress >= 1.0f)
                {
                    SetEyePositionFromLookat(CameraDirection, Zoom1Distance / zoomSlewTarget);
                    //System.Diagnostics.Debug.WriteLine($"{Environment.TickCount % 10000} Zoom {zoomSlewTarget} over {ZoomFactor}");
                }
                else
                {
                    float newzoom = zoomSlewStart + (zoomSlewTarget - zoomSlewStart) * newprogress;
                   // System.Diagnostics.Debug.WriteLine($"{Environment.TickCount % 10000} Zoom {zoomSlewTarget} zoomfactor {ZoomFactor} -> set new {newzoom}");
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
                    Vector2 newpos = new Vector2(cameraDirSlewStart.X + (cameraDirSlewTarget.X - cameraDirSlewStart.X) * newprogress,
                                             cameraDirSlewStart.Y + (cameraDirSlewTarget.Y - cameraDirSlewStart.Y) * newprogress);
                    SetLookatPositionFromEye(newpos, EyeDistance);
             //       System.Diagnostics.Debug.WriteLine($"Slewing {cameraDirSlewProgress} to {newpos}");
                 }
                 cameraDirSlewProgress = newprogress;
            }
        }

        #endregion

        #region Different tracker

        private Vector3 lastlookat;
        private Vector3 lasteyepos;
        private float lastcamerarotation;

        /// <summary> Reset difference tracker</summary>
        public void ResetDifferenceTracker()
        {
            lasteyepos = EyePosition;
            lastlookat = Vector3.Zero;
            lastcamerarotation = 0;
        }

        /// <summary> Has distance tracker detected movement </summary>
        public bool IsMoved(float minmovement = 0.1f, float cameramove = 1.0f)
        {
            bool moved = Vector3.Subtract(lastlookat, LookAt).Length >= minmovement;
            if (moved)
                lastlookat = LookAt;
            bool eyemoved = Vector3.Subtract(lasteyepos, EyePosition).Length >= minmovement;
            if (eyemoved)
                lasteyepos = EyePosition;
            bool rotated = Math.Abs(CameraRotation - lastcamerarotation) >= cameramove;
            if (rotated)
                lastcamerarotation = CameraRotation;
            return moved | eyemoved | rotated;
        }

        #endregion

        #region Privates

        private Vector3 lookat = Vector3.Zero;                // point where we are viewing. 
        private Vector3 eyeposition = new Vector3(10, 10, 10);  // and the eye position
        private Vector2 cameradir = Vector2.Zero;               // camera dir, kept in track
        private float camerarot = 0;                            // and rotation

        private Vector3 targetposSlewTarget;                    // where to slew to.
        private float targetposSlewProgress = 1.0f;             // 0 -> 1 slew progress
        private float targetposSlewTime;                        // how long to take to do the slew

        private float zoomSlewTarget = 0;
        private float zoomSlewStart = 0;
        private float zoomSlewProgress = 1.0f;
        private float zoomSlewTime = 0;

        private Vector2 cameraDirSlewTarget;                    // where to slew to.
        private Vector2 cameraDirSlewStart;                     // where it started
        private float cameraDirSlewProgress = 1.0f;             // 0 -> 1 slew progress
        private float cameraDirSlewTime;                        // how long to take to do the slew

        #endregion
    }
}

