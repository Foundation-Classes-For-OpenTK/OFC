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
    /// Inherit from this if you have a shader which items makes its own set of vertext/fragment shaders all in one go, non pipelined
    /// A program shader has a Start(), called by RenderableList when the shader is started
    /// StartAction, an optional hook to supply more start functionality
    /// A Finish() to clean up
    /// FinishAction, an optional hook to supply more finish functionality
    /// You can use the CompileAndLink() function to quickly compile and link multiple shaders
    /// </summary>

    public abstract class GLShaderStandard : IGLProgramShader
    {
        /// <summary> GL ID</summary>
        public int Id { get { return Program.Id; } }
        /// <summary> Program object</summary>
        public GLProgram Program { get; private set; }
        /// <summary> If Enabled</summary>
        public bool Enable { get; set; } = true;                        // if not enabled, no render items below it will be visible

        /// <summary>Is it successfully compiled</summary>
        public bool Compiled { get; private set; } = false;

        /// <summary>Optional Render state. The shader can order a render state instead of each renderable item having one, if required </summary>
        public GLRenderState RenderState { get; set; }

        /// <summary>Name of shader. Standard name is type name, override to give a better name if required </summary>
        public virtual string Name { get { return "Standard:" + GetType().Name; } }     // override to give meaningful name

        /// <summary> Get shader. Type is irrelevant. </summary>
        public IGLShader GetShader(ShaderType t) { return this; }
        /// <summary>Not implemented</summary>
        public T GetShader<T>(ShaderType t) where T : IGLShader { throw new NotImplementedException(); }    // get a subcomponent of type T. Excepts if not present
        /// <summary> Not implemented</summary>
        public T GetShader<T>() where T : IGLShader { throw new NotImplementedException(); }    // get a subcomponent of type T. Excepts if not present

        /// <summary> Start Action callback </summary>
        public Action<IGLProgramShader, GLMatrixCalc> StartAction { get; set; }
        /// <summary> Finish Action callback </summary>
        public Action<IGLProgramShader> FinishAction { get; set; }

        /// <summary> Default Constructor </summary>
        public GLShaderStandard()
        {
        }

        /// <summary> Constructor setting up start action and optionally finish action</summary>
        public GLShaderStandard(Action<IGLProgramShader, GLMatrixCalc> sa, Action<IGLProgramShader> fa = null) : this()
        {
            StartAction = sa;
            FinishAction = fa;
        }

        /// <summary>
        /// Compile and link the pipeline component. 
        /// If you specify varyings, you must set up a buffer, and a start action of Gl.BindBuffer(GL.TRANSFORM_FEEDBACK_BUFFER,bufid) AND BeingTransformFeedback.
        /// </summary>
        /// <param name="vertex">The code for this shader type, or null</param>
        /// <param name="tcs">The code for this shader type, or null</param>
        /// <param name="tes">The code for this shader type, or null</param>
        /// <param name="geo">The code for this shader type, or null</param>
        /// <param name="frag">The code for this shader type, or null</param>
        /// <param name="vertexconstvars">List of constant values to use. Set of {name,value} pairs</param>
        /// <param name="tcsconstvars">List of constant values to use. Set of {name,value} pairs</param>
        /// <param name="tesconstvars">List of constant values to use. Set of {name,value} pairs</param>
        /// <param name="geoconstvars">List of constant values to use. Set of {name,value} pairs</param>
        /// <param name="fragconstvars">List of constant values to use. Set of {name,value} pairs</param>
        /// <param name="varyings">List of varyings to report</param>
        /// <param name="varymode">How to write the varying to the buffer</param>
        /// <param name="saveable">True if want to save to binary</param>
        /// <param name="completeoutfile">If non null, save compiler source to this root namd and extension</param>
        ///<returns>true if shader compiled and linked, even if compiler reported text</returns>

        public bool CompileLink(string vertex = null, string tcs = null, string tes = null, string geo = null, string frag = null,
                                 object[] vertexconstvars = null, object[] tcsconstvars = null, object[] tesconstvars = null, object[] geoconstvars = null, object[] fragconstvars = null,
                                 string[] varyings = null, TransformFeedbackMode varymode = TransformFeedbackMode.InterleavedAttribs, bool saveable = false, 
                                 string completeoutfile = null
                                )
        {
            return CompileLink(out string unused, vertex, tcs, tes, geo, frag,
                                vertexconstvars, tcsconstvars, tesconstvars, geoconstvars, fragconstvars, 
                                varyings, varymode, saveable, completeoutfile);
        }


        /// <summary>
        /// Compile and link the pipeline component. 
        /// If you specify varyings, you must set up a buffer, and a start action of Gl.BindBuffer(GL.TRANSFORM_FEEDBACK_BUFFER,bufid) AND BeingTransformFeedback.
        /// </summary>
        /// <param name="compilerreport">Compiler and linker report </param>
        /// <param name="vertex">The code for this shader type, or null</param>
        /// <param name="tcs">The code for this shader type, or null</param>
        /// <param name="tes">The code for this shader type, or null</param>
        /// <param name="geo">The code for this shader type, or null</param>
        /// <param name="frag">The code for this shader type, or null</param>
        /// <param name="vertexconstvars">List of constant values to use. Set of {name,value} pairs</param>
        /// <param name="tcsconstvars">List of constant values to use. Set of {name,value} pairs</param>
        /// <param name="tesconstvars">List of constant values to use. Set of {name,value} pairs</param>
        /// <param name="geoconstvars">List of constant values to use. Set of {name,value} pairs</param>
        /// <param name="fragconstvars">List of constant values to use. Set of {name,value} pairs</param>
        /// <param name="varyings">List of varyings to report</param>
        /// <param name="varymode">How to write the varying to the buffer</param>
        /// <param name="saveable">True if want to save to binary</param>
        /// <param name="completeoutfile">If non null, save compiler source to this root namd and extension</param>
        ///<returns>true if shader compiled and linked, even if compiler reported text</returns>

        public bool CompileLink(out string compilerreport, 
                                string vertex = null, string tcs = null, string tes = null, string geo = null, string frag = null,
                                object[] vertexconstvars = null, object[] tcsconstvars = null, object[] tesconstvars = null, object[] geoconstvars = null, object[] fragconstvars = null,
                                string[] varyings = null, TransformFeedbackMode varymode = TransformFeedbackMode.InterleavedAttribs, bool saveable = false, 
                                string completeoutfile = null
                            )
        {
            Compiled = false;

            Program = new GLProgram();

            compilerreport = "";

            if (vertex != null)
            {
                bool ret = Compile(ShaderType.VertexShader, vertex, ref compilerreport, vertexconstvars, completeoutfile);
                if (!ret)
                    return ret;
            }

            if (tcs != null)
            {
                bool ret = Compile(ShaderType.TessControlShader, tcs, ref compilerreport, tcsconstvars, completeoutfile);
                if (!ret)
                    return ret;
            }

            if (tes != null)
            {
                bool ret = Compile(ShaderType.TessEvaluationShader, tes, ref compilerreport, tesconstvars, completeoutfile);
                if (!ret)
                    return ret;
            }

            if (geo != null)
            {
                bool ret = Compile(ShaderType.GeometryShader, geo, ref compilerreport, geoconstvars, completeoutfile);
                if (!ret)
                    return ret;
            }

            if (frag != null)
            {
                bool ret = Compile(ShaderType.FragmentShader, frag, ref compilerreport, fragconstvars, completeoutfile);
                if (!ret)
                    return ret;
            }

            bool lret = Program.Link(out string linkerreport, false, varyings, varymode, saveable);

            if (linkerreport.HasChars())
            {
                linkerreport = $"Standard shader linker report for {GetType().Name}: {linkerreport}";
                compilerreport = compilerreport.AppendPrePad(linkerreport, Environment.NewLine);
                System.Diagnostics.Trace.WriteLine(linkerreport);
                GLShaderLog.Add(linkerreport);
            }

            if (lret == false && GLShaderLog.AssertOnError)
                System.Diagnostics.Trace.Assert(lret, "", linkerreport);     // note use of trace so its asserts even in release

            Compiled = lret;
            GLShaderLog.Okay = lret;     // update global Okay flag

            return lret;
        }

        /// <summary> Internal compile one shader /// </summary>
        private bool Compile(ShaderType t, string text, ref string compilerreport, object[] vars, string completeoutfile)
        {
            if (completeoutfile != null)        // if not null, mangle path to include shader type
                completeoutfile = System.IO.Path.GetDirectoryName(completeoutfile) + "\\" + System.IO.Path.GetFileNameWithoutExtension(completeoutfile) + "-" + t.ToString() + System.IO.Path.GetExtension(completeoutfile);

            completeoutfile = GLShaderLog.Outfile(completeoutfile, GetType().Name, t.ToString());       // see if shader log wants output file

            bool ret = Program.Compile(t, text, out string report, vars, completeoutfile);

            string errreport = $"Std {t.ToString()} compiler report for {GetType().Name}: {report}";

            if (report.HasChars())
            {
                compilerreport = compilerreport.AppendPrePad(errreport,Environment.NewLine);
                System.Diagnostics.Trace.WriteLine(errreport);
                GLShaderLog.Add(errreport);
            }

            if (ret == false && GLShaderLog.AssertOnError)
                System.Diagnostics.Trace.Assert(ret, "", errreport);     // note use of trace so its asserts even in release

            GLShaderLog.Okay = ret;     // update global Okay flag

            return ret;
        }


        /// <summary> Get the binary of the shader.  Must have linked with wantbinary</summary>
        public byte[] GetBinary(out BinaryFormat binformat)     // must have linked with wantbinary
        {
            return Program.GetBinary(out binformat);
        }

        /// <summary> Load the binary into the shader</summary>
        public void Load(byte[] bin, BinaryFormat binformat)
        {
            Program = new GLProgram(bin, binformat);
        }

        /// <summary> Start the shader. Bind the program and then invoke StartAction</summary>
        public virtual void Start(GLMatrixCalc c)
        {
            GL.UseProgram(Id);
            StartAction?.Invoke(this, c);
        }

        /// <summary> Finish the shader. Invoke FinishAction.</summary>
        public virtual void Finish()
        {
            FinishAction?.Invoke(this);                           // any shader hooks get a chance.
        }

        /// <summary> Dispose of the shader</summary>
        public virtual void Dispose()
        {
            Program.Dispose();
        }

    }
}
