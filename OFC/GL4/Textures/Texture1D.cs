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
using System;

namespace GLOFC.GL4.Textures
{
    /// <summary>
    /// 1 Dimensional texture
    /// </summary>
    public class GLTexture1D : GLTextureBase
    {
        /// <summary> Constructor </summary>
        public GLTexture1D()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="width">Width</param>
        /// <param name="internalformat">Internal format, see InternalFormat in Texture base class</param>
        /// <param name="levels">Number of levels of this texture</param>
        public GLTexture1D(int width, SizedInternalFormat internalformat, int levels = 1)
        {
            CreateOrUpdateTexture(width, internalformat, levels);
        }

        /// <summary>
        /// Create or update the texture with a new size and format
        /// </summary>
        /// <param name="width">Width</param>
        /// <param name="internalformat">Internal format, see InternalFormat in Texture base class</param>
        /// <param name="levels">Number of levels of this texture</param>
        public void CreateOrUpdateTexture(int width, SizedInternalFormat internalformat, int levels = 1)
        {
            if (Id < 0 || Width != width || MipMapLevels != levels)    // if not there, or changed, we can't just replace it, size is fixed. Delete it
            {
                if (Id >= 0)
                    Dispose();

                InternalFormat = internalformat;
                Width = width;
                Height = 1;
                Depth = levels;

                GL.CreateTextures(TextureTarget.Texture1D, 1, out int id);
                GLStatics.RegisterAllocation(typeof(GLTexture1D));
                System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
                Id = id;

                GL.TextureStorage1D( Id, levels,InternalFormat, Width);
            }
        }
    }
}

