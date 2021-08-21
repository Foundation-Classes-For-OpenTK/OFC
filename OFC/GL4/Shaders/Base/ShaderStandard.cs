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

namespace OFC.GL4
{
    // inherit from this is you have a shader which items makes its own set of vertext/fragment shaders all in one go, non pipelined
    // A program shader has a Start(), called by RenderableList when the shader is started
    // StartAction, an optional hook to supply more start functionality
    // A Finish() to clean up
    // FinishAction, an optional hook to supply more finish functionality

    // you can use the CompileAndLink() function to quickly compile and link multiple shaders

    public abstract class GLShaderStandard : IGLProgramShader
    {
        public int Id { get { return program.Id; } }
        public bool Enable { get; set; } = true;                        // if not enabled, no render items below it will be visible
        public virtual string Name { get { return "Standard:" + GetType().Name; } }     // override to give meaningful name

        public IGLShader Get(ShaderType t) { return this; }
        public Action<IGLProgramShader, GLMatrixCalc> StartAction { get; set; }
        public Action<IGLProgramShader> FinishAction { get; set; }

        protected GLProgram program;

        public GLShaderStandard()
        {
        }

        public GLShaderStandard(Action<IGLProgramShader, GLMatrixCalc> sa) : this()
        {
            StartAction = sa;
        }

        public GLShaderStandard(Action<IGLProgramShader, GLMatrixCalc> sa, Action<IGLProgramShader> fa) : this()
        {
            StartAction = sa;
            FinishAction = fa;
        }

        // Compile/link various shaders
        // you can give any combo, and any combo of const vars
        // if you specify varyings, you must set up a buffer, and a start action of Gl.BindBuffer(GL.TRANSFORM_FEEDBACK_BUFFER,bufid) AND BeingTransformFeedback.

        public void CompileLink( string vertex=null, string tcs=null, string tes=null, string geo=null, string frag=null, 
                                 object[] vertexconstvars = null , object[] tcsconstvars = null, object[] tesconstvars = null, object[] geoconstvars = null, object[] fragconstvars = null,
                                 string[] varyings = null, TransformFeedbackMode varymode = TransformFeedbackMode.InterleavedAttribs
                                )
        {
            program = new OFC.GL4.GLProgram();
            string ret;

            if (vertex != null)
            {
                ret = program.Compile(OpenTK.Graphics.OpenGL4.ShaderType.VertexShader, vertex, vertexconstvars);
                System.Diagnostics.Debug.Assert(ret == null, "Vertex Shader", ret);
            }

            if (tcs != null)
            {
                ret = program.Compile(OpenTK.Graphics.OpenGL4.ShaderType.TessControlShader, tcs, tcsconstvars);
                System.Diagnostics.Debug.Assert(ret == null, "Tesselation Control Shader", ret);
            }

            if (tes != null)
            {
                ret = program.Compile(OpenTK.Graphics.OpenGL4.ShaderType.TessEvaluationShader, tes , tesconstvars );
                System.Diagnostics.Debug.Assert(ret == null, "Tesselation Evaluation Shader", ret);
            }

            if (geo != null)
            {
                ret = program.Compile(OpenTK.Graphics.OpenGL4.ShaderType.GeometryShader, geo, geoconstvars);
                System.Diagnostics.Debug.Assert(ret == null, "Geometry shader", ret);
            }

            if (frag != null)
            {
                ret = program.Compile(OpenTK.Graphics.OpenGL4.ShaderType.FragmentShader, frag ,fragconstvars);
                System.Diagnostics.Debug.Assert(ret == null, "Fragment Shader", ret);
            }

            if ( varyings != null )
            {
                GL.TransformFeedbackVaryings(program.Id, varyings.Length, varyings, varymode);      // this indicate varyings.
            }

            ret = program.Link();
            System.Diagnostics.Debug.Assert(ret == null, "Link", ret);

            OFC.GLStatics.Check();
        }

        public virtual void Start(GLMatrixCalc c)     
        {
            GL.UseProgram(Id);
            StartAction?.Invoke(this,c);
        }

        public virtual void Finish()                 
        {
            FinishAction?.Invoke(this);                           // any shader hooks get a chance.
        }

        public virtual void Dispose()
        {
            program.Dispose();
        }

    }
}
