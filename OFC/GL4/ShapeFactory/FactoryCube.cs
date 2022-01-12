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
using System;
using System.Collections.Generic;

namespace GLOFC.GL4.ShapeFactory
{
    /// <summary>
    /// These factory classes allow you to create some common shapes for use in drawing
    /// </summary>
    internal static class NamespaceDoc { } // just for documentation purposes

    /// <summary>
    /// Shape factory for cubes
    /// </summary>

    static public class GLCubeObjectFactory
    {
        /// <summary>Cube Sides</summary>
        public enum Sides
        {
            /// <summary>Left</summary>
            Left,
            /// <summary>Right</summary>
            Right,
            /// <summary>Front</summary>
            Front,
            /// <summary>Back</summary>
            Back,
            /// <summary>Bottom</summary>
            Bottom,
            /// <summary>Top</summary>
            Top,
            /// <summary>All</summary>
            All
        };

        /// <summary>A solid cube built with triangles</summary>
        /// <param name="size">Size of sides</param>
        /// <param name="pos">Optional, offset position to place model</param>
        /// <returns>Vector4 array of positions (w=1)</returns>
        public static Vector4[] CreateSolidCubeFromTriangles(float size, Vector3? pos = null)
        {
            return CreateSolidCubeFromTriangles(size, new Sides[] { Sides.All }, pos);
        }

        /// <summary>A solid cube built with triangles</summary>
        /// <param name="size">Size of sides</param>
        /// <param name="sides">What sides to construct</param>
        /// <param name="pos">Optional, offset position to place model</param>
        /// <returns>Vector4 array of positions (w=1)</returns>
        public static Vector4[] CreateSolidCubeFromTriangles(float size, Sides[] sides, Vector3? pos = null)
        {
            size = size / 2f; // halv side - and other half +
            List<Vector4> vert = new List<Vector4>();

            bool all = Array.IndexOf(sides, Sides.All) >= 0;

            if (all || Array.IndexOf(sides, Sides.Left) >= 0)
            {
                vert.AddRange(new Vector4[] {
                                            new Vector4(new Vector4(-size, -size, -size, 1.0f)),        // left side, lower right triangle
                                            new Vector4(new Vector4(-size, size, -size, 1.0f)),
                                            new Vector4(new Vector4(-size, -size, size, 1.0f)),
                                            new Vector4(new Vector4(-size, -size, size, 1.0f)),         // left side, upper left triangle
                                            new Vector4(new Vector4(-size, size, -size, 1.0f)),
                                            new Vector4(new Vector4(-size, size, size, 1.0f)),
                });
            }
            if (all || Array.IndexOf(sides, Sides.Right) >= 0)
            {
                vert.AddRange(new Vector4[] {
                new Vector4(new Vector4(size, -size, size, 1.0f)),         // right side, lower right triangle
                new Vector4(new Vector4(size, size, size, 1.0f)),
                new Vector4(new Vector4(size, -size, -size, 1.0f)),
                new Vector4(new Vector4(size, -size, -size, 1.0f)),         // right side, upper left triangle
                new Vector4(new Vector4(size, size, size, 1.0f)),
                new Vector4(new Vector4(size, size, -size, 1.0f)),
                });
            }
            if (all || Array.IndexOf(sides, Sides.Bottom) >= 0)
            {
                vert.AddRange(new Vector4[] {
                new Vector4(new Vector4(size, -size, size, 1.0f)),        // bottom face, lower right
                new Vector4(new Vector4(size, -size, -size, 1.0f)),
                new Vector4(new Vector4(-size, -size, size, 1.0f)),
                new Vector4(new Vector4(-size, -size, size, 1.0f)),         //bottom face, upper left
                new Vector4(new Vector4(size, -size, -size, 1.0f)),
                new Vector4(new Vector4(-size, -size, -size, 1.0f)),
                });
            }
            if (all || Array.IndexOf(sides, Sides.Top) >= 0)
            {
                vert.AddRange(new Vector4[] {
                new Vector4(new Vector4(size, size, -size, 1.0f)),         // top face
                new Vector4(new Vector4(size, size, size, 1.0f)),
                new Vector4(new Vector4(-size, size, -size, 1.0f)),
                new Vector4(new Vector4(-size, size, -size, 1.0f)),
                new Vector4(new Vector4(size, size, size, 1.0f)),
                new Vector4(new Vector4(-size, size, size, 1.0f)),
                });
            }
            if (all || Array.IndexOf(sides, Sides.Front) >= 0)
            {
                vert.AddRange(new Vector4[] {
                new Vector4(new Vector4(size, -size, -size, 1.0f)),         // front face, lower right
                new Vector4(new Vector4(size, size, -size, 1.0f)),
                new Vector4(new Vector4(-size, -size, -size, 1.0f)),
                new Vector4(new Vector4(-size, -size, -size, 1.0f)),        // front face, upper left
                new Vector4(new Vector4(size, size, -size, 1.0f)),
                new Vector4(new Vector4(-size, size, -size, 1.0f)),
                });
            }
            if (all || Array.IndexOf(sides, Sides.Back) >= 0)
            {
                vert.AddRange(new Vector4[] {
                new Vector4(new Vector4(-size, -size, size, 1.0f)),         // back face, lower right
                new Vector4(new Vector4(-size, size, size, 1.0f)),
                new Vector4(new Vector4(size, -size, size, 1.0f)),
                new Vector4(new Vector4(size, -size, size, 1.0f)),          // back face, upper left
                new Vector4(new Vector4(-size, size, size, 1.0f)),
                new Vector4(new Vector4(size, size, size, 1.0f)),
                });
            }

            var array = vert.ToArray();
            if (pos != null)
                GLStaticsVector4.Translate(ref array, pos.Value);

            return array;
        }

