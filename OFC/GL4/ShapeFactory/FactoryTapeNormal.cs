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
using OpenTK.Graphics.OpenGL4;
using System.Drawing;
using GLOFC.Utils;

namespace GLOFC.GL4.ShapeFactory
{
    /// <summary>
    /// Shape factor for tapes
    /// </summary>

    static public class GLTapeNormalObjectFactory
    {
        /// <summary>
        /// Create a tape returning vectors and normals with element indexes. Use with a TriangleStrip
        /// The tape is segmented, and roty determines if its flat to Y or not
        /// The series of tapes with a margin between them.  
        /// This provides the element index buffer indices as well
        /// Aligns the element indexes start point for each tape to modulo N to allow trianglestrip to work properly (4 normally)
        /// You pass in colour array, or null, to add colour information to tape W values.  Note colour is one less in length than points array
        /// </summary>
        /// <param name="points">Points along which the tape should go. Minimum 2 points</param>
        /// <param name="colours">Array of colours to use, per segment. Color is packed into W of Vector4 positions on every vertex of a segment</param>
        /// <param name="segmentlength">Segment length, for each triangle pair making up the tape</param>
        /// <param name="rotationaroundyradians">Rotate the tape around its axis</param>
        /// <param name="margin">Space between each segment</param>
        /// <param name="restartindex">The element restart index to use</param>
        /// <param name="modulo">The alignment of each segment start vertex index, to align for triangle strip use</param>
        /// <returns>Return list of points, List of normals, element buffer indexes, and the indexor draw element type</returns>

        public static Tuple<List<Vector4>, List<Vector4>, List<uint>, DrawElementsType> CreateTape(Vector4[] points, Color[] colours, float segmentlength = 1, 
                                                       float rotationaroundyradians = 0,
                                                       float margin = 0, uint restartindex = 0xffffffff, int modulo = 4)
        {
            List<Vector4> vec = new List<Vector4>();
            List<Vector4> normals = new List<Vector4>();
            List<uint> eids = new List<uint>();
            DrawElementsType det = DrawElementsType.UnsignedByte;

            if (points.Length >= 2)
            {
                uint vno = 0;

                for (int i = 0; i < points.Length - 1; i++)
                {
                    var segment = CreateTape(points[i].ToVector3(), points[i + 1].ToVector3(), segmentlength, rotationaroundyradians, margin);

                    if (colours != null)
                    {
                        int w = colours[i].PackRGB();
                        for (int j = 0; j < segment.Item1.Length; j++)
                            segment.Item1[j].W = w;
                    }

                    while (vec.Count % modulo != 0)     // must be on a boundary of four for the vertex shaders which normally are used
                    {
                        vec.Add(new Vector4(1000, 2000, 3000, 1));     // dummy value we can recognise
                        normals.Add(new Vector4(1000, 2000, 3000, 1));  
                        vno++;
                    }

                    //  System.Diagnostics.Debug.WriteLine($"At {vno} vec {vec.Count} add {vec1.Length}");

                    vec.AddRange(segment.Item1);
                    normals.AddRange(segment.Item2);

                    for (int l = 0; l < segment.Item1.Length; l++)
                        eids.Add(vno++);

                    eids.Add(restartindex);
                }

                eids.RemoveAt(eids.Count - 1);  // remove last restart
                det = GL4Statics.DrawElementsTypeFromMaxEID(vno - 1);
            }

            return new Tuple<List<Vector4>, List<Vector4>,List<uint>, DrawElementsType>(vec, normals, eids, det);
        }

        /// <summary>
        /// Create a tape segment returning vertex points and normals
        /// A tape, between start and end, of width. Minimum of 4 vertex is created.
        /// You can select rotation around y in radians
        /// Segment length is the length between each set of vector points
        /// Margin is offset to start from and end from from points
        /// </summary>
        /// <param name="start">Start point</param>
        /// <param name="end">End point</param>
        /// <param name="segmentlength">Segment length, for each triangle pair making up the tape</param>
        /// <param name="rotationaroundyradians">Rotate the tape around its axis</param>
        /// <param name="margin">Space between each segment</param>
        /// <returns>Tuple of Return list of points, Normals to each point</returns>

