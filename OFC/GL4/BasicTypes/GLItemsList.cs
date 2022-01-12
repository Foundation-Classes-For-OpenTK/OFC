/*
 * Copyright 2019-2021 Robbyxp1 @ github.com
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

using GLOFC.GL4.Bitmaps;
using GLOFC.GL4.Shaders;
using GLOFC.GL4.Textures;
using GLOFC.Utils;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace GLOFC.GL4
{
    /// <summary>
    /// This is a memory class in which you can register GL type items and it will manage them 
    /// Items have names to find them again
    /// </summary>

    public class GLItemsList : IDisposable
    {
        /// <summary> Enable for stack tracing on disposal. A stack trace at every add/creation is kept </summary>
        public static bool StackTrace { get; set; } = false;

        /// <summary> Get existing item by name</summary>
        public bool Contains(string name )
        {
            return items.ContainsKey(name);
        }

        /// <summary> Find by name this type, will except if not found or wrong type </summary>
        public GLTextureBase Tex(string name)
        {
            return (GLTextureBase)items[name];
        }

        /// <summary> Find by name this type, will except if not found or wrong type  </summary>
        public IGLProgramShader Shader(string name)
        {
            return (IGLProgramShader)items[name];
        }

        /// <summary>  Find by name this type, will except if not found or wrong type </summary>
        public IGLPipelineComponentShader PLShader(string name)
        {
            return (IGLPipelineComponentShader)items[name];
        }

        /// <summary> Find by name this type, will except if not found or wrong type  </summary>
        public GLVertexArray VA(string name)
        {
            return (GLVertexArray)items[name];
        }

        /// <summary> Find by name this type, will except if not found or wrong type  </summary>
        public GLUniformBlock UB(string name)
        {
            return (GLUniformBlock)items[name];
        }

        /// <summary>  Find by name this type, will except if not found or wrong type </summary>
        public GLStorageBlock SB(string name)
        {
            return (GLStorageBlock)items[name];
        }

        /// <summary> Find by name this type, will except if not found or wrong type  </summary>
        public GLAtomicBlock AB(string name)
        {
            return (GLAtomicBlock)items[name];
        }

        /// <summary> Find by name this type, will except if not found or wrong type  </summary>
        public GLBuffer B(string name)
        {
            return (GLBuffer)items[name];
        }

        /// <summary> Find by name this type, will except if not found or wrong type  </summary>
        public Bitmap Bitmap(string name)
        {
            return (Bitmap)items[name];
        }

        /// <summary> Find last buffer, will except if no buffer exists </summary>
        public GLBuffer LastBuffer(int c = 1)
        {
            return (GLBuffer)items.Last(typeof(GLBuffer), c);
        }

        /// <summary> Find last of type, will except if does not exist </summary>
        public T Last<T>(int c = 1) where T : class
        {
            return (T)items.Last(typeof(T), c);
        }

        /// <summary> Get this type by name, will except if not found or wrong type </summary>
        public T Get<T>(string name)
        {
            return (T)items[name];
        }

        // Add existing items. Name can be null and will get a unique name

        /// <summary> Add this type with an optional name </summary>
        public IGLTexture Add(IGLTexture disp, string name = null)
        {
            System.Diagnostics.Debug.Assert(!items.ContainsValue(disp));
            items.Add(EnsureName(name), disp);
            return disp;
        }

        /// <summary> Add this type with an optional name </summary>
        public IGLProgramShader Add(IGLProgramShader disp, string name = null)
        {
            System.Diagnostics.Debug.Assert(!items.ContainsValue(disp));
            items.Add(EnsureName(name), disp);
            return disp;
        }

        /// <summary> Add this type with an optional name </summary>
        public IGLPipelineComponentShader Add(IGLPipelineComponentShader disp, string name = null)
        {
            System.Diagnostics.Debug.Assert(!items.ContainsValue(disp));
            items.Add(EnsureName(name), disp);
            return disp;
        }

        /// <summary> Add this type with an optional name </summary>
        public GLVertexArray Add(GLVertexArray disp, string name = null)
        {
            System.Diagnostics.Debug.Assert(!items.ContainsValue(disp));
            items.Add(EnsureName(name), disp);
            return disp;
        }

        /// <summary> Add this type with an optional name </summary>
        public GLUniformBlock Add(GLUniformBlock disp, string name = null)
        {
            System.Diagnostics.Debug.Assert(!items.ContainsValue(disp));
            items.Add(EnsureName(name), disp);
            return disp;
        }

        /// <summary> Add this type with an optional name </summary>
        public GLStorageBlock Add(GLStorageBlock disp, string name = null)
        {
            System.Diagnostics.Debug.Assert(!items.ContainsValue(disp));
            items.Add(EnsureName(name), disp);
            return disp;
        }

        /// <summary> Add this type with an optional name </summary>
        public GLAtomicBlock Add( GLAtomicBlock disp, string name = null)
        {
            System.Diagnostics.Debug.Assert(!items.ContainsValue(disp));
            items.Add(EnsureName(name), disp);
            return disp;
        }

        /// <summary> Add this type with an optional name </summary>
        public GLBuffer Add(GLBuffer disp, string name = null)
        {
            System.Diagnostics.Debug.Assert(!items.ContainsValue(disp));
            items.Add(EnsureName(name), disp);
            return disp;
        }

        /// <summary> Add this type with an optional name </summary>
        public Bitmap Add(Bitmap disp, string name = null)
        {
            System.Diagnostics.Debug.Assert(!items.ContainsValue(disp));
            items.Add(EnsureName(name), disp);
            return disp;
        }

        /// <summary> Add this type with an optional name </summary>
        public GLBitmaps Add(GLBitmaps disp, string name = null)
        {
            System.Diagnostics.Debug.Assert(!items.ContainsValue(disp));
            items.Add(EnsureName(name), disp);
            return disp;
        }

        /// <summary> Add this type with an optional name </summary>
        public void Add(IDisposable disp, string name = null)
        {
            System.Diagnostics.Debug.Assert(!items.ContainsValue(disp));
            items.Add(EnsureName(name), disp);
        }

        // New items

        /// <summary> Make a new entry of this type with an optional name </summary>
        public GLVertexArray NewArray(string name = null)
        {
            GLVertexArray b = new GLVertexArray();
            items[EnsureName(name)] = b;
            return b;
        }

        /// <summary> Make a new entry of this type with an optional name </summary>
        public GLUniformBlock NewUniformBlock(int bindingindex, string name = null)
        {
            GLUniformBlock sb = new GLUniformBlock(bindingindex);
            items[EnsureName(name)] = sb;
            return sb;
        }

        /// <summary> Make a new entry of this type with an optional name </summary>
        public GLStorageBlock NewStorageBlock(int bindingindex, bool std430 = false, string name = null)
        {
            GLStorageBlock sb = new GLStorageBlock(bindingindex, std430);
            items[EnsureName(name)] = sb;
            return sb;
        }

        /// <summary> Make a new entry of this type with an optional name </summary>
        public GLAtomicBlock NewAtomicBlock(int bindingindex, string name = null)
        {
            GLAtomicBlock sb = new GLAtomicBlock(bindingindex);
            items[EnsureName(name)] = sb;
            return sb;
        }

        // a buffer returned
        /// <summary> Make a new entry of this type with an optional name </summary>
        public GLBuffer NewBuffer(bool std430 = true, string name = null)
        {
            GLBuffer b = new GLBuffer(std430);        
            items[EnsureName(name)] = b;
            return b;
        }
        
        /// <summary> Make a new entry of this type with an optional name </summary>
        public GLBuffer NewBuffer(int size, bool std430 = false, BufferUsageHint hint = BufferUsageHint.StaticDraw, string name = null)
        {
            GLBuffer b = new GLBuffer(size,std430,hint);        
            items[EnsureName(name)] = b;
            return b;
        }

        /// <summary> Make a new entry of this type with an optional name </summary>
        public GLVertexArray NewVertexArray(string name = null)
        {
            var b = new GLVertexArray();        // a standard buffer returned is not for uniforms do not suffer the std140 restrictions
            items[EnsureName(name)] = b;
            return b;
        }

        /// <summary> Make a new entry of this type with an optional name </summary>
        public GLBindlessTextureHandleBlock NewBindlessTextureHandleBlock(int bindingpoint, string name = null)
        {
            var b = new GLBindlessTextureHandleBlock(bindingpoint);
            items[EnsureName(name)] = b;
            return b;
        }

        /// <summary> Make a new entry of this type with an optional name </summary>
        public GLBindlessTextureHandleBlock NewBindlessTextureHandleBlock(int bindingpoint, IGLTexture[] textures, string name = null)
        {
            var b = new GLBindlessTextureHandleBlock(bindingpoint,textures);
            items[EnsureName(name)] = b;
            return b;
        }

        /// <summary> Make a new entry of this type with an optional name </summary>
        public GLShaderPipeline NewShaderPipeline(string name, params Object[] cnst)
        {
            GLShaderPipeline s = (GLShaderPipeline)Activator.CreateInstance(typeof(GLShaderPipeline), cnst, null);
            items[EnsureName(name)] = s;
            return s;
        }

        /// <summary> Make a new entry of this type with an optional name </summary>
        public GLShaderPipeline NewShaderPipeline(string name, IGLPipelineComponentShader vertex, IGLPipelineComponentShader fragment)
        {
            GLShaderPipeline s = new GLShaderPipeline(vertex, fragment);
            items[EnsureName(name)] = s;
            return s;
        }

        /// <summary> Make a new entry of this type with an optional name </summary>
        public GLShaderStandard NewShaderStandard(string name, params Object[] cnst)
        {
            GLShaderStandard s = (GLShaderStandard)Activator.CreateInstance(typeof(GLShaderStandard), cnst, null);
            items[EnsureName(name)] = s;
            return s;
        }

        /// <summary> Make a new entry of this type with an optional name </summary>
        public GLShaderCompute NewShaderCompute(string name, params Object[] cnst)
        {
            var s = (GLShaderCompute)Activator.CreateInstance(typeof(GLShaderCompute), cnst, null);
            items[EnsureName(name)] = s;
            return s;
        }

        /// <summary> Make a new entry of this type with an optional name </summary>
        public GLTexture1D NewTexture1D(string name, params Object[] cnst)
        {
            var s = (GLTexture1D)Activator.CreateInstance(typeof(GLTexture1D), cnst, null);
            items[EnsureName(name)] = s;
            return s;
        }

        /// <summary> Make a new entry of this type with an optional name </summary>
        public GLTexture2D NewTexture2D(string name)
        {
            var s = new GLTexture2D();
            items[EnsureName(name)] = s;
            return s;
        }

        /// <summary> Make a new entry of this type with an optional name </summary>
        public GLTexture2D NewTexture2D(string name, Bitmap bmp, SizedInternalFormat internalformat, int bitmipmaplevel = 1,
                            int genmipmaplevel = 1, bool ownbitmaps = false, ContentAlignment alignment = ContentAlignment.TopLeft)
        {
            var s = new GLTexture2D(bmp, internalformat, bitmipmaplevel, genmipmaplevel, ownbitmaps, alignment);
            items[EnsureName(name)] = s;
            return s;
        }

        /// <summary> Make a new entry of this type with an optional name </summary>
        public GLTexture2DArray NewTexture2DArray(string name, params Object[] cnst)
        {
            var s = (GLTexture2DArray)Activator.CreateInstance(typeof(GLTexture2DArray), cnst, null);
            items[EnsureName(name)] = s;
            return s;
        }
        
        /// <summary> Make a new entry of this type with an optional name </summary>
        public GLTexture3D NewTexture3D(string name, params Object[] cnst)
        {
            var s = (GLTexture3D)Activator.CreateInstance(typeof(GLTexture3D), cnst, null);
            items[EnsureName(name)] = s;
            return s;
        }

        /// <summary> Dispose of this object </summary>
        public void Dispose(Object obj)     
        {
            string keytodelete = null;
            foreach (var kvp in items.Keys) 
            {
                if (items[kvp] == obj)
                {
                    (obj as IDisposable).Dispose();
                    keytodelete = kvp;
                    break;
                }
            }

            if (keytodelete != null)
                items.Remove(keytodelete);
        }

        /// <summary> Dispose of all objects </summary>
        public void Dispose()
        {
            if (StackTrace)
            {
                foreach (var r in items)
                {
                    System.Diagnostics.Debug.WriteLine($"Disposing of {r.Key} {stacktrace[r.Key]}");
                    r.Value.Dispose();
                    System.Diagnostics.Debug.WriteLine($"----");
                }

                items.Clear();
            }
            else
            {
                items.Dispose();
            }
        }
        
        // helpers

        private string EnsureName(string name)
        {
            name = (name == null) ? ("Unnamed_" + (unnamed++)) : name;
            if ( StackTrace )
                stacktrace[name] = Environment.StackTrace;
            return name; 
        }

        private Dictionary<string, string> stacktrace = new Dictionary<string, string>();
        private DisposableDictionary<string, IDisposable> items = new DisposableDictionary<string, IDisposable>();
        private int unnamed = 0;
    }
}
