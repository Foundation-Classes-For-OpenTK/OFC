﻿/*
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

namespace OFC.GL4
{
    // this takes a basic GLBuffer and adds on features for Uniform and storage blocks etc

    public abstract class GLDataBlock : GLBuffer
    {
        public int BindingIndex { get; private set; }

        public GLDataBlock(int bindingindex, bool std430, BufferRangeTarget tgr) : base(std430)
        {
            BindingIndex = bindingindex;
            Bind(BindingIndex, tgr);
        }
    }

    // uniform blocks - std140 only.  Uniform blocks are global across shaders.  IDs really need to be unique or you will have to rebind
    public class GLUniformBlock : GLDataBlock
    {
        public GLUniformBlock(int bindingindex) : base(bindingindex, false,  BufferRangeTarget.UniformBuffer)
        {
        }
    }

    // storage blocks - std140 and 430. Writable. Can perform Atomics.  Storage blocks are global across shaders.  IDs really need to be unique or you will have to rebind
    public class GLStorageBlock : GLDataBlock
    {
        public GLStorageBlock(int bindingindex, bool std430 = false) : base(bindingindex, std430,  BufferRangeTarget.ShaderStorageBuffer)
        {
        }
    }

    // atomic blocks. Storage blocks are global across shaders.  IDs really need to be unique or you will have to rebind
    public class GLAtomicBlock : GLDataBlock
    {
        public GLAtomicBlock(int bindingindex) : base(bindingindex, false, BufferRangeTarget.AtomicCounterBuffer)
        {
        }
    }
}