        static private Vector2[] tricoords = new Vector2[]
        {
                new Vector2(1.0f, 1.0f),      // lower right triangle
                new Vector2(1.0f, 0),
                new Vector2(0, 1.0f),
                new Vector2(0, 1.0f),      // upper left triangle
                new Vector2(1.0f, 0),
                new Vector2(0, 0),
        };

        /// <summary>
        /// Create Tex Triangles for textures - 1/0 co-ordinates. Two triangles per side
        /// </summary>
        /// <param name="number">Number of sides</param>
        /// <returns>Vector2[] </returns>
        public static Vector2[] CreateTexTriangles(int number)
        {
            Vector2[] t = new Vector2[number * 6];
            for (int i = 0; i < number * 6; i++)
                t[i] = tricoords[i % 6];

            return t;
        }

        /// <summary>
        /// Create Tex Triangles for a cube
        /// </summary>
        /// <returns>Vector2[] </returns>
        public static Vector2[] CreateCubeTexTriangles()
        {
            return CreateTexTriangles(6);
        }

        /// <summary>
        /// Create a point cube - a set of vertex on each point of a cube
        /// </summary>
        /// <param name="size">Size of cub</param>
        /// <param name="pos">Optional, offset position to place model</param>
        /// <returns></returns>

        public static Vector4[] CreateVertexPointCube(float size, Vector3? pos = null)
        {
            size = size / 2f; // halv side - and other half +
            Vector4[] vertices =
            {
                new Vector4(new Vector4(-size, size, size, 1.0f)),       // arranged as wound clockwise around top, then around bottom
                new Vector4(new Vector4(size, size, size, 1.0f)),
                new Vector4(new Vector4(size, size, -size, 1.0f)),
                new Vector4(new Vector4(-size, size, -size, 1.0f)),
                new Vector4(new Vector4(-size, -size, size, 1.0f)),
                new Vector4(new Vector4(size, -size, size, 1.0f)),
                new Vector4(new Vector4(size, -size, -size, 1.0f)),
                new Vector4(new Vector4(-size, -size, -size, 1.0f)),
            };

            if (pos != null)
                GLStaticsVector4.Translate(ref vertices, pos.Value);

            return vertices;
        }

    }
}