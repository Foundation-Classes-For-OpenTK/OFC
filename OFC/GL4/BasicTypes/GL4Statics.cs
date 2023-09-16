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

namespace GLOFC.GL4
{
    /// <summary>
    /// This namespace contains the base GL4 classes which wrap the open GL elements.
    /// </summary>
    internal static class NamespaceDoc { } // just for documentation purposes

    /// <summary>
    /// GL4 Statics functions are specific functions for GL4.  Some of these are used by the render state so you don't have to control them manually.
    /// </summary>
    public static class GL4Statics
    {
        /// <summary> Return DrawElementsType (Byte/Short/Uint) given maximum ID </summary>
        public static DrawElementsType DrawElementsTypeFromMaxEID(uint eid)
        {
            if (eid < byte.MaxValue)
                return DrawElementsType.UnsignedByte;
            else if (eid < ushort.MaxValue)
                return DrawElementsType.UnsignedShort;
            else
                return DrawElementsType.UnsignedInt;
        }

        /// <summary> Return nominal restart value given a DrawElementsType </summary>
        public static uint DrawElementsRestartValue(DrawElementsType t)
        {
            if (t == DrawElementsType.UnsignedByte)
                return 0xff;
            else if (t == DrawElementsType.UnsignedShort)
                return 0xffff;
            else
                return 0xffffffff;
        }

        /// <summary> Given a PixelFormat how many elements are in it (1-4) </summary>
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

        /// <summary> Return from GL maximum Uniform Block Size </summary>
        public static int GetMaxUniformBlockSize()
        {
            return GL.GetInteger(OpenTK.Graphics.OpenGL4.GetPName.MaxUniformBlockSize);     // biggest uniform buffer (64k on Nvidia in 2020)
        }

        /// <summary> Return from GL maximum Uniform Buffers for vertex, fragmand, geo, tess control, tess eval </summary>

        public static void GetMaxUniformBuffers(out int vertex, out int fragment, out int geo, out int tesscontrol, out int tesseval)
        {
            vertex = GL.GetInteger(OpenTK.Graphics.OpenGL4.GetPName.MaxVertexUniformBlocks);
            fragment = GL.GetInteger(OpenTK.Graphics.OpenGL4.GetPName.MaxFragmentUniformBlocks);
            geo = GL.GetInteger(OpenTK.Graphics.OpenGL4.GetPName.MaxGeometryUniformBlocks);
            tesscontrol = GL.GetInteger(OpenTK.Graphics.OpenGL4.GetPName.MaxTessControlUniformBlocks);
            tesseval = GL.GetInteger(OpenTK.Graphics.OpenGL4.GetPName.MaxTessEvaluationUniformBlocks);
        }

        /// <summary> Return maximum width or height of pixels in a texture </summary>
        public static int GetMaxTextureWidthAndHeight()
        {
            return GL.GetInteger(OpenTK.Graphics.OpenGL4.GetPName.MaxRectangleTextureSize);
        }
        /// <summary> Return maximum number of array textures for a one or two dimensional array textures per draw</summary>
        public static int GetMaxTextureDepth()
        {
            return GL.GetInteger(OpenTK.Graphics.OpenGL4.GetPName.MaxArrayTextureLayers);
        }

        /// <summary> Return maximum number of vertex and fragment textures per draw </summary>
        public static int GetMaxVertexAndFragmentTexturesCombined()
        {
            return GL.GetInteger(OpenTK.Graphics.OpenGL4.GetPName.MaxCombinedTextureImageUnits);
        }

        /// <summary> Return maximum number of fragment textures per draw </summary>
        public static int GetMaxFragmentTextures()
        {
            return GL.GetInteger(OpenTK.Graphics.OpenGL4.GetPName.MaxTextureImageUnits);
        }

        /// <summary> Return maximum number of vertex attributes per draw </summary>
        public static int GetMaxVertexAttribs()
        {
            return GL.GetInteger(OpenTK.Graphics.OpenGL4.GetPName.MaxVertexAttribs);
        }


        /// <summary> Return maximum shader storage buffer number +1 (all binding indexes must be below this)</summary>
        public static int GetShaderStorageMaxBindingNumber()
        {
            return GL.GetInteger((GetPName)All.MaxShaderStorageBufferBindings);
        }
    }
}

