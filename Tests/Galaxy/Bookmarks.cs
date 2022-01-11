/*
 * Copyright © 2016 EDDiscovery development team
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
 * 
 * EDDiscovery is not affiliated with Frontier Developments plc.
 */

using BaseUtils;
using GLOFC.GL4;
using GLOFC.GL4.Shaders;
using GLOFC.GL4.Shaders.Fragment;
using GLOFC.GL4.Shaders.Geo;
using Newtonsoft.Json.Linq;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using TestOpenTk;

namespace EliteDangerousCore.EDSM
{
    public class Bookmarks
    {
        public void Create(GLItemsList items, GLRenderProgramSortedList rObjects, List<SystemClass> incomingsys, float bookmarksize, GLStorageBlock findbufferresults, bool depthtest)
        {
            if (ridisplay == null)
            {
                //var vert = new GLPLVertexScaleLookat(rotate: dorotate, rotateelevation: doelevation, commontransform: false, texcoords: true,      // a look at vertex shader
                //                                                          
                //                var vert = new GLPLVertexShaderWorldCoord();
                var vert = new GLPLVertexScaleLookat(rotate: dorotate, rotateelevation: doelevation, texcoords: true, generateworldpos:true,
                                                                autoscale: 500, autoscalemin: 1f, autoscalemax: 20f); // below 500, 1f, above 500, scale up to 20x


                const int texbindingpoint = 1;
                var frag = new GLPLFragmentShaderTexture(texbindingpoint);       // binding - simple texturer based on vs model coords

                objectshader = new GLShaderPipeline(vert, null, null, null, frag);
                items.Add(objectshader);

                var objtex = items.NewTexture2D("Bookmarktex", TestOpenTk.Properties.Resources.dotted2, OpenTK.Graphics.OpenGL4.SizedInternalFormat.Rgba8);

                objectshader.StartAction += (s, m) =>
                {
                    objtex.Bind(texbindingpoint);   // bind tex array to, matching above
                };

                bookmarkposbuf = items.NewBuffer();         // where we hold the vertexes for the suns, used by renderer and by finder

                GLRenderState rt = GLRenderState.Tri();
                rt.DepthTest = depthtest;

                bookmarksize *= 10;

                // 0 is model pos, 1 is world pos by a buffer, 2 is tex co-ords
                ridisplay = GLRenderableItem.CreateVector4Vector4Vector2(items, OpenTK.Graphics.OpenGL4.PrimitiveType.TriangleStrip, rt,
                                    GLShapeObjectFactory.CreateQuad2(bookmarksize, bookmarksize),         // quad2 4 vertexts as the model positions
                                    bookmarkposbuf, 0,       // world positions come from here - not filled as yet
                                    GLShapeObjectFactory.TexTriStripQuad,
                                    ic: 0, seconddivisor: 1);

                rObjects.Add(objectshader, "bookmarks", ridisplay);

                var geofind = new GLPLGeoShaderFindTriangles(findbufferresults, 16);//, forwardfacing:false);
                findshader = items.NewShaderPipeline(null, vert, null,null, geofind, null, null, null);

                // hook to modelworldbuffer, at modelpos and worldpos.  UpdateEnables will fill in instance count
                rifind = GLRenderableItem.CreateVector4Vector4Vector2(items, OpenTK.Graphics.OpenGL4.PrimitiveType.TriangleStrip, rt,
                                                                GLShapeObjectFactory.CreateQuad2(bookmarksize, bookmarksize),         // quad2 4 vertexts as the model positions
                                                                bookmarkposbuf, 0,
                                                                GLShapeObjectFactory.TexTriStripQuad,
                                                                ic: 0, seconddivisor: 1);

            }

            bookmarkposbuf.AllocateFill(incomingsys.Select(x => new Vector4((float)x.X, (float)x.Y, (float)x.Z, 1)).ToArray());
            ridisplay.InstanceCount = rifind.InstanceCount = incomingsys.Count;
        }

        public object Find(Point loc, GLRenderState state, Size viewportsize, out float z)
        {
            z = 0;

            if (!objectshader.Enable)
                return null;

            var geo = findshader.GetShader<GLPLGeoShaderFindTriangles>(OpenTK.Graphics.OpenGL4.ShaderType.GeometryShader);
            geo.SetScreenCoords(loc, viewportsize);

            rifind.Execute(findshader, state); // execute. Geoshader discards geometry by not outputting anything

            var res = geo.GetResult();
            if (res != null)
            {
                for (int i = 0; i < res.Length; i++) System.Diagnostics.Debug.WriteLine($"bk {i} {res[i]}");
                z = res[0].Z;
                return null;
            }

            return null;
        }


        private GLBuffer bookmarkposbuf;
        private GLRenderableItem ridisplay;
        private GLShaderPipeline objectshader;
        private GLShaderPipeline findshader;
        private GLRenderableItem rifind;
        private const bool dorotate = true;
        private const bool doelevation = false;

    }
}

