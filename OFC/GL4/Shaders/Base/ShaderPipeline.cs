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

namespace GLOFC.GL4
{
    // inherit from this to make a shader which uses a set of pipeline shaders (from ShaderPipelineShadersBase) to make up your whole shader

    // A pipeline shader has a Start(), called by RenderableList when the shader is started
    //  this calls all the Start() in each ShaderPipelineShaderBase
    // StartAction, an optional hook to supply more start functionality
    // A Finish() to clean up
    //  this calls all the Finish() in each ShaderPipelineShaderBase
    // FinishAction, an optional hook to supply more finish functionality

    public class GLShaderPipeline : IGLProgramShader
    {
        public int Id { get { return pipelineid + 100000; } }           // to avoid clash with standard ProgramIDs, use an offset for pipeline IDs
        public bool Enable { get; set; } = true;                        // if not enabled, no render items below it will be visible

        // standard name is type name and pipeline shaders, override to give a better name if required
        public virtual string Name { get { string s = ""; foreach (var sh in shaders) s = s.AppendPrePad(sh.Value.GetType().Name,",") ; return this.GetType().Name + ":"+ s; } } 

        public Action<IGLProgramShader, GLMatrixCalc> StartAction { get; set; }
        public Action<IGLProgramShader> FinishAction { get; set; }

        public IGLShader GetShader(ShaderType t) { return shaders[t]; }
        public T GetShader<T>(ShaderType t) where T : IGLShader { return (T)shaders[t]; }
        public T GetShader<T>() where T : IGLShader { foreach (var s in shaders) if (s.Value.GetType() == typeof(T)) return (T)s.Value; throw new InvalidCastException(); }    // get a subcomponent of type T. Excepts if not present

        private int pipelineid;
        private Dictionary<ShaderType, IGLPipelineComponentShader> shaders;

        public GLShaderPipeline()
        {
            pipelineid = GL.GenProgramPipeline();
            shaders = new Dictionary<ShaderType, IGLPipelineComponentShader>();
        }

        public GLShaderPipeline(Action<IGLProgramShader, GLMatrixCalc> sa, Action<IGLProgramShader> fa = null) : this()
        {
            StartAction = sa;
            FinishAction = fa;
        }

        public GLShaderPipeline(IGLPipelineComponentShader vertex, Action<IGLProgramShader, GLMatrixCalc> sa = null, Action<IGLProgramShader> fa = null) : this()
        {
            AddVertex(vertex);
            StartAction = sa;
            FinishAction = fa;
        }

        public GLShaderPipeline(IGLPipelineComponentShader vertex, IGLPipelineComponentShader fragment, Action<IGLProgramShader, GLMatrixCalc> sa = null, 
                                Action<IGLProgramShader> fa = null) : this()
        {
            AddVertexFragment(vertex, fragment);
            StartAction = sa;
            FinishAction = fa;
        }

        public GLShaderPipeline(IGLPipelineComponentShader vertex, IGLPipelineComponentShader tcs, IGLPipelineComponentShader tes, IGLPipelineComponentShader geo, IGLPipelineComponentShader fragment, 
                                Action<IGLProgramShader, GLMatrixCalc> sa = null, Action<IGLProgramShader> fa = null) : this()
        {
            AddVertexTCSTESGeoFragment(vertex, tcs,tes,geo, fragment);
            StartAction = sa;
            FinishAction = fa;
        }

        public void AddVertex(IGLPipelineComponentShader p)
        {
            Add(p, ShaderType.VertexShader);
        }

        public void AddVertexFragment(IGLPipelineComponentShader p, IGLPipelineComponentShader f)
        {
            Add(p, ShaderType.VertexShader);
            Add(f, ShaderType.FragmentShader);
        }

        public void AddVertexTCSTESGeoFragment(IGLPipelineComponentShader p, IGLPipelineComponentShader tcs, IGLPipelineComponentShader tes, IGLPipelineComponentShader g, IGLPipelineComponentShader f)
        {
            if (p != null)
                Add(p, ShaderType.VertexShader);
            if (tcs != null)
                Add(tcs, ShaderType.TessControlShader);
            if (tes != null)
                Add(tes, ShaderType.TessEvaluationShader);
            if ( g != null )
                Add(g, ShaderType.GeometryShader);
            if ( f != null )
                Add(f, ShaderType.FragmentShader);
        }

        public void Add(IGLPipelineComponentShader p, ShaderType m)
        {
            shaders[m] = p;
            GL.UseProgramStages(pipelineid, convmask[m], p.Id);
            GLStatics.Check();
        }

        public virtual void Start(GLMatrixCalc c)
        {
            GL.UseProgram(0);           // ensure no active program - otherwise the stupid thing picks it
            GL.BindProgramPipeline(pipelineid);

            foreach (var x in shaders)                             // let any programs do any special set up
                x.Value.Start(c);

            StartAction?.Invoke(this,c);                           // any shader hooks get a chance.
        }

        public virtual void Finish()                                        // and clean up afterwards
        {
            foreach (var x in shaders)
                x.Value.Finish();

            FinishAction?.Invoke(this);                           // any shader hooks get a chance.

            GL.BindProgramPipeline(0);
        }

        public virtual void Dispose()
        {
            foreach (var x in shaders)
                x.Value.Dispose();

            GL.DeleteProgramPipeline(pipelineid);
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