        public static Tuple<Vector4[],Vector4[]> CreateTape(Vector3 start, Vector3 end, float segmentlength = 1, float rotationaroundyradians = 0, float margin = 0)
        {
            Vector3 vectorto = Vector3.Normalize(end - start);                  // vector between the points, normalised

            if (margin > 0)
            {
                start = new Vector3(start.X + vectorto.X * margin, start.Y + vectorto.Y * margin, start.Z + vectorto.Z * margin);
                end = new Vector3(end.X - vectorto.X * margin, end.Y - vectorto.Y * margin, end.Z - vectorto.Z * margin);
            }

            float length = (end - start).Length;
            int innersegments = (int)(length / segmentlength);
            if (innersegments < 1)  // must have at least 1 inner segment, since we need at least 4 vectors
                innersegments = 1;

            segmentlength = length / innersegments;

            // the vectorto and left/right normals are on the notional direction plane of the vector in this view
            // given the normalised vector between the start end, create a normal vector (90 to it) pointing left and right

            Vector3 leftnormal = Vector3.TransformNormal(vectorto, Matrix4.CreateRotationY(-(float)Math.PI / 2)); // + is clockwise.  Generate normal to vector on left side
            Vector3 rightnormal = Vector3.TransformNormal(vectorto, Matrix4.CreateRotationY((float)Math.PI / 2)); // On right side.

            // the way this works, is that we rotate the l/r normals around Y, to align then with the YZ plane (YZ of the direction plane)
            // The rotation is the difference between the facing angle in the ZX plane (xzangle) and the YZ plane itself which is at +90degrees direction on the ZX plane
            // then we rotate around X
            // then we rotate it back by the facing angle
            // lets just say this took a bit of thinking about!  
            // This is a generic way of rotating around an arbitary plane - rotate it back to a plane without the one you want to rotate on.

            double xzangle = Math.Atan2(end.Z - start.Z, end.X - start.X);      // angle on the ZX plane between start/end
            double rotatetoyzangle = Math.PI / 2 - (xzangle + Math.PI / 2);           // angle to rotate back to the YZ plane, noting the normals are 90 to the xyangle

            leftnormal = Vector3.TransformNormal(leftnormal, Matrix4.CreateRotationY(-(float)rotatetoyzangle));     // rotate the normals to YZ plane
            rightnormal = Vector3.TransformNormal(rightnormal, Matrix4.CreateRotationY(-(float)rotatetoyzangle));

            leftnormal = Vector3.TransformNormal(leftnormal, Matrix4.CreateRotationX(-(float)rotationaroundyradians));     // rotate on the YZ plane around X to tip it up
            rightnormal = Vector3.TransformNormal(rightnormal, Matrix4.CreateRotationX(-(float)rotationaroundyradians));

            leftnormal = Vector3.TransformNormal(leftnormal, Matrix4.CreateRotationY((float)rotatetoyzangle));      // rotate back to angle on XZ plane
            rightnormal = Vector3.TransformNormal(rightnormal, Matrix4.CreateRotationY((float)rotatetoyzangle));

            Vector3 segoff = new Vector3((end.X - start.X) / length * segmentlength, (end.Y - start.Y) / length * segmentlength, (end.Z - start.Z) / length * segmentlength);

            Vector4[] tape = new Vector4[2 + 2 * innersegments];                // 2 start, plus 2 for any inners
            Vector4[] normals = new Vector4[2 + 2 * innersegments];                // 2 start, plus 2 for any inners

            int i;
            for (i = 0; i <= innersegments; i++)   // include at least the start
            {
                tape[i * 2 + 1]  = tape[i * 2] = new Vector4(start,1);      // same point for both
                normals[i * 2] = new Vector4(leftnormal,0);                 // first goes left
                normals[i * 2 + 1] = new Vector4(rightnormal,0);            // next goes right
                start += segoff;
            }

            return new Tuple<Vector4[],Vector4[]>(tape,normals);
        }
    }
}
