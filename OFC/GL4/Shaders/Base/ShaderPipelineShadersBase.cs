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
using GLOFC.Utils;
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

        /// <summary>Is it successfully compiled</summary>
        public bool Compiled { get; private set; } = false;

        /// <summary>
        /// Compile and link the pipeline component.
        /// If you specify varyings, you must set up a buffer, and a start action of Gl.BindBuffer(GL.TRANSFORM_FEEDBACK_BUFFER,bufid) AND BeingTransformFeedback.
        /// </summary>
        /// <param name="shadertype">Shader type,Fragment, Vertex etc </param>
        /// <param name="codelisting">The code</param>
        /// <param name="compilerreport">Compiler and linker report </param>
        /// <param name="constvalues">List of constant values to use. Set of {name,value} pairs</param>
        /// <param name="varyings">List of varyings to report</param>
        /// <param name="varymode">How to write the varying to the buffer</param>
        /// <param name="saveable">True if want to save to binary</param>
        /// <param name="completeoutfile">If non null, output the post processed code listing to this file</param>
        ///<returns>true if shader compiled and linked, even if compiler reported text</returns>
        protected bool CompileLink(ShaderType shadertype, string codelisting, out string compilerreport, 
                                        object[] constvalues = null, string[] varyings = null, TransformFeedbackMode varymode = TransformFeedbackMode.InterleavedAttribs,
                                        bool saveable = false,
                                        string completeoutfile = null)
        {
            Compiled = false;

            Program = new GLProgram();
            bool ret = Program.Compile(shadertype, codelisting, out compilerreport, constvalues, completeoutfile);

            if (compilerreport.HasChars())
            {
                compilerreport = $"Pipeline shader compiler report for {GetType().Name}: {compilerreport}";
                System.Diagnostics.Trace.WriteLine(compilerreport);
                GLShaderLog.Add(compilerreport);
            }

            GLShaderLog.Okay = ret;     // upate global Okay flag

            if (ret == false)
            {
                if (GLShaderLog.AssertOnError)
                    System.Diagnostics.Trace.Assert(ret, "", compilerreport);     // note use of trace so its asserts even in release
                return ret;
            }

            ret = Program.Link(out string linkerreport, separable: true, varyings, varymode, saveable);

            if (linkerreport.HasChars())
            {
                linkerreport = $"Pipeline shader linker report for {GetType().Name}: {linkerreport}";
                compilerreport = compilerreport.AppendPrePad(linkerreport, Environment.NewLine);
                System.Diagnostics.Trace.WriteLine(linkerreport);
                GLShaderLog.Add(linkerreport);
            }

            if (ret == false && GLShaderLog.AssertOnError)
                System.Diagnostics.Trace.Assert(ret, "", linkerreport);

            Compiled = ret;
            GLShaderLog.Okay = ret;     // upate global Okay flag

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
