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
    /// Static helper functions for Vector2
    /// </summary>

    public static class GLStaticsVector2
    {
        /// <summary> Floor the components</summary>
        public static Vector2 Floor(this Vector2 a)
        {
            return new Vector2((float)Math.Floor(a.X), (float)Math.Floor(a.Y));
        }

        /// <summary>Fract the components </summary>
        public static Vector2 Fract(this Vector2 a)
        {
            float x = (float)(a.X - Math.Floor(a.X));
            float y = (float)(a.Y - Math.Floor(a.Y));
            return new Vector2(x, y);
        }

        /// <summary> Find position between two vectors with a definable position </summary>
        public static Vector2 Mix(Vector2 a, Vector2 b, float mix)
        {
            float x = (float)(a.X + (b.X - a.X) * mix);
            float y = (float)(a.Y - (b.Y - a.Y) * mix);
            return new Vector2(x, y);
        }

        /// <summary> Absolute the components </summary>
        static public Vector2 Abs(this Vector2 v)
        {
            return new Vector2(Math.Abs(v.X), Math.Abs(v.Y));
        }


        /// <summary> Randomise vector to float </summary>
        public static float randA(Vector2 n)
        {
            Vector2 i0 = new Vector2(12.9898f, 4.1414f);
            float i1 = Vector2.Dot(n, i0);
            float i2 = (float)Math.Sin(i1) * 43758.5453f;
            return i2.Fract();
        }

        /// <summary> generate noise </summary>
        public static float noiseA(Vector2 p)
        {
            Vector2 ip = p.Floor();
            Vector2 u = p.Fract();
            u = u * u * (new Vector2(3, 3) - 2.0f * u);

            float res =
                ObjectExtensionsNumbersBool.Mix(
                    ObjectExtensionsNumbersBool.Mix(randA(ip), randA(ip + new Vector2(1.0f, 0.0f)), u.X),
                    ObjectExtensionsNumbersBool.Mix(randA(ip + new Vector2(0.0f, 1.0f)), randA(ip + new Vector2(1.0f, 1.0f)), u.X),
                    u.Y);
            return res * res;
        }

        /// <summary> Rotate the vector </summary>
        public static Vector2 Rotate(this Vector2 v, float degreesrad)
        {
            double sin = Math.Sin(degreesrad);
            double cos = Math.Cos(degreesrad);

            float tx = v.X;
            float ty = v.Y;
            v.X = (float)((cos * tx) - (sin * ty));
            v.Y = (float)((sin * tx) + (cos * ty));
            return v;
        }

        /// <summary> Angle between two vectors </summary>
        public static float Angle(this Vector2 start, Vector2 end)
        {
            return (float)Math.Atan2(end.Y - start.Y, end.X - start.X);
        }

        /// <summary> Vector2 to Vector4 </summary>
        public static Vector4 ToVector4XZ(this Vector2 vxz, float y = 0, float w = 1)
        {
            return new Vector4(vxz.X, y, vxz.Y, w);
        }

        /// <summary>
        /// To string, invariant, with separator
        /// </summary>
        /// <param name="v">Value</param>
        /// <param name="separ">Character uses as vector parts delimiter</param>
        /// <returns>Invariant string of values</returns>
        public static string ToStringInvariant(this Vector2 v, char separ = ',')
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}{1}{2}", v.X, separ, v.Y);
        }

        /// <summary>
        /// Parse a string for a vector 2 invariant
        /// </summary>
        /// <param name="s">string</param>
        /// <param name="separ">Character uses as vector parts delimiter</param>
        /// <returns>Vector2 or null </returns>
        public static Vector2? InvariantParseVector2(this string s, char separ = ',')
        {
            string[] sl = s.Split(separ);
            if (sl.Length == 2)
            {
                float? x = sl[0].InvariantParseFloatNull();
                float? y = sl[1].InvariantParseFloatNull();
                if (x != null && y != null)
                    return new Vector2(x.Value, y.Value);
            }

            return null;
        }



    }
}
