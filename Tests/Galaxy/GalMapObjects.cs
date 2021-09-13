﻿/*
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

using EliteDangerousCore.EDSM;
using GLOFC;
using GLOFC.GL4;
using OpenTK;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace TestOpenTk
{
    public class GalMapObjects
    {
        public GalMapObjects()
        {
        }

        public bool Enable { get { return objectshader.Enable; } set { textrenderer.Enable = objectshader.Enable = value; } }

        public void SetGalObjectTypeEnable(string id, bool state) { State[id] = state; UpdateEnables(); }
        public bool GetGalObjectTypeEnable(string id) { return !State.ContainsKey(id) || State[id] == true; }
        public void SetAllEnables(string settings)
        {
            string[] ss = settings.Split(',');
            int i = 0;
            foreach (var o in galmap.RenderableMapTypes)
            {
                State[o.Typeid] = i >= ss.Length || !ss[i].Equals("-");              // on if we don't have enough, or on if its not -
                i++;
            }
            UpdateEnables();
        }
        public string GetAllEnables()
        {
            string s = "";
            foreach (var o in galmap.RenderableMapTypes)
            {
                s += GetGalObjectTypeEnable(o.Typeid) ? "+," : "-,";
            }
            return s;
        }

        public void CreateObjects(GLItemsList items, GLRenderProgramSortedList rObjects, GalacticMapping galmap, int bufferfindbinding, bool depthtest)
        {
            this.galmap = galmap;

            // first gets the images and make a 2d array texture for them

            Bitmap[] images = galmap.RenderableMapTypes.Select(x => x.Image as Bitmap).ToArray();
            // 256 is defined normal size
            var objtex = new GLTexture2DArray(images, mipmaplevel: 1, genmipmaplevel: 3, bmpsize: new Size(256, 256), internalformat: OpenTK.Graphics.OpenGL4.SizedInternalFormat.Rgba8, alignment: ContentAlignment.BottomCenter);
            IGLTexture texarray = items.Add(objtex, "GalObjTex");

            // now build the shaders

            const int texbindingpoint = 1;
            var vert = new GLPLVertexScaleLookat(rotate: dorotate, rotateelevation: doelevation, commontransform: false,       // a look at vertex shader
                                                        autoscale: 500, autoscalemin: 1f, autoscalemax: 20f); // below 500, 1f, above 500, scale up to 20x
            var tcs = new GLPLTesselationControl(2f);
            tes = new GLPLTesselationEvaluateSinewave(0.2f, 1f);         // this uses the world position from the vertex scaler to position the image, w controls image + animation (b16)
            var frag = new GLPLFragmentShaderTexture2DDiscard(texbindingpoint);       // binding - takes image pos from tes. imagepos < 0 means discard

            objectshader = new GLShaderPipeline(vert, tcs, tes, null, frag);
            items.Add(objectshader);

            objectshader.StartAction += (s, m) =>
            {
                texarray.Bind(texbindingpoint);   // bind tex array to, matching above
            };

            // now the RenderControl for the objects

            GLRenderState rt = GLRenderState.Patches(4);
            rt.DepthTest = depthtest;

            // create a quad and all entries of the renderable map objects, zero at this point, with a zero instance count. UpdateEnables will fill it in later
            // but we need to give it the maximum buffer length at this point

            const float objsize = 10.0f;        // size of object on screen

            ridisplay = GLRenderableItem.CreateVector4Vector4(items, OpenTK.Graphics.OpenGL4.PrimitiveType.Patches, rt,
                                GLShapeObjectFactory.CreateQuad2(objsize, objsize),         // quad2 4 vertexts
                                new Vector4[galmap.RenderableMapObjects.Length],        // world positions
                                ic: 0, seconddivisor: 1);

            modelworldbuffer = items.LastBuffer();
            int modelpos = modelworldbuffer.Positions[0];
            worldpos = modelworldbuffer.Positions[1];

            rObjects.Add(objectshader, "galmapobj", ridisplay);

            // add a find shader to look them up

            var geofind = new GLPLGeoShaderFindTriangles(bufferfindbinding, 16);        // pass thru normal vert/tcs/tes then to geoshader for results
            items.Add(geofind);

            findshader = items.NewShaderPipeline(null, vert, tcs, tes, new GLPLGeoShaderFindTriangles(bufferfindbinding, 16), null, null, null);

            // hook to modelworldbuffer, at modelpos and worldpos.  UpdateEnables will fill in instance count
            rifind = GLRenderableItem.CreateVector4Vector4(items, OpenTK.Graphics.OpenGL4.PrimitiveType.Patches, GLRenderState.Patches(4), modelworldbuffer, modelpos, ridisplay.DrawCount,
                                                                            modelworldbuffer, worldpos, null, ic: 0, seconddivisor: 1);

            GLStatics.Check();

            // Text renderer for the labels

            textrenderer = new GLBitmaps("bm-galmapobjects", rObjects, new Size(128, 40), depthtest: depthtest, cullface: false, textureformat: OpenTK.Graphics.OpenGL4.SizedInternalFormat.Rgba8);
            items.Add(textrenderer);

            // now make the text up for all the objects above

            using (Font fnt = new Font("Arial", 8.5F))
            {
                using (StringFormat fmt = new StringFormat())
                {
                    fmt.Alignment = StringAlignment.Center;

                    var renderablegalmapobjects = galmap.RenderableMapObjects; // list of enabled entries

                    List<Vector3> posset = new List<Vector3>();

                    float offscale = objsize * (0.5f + (float)textrenderer.BitmapSize.Height / (float)textrenderer.BitmapSize.Width / 2);       // this is the nominal centre of the text bitmap, offset in Y to the object

                    for (int i = 0; i < renderablegalmapobjects.Length; i++)
                    {
                        var o = renderablegalmapobjects[i];
                        float offset = -offscale;

                        for (int j = 0; j < i; j++)     // look up previous ones and see if we labeled it before
                        {
                            var d1 = new Vector3(o.points[0].X, o.points[0].Y + offset, o.points[0].Z);
                            var d2 = posset[j];     // where it was placed.
                            var diff = d1 - d2;

                            if (diff.Length < offscale)        // close
                            {
                                if (offset > 0)         // if offset is positive, flip below and increase again
                                    offset = -offset - offscale;
                                else
                                    offset *= -1;       // flip over top
                                                        // System.Diagnostics.Debug.WriteLine($"close {renderablegalmapobjects[i].name} {d1} to {renderablegalmapobjects[j].name} {d2} {diff} select {offset}");
                            }
                        }

                        Vector3 pos = new Vector3(o.points[0].X, o.points[0].Y + offset, o.points[0].Z);
                        posset.Add(pos);
                        //System.Diagnostics.Debug.WriteLine($"{renderablegalmapobjects[i].name} at {pos} {offset}");

                        textrenderer.Add(o.id, o.name, fnt,
                            Color.White, Color.FromArgb(0, 255, 0, 255),
                            pos,
                            new Vector3(objsize, 0, 0), new Vector3(0, 0, 0), fmt: fmt, rotatetoviewer: dorotate, rotateelevation: doelevation,
                            alphafadescalar: -100, alphafadepos: 500); // fade in, alpha = 0 at >500, 1 at 400

                    }
                }
            }

            UpdateEnables();      // fill in worldpos's and update instance count, taking into 
        }

        private void UpdateEnables()           // rewrite the modelworld buffer with the ones actually enabled
        {
            modelworldbuffer.StartWrite(worldpos);                  // overwrite world positions

            var renderablegalmapobjects = galmap.RenderableMapObjects; // list of displayable entries
            indextoentry = new int[renderablegalmapobjects.Length];
            int mwpos = 0,entry=0;

            foreach (var o in renderablegalmapobjects)
            {
                bool en = GetGalObjectTypeEnable(o.galMapType.Typeid);
                if (en)
                {
                    modelworldbuffer.Write(new Vector4(o.points[0].X, o.points[0].Y, o.points[0].Z, o.galMapType.Index + (!o.galMapType.Animate ? 65536 : 0)));
                    indextoentry[mwpos++] = entry;
                }

                textrenderer.SetVisiblityRotation(o.id, en, dorotate, doelevation);
                entry++;
            }

            modelworldbuffer.StopReadWrite();
            //var f = modelworldbuffer.ReadVector4(worldpos, renderablegalmapobjects.Count());  foreach (var v in f) System.Diagnostics.Debug.WriteLine("Vector " + v);
            ridisplay.InstanceCount = rifind.InstanceCount = mwpos;
        }


        public GalacticMapObject FindPOI(Point viewportloc, GLRenderState state, Size viewportsize)
        {
            if (!objectshader.Enable)
                return null;

            var geo = findshader.GetShader<GLPLGeoShaderFindTriangles>(OpenTK.Graphics.OpenGL4.ShaderType.GeometryShader);
            geo.SetScreenCoords(viewportloc, viewportsize);

            GLStatics.Check();
            rifind.Execute(findshader, state, discard:true); // execute, discard

            var res = geo.GetResult();
            if (res != null)
            {
//                for (int i = 0; i < res.Length; i++) System.Diagnostics.Debug.WriteLine(i + " = " + res[i]);

                int instance = (int)res[0].Y;
                // tbd wrong! not a one to one mapping
                return galmap.RenderableMapObjects[indextoentry[instance]];       //TBD
            }

            return null;
        }

        public void Update(ulong time, float eyedistance)
        {
            const int rotperiodms = 5000;
            time = time % rotperiodms;
            float fract = (float)time / rotperiodms;
     //       System.Diagnostics.Debug.WriteLine("Time " + time + "Phase " + fract);
            tes.Phase = fract;
        }

        private GLPLTesselationEvaluateSinewave tes;
        private GLShaderPipeline objectshader;
        private GLBuffer modelworldbuffer;
        private int worldpos;
        private GLRenderableItem ridisplay;

        private GLBitmaps textrenderer;     // gmo names

        private GLShaderPipeline findshader;
        private GLRenderableItem rifind;
        private int[] indextoentry;
        private Dictionary<string, bool> State { get; set; } = new Dictionary<string, bool>();       // if not present, its on, else state 
        private GalacticMapping galmap;

        private const bool dorotate = true;
        private const bool doelevation = false;
    }

}
