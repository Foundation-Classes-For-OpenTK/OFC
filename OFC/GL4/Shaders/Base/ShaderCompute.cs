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
    /// This namespace contains the four types of shaders base classes:
    /// * Compute shaders - all compute shaders inherit from this class
    /// * Pipeline - all pipeline component shaders are attached to this class to form a pipeline shader
    /// * ShaderPipelineComponentShaderBase - all pipeline shader components (vertex, fragment etc) inherit from this class so they can be inserted into the pipeline class
    /// * ShaderStandard - all standard shaders inherit from this class. Standard shaders are unitary and have all the shaders types inside them and are compiled as a whole.
    /// 
    /// Also included is a set of GLSL functions, incorporated into your code using #include Shaders.Functions.funcfile.glsl, replace funcfile with:
    /// * colors.glsl : A set of color functions
    /// * distribution.glsl : Gaussian distributions
    /// * mat4.glsl : Matrix4 helpers
    /// * noise2.glsl : Noise functions Vector 2
    /// * noise3.glsl : Noise functions Vector 3
    /// * random.glsl : Random numbers
    /// * snoise3.glsl : Noise functions Vector 3
    /// * snoise4.glsl : Noise functions Vector 4
    /// * trig.glsl : Tri funcs
    /// * vec4.glsl : Vector4 helpers
    /// </summary>
    internal static class NamespaceDoc { } // just for documentation purposes

    /// <summary>
    /// Compute Shader class. Inherit from this to make a compute shader 
    /// You can either run it directly, or you can add it to a RenderableList to mix it with renderable items
    /// </summary>
    public abstract class GLShaderCompute : IGLProgramShader
    {
        /// <summary> GL ID</summary>
        public int Id { get { return Program.Id; } }

        /// <summary> Program object</summary>
        public GLProgram Program { get; private set; }

        /// <summary> If Enabled </summary>
        public bool Enable { get; set; } = true;

        /// <summary> Name of shader </summary>
        public virtual string Name { get { return "Compute:" + GetType().Name; } }     // override to give meaningful name

        /// <summary> Get shader, always returns this</summary>
        public IGLShader GetShader(ShaderType t) { return this; }

        /// <summary> Not implemented</summary>
        public T GetShader<T>(ShaderType t) where T : IGLShader { throw new NotImplementedException(); }
        /// <summary> Not implemented</summary>
        public T GetShader<T>() where T : IGLShader { throw new NotImplementedException(); }

        /// <summary> Start Action callback </summary>
        public Action<IGLProgramShader, GLMatrixCalc> StartAction { get; set; }
        /// <summary> Finish Action callback </summary>
        public Action<IGLProgramShader> FinishAction { get; set; }

        /// <summary> Workgroup X size </summary>
        public int XWorkgroupSize { get; set; } = 1;
        /// <summary> Workgroup Y size</summary>
        public int YWorkgroupSize { get; set; } = 1;
        /// <summary> Workgroup Z size</summary>
        public int ZWorkgroupSize { get; set; } = 1;

        /// <summary> Create a compute shader with default workgroup size</summary>
        public GLShaderCompute()
        {
        }

        /// <summary> Create a compute shader with default workgroup size and optional start action</summary>
        public GLShaderCompute(Action<IGLProgramShader, GLMatrixCalc> sa = null) : this()
        {
            StartAction = sa;
        }

        /// <summary> Create a compute shader with these workgroups and optional start action</summary>
        public GLShaderCompute(int x, int y, int z, Action<IGLProgramShader, GLMatrixCalc> sa = null) : this()
        {
            XWorkgroupSize = x; YWorkgroupSize = y; ZWorkgroupSize = z;
            StartAction = sa;
        }

        /// <summary>
        /// Compile the compute program
        /// </summary>
        /// <param name="codelisting">The code</param>
        /// <param name="constvalues">List of constant values to use. Set of {name,value} pairs</param>
        /// <param name="saveable">True if want to save to binary</param>
        /// <param name="completeoutfile">If non null, output the post processed code listing to this file</param>
        /// <param name="assertonerror">If set, trace assert on error</param>
        /// <returns>Null string if successful, or error text, if assert is disabled</returns>/// 

        public string CompileLink(string codelisting, object[] constvalues = null, bool saveable = false, string completeoutfile = null, bool assertonerror = true)
        {
            Program = new GLProgram();
            string ret = Program.Compile(ShaderType.ComputeShader, codelisting, constvalues, completeoutfile);

            if (assertonerror)
                System.Diagnostics.Trace.Assert(ret == null, "", ret);     // note use of trace so its asserts even in release

            if (ret != null)
                return ret;

            ret = Program.Link(wantbinary: saveable);

            if (assertonerror)
                System.Diagnostics.Trace.Assert(ret == null, "", ret);

            GLStatics.Check();
            return ret;
        }

        /// <summary> Get binary. Must have linked with wantbinary</summary>
        public byte[] GetBinary(out BinaryFormat binformat)
        {
            return Program.GetBinary(out binformat);
        }

        /// <summary> Load a binary compute shader</summary>
        public void Load(byte[] bin, BinaryFormat binformat)
        {
            Program = new GLProgram(bin, binformat);
        }

        /// <summary> Start shader - execute compute. StartAction is called </summary>
        public void Start(GLMatrixCalc c)
        {
            GL.UseProgram(Id);
            StartAction?.Invoke(this, c);
            GL.DispatchCompute(XWorkgroupSize, YWorkgroupSize, ZWorkgroupSize);
        }

        /// <summary> Finish shader. FinishAction is called </summary>
        public virtual void Finish()
        {
            FinishAction?.Invoke(this);                           // any shader hooks get a chance.
        }

        /// <summary> Dispose of shader</summary>
        public virtual void Dispose()
        {
            Program.Dispose();
        }

        /// <summary> Run the shader </summary>
        public void Run()                           // for compute shaders, we can just run them.  
        {
            Start(null);
            Finish();
        }
    }
}
