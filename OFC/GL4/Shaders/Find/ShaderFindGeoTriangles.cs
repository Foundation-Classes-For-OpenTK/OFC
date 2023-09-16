/*
 * Copyright 2019-2022 Robbyxp1 @ github.com
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

using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace GLOFC.GL4.Shaders.Geo
{
    /// <summary>
    /// This namespace contains pipeline geo shaders.
    /// </summary>
    internal static class NamespaceDoc { } // just for documentation purposes


    /// <summary>
    /// Geo shader, find triangle under cursor. Combine with your chosen vertex shader feeding in ProjectionModelMatrix values
    /// using a RenderableItem. Call SetScreenCoords before render executes 
    /// </summary>

    public class GLPLGeoShaderFindTriangles : GLShaderPipelineComponentShadersBase
    {
        /// <summary> Access to the buffer used to store search results </summary>
        public GLStorageBlock VectorOutBuffer { get; set; }
        /// <summary> Access to the debug buffer if debug buffering is enabled, else null</summary>
        public GLStorageBlock DebugCalcBuffer { get; set; }

        /// <summary>
        /// Constructor
        /// Requires:
        ///     gl_in positions triangles
        ///     2 : instance[] instance number. 
        ///     4 : drawid[] instance number. 
        /// output:
        ///     To buffer bound structure Positions
        /// Optional call SetGroup to pass in a group number for the results to pass it back out
        /// Call GetResult after Executing the shader/RI combo. 
        /// </summary>
        /// <param name="buffer">Storage Buffer to place results in</param>
        /// <param name="maximumresultsp">Maximum number of results</param>
        /// <param name="forwardfacing">Triangles are forward facing</param>
        /// <param name="obeyculldistance">Reject primitives with cull distance less than 0</param>
        /// <param name="debugbuffer">Pass in a debug buffer if require debug output</param>
        public GLPLGeoShaderFindTriangles(GLStorageBlock buffer, int maximumresultsp, bool forwardfacing = true, bool obeyculldistance = true, GLStorageBlock debugbuffer = null)
        {
            maximumresults = maximumresultsp;

            int sizeneeded = 16 + sizeof(float) * 4 * maximumresults;
            if (buffer.Length < sizeneeded)
                buffer.AllocateBytes(sizeneeded);
            VectorOutBuffer = buffer;

            DebugCalcBuffer = debugbuffer;

            CompileLink(ShaderType.GeometryShader, Code(), out string unused,
                                constvalues: new object[] { "bindingoutdata", VectorOutBuffer.BindingIndex, "maximumresults", maximumresults, "forwardfacing", forwardfacing , 
                                            "bindingdebug" , debugbuffer != null ? debugbuffer.BindingIndex : 0, "debugoutput" , debugbuffer!=null  , "obeyculldistance", obeyculldistance});
        }

        /// <summary>
        /// Set up screen coords ready for renderitem execute
        /// </summary>
        /// <param name="cursorpos">Cursor position in viewport</param>
        /// <param name="windowsize">Size of window</param>
        /// <param name="margin">Size of find margin, the wider, the less accurate the position needs to be</param>
        // Set up screen coords for find
        public void SetScreenCoords(Point cursorpos, Size windowsize, int margin = 10)
        {
            Vector4 v = new Vector4((float)cursorpos.X / (windowsize.Width / 2) - 1.0f, 1.0f - (float)cursorpos.Y / (windowsize.Height / 2), 0, 0);   // convert to clip space
            GL.ProgramUniform4(Id, 10, v);
            float pixd = (float)(margin / (float)((windowsize.Width + windowsize.Height) / 2 / 2));
            GL.ProgramUniform1(Id, 11, pixd);

            //System.Diagnostics.Debug.WriteLine($"Geo Find set Cursor {cursorpos} windowsize {windowsize} margin {margin} pixd area {pixd} U10={v}");
        }

        /// <summary>
        /// Setting this value OR in this group ID with the gl_drawid to return extra info 
        /// </summary>
        public void SetGroup(int g)
        {
            GL.ProgramUniform1(Id, 12, g);
        }

        /// <summary> Start shader, called by execute </summary>
        public override void Start(GLMatrixCalc c)
        {
            base.Start(c);
            VectorOutBuffer.ZeroBuffer();
            if ( DebugCalcBuffer != null )
                DebugCalcBuffer.ZeroBuffer();
        }

        /// <summary>
        /// Get results, sorted so lowest z is first.
        /// </summary>
        /// <returns>null or vec4 array. Each vec4: PrimitiveID (-1 if whole detect), InstanceID, average Z of triangle points, draw ID | group</returns>
        public Vector4[] GetResult()
        {
            GLMemoryBarrier.All();

            VectorOutBuffer.StartRead(0);
            int count = Math.Min(VectorOutBuffer.ReadInt(), maximumresults);       // atomic counter keeps on going if it exceeds max results, so limit to it
            int examined = VectorOutBuffer.ReadInt();       // atomic counter keeps on going if it exceeds max results, so limit to it

            if (DebugCalcBuffer != null)
            {
                System.Diagnostics.Debug.WriteLine($"Geo Find results sorted found {count} checked {examined}");

                int cstride = 8;
                DebugCalcBuffer.StartRead(0);
                int calccount = DebugCalcBuffer.ReadInt() * cstride;
                var carray = DebugCalcBuffer.ReadVector4s(calccount);
                DebugCalcBuffer.StopReadWrite();
                for (int j = 0; j < calccount; j++)
                {
                    if (j % cstride == 0)
                        System.Diagnostics.Debug.WriteLine($"Find Debug Set {j / cstride}");

                    System.Diagnostics.Debug.WriteLine($"     {carray[j]}");
                }
            }

            Vector4[] d = null;

            if (count > 0)
            {
                d = VectorOutBuffer.ReadVector4s(count);      // align 16 for vec4

                Array.Sort(d, delegate (Vector4 left, Vector4 right) { return left.Z.CompareTo(right.Z); });

                if (DebugCalcBuffer != null)
                {
                    for (int i = 0; i < d.Length; i++) 
                        System.Diagnostics.Debug.WriteLine($"  {i} PrimId {d[i].X} Instance {d[i].Y} avgz {d[i].Z} drawid|grp {d[i].W}");
                }
            }

            VectorOutBuffer.StopReadWrite();
            return d;
        }

        private string Code()
        {
            return
@"
#version 460 core
#include UniformStorageBlocks.matrixcalc.glsl
#include Shaders.Functions.trig.glsl

layout (triangles) in;               // triangles come in
layout (triangle_strip) out;        // norm op is not to sent them on
layout (max_vertices=3) out;	    // 1 triangle max

layout (location = 2) in flat int instance[];        // vertex shader pass in instance
layout (location = 4) in flat int drawid[];          // vertex shader may pass in drawid

layout (location = 10) uniform vec4 screencoords;
layout (location = 11) uniform float pointdist;
layout (location = 12) uniform int group;

in gl_PerVertex
{
    vec4 gl_Position;
    float gl_PointSize;
    float gl_ClipDistance[];
    float gl_CullDistance[];
   
} gl_in[];

out gl_PerVertex 
{
    vec4 gl_Position;
    float gl_PointSize;
    float gl_ClipDistance[];
};

const int bindingoutdata = 1;
layout (binding = bindingoutdata, std430) buffer Positions      // StorageBlock - holds results
{
    uint count;     // ones found
    uint examined;  // total triangles checked
    vec4 values[];
};

const int bindingdebug = 2;
layout (binding = bindingdebug, std430) buffer CalcData      // Debug side buffer
{
    uint calccount;
    vec4 calcdata[];
};

const int maximumresults = 1;       // is overriden by compiler feed in const
const bool forwardsfacing = true;   // compiler overriden
const bool obeyculldistance = false;    // obey cull distance
const bool debugoutput = false;    // debug output on

void main(void)
{
    if ( obeyculldistance && gl_in[0].gl_CullDistance[0] < 0)       // if we are obeying clipping, check it, and if true, ignore the primitive
    {
        if ( debugoutput && gl_PrimitiveIDIn==0)
        {
            uint pos = atomicAdd(calccount,1)*8;
            calcdata[pos+0] = gl_in[0].gl_Position;
            calcdata[pos+1] = gl_in[1].gl_Position;
            calcdata[pos+2] = gl_in[2].gl_Position;
            calcdata[pos+3] = vec4(0,0,0,0);
            calcdata[pos+4] = vec4(0,0,0,0);
            calcdata[pos+5] = vec4(0,0,0,0);
            calcdata[pos+6] = screencoords;
            calcdata[pos+7] = vec4(9999,instance[0],drawid[0],gl_in[0].gl_CullDistance[0]);
        }
    }
    else
    {    
        vec4 p0 = gl_in[0].gl_Position / gl_in[0].gl_Position.w;        // normalise w to produce screen pos in x/y, +/- 1
        vec4 p1 = gl_in[1].gl_Position / gl_in[1].gl_Position.w;        
        vec4 p2 = gl_in[2].gl_Position / gl_in[2].gl_Position.w;

        if ( debugoutput )
            atomicAdd(examined,1); // for debug only

        if ( !forwardsfacing || PMSquareS(p0,p1,p2) < 0 )     // if wound okay, so its forward facing (p0->p1 vector, p2 is on the right)
        {
            // only check for approximate cursor position on first triangle of primitive (if small, all would respond)wa

            if ( gl_PrimitiveIDIn == 0 && abs(p0.x-screencoords.x) < pointdist && abs(p0.y-screencoords.y) < pointdist )
            {
                uint ipos = atomicAdd(count,1);
                if ( ipos < maximumresults )
                {
                    float avgz = (p0.z+p1.z+p2.z)/3;
                    values[ipos] = vec4(-1,instance[0],avgz,drawid[0] | group);

                    if ( debugoutput )
                    {
                        uint pos = atomicAdd(calccount,1)*8;
                        calcdata[pos+0] = p0;
                        calcdata[pos+1] = p1;
                        calcdata[pos+2] = p2;
                        calcdata[pos+3] = screencoords;
                        calcdata[pos+4] = vec4(abs(p0.x-screencoords.x), abs(p0.y-screencoords.y), pointdist , 0 );
                        calcdata[pos+5] = vec4(pointdist-abs(p0.x-screencoords.x), pointdist-abs(p0.y-screencoords.y), 0 , 0 );
                        calcdata[pos+7] = vec4(-1,instance[0],drawid[0],gl_in[0].gl_CullDistance[0]);
                    }
                }
            }
            else 
            {
                if ( p0.z > -1 && p1.z > -1 && p2.z > -1 && p0.z <1 && p1.z < 1 && p2.z < 1)       // all must be on screen
                {
                    float p0s = PMSquareS(p0,p1,screencoords);      // perform point to line detection on all three lines
                    float p1s = PMSquareS(p1,p2,screencoords);
                    float p2s = PMSquareS(p2,p0,screencoords);

                    if ( p0s == p1s && p0s == p2s)      // all signs agree, its within the triangle
                    {
                        uint ipos = atomicAdd(count,1);     // this keeps on going even if we exceed max results, the results are just not stored
                        if ( ipos < maximumresults )
                        {
                            float avgz = (p0.z+p1.z+p2.z)/3;
                            values[ipos] = vec4(gl_PrimitiveIDIn,instance[0],avgz,drawid[0] | group);

                            if ( debugoutput )
                            {
                                uint pos = atomicAdd(calccount,1)*8;
                                calcdata[pos+0] = p0;
                                calcdata[pos+1] = p1;
                                calcdata[pos+2] = p2;
                                calcdata[pos+3] = screencoords;
                                calcdata[pos+7] = vec4(gl_PrimitiveIDIn,instance[0],drawid[0],gl_in[0].gl_CullDistance[0]);
                            }
                        }
                    }
                }   
            }
        }
    }
}
";
        }

        private int maximumresults;
    }

}

