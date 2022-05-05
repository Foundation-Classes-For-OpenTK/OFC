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
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace GLOFC.GL4
{
    /// <summary>
    /// Called per object, by the RenderableItem, to bind any data needed to place/rotate the object etc
    /// Translation,scaling and rotation of object, placed at 0,0,0, to position.
    /// optional Lookat to look at viewer
    /// optional texture bind
    /// </summary>

    public class GLRenderDataTranslationRotation : IGLRenderItemData
    {
        /// <summary> Look at uniform number </summary>
        public int LookAtUniform { get; set; } = 21;
        /// <summary> Transform Uniform </summary>
        public int TransformUniform { get; set; } = 22;

        /// <summary> Position to translate to</summary>
        public Vector3 Position { get { return pos; } set { pos = value; Calc(); } }
        /// <summary> Scale</summary>
        public float Scale { get { return scale; } set { scale = value; Calc(); } }

        /// <summary> Translate the position </summary>
        public void Translate(Vector3 off) { pos += off; Calc(); }
        /// <summary> Rotation (radians) </summary>
        public Vector3 RotationRadians { get { return rot; } set { rot = value; Calc(); } }
        /// <summary> Rotation (degrees) </summary>
        public Vector3 RotationDegrees { get { return new Vector3(rot.X.Degrees(),rot.Y.Degrees(),rot.Z.Degrees()); } set { rot = new Vector3(value.X.Radians(),value.Y.Radians(),value.Z.Radians()); Calc(); } }
        /// <summary> X rotation degrees</summary>
        public float XRotDegrees { get { return rot.X.Degrees(); } set { rot.X = value.Radians(); Calc(); } }
        /// <summary> Y rotation degrees</summary>
        public float YRotDegrees { get { return rot.Y.Degrees(); } set { rot.Y = value.Radians(); Calc(); } }
        /// <summary> Z rotation degrees</summary>
        public float ZRotDegrees { get { return rot.Z.Degrees(); } set { rot.Z = value.Radians(); Calc(); } }
        /// <summary> X rotation radians </summary>
        public float XRotRadians { get { return rot.X; } set { rot.X = value; Calc(); } }
        /// <summary> Y rotation radians </summary>
        public float YRotRadians { get { return rot.Y; } set { rot.Y = value; Calc(); } }
        /// <summary> Z rotation radians </summary>
        public float ZRotRadians { get { return rot.Z; } set { rot.Z = value; Calc(); } }
        /// <summary> Transformation matrix </summary>
        public Matrix4 Transform { get { return transform; } }

        /// <summary> User data tag </summary>
        public object Tag { get; set; }     // to associate data with this RD

        /// <summary> Constructor for rotation and scale. Calclookat determins if look at angle computed</summary>
        public GLRenderDataTranslationRotation(float rxradians = 0, float ryradians = 0, float rzradians = 0, float scale = 1.0f, bool calclookat = false)
        {
            pos = new Vector3(0, 0, 0);
            rot = new Vector3(rxradians, ryradians, rzradians);
            this.scale = scale;
            lookatangle = calclookat;
            Calc();
        }

        /// <summary> Constructor for position, rotation and scale. Calclookat determins if look at angle computed</summary>
        public GLRenderDataTranslationRotation(Vector3 p, float rxradians = 0, float ryradians = 0, float rzradians = 0, float scale = 1.0f, bool calclookat = false)
        {
            pos = p;
            rot = new Vector3(rxradians, ryradians, rzradians);
            this.scale = scale;
            lookatangle = calclookat;
            Calc();
        }

        /// <summary> Constructor for position, rotation and scale. Calclookat determins if look at angle computed</summary>
        public GLRenderDataTranslationRotation(Vector3 p, Vector3 rotpradians, float sc = 1.0f , bool calclookat = false)
        {
            pos = p;
            rot = rotpradians;
            scale = sc;
            lookatangle = calclookat;
            Calc();
        }

        private void Calc()
        {
            transform = Matrix4.Identity;
            transform *= Matrix4.CreateScale(scale);
            transform *= Matrix4.CreateRotationX(rot.X);
            transform *= Matrix4.CreateRotationY(rot.Y);
            transform *= Matrix4.CreateRotationZ(rot.Z);
            transform *= Matrix4.CreateTranslation(pos);

          //  System.Diagnostics.Debug.WriteLine("Transform " + transform);
        }

        /// <summary>Bind data to uniforms </summary>
        public virtual void Bind(IGLRenderableItem ri, IGLProgramShader shader, GLMatrixCalc c)
        {
            int sid = shader.GetShader(ShaderType.VertexShader).Id;
            GL.ProgramUniformMatrix4(sid, TransformUniform, false, ref transform);

            if (lookatangle)
            {
                Vector2 res = pos.AzEl(c.EyePosition, false);
                System.Diagnostics.Debug.WriteLine("Object Bind eye " + c.EyePosition + " to " + pos + " az " + res.Y.Degrees() + " inc " + res.X.Degrees());
                Matrix4 tx2 = Matrix4.Identity;
                tx2 *= Matrix4.CreateRotationX((-res.X));
                tx2 *= Matrix4.CreateRotationY(((float)Math.PI+res.Y));
                GL.ProgramUniformMatrix4(sid, LookAtUniform, false, ref tx2);
            }

            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

        private Vector3 pos;
        private Vector3 rot;
        private float scale = 1.0f;
        private Matrix4 transform;
        private bool lookatangle = false;
    }

    /// <summary>
    /// Rotation/translation scale with texture bind. Expands on GLRenderDataTranslationRotation
    /// </summary>

    public class GLRenderDataTranslationRotationTexture : GLRenderDataTranslationRotation
    {
        /// <summary>Texture bind number</summary>
        public int TextureBind { get; set; } = 1;

        /// <summary>Constructor</summary>
        public GLRenderDataTranslationRotationTexture(IGLTexture tex, Vector3 p, float rx = 0, float ry = 0, float rz = 0, float scale = 1.0f) : base(p, rx, ry, rx, scale)
        {
            Texture = tex;
        }

        /// <summary>Constructor</summary>
        public GLRenderDataTranslationRotationTexture(IGLTexture tex, Vector3 p, Vector3 rotp, float scale = 1.0f) : base(p, rotp, scale)
        {
            Texture = tex;
        }

        /// <summary>Bind data to uniforms </summary>
        public override void Bind(IGLRenderableItem ri, IGLProgramShader shader, GLMatrixCalc c)
        {
            base.Bind(ri, shader, c);
            Texture.Bind(TextureBind);
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

        private IGLTexture Texture;                      // set to bind texture.
    }

    /// <summary>
    /// Rotation, Position, Scale plus Color data in ColorBind. Expands on GLRenderDataTranslationRotation
    /// </summary>
    public class GLRenderDataTranslationRotationColor : GLRenderDataTranslationRotation
    {
        /// <summary>Color bind</summary>  
        public int ColorBind { get; set; }

        /// <summary>Constructor</summary>
        public GLRenderDataTranslationRotationColor(System.Drawing.Color c, Vector3 p, float rx = 0, float ry = 0, float rz = 0, float scale = 1.0f, int uniformbinding = 25) : base(p, rx, ry, rx, scale)
        {
            col = c;
            ColorBind = uniformbinding;
        }
        /// <summary>Constructor</summary>
        public GLRenderDataTranslationRotationColor(System.Drawing.Color c, Vector3 p, Vector3 rotp, float scale = 1.0f, int uniformbinding = 25) : base(p, rotp, scale)
        {
            col = c;
            ColorBind = uniformbinding;
        }

        /// <summary>Bind color and other data to uniforms </summary>
        public override void Bind(IGLRenderableItem ri, IGLProgramShader shader, GLMatrixCalc c)
        {
            base.Bind(ri, shader, c);
            GL.ProgramUniform4(shader.GetShader(ShaderType.FragmentShader).Id,ColorBind, col.ToVector4());
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

        private System.Drawing.Color col;
    }

    /// <summary>
    /// Texture only bind
    /// </summary>

    public class GLRenderDataTexture : IGLRenderItemData
    {
        /// <summary>Texture bind start number </summary>
        public int TextureBind { get; set; } = 1;

        /// <summary>Constructor, taking an array of textures to bind from bind position onwards</summary>  
        public GLRenderDataTexture(IGLTexture[] tex, int bind = 1)
        {
            Textures = new int[tex.Length];
            for (int i = 0; i < tex.Length; i++)
                Textures[i] = tex[i].Id;
            TextureBind = bind;
        }

        /// <summary>Constructor for a single texture</summary>  
        public GLRenderDataTexture(IGLTexture tex, int bind = 1) : this(new IGLTexture[] { tex }, bind)
        {
        }

        /// <summary>Bind textures </summary>
        public virtual void Bind(IGLRenderableItem ri, IGLProgramShader shader, GLMatrixCalc c)
        {
            GL.BindTextures(TextureBind, Textures.Length, Textures);
        }

        private int[] Textures;
    }

    /// <summary>
    /// Bind color data
    /// </summary>
    
    public class GLRenderDataColor : IGLRenderItemData
    {
        /// <summary>Color bind number </summary>
        public int ColorBind { get; set; }
        
        /// <summary>Constructor</summary>  
        public GLRenderDataColor(System.Drawing.Color c, int uniformbinding = 25) 
        {
            col = c;
            ColorBind = uniformbinding;
        }

        /// <summary>Bind color data to uniform </summary>
        public virtual void Bind(IGLRenderableItem ri, IGLProgramShader shader, GLMatrixCalc c)
        {
            GL.ProgramUniform4(shader.GetShader(ShaderType.FragmentShader).Id, ColorBind, col.ToVector4());
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

        private System.Drawing.Color col;
    }

    /// <summary>
    /// World Position and Color Index bind into one Vector4
    /// </summary>

    public class GLRenderDataWorldPositionColor : IGLRenderItemData
    {
        /// <summary>World position</summary>
        public Vector3 WorldPosition { get; set; }

        /// <summary>Color index number (w in Vector4) </summary>
        public int ColorIndex { get; set; } = 0;

        /// <summary>Position bind number (xyz in Vector4) </summary>
        public int PositionBind { get; set; } = 22;

        /// <summary>Constructor</summary>  
        public GLRenderDataWorldPositionColor()
        {
        }

        /// <summary>Bind to Vector4 Position and ColorIndex to uniforms </summary>
        public virtual void Bind(IGLRenderableItem ri, IGLProgramShader shader, GLMatrixCalc c)
        {
            GL.ProgramUniform4(shader.GetShader(ShaderType.VertexShader).Id, PositionBind, new Vector4(WorldPosition.X, WorldPosition.Y, WorldPosition.Z,ColorIndex));
        }
    }

}
