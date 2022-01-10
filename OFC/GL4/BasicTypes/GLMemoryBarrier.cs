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
using System.Drawing;

namespace GLOFC.GL4
{
    /// <summary>
    /// This class wraps GL memory barriers
    /// </summary>
    public static class GLMemoryBarrier
    {
        /// <summary>Wait till all have completed </summary>
        static public void All()
        {
            GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);
        }

        /// <summary>Wait till shader storage buffers have completed </summary>
        static public void StorageBuffers()     
        {
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);
        }

        /// <summary>Wait till atomic buffers have completed </summary>
        static public void Atomics()            
        {
            GL.MemoryBarrier(MemoryBarrierFlags.AtomicCounterBarrierBit);
        }

        /// <summary>Wait till frame buffers have completed </summary>
        static public void FrameBuffers()       
        {
            GL.MemoryBarrier(MemoryBarrierFlags.FramebufferBarrierBit);
        }

        /// <summary>Wait till buffer update have completed </summary>
        static public void BufferSubData()   
        {
            GL.MemoryBarrier(MemoryBarrierFlags.BufferUpdateBarrierBit);
        }

        /// <summary>Wait till buffer uniforms have completed </summary>
        static public void Uniforms()         
        {
            GL.MemoryBarrier(MemoryBarrierFlags.UniformBarrierBit);
        }

        /// <summary>Wait till vertex data from buffer objects writen by shaders have completed </summary>
        static public void Vertex()             
        {
            GL.MemoryBarrier(MemoryBarrierFlags.VertexAttribArrayBarrierBit);
        }
    }
}
