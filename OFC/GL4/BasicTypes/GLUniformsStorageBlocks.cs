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

namespace GLOFC.GL4
{
    /// <summary>
    /// GLDataBlock extends a GLBuffer and allows it to bind to a binding index 
    /// </summary>
    public abstract class GLDataBlock : GLBuffer
    {
        /// <summary> Binding index</summary>
        public int BindingIndex { get; private set; }

        /// <summary> Construct and bind to binding index on target</summary>
        public GLDataBlock(int bindingindex, bool std430, BufferRangeTarget tgr) : base(std430)
        {
            BindingIndex = bindingindex;
            Bind(BindingIndex, tgr);
        }
    }

    /// <summary>
    /// Uniform blocks - std140 only.  Blocks are global across shaders.  IDs really need to be unique or you will have to rebind 
    /// </summary>
    public class GLUniformBlock : GLDataBlock
    {
        /// <summary> Construct and bind to binding index</summary>
        public GLUniformBlock(int bindingindex) : base(bindingindex, false,  BufferRangeTarget.UniformBuffer)
        {
        }
    }

    /// <summary>
    /// Storage blocks - std140 and 430. Writable. Can perform Atomics.  Storage blocks are global across shaders.  IDs really need to be unique or you will have to rebind 
    /// </summary>
    public class GLStorageBlock : GLDataBlock
    {
        /// <summary> Construct and bind to binding index</summary>
        public GLStorageBlock(int bindingindex, bool std430 = false) : base(bindingindex, std430,  BufferRangeTarget.ShaderStorageBuffer)
        {
        }
    }

    /// <summary>
    /// Atomic counter blocks. Blocks are global across shaders.  IDs really need to be unique or you will have to rebind 
    /// </summary>
    public class GLAtomicBlock : GLDataBlock
    {
        /// <summary> Construct and bind to binding index</summary>
        public GLAtomicBlock(int bindingindex) : base(bindingindex, false, BufferRangeTarget.AtomicCounterBuffer)
        {
        }
    }
}

