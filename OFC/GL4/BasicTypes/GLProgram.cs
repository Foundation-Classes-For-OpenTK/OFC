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
using System.Collections.Generic;
using GLOFC.Utils;
using OpenTK.Graphics.OpenGL4;

namespace GLOFC.GL4
{
    ///<summary>This is the base for all programs
    /// programs hold GLShaders which are compiled and linked into the Program
    /// Once linked, the program can execute the shaders
    /// it holds one or more GLShaders (Vertex/ Geo/ Fragment etc) </summary> 

    public class GLProgram : IDisposable
    {
        /// <summary>GL ID</summary>
        public int Id { get; private set; } = -1;
        /// <summary>Has it been created?</summary>
        public bool Created { get { return Id != -1; } }

        private List<GLShader> shaders;

        /// <summary>Create a program</summary>
        public GLProgram()
        {
            Id = GL.CreateProgram();
            GLStatics.RegisterAllocation(typeof(GLProgram));
            shaders = new List<GLShader>();
        }

        /// <summary>Create a program from a binary object</summary>
        public GLProgram(byte[]bin, BinaryFormat binformat)
        {
            Id = GL.CreateProgram();        // no shader list on load this direct
            Load(bin, binformat);
        }

        /// <summary>Add to program a shader </summary>
        public void Add(GLShader s)
        {
            System.Diagnostics.Debug.Assert(s.Compiled);
            shaders.Add(s);
        }

        // completeoutfile is output of file for debugging
        /// <summary>
        /// Compile the program
        /// </summary>
        /// <param name="shadertype">Shader type,Fragment, Vertex etc </param>
        /// <param name="codelisting">The code</param>
        /// <param name="constvalues">List of constant values to use. Set of {name,value} pairs</param>
        /// <param name="completeoutfile">If non null, output the post processed code listing to this file</param>
        /// <returns>Null string if sucessful, or error text</returns>
        public string Compile( ShaderType shadertype, string codelisting, Object[] constvalues = null, string completeoutfile = null )        
        {
            GLShader shader = new GLShader(shadertype);

            string ret = shader.Compile(codelisting, constvalues, completeoutfile);

            if (ret == null)
            {
                Add(shader);
                return null;
            }
            else
                return ret;
        }

        /// <summary>
        /// Link the program.
        /// If you specify varyings, you must set up a buffer, and a start action of Gl.BindBuffer(GL.TRANSFORM_FEEDBACK_BUFFER,bufid) AND BeingTransformFeedback.
        /// </summary>
        /// <param name="separable">Set to true to allow for pipeline shaders</param>
        /// <param name="varyings">List of varyings to report. See <href>https://www.khronos.org/opengl/wiki/Transform_Feedback</href> for details on how you can send varying to various binding indexes</param>
        /// <param name="varymode">How to write the varying to the buffer</param>
        /// <param name="wantbinary">Set to true to allow GetBinary to work</param>
        /// <returns></returns>

        public string Link( bool separable = false, string[] varyings = null, TransformFeedbackMode varymode = TransformFeedbackMode.InterleavedAttribs, bool wantbinary= false)            // link, seperable or not.  Disposes of shaders. null if okay
        {
            if (shaders.Count == 0)
                return "No shaders attached";

            foreach (GLShader s in shaders)
                GL.AttachShader(Id, s.Id);

            GL.ProgramParameter(Id, ProgramParameterName.ProgramSeparable, separable ? 1:0);

            if (varyings != null)
                GL.TransformFeedbackVaryings(Id, varyings.Length, varyings, varymode);      // this indicate varyings.

            GL.ProgramParameter(Id, ProgramParameterName.ProgramBinaryRetrievableHint, wantbinary ? 1:0);

            GL.LinkProgram(Id);
            var info = GL.GetProgramInfoLog(Id);

            foreach (GLShader s in shaders)
            {
                GL.DetachShader(Id, s.Id);
                s.Dispose();
            }

            return info.HasChars() ? info : null;
        }

        /// <summary> Get binary. Must have linked with wantbinary </summary>
        public byte[] GetBinary(out BinaryFormat binformat)     
        {
            GL.GetProgram(Id, (GetProgramParameterName)0x8741, out int len);
            byte[] array = new byte[len];
            GL.GetProgramBinary(Id, len, out int binlen, out binformat, array);
            GLStatics.Check();
            return array;
        }

        /// <summary> Load from binary, in binformat format </summary>
        public void Load(byte[] bin, BinaryFormat binformat)    // load program direct from bin
        {
            GL.ProgramBinary(Id, binformat, bin, bin.Length);
            GLStatics.Check();
        }

        /// <summary> Use this program </summary>
        public void Use()
        {
            GL.UseProgram(Id);
        }

        /// <summary> Dispose of the program object </summary>
        public void Dispose()               // you can double dispose
        {
            if (Id != -1)
            {
                GL.DeleteProgram(Id);
                GLStatics.RegisterDeallocation(typeof(GLProgram));
                Id = -1;
            }
            else
                System.Diagnostics.Trace.WriteLine($"OFC Warning - double disposing of ${this.GetType().FullName}");
        }
    }
}
