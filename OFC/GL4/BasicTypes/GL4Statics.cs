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

using OpenTK.Graphics.OpenGL4;

namespace OFC.GL4
{
    public static class GL4Statics
    {
        public static DrawElementsType DrawElementsTypeFromMaxEID(uint eid)
        {
            if (eid < byte.MaxValue)
                return DrawElementsType.UnsignedByte;
            else if (eid < ushort.MaxValue)
                return DrawElementsType.UnsignedShort;
            else
                return DrawElementsType.UnsignedInt;
        }

        public static uint DrawElementsRestartValue(DrawElementsType t)
        {
            if (t == DrawElementsType.UnsignedByte)
                return 0xff;
            else if (t == DrawElementsType.UnsignedShort)
                return 0xffff;
            else
                return 0xffffffff;
        }

        public static int ElementsPerPixel(PixelFormat pxformatback)
        {
            if (pxformatback == PixelFormat.Rgba || pxformatback == PixelFormat.Bgra)
                return 4;
            else if (pxformatback == PixelFormat.Rg)
                return 2;
            else if (pxformatback == PixelFormat.Red || pxformatback == PixelFormat.Green || pxformatback == PixelFormat.Blue)
                return 1;
            else if (pxformatback == PixelFormat.Rgb || pxformatback == PixelFormat.Bgr)
                return 3;
            else
                System.Diagnostics.Debug.Assert(false, "Not supported px format");

            return 0;
        }

        public static int GetMaxUniformBlockSize()
        {
            return GL.GetInteger(OpenTK.Graphics.OpenGL4.GetPName.MaxUniformBlockSize);     // biggest uniform buffer (64k on Nvidia in 2020)
        }


        public static void GetMaxUniformBuffers(out int vertex, out int fragment, out int geo, out int tesscontrol, out int tesseval)
        {
            vertex = GL.GetInteger(OpenTK.Graphics.OpenGL4.GetPName.MaxVertexUniformBlocks);
            fragment = GL.GetInteger(OpenTK.Graphics.OpenGL4.GetPName.MaxFragmentUniformBlocks);
            geo = GL.GetInteger(OpenTK.Graphics.OpenGL4.GetPName.MaxGeometryUniformBlocks);
            tesscontrol = GL.GetInteger(OpenTK.Graphics.OpenGL4.GetPName.MaxTessControlUniformBlocks);
            tesseval = GL.GetInteger(OpenTK.Graphics.OpenGL4.GetPName.MaxTessEvaluationUniformBlocks);
        }

        public static int GetMaxTextureDepth()
        {
            return GL.GetInteger(OpenTK.Graphics.OpenGL4.GetPName.MaxArrayTextureLayers);
        }

        public static int GetMaxVertexAndFragmentTexturesCombined()
        {
            return GL.GetInteger(OpenTK.Graphics.OpenGL4.GetPName.MaxCombinedTextureImageUnits);
        }

        public static int GetMaxFragmentTextures()
        {
            return GL.GetInteger(OpenTK.Graphics.OpenGL4.GetPName.MaxTextureImageUnits);
        }

        public static int GetMaxVertexAttribs()
        {
            return GL.GetInteger(OpenTK.Graphics.OpenGL4.GetPName.MaxVertexAttribs);
        }

        public static void BeginTransformFeedback(TransformFeedbackPrimitiveType t)
        {
            GL.BeginTransformFeedback(t);
        }

        public static void EndTransformFeedback()
        {
            GL.EndTransformFeedback();
        }

    }
}

