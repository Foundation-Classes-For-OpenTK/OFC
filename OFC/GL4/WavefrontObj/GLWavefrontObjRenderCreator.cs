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

using GLOFC.GL4.Shaders.Basic;
using GLOFC.Utils;
using GLOFC.WaveFront;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
using System.Drawing;

#pragma warning disable 1591

namespace GLOFC.GL4
{
    // class takes a list of waveform objects, and creates shaders/renderable items to represent them, adding them
    // to the GLItemsList and GLRenderProgramSortedList.

    public class GLWavefrontObjCreator
    {
        public Color DefaultColor { get; set; } = Color.Transparent;

        private GLItemsList items;
        private GLRenderProgramSortedList rlist;
        private GLUniformColorShaderWithObjectTranslation shadercolor = null;
        private GLTexturedShaderWithObjectTranslation shadertexture = null;

        // give the item store and the render list to add to.

        public GLWavefrontObjCreator(GLItemsList itemsp, GLRenderProgramSortedList rlistp)
        {
            items = itemsp;
            rlist = rlistp;
        }

        // may use multiple creates on the same GLWaveFormObjCreater object

        public bool Create(List<GLWaveformObject> objects, Vector3 worldpos, Vector3 rotp, float scale = 1.0f)      
        {
            if (objects == null)
                return false;

            GLBuffer vert = null;
            GLRenderState rts = GLRenderState.Tri();
            bool okay = false;

            foreach (var obj in objects)
            {
                if (obj.Material.HasChars() && obj.Vertices.Vertices.Count > 0)
                {
                    if (vert == null)
                    {
                        vert = items.NewBuffer();
                        vert.AllocateFill(obj.Vertices.Vertices.ToArray(), obj.Vertices.TextureVertices2.ToArray());    // store all vertices and textures into 
                    }

                    bool textured = obj.Indices.TextureIndices.Count > 0;

                    string name = obj.Objectname != null ? obj.Objectname : obj.Groupname;  // name to use for texture/colour

                    if (textured)       // using textures need texture indicies
                    {
                        IGLTexture tex = items.Contains(obj.Material) ? items.Tex(obj.Material) : null;

                        if (tex == null)
                            return false;

                        if (shadertexture == null)
                        {
                            shadertexture = new GLTexturedShaderWithObjectTranslation();
                            items.Add(shadertexture);
                        }

                        obj.Indices.RefactorVertexIndiciesIntoTriangles();

                        var ri = GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Triangles, rts, vert, vert.Positions[0], vert.Positions[1], 0,
                                new GLRenderDataTranslationRotationTexture(tex, worldpos, rotp, scale));           // renderable item pointing to vert for vertexes

                        ri.CreateElementIndex(items.NewBuffer(), obj.Indices.VertexIndices.ToArray(), 0);       // using the refactored indexes, create an index table and use

                        rlist.Add(shadertexture, name, ri);
                        okay = true;
                    }
                    else
                    {                   // use the name as a colour.
                        Color c = Color.FromName(obj.Material);

                        if (c.A == 0 && c.R == 0 && c.G == 0 && c.B == 0)
                        {
                            if (DefaultColor != Color.Transparent)
                                c = DefaultColor;
                            else
                                return false;
                        }

                        if (shadercolor == null)
                        {
                            shadercolor = new GLUniformColorShaderWithObjectTranslation();
                            items.Add(shadercolor);
                        }

                        obj.Indices.RefactorVertexIndiciesIntoTriangles();

                        var ri = GLRenderableItem.CreateVector4(items, PrimitiveType.Triangles, rts, vert, 0, 0, new GLRenderDataTranslationRotationColor(c, worldpos, rotp, scale));           // renderable item pointing to vert for vertexes
                        ri.CreateElementIndex(items.NewBuffer(), obj.Indices.VertexIndices.ToArray(), 0);       // using the refactored indexes, create an index table and use

                        rlist.Add(shadercolor, name, ri);
                        okay = true;
                    }

                }
            }

            return okay;
        }
    }
}
