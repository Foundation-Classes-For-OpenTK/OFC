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

namespace OFC.GL4
{
    public static class GLMemoryBarrier
    {
        static public void All()
        {
            GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);
        }

        static public void StorageBuffers()        // shader storage buffers
        {
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);
        }

        static public void Atomics()            // Atomic counters written to by shaders
        {
            GL.MemoryBarrier(MemoryBarrierFlags.AtomicCounterBarrierBit);
        }

        static public void FrameBuffers()       // Frame buffer attachments
        {
            GL.MemoryBarrier(MemoryBarrierFlags.FramebufferBarrierBit);
        }

        static public void BufferSubData()       // Copy/SubData to buffers
        {
            GL.MemoryBarrier(MemoryBarrierFlags.BufferUpdateBarrierBit);
        }

        static public void Uniforms()           // Uniforms from buffer objects
        {
            GL.MemoryBarrier(MemoryBarrierFlags.UniformBarrierBit);
        }

        static public void Vertex()             // Vertex data from buffer objects written to by shaders
        {
            GL.MemoryBarrier(MemoryBarrierFlags.VertexAttribArrayBarrierBit);
        }
    }
}
