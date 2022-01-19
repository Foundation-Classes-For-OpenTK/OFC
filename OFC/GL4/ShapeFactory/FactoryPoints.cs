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


using OpenTK;
using OpenTK.Graphics;
using System;
using System.Collections.Generic;

namespace GLOFC.GL4.ShapeFactory
{
    /// <summary>
    /// Shape factory for Points
    /// </summary>
    
    static public class GLPointsFactory
    {
        /// <summary>
        /// Random stars
        /// </summary>
        /// <param name="number">Count</param>
        /// <param name="left">Left side of box</param>
        /// <param name="right">Right side of box</param>
        /// <param name="front">Front side of box</param>
        /// <param name="back">Back side of box</param>
        /// <param name="top">Top side of box</param>
        /// <param name="bottom">Bottom side of box</param>
        /// <param name="rnd">Random class to get values from, or null for autocreate</param>
        /// <param name="seed">Seed for random class if rnd=null</param>
        /// <returns>Vector3[]</returns>

        public static Vector3[] RandomStars(int number, float left, float right, float front, float back, float top, float bottom, Random rnd = null, int seed = 23)
        {
            if (rnd == null)
                rnd = new Random(seed);

            Vector3[] array = new Vector3[number];

            for (int s = 0; s < number; s++)
            {
                float x = rnd.Next(100000) * (right - left) / 100000.0f + left;
                float y = rnd.Next(100000) * (top - bottom) / 100000.0f + bottom;
                float z = rnd.Next(100000) * (back - front) / 100000.0f + front;

                array[s] = new Vector3(x, y, z);
            }

            return array;
        }

        /// <summary>
        /// Random stars in a box
        /// </summary>
        /// <param name="number">Count</param>
        /// <param name="left">Left side of box</param>
        /// <param name="right">Right side of box</param>
        /// <param name="front">Front side of box</param>
        /// <param name="back">Back side of box</param>
        /// <param name="top">Top side of box</param>
        /// <param name="bottom">Bottom side of box</param>
        /// <param name="rnd">Random class to get values from, or null for autocreate</param>
        /// <param name="seed">Seed for random class if rnd=null</param>
        /// <param name="w">Value for Vector4.w</param>
        /// <returns>Vector4[]</returns>

        public static Vector4[] RandomStars4(int number, float left, float right, float front, float back, float top, float bottom, Random rnd = null, int seed = 23, float w = 1)
        {
            if (rnd == null)
                rnd = new Random(seed);

            Vector4[] array = new Vector4[number];

            for (int s = 0; s < number; s++)
            {
                float x = rnd.Next(100000) * (right - left) / 100000.0f + left;
                float y = rnd.Next(100000) * (top - bottom) / 100000.0f + bottom;
                float z = rnd.Next(100000) * (back - front) / 100000.0f + front;

                array[s] = new Vector4(x, y, z, w);
            }

            return array;
        }

        /// <summary>
        /// Random stars in a disc
        /// </summary>
        /// <param name="number">Count</param>
        /// <param name="x">Centre of x</param>
        /// <param name="z">Centre of z</param>
        /// <param name="dist">Size of disc</param>
        /// <param name="top">Top side of area</param>
        /// <param name="bottom">Bottom side of area</param>
        /// <param name="rnd">Random class to get values from, or null for autocreate</param>
        /// <param name="seed">Seed for random class if rnd=null</param>
        /// <param name="w">Value for Vector4.w</param>
        /// <returns>Vector4[]</returns>

        public static Vector4[] RandomStars4(int number, float x, float z, float dist, float top, float bottom, Random rnd = null, int seed = 23, float w = 1)
        {
            if (rnd == null)
                rnd = new Random(seed);

            Vector4[] array = new Vector4[number];

            int s = 0;
            while (s < number)
            {
                float xd = rnd.Next(100000) * dist * 2 / 100000.0f - dist;
                float zd = rnd.Next(100000) * dist * 2 / 100000.0f - dist;

                if (xd * xd + zd * zd < dist * dist)
                {
                    float yp = rnd.Next(100000) * (top - bottom) / 100000.0f + bottom;
                    array[s++] = new Vector4(x + xd, yp, z + zd, w);
                }
            }

            return array;
        }

        /// <summary>
        /// Random stars in a box, filling a buffer
        /// </summary>
        /// <param name="buffer">GL Buffer</param>
        /// <param name="number">Count</param>
        /// <param name="left">Left side of box</param>
        /// <param name="right">Right side of box</param>
        /// <param name="front">Front side of box</param>
        /// <param name="back">Back side of box</param>
        /// <param name="top">Top side of box</param>
        /// <param name="bottom">Bottom side of box</param>
        /// <param name="rnd">Random class to get values from, or null for autocreate</param>
        /// <param name="seed">Seed for random class if rnd=null </param>
        /// <param name="w">Value for Vector4.w</param>
        /// <returns>Vector4[]</returns>

        public static void RandomStars4(GLBuffer buffer, int number, float left, float right, float front, float back, float top, float bottom, 
                                        Random rnd = null, int seed = 23, float w = 1)
        {
            if (rnd == null)
                rnd = new Random(seed);

            buffer.AlignFloat();

            for (int s = 0; s < number; s++)
            {
                float[] a = new float[] {   rnd.Next(100000) * (right - left) / 100000.0f + left ,
                                            rnd.Next(100000) * (top - bottom) / 100000.0f + bottom,
                                            rnd.Next(100000) * (back - front) / 100000.0f + front,
                                            w };
                buffer.WriteCont(a);
            }
        }

    }
}