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

namespace GLOFC.GL4
{
    /// <summary> Vertex Array Interface. To be attached to a renderableitem, vertex arrays need to be based on this </summary>
    public interface IGLVertexArray : IDisposable
    {
        /// <summary></summary>
        int Id { get; }
        /// <summary>called just before the item is drawn</summary>
        void Bind();
    }

    /// <summary> Shader Interface. All shaders inherit from this </summary>
    public interface IGLShader : IDisposable                
    {
        /// <summary>GL ID</summary>
        int Id { get; }
        /// <summary>Called at shader start, do work at this point</summary>
        void Start(GLMatrixCalc c);                    
        /// <summary>Called at shader removal, clean up</summary>
        void Finish();                                 
    }

    /// <summary> Pipleine Component Shader Interface. All pipeline components must come from this  </summary>
    public interface IGLPipelineComponentShader : IGLShader  
    {
        /// <summary>Number of references across shaders to this component</summary>
        int References { get; set; }
        /// <summary>Return the binary version of the shader for storage.  Must have linked with wantbinary</summary>
        byte[] GetBinary(out OpenTK.Graphics.OpenGL4.BinaryFormat binformat);     
        /// <summary>Load a binary image of a shader from storage</summary>
        void Load(byte[] bin, OpenTK.Graphics.OpenGL4.BinaryFormat binformat);
    }

    /// <summary> Program Shader Interface </summary>

    public interface IGLProgramShader : IGLShader           // Shaders suitable for the rendering queue inherit from this - standard and pipeline
    {
        /// <summary>Name of shader</summary>
        string Name { get; }
        /// <summary>Shader enable</summary>
        bool Enable { get; set; }

        /// <summary>Return a component shader. If the shader does not have subcomponents, its will return itself.</summary>
        IGLShader GetShader(OpenTK.Graphics.OpenGL4.ShaderType st);
        /// <summary>Get a subcomponent of type T from shader type st. Excepts if not present or no subcomponents</summary>
        T GetShader<T>(OpenTK.Graphics.OpenGL4.ShaderType st) where T : IGLShader;
        /// <summary>Get a subcomponent of type T. Excepts if not present or no subcomponents</summary>
        T GetShader<T>() where T : IGLShader;

        /// <summary>Return the binary version of the shader for storage.  Must have linked with wantbinary</summary>
        byte[] GetBinary(out OpenTK.Graphics.OpenGL4.BinaryFormat binformat);     // must have linked with wantbinary
        /// <summary>Load a binary image of a shader from storage</summary>
        void Load(byte[] bin, OpenTK.Graphics.OpenGL4.BinaryFormat binformat);

        /// <summary>Called when shader starts.</summary>
        Action<IGLProgramShader, GLMatrixCalc> StartAction { get; set; }      // allow start and finish action events to be added to the shader..
        /// <summary>Called when shader is removed.</summary>
        Action<IGLProgramShader> FinishAction { get; set; }
    }

    /// <summary> Texture interface </summary>
    public interface IGLTexture : IDisposable               // all textures from this..
    {
        /// <summary>GL ID</summary>
        int Id { get; }
        /// <summary>ARB ID of texture. Call to assign.</summary>
        long ArbNumber { get; }
        /// <summary>Ask for an ARB ID for this texture.</summary>
        long AcquireArbId();
        /// <summary>Image width. Primary width of mipmap level 0 bitmap on first array entry</summary>
        int Width { get; }                                 
        /// <summary>Image Height</summary>
        int Height { get; }
        /// <summary>Bind image to texture binding point. Textures have a chance to bind themselves, called either by instance data (if per object texture) or by shader (either internally or via StartAction)</summary>
        void Bind(int bindingpoint);
    }

    /// <summary> Renderable item interface</summary>
    public interface IGLRenderableItem                     
    {
        /// <summary>Bind interface, bind data to draw</summary>
        void Bind(GLRenderState rc, IGLProgramShader shader, GLMatrixCalc c);                 
        /// <summary> Render interface, execute draw </summary>
        void Render();                                      
        /// <summary> Optional (but normally set) RenderState to enforce on draw </summary>
        GLRenderState RenderState { get; set; }         
        /// <summary> Optional render data to bind </summary>
        IGLRenderItemData RenderData { get; set; }          
        /// <summary> Number of draws </summary>
        int DrawCount { get; set; }
        /// <summary> Number of instances to draw </summary>
        int InstanceCount { get; set; }          
        /// <summary> If Visible </summary>
        bool Visible { get; set; }
    }

    /// <summary> Renderable item data, called at binding point</summary>
    public interface IGLRenderItemData                     // to be attached to a rendableitem, instance control/data need to be based on this. Should not need to be disposable..
    {
        /// <summary>Bind interface, bind data to draw</summary>
        void Bind(IGLRenderableItem ri,IGLProgramShader shader, GLMatrixCalc c);  // called just before the item is drawn
    }

}