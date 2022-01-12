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

namespace GLOFC.GL4.ShapeFactory
{
    /// <summary>
    /// Shade factory for lines
    /// </summary>

    static public class GLShapeObjectFactory
    {
        /// <summary>
        /// Create lines
        /// </summary>
        /// <param name="startpos">Start position vector</param>
        /// <param name="endpos">End position vector</param>
        /// <param name="offset">Offset to add to start/end after each line</param>
        /// <param name="lines">Number of lines</param>
        /// <returns>Line start/end points</returns>
        public static Vector4[] CreateLines(Vector3 startpos, Vector3 endpos, Vector3 offset, int lines)
        {
            Vector4[] vertices = new Vector4[lines * 2];

            for (int i = 0; i < lines; i++)
            {
                vertices[i * 2] = new Vector4(new Vector4(startpos.X, startpos.Y, startpos.Z, 1.0f));
                vertices[i * 2 + 1] = new Vector4(new Vector4(endpos.X, endpos.Y, endpos.Z, 1.0f));
                startpos += offset;
                endpos += offset;
            }

            return vertices;
        }

        /// <summary>
        /// Create a box using lines
        /// </summary>
        /// <param name="width">Box width</param>
        /// <param name="depth">Box depth</param>
        /// <param name="height">Box height</param>
        /// <param name="pos">Position of box in world</param>
        /// <param name="rotationradians">Any box rotation</param>
        /// <returns>Array of points of the box</returns>
        public static Vector4[] CreateBox(float width, float depth, float height, Vector3 pos, Vector3? rotationradians = null)
        {
            Vector4[] botvertices = CreateQuad(width, depth, pos: new Vector3(pos.X, pos.Y - height / 2, pos.Z));
            Vector4[] topvertices = CreateQuad(width, depth, pos: new Vector3(pos.X, pos.Y + height / 2, pos.Z));

            Vector4[] box = new Vector4[24];
            box[0] = botvertices[0]; box[1] = botvertices[1]; box[2] = botvertices[1]; box[3] = botvertices[2];
            box[4] = botvertices[2]; box[5] = botvertices[3]; box[6] = botvertices[3]; box[7] = botvertices[0];
            box[8] = topvertices[0]; box[9] = topvertices[1]; box[10] = topvertices[1]; box[11] = topvertices[2];
            box[12] = topvertices[2]; box[13] = topvertices[3]; box[14] = topvertices[3]; box[15] = topvertices[0];
            box[16] = botvertices[0]; box[17] = topvertices[0]; box[18] = botvertices[1]; box[19] = topvertices[1];
            box[20] = botvertices[2]; box[21] = topvertices[2]; box[22] = botvertices[3]; box[23] = topvertices[3];

            GLStaticsVector4.RotPos(ref box, rotationradians);

            return box;
        }

        /// <summary>
        /// Create Quad for bitmaps, scaling height to bitmap height/width ratio
        /// </summary>
        /// <param name="width">Width required</param>
        /// <param name="bitmapwidth">Width of bitmap</param>
        /// <param name="bitmapheight">Height of bitmap</param>
        /// <param name="rotationradians">Any rotation</param>
        /// <param name="pos">World position</param>
        /// <param name="scale">Any sizing for both axis</param>
        /// <returns>Quad</returns>

        public static Vector4[] CreateQuad(float width, int bitmapwidth, int bitmapheight, Vector3? rotationradians = null, Vector3? pos = null, float scale = 1.0f)
        {
            return CreateQuad(width, width * bitmapheight / bitmapwidth, rotationradians, pos, scale);
        }

        /// <summary>
        /// Create Square Quad 
        /// </summary>
        /// <param name="widthheight">Width and Height required</param>
        /// <param name="rotationradians">Any rotation</param>
        /// <param name="pos">World position</param>
        /// <param name="scale">Any sizing for both axis</param>
        /// <returns>Quad</returns>

        public static Vector4[] CreateQuad(float widthheight, Vector3? rotationradians = null, Vector3? pos = null, float scale = 1.0f)
        {
            return CreateQuad(widthheight, widthheight, rotationradians, pos, scale);
        }

        /// <summary>
        /// Create a Quad  (--,+-,++,-+) winding anticlockwise
        /// </summary>
        /// <param name="width">Width required</param>
        /// <param name="height">Height required</param>
        /// <param name="rotationradians">Any rotation</param>
        /// <param name="pos">World position</param>
        /// <param name="scale">Any sizing for both axis</param>
        /// <returns>Quad</returns>

        public static Vector4[] CreateQuad(float width, float height, Vector3? rotationradians = null, Vector3? pos = null, float scale = 1.0f)
        {
            width = width / 2.0f * scale;
            height = height / 2.0f * scale;

            Vector4[] vertices1 =
            {
                new Vector4(-width, 0, -height, 1.0f),          // -, -
                new Vector4(+width, 0, -height, 1.0f),          // +, -
                new Vector4(+width, 0, +height, 1.0f),          // +, +
                new Vector4(-width, 0, +height, 1.0f),          // -, +
            };

            GLStaticsVector4.RotPos(ref vertices1, rotationradians, pos);

            return vertices1;
        }

        /// <summary>
        /// Create a Quad (--,+-,-+,++) for tristrips
        /// </summary>
        /// <param name="width">Width required</param>
        /// <param name="height">Height required</param>
        /// <param name="rotationradians">Any rotation</param>
        /// <param name="pos">World position</param>
        /// <param name="scale">Any sizing for both axis</param>
        /// <returns>Quad</returns>
        public static Vector4[] CreateQuadTriStrip(float width, float height, Vector3? rotationradians = null, Vector3? pos = null, float scale = 1.0f)
        {
            width = width / 2.0f * scale;
            height = height / 2.0f * scale;

            Vector4[] vertices2 =
            {
               new Vector4(-width, 0, -height, 1.0f),      //BL
               new Vector4(+width, 0, -height, 1.0f),      //BR
               new Vector4(-width, 0, +height, 1.0f),      //TL
               new Vector4(+width, 0, +height, 1.0f),      //TR
            };

            GLStaticsVector4.RotPos(ref vertices2, rotationradians, pos);

            return vertices2;
        }

        /// <summary> A Tex Quad (-+,++,+-,--) clockwise winding</summary>
        static public Vector2[] TexQuad { get; set; } = new Vector2[]
        {
            new Vector2(0, 1.0f),
            new Vector2(1.0f, 1.0f),
            new Vector2(1.0f, 0),
            new Vector2(0, 0),
        };

        /// <summary> A Tex Quad Inverted (--,+-,++,-+) anticlockwise winding</summary>
        static public Vector2[] TexQuadInv { get; set; } = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1.0f, 0),
            new Vector2(1.0f, 1.0f),
            new Vector2(0, 1.0f),
        };

        /// <summary> A Tex Tri strip Quad inverted (--,+-,-+,++) winding</summary>
        static public Vector2[] TexTriStripQuadInv { get; set; } = new Vector2[]
        {
            new Vector2(0, 0.0f),
            new Vector2(1.0f, 0f),
            new Vector2(0f, 1.0f),
            new Vector2(1.0f, 1.0f),
        };

        /// <summary> A Tex Tri strip Quad (-+,++,--,+-) </summary>
        static public Vector2[] TexTriStripQuad { get; set; } = new Vector2[]        
        {
            new Vector2(0, 1.0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
        };


    }
}