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
using OpenTK.Graphics.OpenGL4;

namespace GLOFC.GL4.Shaders
{
    /// <summary>
    /// Base class for all pipeline shaders parts. Derive from this to create a pipeline shader
    /// Once compiled and linked these are added to a GLShaderPipeline to form a complete shader
    /// </summary>

    [System.Diagnostics.DebuggerDisplay("P-Comp {Id} ref {References}")]
    public abstract class GLShaderPipelineComponentShadersBase : IGLPipelineComponentShader
    {
        /// <summary>GL ID</summary>
        public int Id { get { return Program.Id; } }
        /// <summary> Number of references for this component. If a pipeline component is shared between multiple pipeline shaders, this will be 2 or more</summary>
        public int References { get; set; } = 0;
        /// <summary> Program object </summary>
        protected GLProgram Program { get; private set; }

        /// <summary>
        /// Compile and link the pipeline component.
        /// If you specify varyings, you must set up a buffer, and a start action of Gl.BindBuffer(GL.TRANSFORM_FEEDBACK_BUFFER,bufid) AND BeingTransformFeedback.
        /// </summary>
        /// <param name="shadertype">Shader type,Fragment, Vertex etc </param>
        /// <param name="codelisting">The code</param>
        /// <param name="constvalues">List of constant values to use. Set of {name,value} pairs</param>
        /// <param name="varyings">List of varyings to report</param>
        /// <param name="varymode">How to write the varying to the buffer</param>
        /// <param name="saveable">True if want to save to binary</param>
        /// <param name="auxname">For reporting purposes on error, name to give to shader </param>
        /// <param name="completeoutfile">If non null, output the post processed code listing to this file</param>
        /// <param name="assertonerror">If set, trace assert on error</param>
        /// <returns>Null string if successful, or error text, if assert is disabled</returns>
        protected string CompileLink(ShaderType shadertype, string codelisting,
                                        object[] constvalues = null, string[] varyings = null, TransformFeedbackMode varymode = TransformFeedbackMode.InterleavedAttribs,
                                        bool saveable = false,
                                        string auxname = "", string completeoutfile = null, bool assertonerror = true)
        {
            Program = new GLProgram();
            string ret = Program.Compile(shadertype, codelisting, constvalues, completeoutfile);

            if (assertonerror)
                System.Diagnostics.Trace.Assert(ret == null, auxname, ret);     // note use of trace so its asserts even in release

            if (ret != null)
                return ret;

            ret = Program.Link(separable: true, varyings, varymode, saveable);

            if (assertonerror)
                System.Diagnostics.Trace.Assert(ret == null, auxname, ret);

            return ret;
        }

        /// <summary>Get the binary of the shader. Must have linked with wantbinary </summary>
        public byte[] GetBinary(out BinaryFormat binformat)
        {
            return Program.GetBinary(out binformat);
        }

        /// <summary> Load the binary into the shader </summary>
        public void Load(byte[] bin, BinaryFormat binformat)
        {
            Program = new GLProgram(bin, binformat);
        }

        /// <summary> Start action, override to intercept </summary>
        public virtual void Start(GLMatrixCalc c)
        {
        }

        /// <summary> Finish action, override to intercept </summary>
        public virtual void Finish()
        {
        }

        /// <summary> Dispose of the pipeline subcomponent </summary>
        public virtual void Dispose()
        {
            if (References <= 0)       // reference count error
                System.Diagnostics.Trace.WriteLine($"OFC Warning - Pipelineshader Ref Count {References} for {GetType().FullName}");
            References--;
            if (References == 0)
                Program.Dispose();
        }
    }
}
