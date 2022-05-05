/*
 * Copyright 2019-2021 Robbyxp1 @ github.com
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
using System;
using System.Drawing;

namespace GLOFC.GL4.Textures
{
    /// <summary>
    /// 1 Dimensional array texture. 
    /// </summary>

    public class GLTexture1DArray : GLTextureBase          // load a 2D set of textures into open gl
    {
        /// <summary> Constructor </summary>
        public GLTexture1DArray()
        {
        }

        /// <summary>
        /// Create of update the texture with a new size and format
        /// You can call as many times to create textures. Only creates one if required
        /// Rgba8 is the normal one to pick
        /// </summary>
        /// <param name="width">Width of texture</param>
        /// <param name="depth">Number of levels of texture</param>
        /// <param name="internalformat">Internal format, see InternalFormat in Texture base class</param>/// 
        /// <param name="wantedmipmaplevels">Mip map levels wanted in texture</param>
        public void CreateOrUpdateTexture(int width, int depth, SizedInternalFormat internalformat, int wantedmipmaplevels = 1)
                                    
        {
            if (Id < 0 || Width != width || Depth != depth || wantedmipmaplevels != MipMapLevels )
            {
                if (Id >= 0)
                    Dispose();

                InternalFormat = internalformat;
                Width = width;
                Height = 1;
                Depth = depth;
                MipMapLevels = wantedmipmaplevels;

                GL.CreateTextures(TextureTarget.Texture1DArray, 1, out int id);
                GLStatics.RegisterAllocation(typeof(GLTexture2DArray));
                System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
                Id = id;

                GL.TextureStorage2D(Id, wantedmipmaplevels, InternalFormat, Width, Height);

                SetMinMagFilter();

                System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr2), glasserterr2);
            }
        }
    }
}
