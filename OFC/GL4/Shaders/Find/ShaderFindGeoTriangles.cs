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

        public GLPLGeoShaderFindTriangles(GLStorageBlock buffer, int maximumresultsp, bool forwardfacing = true)
        {
            maximumresults = maximumresultsp;
            int sizeneeded = 16 + sizeof(float) * 4 * maximumresults;
            if (buffer.Length < sizeneeded)
                buffer.AllocateBytes(sizeneeded);
            vecoutbuffer = buffer;
            CompileLink(ShaderType.GeometryShader, Code(false), out string unused,
                                constvalues: new object[] { "bindingoutdata", vecoutbuffer.BindingIndex, "maximumresults", maximumresults, "forwardfacing", forwardfacing });
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
            //   System.Diagnostics.Debug.WriteLine("Set CP {0} Pixd {1}", v , pixd);
            GL.ProgramUniform1(Id, 11, pixd);
        }

        /// <summary>
        /// Setting this value OR in this group ID with the index to return extra info 
        /// </summary>
        public void SetGroup(int g)
        {
            GL.ProgramUniform1(Id, 12, g);
        }

        /// <summary> Start shader, called by execute </summary>
        public override void Start(GLMatrixCalc c)
        {
            base.Start(c);
            vecoutbuffer.ZeroBuffer();
        }

        /// <summary>
        /// Get results
        /// </summary>
        /// <returns>null or vec4 array. Each vec4: PrimitiveID, InstanceID, average Z of triangle points, draw ID | group</returns>
        public Vector4[] GetResult()
        {
            GLMemoryBarrier.All();

            vecoutbuffer.StartRead(0);
            int count = Math.Min(vecoutbuffer.ReadInt(), maximumresults);       // atomic counter keeps on going if it exceeds max results, so limit to it

            Vector4[] d = null;

            if (count > 0)
            {
                d = vecoutbuffer.ReadVector4s(count);      // align 16 for vec4
                Array.Sort(d, delegate (Vector4 left, Vector4 right) { return left.Z.CompareTo(right.Z); });
            }

            vecoutbuffer.StopReadWrite();
            return d;
        }

        private string Code(bool passthru)
        {
            return
@"
#version 450 core
#include UniformStorageBlocks.matrixcalc.glsl
#include Shaders.Functions.trig.glsl

layout (triangles) in;               // triangles come in
layout (triangle_strip) out;        // norm op is not to sent them on
layout (max_vertices=3) out;	    // 1 triangle max

layout (location = 2) in int instance[];        // vertex shader pass in instance
layout (location = 4) in int drawid[];          // vertex shader may pass in drawid

layout (location = 10) uniform vec4 screencoords;
layout (location = 11) uniform float pointdist;
layout (location = 12) uniform int group;

in gl_PerVertex
{
    vec4 gl_Position;
    float gl_PointSize;
    float gl_ClipDistance[];
   
} gl_in[];

out gl_PerVertex 
{
    vec4 gl_Position;
    float gl_PointSize;
    float gl_ClipDistance[];
};

// shows how to pass thru modelpos..
layout (location = 1) in vec3[] modelpos;
out MP
{
    layout (location = 1) out vec3 modelpos;
} MPOUT;


const int bindingoutdata = 20;
layout (binding = bindingoutdata, std430) buffer Positions      // StorageBlock note - buffer
{
    uint count;
    vec4 values[];
};

const int maximumresults = 1;       // is overriden by compiler feed in const
const bool forwardsfacing = true;   // compiler overriden

void main(void)
{
" + (passthru ? // pass thru is for testing purposes only
@"
        gl_Position = gl_in[0].gl_Position;
        MPOUT.modelpos =modelpos[0];
        EmitVertex();

        gl_Position = gl_in[1].gl_Position;
        MPOUT.modelpos =modelpos[1];
        EmitVertex();

        gl_Position = gl_in[2].gl_Position;
        MPOUT.modelpos =modelpos[2];
        EmitVertex();
        EndPrimitive();
" : "") +
@"
        vec4 p0 = gl_in[0].gl_Position / gl_in[0].gl_Position.w;        // normalise w to produce screen pos in x/y, +/- 1
        vec4 p1 = gl_in[1].gl_Position / gl_in[1].gl_Position.w;        
        vec4 p2 = gl_in[2].gl_Position / gl_in[2].gl_Position.w;

        if ( !forwardsfacing || PMSquareS(p0,p1,p2) < 0 )     // if wound okay, so its forward facing (p0->p1 vector, p2 is on the right)
        {
            // only check for approximate cursor position on first triangle of primitive (if small, all would respond)

            if ( gl_PrimitiveIDIn == 0 && abs(p0.x-screencoords.x) < pointdist && abs(p0.y-screencoords.y) < pointdist )
            {
                uint ipos = atomicAdd(count,1);
                if ( ipos < maximumresults )
                {
                    float avgz = (p0.z+p1.z+p2.z)/3;
                    values[ipos] = vec4(gl_PrimitiveIDIn,instance[0],avgz,drawid[0] | group);
                }
            }
            else 
            {
                if ( p0.z > 0 && p1.z > 0 && p2.z > 0 && p0.z <1 && p1.z < 1 && p2.z < 1)       // all must be on screen
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
                        }
                    }
                }   
            }
        }
}
";
        }



        private GLStorageBlock vecoutbuffer;
        private int maximumresults;
    }

}

