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
using System.Collections.Generic;
using System.Linq;
using GLOFC.Utils;
using GLOFC.GL4.Shaders.Fragment;

namespace GLOFC.GL4.Shaders
{
    /// <summary>
    /// Inherit from this to make a shader which uses a set of pipeline shaders (from ShaderPipelineShadersBase) to make up your whole shader
    /// A pipeline shader has a Start(), called by RenderableList when the shader is started. This calls all the Start() in each ShaderPipelineShaderBase
    /// StartAction, an optional hook to supply more start functionality
    /// A Finish() to clean up. This calls all the Finish() in each ShaderPipelineShaderBase
    /// FinishAction, an optional hook to supply more finish functionality
    /// </summary>

    [System.Diagnostics.DebuggerDisplay("Pipeline {Id} {Name} {Enable}")]
    public class GLShaderPipeline : IGLProgramShader
    {
        /// <summary> ID (not a GL ID)</summary>
        public int Id { get { return pipelineid + 100000; } }           // to avoid clash with standard ProgramIDs, use an offset for pipeline IDs

        /// <summary> If Enabled </summary>
        public bool Enable { get; set; } = true;                        // if not enabled, no render items below it will be visible

        /// <summary>Optional Render state. The shader can order a render state instead of each renderable item having one, if required </summary>
        public GLRenderState RenderState { get; set; }

        /// <summary> Name of shader. Standard name is type name and pipeline shaders, override to give a better name if required </summary>
        public virtual string Name { get { string s = ""; foreach (var sh in shaders) s = s.AppendPrePad(sh.Value.GetType().Name, ","); return GetType().Name + ":" + s; } }

        /// <summary> Start Action callback </summary>
        public Action<IGLProgramShader, GLMatrixCalc> StartAction { get; set; }
        /// <summary> Finish Action callback </summary>
        public Action<IGLProgramShader> FinishAction { get; set; }

        /// <summary> Get shader of type. Throws if shader not present. </summary>
        public IGLShader GetShader(ShaderType shadertype) { return shaders[shadertype]; }
        /// <summary> Get shader of type, cast to T. Throws if not correct type or shader not present. </summary>
        public T GetShader<T>(ShaderType t) where T : IGLShader { return (T)shaders[t]; }
        /// <summary> Get shader of type T.  Throws if not present</summary>
        public T GetShader<T>() where T : IGLShader { foreach (var s in shaders) if (s.Value.GetType() == typeof(T)) return (T)s.Value; throw new InvalidCastException(); }    // get a subcomponent of type T. Excepts if not present

        private int pipelineid = -1;
        private Dictionary<ShaderType, IGLPipelineComponentShader> shaders = new Dictionary<ShaderType, IGLPipelineComponentShader>();

        /// <summary> Create a empty pipeline shader</summary>
        public GLShaderPipeline()
        {
            pipelineid = GL.GenProgramPipeline();
            GLStatics.RegisterAllocation(typeof(GLShaderPipeline));
        }

        /// <summary> Create a pipeline shader from binary</summary>
        public GLShaderPipeline(byte[] bin, BinaryFormat binformat)
        {
            pipelineid = GL.GenProgramPipeline();
            GLStatics.RegisterAllocation(typeof(GLShaderPipeline));
            Load(bin, binformat);
        }

        /// <summary> Create a empty pipeline shader with these start and optional finish actions</summary>
        public GLShaderPipeline(Action<IGLProgramShader, GLMatrixCalc> sa, Action<IGLProgramShader> fa = null) : this()
        {
            StartAction = sa;
            FinishAction = fa;
        }

        /// <summary> Create a pipeline shader with a vertex components and with these optional start and finish actions</summary>
        public GLShaderPipeline(IGLPipelineComponentShader vertex, Action<IGLProgramShader, GLMatrixCalc> sa = null, Action<IGLProgramShader> fa = null) : this()
        {
            AddVertex(vertex);
            StartAction = sa;
            FinishAction = fa;
        }

        /// <summary> Create a pipeline shader with a vertex and fragment components and with these optional start and finish actions</summary>
        public GLShaderPipeline(IGLPipelineComponentShader vertex, IGLPipelineComponentShader fragment, Action<IGLProgramShader, GLMatrixCalc> sa = null,
                                Action<IGLProgramShader> fa = null) : this()
        {
            AddVertexFragment(vertex, fragment);
            StartAction = sa;
            FinishAction = fa;
        }

