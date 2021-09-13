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


using System;
using OpenTK.Graphics.OpenGL4;

namespace GLOFC.GL4
{
    // base class for all pipeline shaders parts. these have to be compiled into a ShaderPipeline

    public abstract class GLShaderPipelineComponentShadersBase : IGLPipelineComponentShader
    {
        public int Id { get { return Program.Id; } }
        protected GLProgram Program { get; private set; }

        protected void CompileLink( OpenTK.Graphics.OpenGL4.ShaderType st, string code, 
                                        Object[] constvalues = null, string[] varyings = null, TransformFeedbackMode varymode = TransformFeedbackMode.InterleavedAttribs,
                                        bool saveable = false,
                                        string auxname = "", string completeoutfile = null)
        {
            Program = new GLProgram();
            string ret = Program.Compile(st, code, constvalues, completeoutfile);
            System.Diagnostics.Debug.Assert(ret == null, auxname, ret);
            ret = Program.Link(separable: true, varyings, varymode,saveable);
            System.Diagnostics.Debug.Assert(ret == null, auxname, ret);
        }

        public byte[] GetBinary(out BinaryFormat binformat)     // must have linked with wantbinary
        {
            return Program.GetBinary(out binformat);
        }

        public void Load(byte[] bin, BinaryFormat binformat)
        {
            Program = new GLProgram(bin, binformat);
        }

        public virtual void Start(GLMatrixCalc c)
        {
        }

        public virtual void Finish()
        {
        }

        public virtual void Dispose()
        {
            Program.Dispose();
        }
    }
}
