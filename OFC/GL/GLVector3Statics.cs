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

using GLOFC.Utils;
using OpenTK;
using System;

namespace GLOFC
{
    /// <summary>
    /// Static helper functions for Vector3
    /// </summary>

    public static class GLStaticsVector3
    {
        /// <summary> Floor the components</summary>
        static public Vector3 Floor(this Vector3 v)
        {
            return new Vector3((float)Math.Floor(v.X), (float)Math.Floor(v.Y), (float)Math.Floor(v.Z));
        }

        /// <summary> Fract the components </summary>
        static public Vector3 Fract(this Vector3 v)
        {
            return new Vector3((float)(v.X - Math.Floor(v.X)), (float)(v.Y - Math.Floor(v.Y)), (float)(v.Z - Math.Floor(v.Z)));
        }

        /// <summary> Floor and Fract the components </summary>
        static public Vector3 FloorFract(this Vector3 v, out Vector3 fract) // floor and fract
        {
            float fx = (float)Math.Floor(v.X);
            float fy = (float)Math.Floor(v.Y);
            float fz = (float)Math.Floor(v.Z);
            fract = new Vector3(v.X - fx, v.Y - fy, v.Z - fz);
            return new Vector3(fx, fy, fz);
        }

        /// <summary> Find position between two vectors with a definable position</summary>
        public static Vector3 Mix(Vector3 a, Vector3 b, float mix)
        {
            float x = (float)(a.X + (b.X - a.X) * mix);
            float y = (float)(a.Y - (b.Y - a.Y) * mix);
            float z = (float)(a.Z - (b.Z - a.Z) * mix);
            return new Vector3(x, y, z);
        }

        /// <summary> Absolute the components</summary>
        static public Vector3 Abs(this Vector3 v)
        {
            return new Vector3(Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z));
        }


        /// <summary> AZel between curpos and target </summary>
        public static Vector2 AzEl(this Vector3 curpos, Vector3 target, bool returndegrees)     // az and elevation between curpos and target
        {
            Vector3 delta = Vector3.Subtract(target, curpos);
            //Console.WriteLine("{0}->{1} d {2}", curpos, target, delta);

            float radius = delta.Length;

            if (radius < 0.0000001)
                return new Vector2(180, 0);     // point forward, level

            float inclination = (float)Math.Acos(delta.Y / radius);

            float azimuth = (float)(delta.X==0 ? (delta.Z>=0 ? Math.PI/2 : -Math.PI/2) : Math.Atan(delta.Z / delta.X));

            System.Diagnostics.Debug.Assert(!float.IsNaN(inclination) && !float.IsNaN(azimuth));

            if (delta.X >= 0)      // atan wraps -90 (south)->+90 (north), then -90 to +90 around the y axis, going anticlockwise
                azimuth = (float)(Math.PI / 2) - azimuth;     // adjust
            else
                azimuth = -(float)(Math.PI / 2) - azimuth;

            if (returndegrees)
            {
                inclination = inclination.Degrees();
                azimuth = azimuth.Degrees();
            }


            //System.Diagnostics.Debug.WriteLine("inc " + inclination + " az " + azimuth + " delta" + delta);

            //System.Diagnostics.Debug.WriteLine(" -> inc " + inclination + " az " + azimuth);
            return new Vector2(inclination, azimuth);
        }

        /// <summary>AZel between curpos and target </summary>
        public static Vector2d AzEl(this Vector3d curpos, Vector3d target, bool returndegrees)     // az and elevation between curpos and target
        {
            Vector3d delta = Vector3d.Subtract(target, curpos);
            //Console.WriteLine("{0}->{1} d {2}", curpos, target, delta);

            double radius = delta.Length;

            if (radius < 0.0000001)
                return new Vector2d(180, 0);     // point forward, level

            double inclination = (double)Math.Acos(delta.Y / radius);

            double azimuth = (double)(delta.X == 0 ? (delta.Z >= 0 ? Math.PI / 2 : -Math.PI / 2) : Math.Atan(delta.Z / delta.X));

            System.Diagnostics.Debug.Assert(!double.IsNaN(inclination) && !double.IsNaN(azimuth));

            if (delta.X >= 0)      // atan wraps -90 (south)->+90 (north), then -90 to +90 around the y axis, going anticlockwise
                azimuth = (double)(Math.PI / 2) - azimuth;     // adjust
            else
                azimuth = -(double)(Math.PI / 2) - azimuth;

            if (returndegrees)
            {
                inclination = inclination.Degrees();
                azimuth = azimuth.Degrees();
            }


            //System.Diagnostics.Debug.WriteLine("inc " + inclination + " az " + azimuth + " delta" + delta);

            //System.Diagnostics.Debug.WriteLine(" -> inc " + inclination + " az " + azimuth);
            return new Vector2d(inclination, azimuth);
        }