        /// <summary> Create a pipeline shader with a optional vertex, tcs, tes, geo and fragment components and with these optional start and finish actions</summary>
        public GLShaderPipeline(IGLPipelineComponentShader vertex, IGLPipelineComponentShader tcs, IGLPipelineComponentShader tes, IGLPipelineComponentShader geo, IGLPipelineComponentShader fragment,
                                Action<IGLProgramShader, GLMatrixCalc> sa = null, Action<IGLProgramShader> fa = null) : this()
        {
            AddVertexTCSTESGeoFragment(vertex, tcs, tes, geo, fragment);
            StartAction = sa;
            FinishAction = fa;
        }

        /// <summary> Add vertex shader</summary>
        public void AddVertex(IGLPipelineComponentShader p)
        {
            Add(p, ShaderType.VertexShader);
        }

        /// <summary> Add vertex and fragment shaders</summary>
        public void AddVertexFragment(IGLPipelineComponentShader p, IGLPipelineComponentShader f)
        {
            Add(p, ShaderType.VertexShader);
            Add(f, ShaderType.FragmentShader);
        }

        /// <summary> Add shaders: optional vertex, tcs, tes, geo and fragment shaders</summary>
        public void AddVertexTCSTESGeoFragment(IGLPipelineComponentShader p, IGLPipelineComponentShader tcs, IGLPipelineComponentShader tes, IGLPipelineComponentShader g, IGLPipelineComponentShader f)
        {
            if (p != null)
                Add(p, ShaderType.VertexShader);
            if (tcs != null)
                Add(tcs, ShaderType.TessControlShader);
            if (tes != null)
                Add(tes, ShaderType.TessEvaluationShader);
            if (g != null)
                Add(g, ShaderType.GeometryShader);
            if (f != null)
                Add(f, ShaderType.FragmentShader);
        }

        /// <summary> Add a pipeline shader of shadertype </summary>
        public void Add(IGLPipelineComponentShader pipelineshader, ShaderType shadertype)
        {
            System.Diagnostics.Debug.Assert(!shaders.ContainsKey(shadertype));
            shaders[shadertype] = pipelineshader;
            pipelineshader.References++;
            GL.UseProgramStages(pipelineid, convmask[shadertype], pipelineshader.Id);
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

        /// <summary> Get the binary of the shader.  Must have linked with wantbinary</summary>
        public byte[] GetBinary(out BinaryFormat binformat)
        {
            GL.GetProgram(pipelineid, (GetProgramParameterName)0x8741, out int len);
            byte[] array = new byte[len];
            GL.GetProgramBinary(pipelineid, len, out int binlen, out binformat, array);
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
            return array;
        }

        /// <summary> Load the binary into the shader</summary>
        public void Load(byte[] bin, BinaryFormat binformat)    // load program direct from bin
        {
            GL.ProgramBinary(pipelineid, binformat, bin, bin.Length);
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

        /// <summary> Start the shader. Bind the program and call Start on each sumcomponent.  Then invoke StartAction</summary>
        public virtual void Start(GLMatrixCalc c)
        {
            GL.UseProgram(0);           // ensure no active program - otherwise the stupid thing picks it
            GL.BindProgramPipeline(pipelineid);

            foreach (var x in shaders)                             // let any programs do any special set up
                x.Value.Start(c);

            StartAction?.Invoke(this, c);                           // any shader hooks get a chance.
        }

        /// <summary> Finish the shader. Call Finish on each sumcomponent.  Then invoke FinishAction. Then unbind the program</summary>
        public virtual void Finish()                                        // and clean up afterwards
        {
            foreach (var x in shaders)
                x.Value.Finish();

            FinishAction?.Invoke(this);                           // any shader hooks get a chance.

            GL.BindProgramPipeline(0);
        }

        /// <summary> Dispose of the shader</summary>
        public virtual void Dispose()
        {
            if (pipelineid != -1)
            {
                foreach (var x in shaders)
                {
                    x.Value.Dispose();
                }

                GL.DeleteProgramPipeline(pipelineid);
                GLStatics.RegisterDeallocation(typeof(GLShaderPipeline));
                pipelineid = -1;
            }
            else
                System.Diagnostics.Trace.WriteLine($"OFC Warning - double disposing of ${GetType().FullName}");

        }

        private static Dictionary<ShaderType, ProgramStageMask> convmask = new Dictionary<ShaderType, ProgramStageMask>()
        {
            { ShaderType.FragmentShader, ProgramStageMask.FragmentShaderBit },
            { ShaderType.VertexShader, ProgramStageMask.VertexShaderBit },
            { ShaderType.TessControlShader, ProgramStageMask.TessControlShaderBit },
            { ShaderType.TessEvaluationShader, ProgramStageMask.TessEvaluationShaderBit },
            { ShaderType.GeometryShader, ProgramStageMask.GeometryShaderBit},
            { ShaderType.ComputeShader, ProgramStageMask.ComputeShaderBit },
        };

    }
}
