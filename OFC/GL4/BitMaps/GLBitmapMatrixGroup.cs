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
using System.Drawing;

namespace OFC.GL4
{
    // Group of bitmaps, using underlying MatrixVertexArrayWithGenerations

    public class GLBitmapGroup : GLMatrixBufferWithGenerations
    {
        public GLRenderableItem RenderableItem { get; set; }
        private GLTexture2DArray texture;
        private bool refillmipmaps = false;

        public GLBitmapGroup(GLItemsList items, GLRenderControl rc, int groupsize, int mipmaplevels, Size bitmapsize, int depth) : base(items,groupsize)
        {
            texture = new GLTexture2DArray();
            items.Add(texture);

            var rd = new RenderData(this);

            RenderableItem = GLRenderableItem.CreateMatrix4(items, rc, MatrixBuffer, 4, rd, ic: 0);

            texture.CreateTexture(bitmapsize.Width, bitmapsize.Height, depth, mipmaplevels);
        }

        // return position added as index. If tag == null, you can't find it again
        public int Add(object tag, Bitmap bmp, int bmpmipmaplevels, bool owned, Matrix4 mat)
        {
            int pos = base.Add(tag, owned ? bmp : null, mat);
            texture.LoadBitmap(bmp, pos, false, bmpmipmaplevels);       // texture does not own them, we may do

            if (bmpmipmaplevels < texture.MipMapLevels)        // if our mipmap is less than ordered, we need an auto gen
            {
                refillmipmaps = true;
            }

            RenderableItem.InstanceCount = Count;

            return pos;
        }

        public void Bind()
        {
            if (refillmipmaps)
            {
                texture.GenMipMapTextures();
                refillmipmaps = false;
            }

            if (texture.Id >= 0)
                texture.Bind(1);
        }

        private class RenderData : IGLRenderItemData
        {
            public RenderData(GLBitmapGroup g)
            {
                group = g;
            }

            public virtual void Bind(IGLRenderableItem ri, IGLProgramShader shader, GLMatrixCalc c)     // called per renderable item..
            {
                group.Bind();
            }

            private GLBitmapGroup group;
        }
    }
}

