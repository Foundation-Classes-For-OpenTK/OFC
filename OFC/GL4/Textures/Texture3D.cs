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

namespace GLOFC.GL4.Textures
{
    /// <summary>
    /// 3 Dimensional array texture. 3d arrays interpolate between z pixels
    /// </summary>

    public class GLTexture3D : GLTextureBase         // load a texture into open gl
    {
        /// <summary>
        /// Create a 3D texture
        /// </summary>
        /// <param name="width">Width of texture</param>
        /// <param name="height">Height of texture</param>
        /// <param name="depth">Number of levels of texture</param>
        /// <param name="internalformat">Internal format, see InternalFormat in Texture base class</param>/// 
        /// <param name="wantedmipmaplevels">Mip map levels wanted in texture</param>
        public GLTexture3D(int width, int height, int depth, SizedInternalFormat internalformat, int wantedmipmaplevels = 1)
        {
            CreateOrUpdateTexture(width, height, depth, internalformat, wantedmipmaplevels);
        }

        /// <summary>
        /// Create of update the texture with a new size and format
        /// You can call as many times to create textures. Only creates one if required
        /// mipmaplevels does not apply if multisample > 0 
        /// Rgba8 is the normal one to pick
        /// </summary>
        /// <param name="width">Width of texture</param>
        /// <param name="height">Height of texture</param>
        /// <param name="depth">Number of levels of texture</param>
        /// <param name="internalformat">Internal format, see InternalFormat in Texture base class</param>/// 
        /// <param name="wantedmipmaplevels">Mip map levels wanted in texture</param>
        /// <param name="multisample">Multisample count, normally 0</param>
        /// <param name="fixedmultisampleloc">Fix multisample positions in the same place for all texel in image</param>
        public void CreateOrUpdateTexture(int width, int height, int depth, SizedInternalFormat internalformat, int wantedmipmaplevels = 1,
                                            int multisample = 0, bool fixedmultisampleloc = false)
        {
            if (Id < 0 || Width != width || Height != height || Depth != depth || wantedmipmaplevels != MipMapLevels)    // if not there, or changed, we can't just replace it, size is fixed. Delete it
            {
                if (Id >= 0)
                    Dispose();

                InternalFormat = internalformat;
                Width = width; 
                Height = height; 
                Depth = depth;
                MipMapLevels = wantedmipmaplevels;
                MultiSample = multisample;

                GL.CreateTextures(MultiSample>0 ? TextureTarget.Texture2DMultisampleArray : TextureTarget.Texture3D, 1, out int id);
                GLStatics.RegisterAllocation(typeof(GLTexture3D));
                GLStatics.Check();
                Id = id;

                if (MultiSample > 0)
                {
                    GL.TextureStorage3DMultisample(Id, MultiSample, InternalFormat, Width, Height, Depth, fixedmultisampleloc);
                }
                else
                {
                    GL.TextureStorage3D(Id, wantedmipmaplevels, InternalFormat, Width, Height, Depth);
                }

                SetMinMagFilter();

                GLStatics.Check();
            }
        }
    }
}

