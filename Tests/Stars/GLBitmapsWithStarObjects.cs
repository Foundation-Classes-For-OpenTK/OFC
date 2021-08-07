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

using OFC.GL4;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace TestOpenTk
{
    // Class can hold varying number of bitmaps, all of the same size, each can be rotated/sized/lookat individually.
    // can be alpha blended either by distance in or out. See GLPLVertexShaderQuadTextureWithMatrixTranslation
    // You can delete by tag name or clear all
    // Holds as many text bitmaps as you need, it will grow to fit. It won't shrink, but it will reused deleted slot.

    public class GLBitmapsWithStarObjects : GLBitmaps
    {
        public override bool Enable { get { return base.Enable; } set { sunshader.Enable = base.Enable = value; } }
        public Matrix4 ModelTranslation { get { return vertshader.ModelTranslation; } set { vertshader.ModelTranslation = value; } }
        public float TimeDeltaSpots { get { return fragshader.TimeDeltaSpots; } set { fragshader.TimeDeltaSpots = value; } }
        public float TimeDeltaSurface { get { return fragshader.TimeDeltaSurface; } set { fragshader.TimeDeltaSurface = value; } }

        public GLBitmapsWithStarObjects(string name, GLRenderProgramSortedList rlist, Size bitmapsize, 
                                        Vector3 staroffset, float starsize,
                                        int mipmaplevels = 3, bool cullface = true, bool depthtest = true, int maxpergroup = int.MaxValue,
                                        float sunspots = 0.4f ) :
            base(name, rlist,bitmapsize,mipmaplevels,cullface,depthtest,maxpergroup)
        {
            renderlist = rlist;

            matrixbuffers.AddedNewGroup += AddedNewGroup;       // call back to make a new group..

            fragshader = new GLPLStarSurfaceFragmentShader();
            fragshader.Scutoff = sunspots;          // control spottyness

            vertshader = new GLPLVertexShaderModelCoordWithMatrixWorldTranslationCommonModelTranslation();
            vertshader.WorldPositionOffset = staroffset;        // set offset of stars with respect to bitmaps

            sunshader = new GLShaderPipeline(vertshader, fragshader);
            items.Add(sunshader);

            sunshapebuffer = new GLBuffer();
            sunshapebuffer.AllocateFill(GLSphereObjectFactory.CreateSphereFromTriangles(3, starsize));
            items.Add(sunshapebuffer);

            rt = GLRenderControl.Tri();
            rt.ClipDistanceEnable = 1;  // we are going to cull primitives which are deleted
        }

        // add a bitmap, indicate if owned by class or you
        public override Tuple<int, int, int> Add(object tag,
                            Bitmap bmp,
                            int bmpmipmaplevels,
                            Vector3 starworldpos,       // is star pos
                            Vector3 size,       // Note if Y and Z are zero, then Z is set to same ratio to width as bitmap
                            Vector3 rotationradians,        // ignored if rotates are on
                            bool rotatetoviewer = false, bool rotateelevation = false,   // if set, rotationradians not used
                            float alphascale = 0, float alphaend = 0,
                            bool ownbitmap = false,
                            bool textvisible = true
                         )
        {
            // returning group, pos, count
            starworldpos -= vertshader.WorldPositionOffset;     // the matrix is used directly by bitmap, so we need to move it offset by star offset
            var gpc = base.Add(tag, bmp, bmpmipmaplevels, starworldpos, size, rotationradians, rotatetoviewer, rotateelevation, alphascale, alphaend, ownbitmap, textvisible);
            grouprenderlist[gpc.Item1].InstanceCount = gpc.Item3;
            return gpc;
        }

        private void AddedNewGroup(int groupno, GLBuffer matrixbuffer)      // callback due to new group added, we need a RI
        {
            GLRenderableItem ri = GLRenderableItem.CreateVector4Matrix4(items, rt, sunshapebuffer, matrixbuffer, 
                                    sunshapebuffer.Length/GLLayoutStandards.Vec4size, null, 0, 1);      // ic set by Add..
            renderlist.Add(sunshader, ri);
            grouprenderlist.Add(ri);
        }


        public override void Dispose()           // you can double dispose.
        {
            base.Dispose();
            items.Dispose();
        }

        private List<GLRenderableItem> grouprenderlist = new List<GLRenderableItem>();
        private GLItemsList items = new GLItemsList();      // we have our own item list, which is disposed when we dispose
        private GLRenderProgramSortedList renderlist;
        private GLShaderPipeline sunshader;
        private GLBuffer sunshapebuffer;
        private GLRenderControl rt;
        private GLPLVertexShaderModelCoordWithMatrixWorldTranslationCommonModelTranslation vertshader;
        private GLPLStarSurfaceFragmentShader fragshader;

    }
}

