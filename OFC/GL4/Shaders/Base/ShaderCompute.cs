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

namespace GLOFC.GL4
{
    // inherit from this to make a compute shader 
    // you can either run it directly, or you can add it to a RenderableList to mix it with renderable items

    public abstract class GLShaderCompute : IGLProgramShader
    {
        public int Id { get { return Program.Id; } }
        public GLProgram Program { get; private set; }

        public bool Enable { get; set; } = true;                        // if not enabled, no render items below it will be visible
        public virtual string Name { get { return "Standard:" + GetType().Name; } }     // override to give meaningful name

        public IGLShader GetShader(ShaderType t) { return this; }
        public T GetShader<T>(OpenTK.Graphics.OpenGL4.ShaderType t) where T : IGLShader { throw new NotImplementedException(); }    // get a subcomponent of type T. Excepts if not present
        public T GetShader<T>() where T : IGLShader { throw new NotImplementedException(); }    // get a subcomponent of type T. Excepts if not present

        public Action<IGLProgramShader, GLMatrixCalc> StartAction { get; set; }
        public Action<IGLProgramShader> FinishAction { get; set; }

        public int XWorkgroupSize { get; set; } = 1;
        public int YWorkgroupSize { get; set; } = 1;
        public int ZWorkgroupSize { get; set; } = 1;

        public GLShaderCompute()
        {
        }

        public GLShaderCompute(Action<IGLProgramShader, GLMatrixCalc> sa = null) : this()
        {
            StartAction = sa;
        }

        public GLShaderCompute(int x, int y, int z, Action<IGLProgramShader, GLMatrixCalc> sa = null) : this()
        {
            XWorkgroupSize = x; YWorkgroupSize = y; ZWorkgroupSize = z;
            StartAction = sa;
        }

        // completeoutfile is output of file for debugging
        public void CompileLink(string code, Object[] constvalues = null, string completeoutfile = null )
        {
            Program = new GLProgram();
            string ret = Program.Compile(OpenTK.Graphics.OpenGL4.ShaderType.ComputeShader, code, constvalues, completeoutfile);
            System.Diagnostics.Debug.Assert(ret == null, "Compute Shader", ret);
            ret = Program.Link();
            System.Diagnostics.Debug.Assert(ret == null, "Link", ret);
            GLOFC.GLStatics.Check();
        }

        protected void Load(byte[] bin, BinaryFormat binformat)
        {
            Program = new GLProgram(bin, binformat);
        }

        public void Start(GLMatrixCalc c)                 // override.. but call back.  Executes compute.
        {
            GL.UseProgram(Id);
            StartAction?.Invoke(this, c);
            GL.DispatchCompute(XWorkgroupSize, YWorkgroupSize, ZWorkgroupSize);
        }

        public virtual void Finish()
        {
            FinishAction?.Invoke(this);                           // any shader hooks get a chance.
        }

        public virtual void Dispose()
        {
            Program.Dispose();
        }

        public void Run()                           // for compute shaders, we can just run them.  
        {
            Start(null);
            Finish();
        }
    }
}