        /// <summary> Translate to vector 4 </summary>
        public static Vector4 ToVector4(this Vector3 v, float w = 0)
        {
            return new Vector4(v.X, v.Y, v.Z,w);
        }

        private static Vector3 cameravector { get; set; } = new Vector3(0, 1, 0);        // camera vector, at CameraDir(0,0)

        /// <summary> Calculate look position from eye given distance </summary>
        public static Vector3 CalculateLookatPositionFromEye(this Vector3 eyeposition, Vector2 cameradirdegreesp, float distance)
        {
            Matrix3 transform = Matrix3.Identity;                   // identity nominal matrix, dir is in degrees

            transform *= Matrix3.CreateRotationX((float)(cameradirdegreesp.X * Math.PI / 180.0f));      // we rotate the camera vector around X and Y to get a vector which points from eyepos to lookat pos
            transform *= Matrix3.CreateRotationY((float)(cameradirdegreesp.Y * Math.PI / 180.0f));

            Vector3 eyerel = Vector3.Transform(cameravector, transform);
            return eyeposition + eyerel * distance;
        }

        /// <summary> Calculate look position from eye given distance </summary>
        public static Vector3d CalculateLookatPositionFromEye(this Vector3d eyeposition, Vector2d cameradirdegreesp, double distance)
        {
            Matrix4d transform = Matrix4d.Identity;                   // identity nominal matrix, dir is in degrees

            transform *= Matrix4d.CreateRotationX((cameradirdegreesp.X * Math.PI / 180.0f));      // we rotate the camera vector around X and Y to get a vector which points from eyepos to lookat pos
            transform *= Matrix4d.CreateRotationY((cameradirdegreesp.Y * Math.PI / 180.0f));

            Vector3d eyerel = Vector3d.Transform(new Vector3d(0, 1, 0), transform);
            return eyeposition + eyerel * distance;
        }

        /// <summary> From current lookat, calculate eyeposition, given a camera angle and a distance</summary>
        public static Vector3 CalculateEyePositionFromLookat(this Vector3 lookat, Vector2 cameradirdegreesp, float distance)
        {
            Matrix3 transform = Matrix3.Identity;                   // identity nominal matrix, dir is in degrees

            transform *= Matrix3.CreateRotationX((float)(cameradirdegreesp.X * Math.PI / 180.0f));      // we rotate the camera vector around X and Y to get a vector which points from eyepos to lookat pos
            transform *= Matrix3.CreateRotationY((float)(cameradirdegreesp.Y * Math.PI / 180.0f));

            Vector3 eyerel = Vector3.Transform(cameravector, transform);       // the 0,1,0 sets the axis of the camera dir

            return lookat - eyerel * distance;
        }

        /// <summary> From current lookat, calculate eyeposition, given a camera angle and a distance</summary>
        public static Vector3d CalculateEyePositionFromLookat(this Vector3d lookat, Vector2d cameradirdegreesp, double distance)
        {
            Matrix4d transform = Matrix4d.Identity;                   // identity nominal matrix, dir is in degrees

            transform *= Matrix4d.CreateRotationX((cameradirdegreesp.X * Math.PI / 180.0f));      // we rotate the camera vector around X and Y to get a vector which points from eyepos to lookat pos
            transform *= Matrix4d.CreateRotationY((cameradirdegreesp.Y * Math.PI / 180.0f));

            Vector3d eyerel = Vector3d.Transform(new Vector3d(0, 1, 0), transform);       // the 0,1,0 sets the axis of the camera dir

            return lookat - eyerel * distance;
        }

        /// <summary>
        /// To string, invariant, with separator
        /// </summary>
        /// <param name="v">Value</param>
        /// <param name="separ">Character uses as vector parts delimiter</param>
        /// <returns>Invariant string of values</returns>
        public static string ToStringInvariant(this Vector3 v, char separ = ',')
        {
            return string.Format( System.Globalization.CultureInfo.InvariantCulture,"{0}{1}{2}{3}{4}", v.X, separ, v.Y, separ, v.Z);
        }

        /// <summary>
        /// Parse a string for a vector 3 invariant
        /// </summary>
        /// <param name="s">string</param>
        /// <param name="separ">Character uses as vector parts delimiter</param>
        /// <returns>Vector3 or null </returns>
        public static Vector3? InvariantParseVector3(this string s, char separ = ',')
        {
            string[] sl = s.Split(separ);
            if (sl.Length == 3)
            {
                float? x = sl[0].InvariantParseFloatNull();
                float? y = sl[1].InvariantParseFloatNull();
                float? z = sl[2].InvariantParseFloatNull();
                if (x != null && y != null && z != null)
                    return new Vector3(x.Value, y.Value, z.Value);
            }

            return null;
        }
    }
}
