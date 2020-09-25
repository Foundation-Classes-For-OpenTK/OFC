/*
 * Copyright 2020 Robbyxp1 @ github.com
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

using OpenTK.Graphics.OpenGL4;

namespace OFC.GL4
{
    // Wraps the openGL main state variables in a class so they get selected correctly for each render.

    public class GLRenderControl 
    {
        // static creates of a GLRenderControl for the various OpenGL primitives

        static public GLRenderControl Tri(FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderControl(PrimitiveType.Triangles) { FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }

        static public GLRenderControl Tri(GLRenderControl prev, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderControl(prev,PrimitiveType.Triangles) { FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }


        static public GLRenderControl TriStrip(FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderControl(PrimitiveType.TriangleStrip) { FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }

        static public GLRenderControl TriStrip(GLRenderControl prev, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderControl(prev, PrimitiveType.TriangleStrip) { FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }


        static public GLRenderControl TriStrip(uint primitiverestart, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderControl(PrimitiveType.TriangleStrip) { PrimitiveRestart = primitiverestart, FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }

        static public GLRenderControl TriStrip(GLRenderControl prev, uint primitiverestart, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderControl(prev,PrimitiveType.TriangleStrip) { PrimitiveRestart = primitiverestart, FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }


        static public GLRenderControl TriStrip(DrawElementsType primitiverestarttype, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderControl(PrimitiveType.TriangleStrip) { PrimitiveRestart = GL4Statics.DrawElementsRestartValue(primitiverestarttype), FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }

        static public GLRenderControl TriStrip(GLRenderControl prev, DrawElementsType primitiverestarttype, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderControl(prev, PrimitiveType.TriangleStrip) { PrimitiveRestart = GL4Statics.DrawElementsRestartValue(primitiverestarttype), FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }


        static public GLRenderControl TriFan(FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderControl(PrimitiveType.TriangleFan) { FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }

        static public GLRenderControl TriFan(GLRenderControl prev, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderControl(prev, PrimitiveType.TriangleFan) { FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }


        static public GLRenderControl TriFan(DrawElementsType primitiverestarttype, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderControl(PrimitiveType.TriangleFan) { PrimitiveRestart = GL4Statics.DrawElementsRestartValue(primitiverestarttype), FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }

        static public GLRenderControl TriFan(GLRenderControl prev, DrawElementsType primitiverestarttype, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderControl(prev, PrimitiveType.TriangleFan) { PrimitiveRestart = GL4Statics.DrawElementsRestartValue(primitiverestarttype), FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }


        static public GLRenderControl Quads(FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderControl(PrimitiveType.Quads) { FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }

        static public GLRenderControl Quads(GLRenderControl prev, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderControl(prev, PrimitiveType.Quads) { FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }


        static public GLRenderControl Points(float pointsize = 1, bool smooth = true)
        { return new GLRenderControl(PrimitiveType.Points) { PointSize = pointsize, PointSprite = false, PointSmooth = smooth }; }

        static public GLRenderControl Points(GLRenderControl prev, float pointsize = 1, bool smooth = true)
        { return new GLRenderControl(prev, PrimitiveType.Points) { PointSize = pointsize, PointSprite = false, PointSmooth = smooth }; }


        static public GLRenderControl PointsByProgram(bool pointsprite = false, bool smooth = true)
        { return new GLRenderControl(PrimitiveType.Points) { PointSize = 0, PointSprite = false, PointSmooth = smooth }; }

        static public GLRenderControl PointsByProgram(GLRenderControl prev, bool pointsprite = false, bool smooth = true)
        { return new GLRenderControl(prev, PrimitiveType.Points) { PointSize = 0, PointSprite = false, PointSmooth = smooth }; }


        static public GLRenderControl PointSprites(bool depthtest = true)
        { return new GLRenderControl(PrimitiveType.Points) { PointSize = 0, PointSprite = true, DepthTest = depthtest }; }

        static public GLRenderControl PointSprites(GLRenderControl prev, bool depthtest = true)
        { return new GLRenderControl(prev,PrimitiveType.Points) { PointSize = 0, PointSprite = true, DepthTest = depthtest }; }


        static public GLRenderControl Patches(int patchsize = 4, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true, PolygonMode polygonmode = PolygonMode.Fill)
        { return new GLRenderControl(PrimitiveType.Patches) { PatchSize = patchsize, FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode }; }

        static public GLRenderControl Patches(GLRenderControl prev, int patchsize = 4, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true, PolygonMode polygonmode = PolygonMode.Fill)
        { return new GLRenderControl(prev,PrimitiveType.Patches) { PatchSize = patchsize, FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode }; }


        static public GLRenderControl Lines(float linewidth = 1, bool smooth = true)        // vertex 0/1 line, 2/3 line
        { return new GLRenderControl(PrimitiveType.Lines) { LineWidth = linewidth, LineSmooth = smooth }; }

        static public GLRenderControl Lines(GLRenderControl prev, float linewidth = 1, bool smooth = true)        // vertex 0/1 line, 2/3 line
        { return new GLRenderControl(prev, PrimitiveType.Lines) { LineWidth = linewidth, LineSmooth = smooth }; }


        static public GLRenderControl LineLoop(float linewidth = 1, bool smooth = true)     // vertex 0->1->2->0
        { return new GLRenderControl(PrimitiveType.LineLoop) { LineWidth = linewidth, LineSmooth = smooth }; }

        static public GLRenderControl LineLoop(GLRenderControl prev, float linewidth = 1, bool smooth = true)     // vertex 0->1->2->0
        { return new GLRenderControl(prev,PrimitiveType.LineLoop) { LineWidth = linewidth, LineSmooth = smooth }; }


        static public GLRenderControl LineStrip(float linewidth = 1, bool smooth = true)    // vertex 0->1->2
        { return new GLRenderControl(PrimitiveType.LineStrip) { LineWidth = linewidth, LineSmooth = smooth }; }

        static public GLRenderControl LineStrip(GLRenderControl prev, float linewidth = 1, bool smooth = true)    // vertex 0->1->2
        { return new GLRenderControl(prev,PrimitiveType.LineStrip) { LineWidth = linewidth, LineSmooth = smooth }; }

        // geoshaders which change the primitive type need the values for the output, but a different input type
        // good for any triangle type at the geoshader output

        static public GLRenderControl ToTri(PrimitiveType t, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true, PolygonMode polygonmode = PolygonMode.Fill)
        { return new GLRenderControl(t) { FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode }; }

        static public GLRenderControl ToTri(GLRenderControl prev, PrimitiveType t, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true, PolygonMode polygonmode = PolygonMode.Fill)
        { return new GLRenderControl(prev, t) { FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode }; }

        // creators

        public GLRenderControl()                                          // fully default 
        {
            PrimitiveType = PrimitiveType.Points;
        }

        public GLRenderControl(PrimitiveType p)                           // use default fixed settings with primitive type
        {
            PrimitiveType = p;
        }

        public GLRenderControl(GLRenderControl prev, PrimitiveType p)      // copy previous fixed settings
        {
            PrimitiveRestart = prev.PrimitiveRestart;
            ClipDistanceEnable = prev.ClipDistanceEnable;
            DepthTest = prev.DepthTest;
            DepthFunctionMode = prev.DepthFunctionMode;
            WriteDepthBuffer = prev.WriteDepthBuffer;
            DepthClamp = prev.DepthClamp;
            BlendEnable = prev.BlendEnable;
            BlendSourceRGB = prev.BlendSourceRGB;
            BlendSourceA = prev.BlendSourceA;
            BlendDestRGB = prev.BlendDestRGB;
            BlendDestA = prev.BlendDestA;
            BlendEquationA = prev.BlendEquationA;
            BlendEquationRGB = prev.BlendEquationRGB;
            ColorMasking = prev.ColorMasking;
            PrimitiveType = p;
        }

        static public GLRenderControl Start()
        {
            var startstate = new GLRenderControl(PrimitiveType.Points)        // Set the default state we want to be in at start (some state defaults are at bottom)
            {
                FrontFace = FrontFaceDirection.Ccw,
                CullFace = true,
                PolygonModeFrontAndBack = PolygonMode.Fill,
                PatchSize = 1,
                PointSize = 1,
                PointSprite = false,
                PointSmooth = true,
                PolygonSmooth = false,
                LineWidth = 1,
                LineSmooth = true,
            };

            var curstate = new GLRenderControl(PrimitiveType.Points)        // set to be purposely not default constructor state and the above state 
            {                                                               // to make all get set.
                PrimitiveRestart = 1,
                ClipDistanceEnable = 1,
                DepthTest = false,
                DepthFunctionMode = DepthFunction.Never,
                WriteDepthBuffer = false,
                DepthClamp = true,
                BlendEnable = false,
                BlendSourceRGB = BlendingFactorSrc.ConstantAlpha,
                BlendSourceA = BlendingFactorSrc.ConstantAlpha,
                BlendDestRGB = BlendingFactorDest.ConstantAlpha,
                BlendDestA = BlendingFactorDest.ConstantAlpha,
                BlendEquationA = BlendEquationMode.Min,
                BlendEquationRGB = BlendEquationMode.Min,
                ColorMasking = 0,
            };

            curstate.ApplyState(startstate);        // from curstate, apply state

            return startstate;
        }

        public void ApplyState( GLRenderControl newstate)      // apply deltas to GL
        {
            // general

            //System.Diagnostics.Debug.WriteLine("Apply " + newstate.PrimitiveType + " Fixed state " + newstate.ApplyFixed);

            if (newstate.PrimitiveRestart != PrimitiveRestart)
            {
                //System.Diagnostics.Debug.WriteLine("Set PR to {0}", newstate.PrimitiveRestart);
                if (newstate.PrimitiveRestart.HasValue)         // is new state has value
                {
                    if (PrimitiveRestart == null)              // if last was off, turn it on
                        GL.Enable(EnableCap.PrimitiveRestart);

                    GL.PrimitiveRestartIndex(newstate.PrimitiveRestart.Value);  // set
                }
                else
                    GL.Disable(EnableCap.PrimitiveRestart);     // else disable

                PrimitiveRestart = newstate.PrimitiveRestart;
            }

            if (ClipDistanceEnable != newstate.ClipDistanceEnable)        // if changed
            {
                if (newstate.ClipDistanceEnable > ClipDistanceEnable)
                {
                    for (int i = ClipDistanceEnable; i < newstate.ClipDistanceEnable; i++)
                        GL.Enable(EnableCap.ClipDistance0 + i);
                }
                else if (newstate.ClipDistanceEnable < ClipDistanceEnable)
                {
                    for (int i = ClipDistanceEnable - 1; i >= newstate.ClipDistanceEnable; i--)
                        GL.Disable(EnableCap.ClipDistance0 + i);
                }

                ClipDistanceEnable = newstate.ClipDistanceEnable;
            }

            if (DepthTest != newstate.DepthTest)
            {
                DepthTest = newstate.DepthTest;
                GLStatics.SetEnable(OpenTK.Graphics.OpenGL.EnableCap.DepthTest, DepthTest);
                //  System.Diagnostics.Debug.WriteLine("Depth Test " + DepthTest);
            }

            if (DepthFunctionMode != newstate.DepthFunctionMode)
            {
                DepthFunctionMode = newstate.DepthFunctionMode;
                GL.DepthFunc(DepthFunctionMode);
            }

            if (WriteDepthBuffer != newstate.WriteDepthBuffer)
            {
                WriteDepthBuffer = newstate.WriteDepthBuffer;
                GL.DepthMask(WriteDepthBuffer);
            }

            if (DepthClamp != newstate.DepthClamp)
            {
                DepthClamp = newstate.DepthClamp;
                GLStatics.SetEnable(OpenTK.Graphics.OpenGL.EnableCap.DepthClamp, DepthClamp);
                // System.Diagnostics.Debug.WriteLine("Depth Clamp" + DepthClamp.Value);
            }

            if (BlendEnable != newstate.BlendEnable)
            {
                BlendEnable = newstate.BlendEnable;
                GLStatics.SetEnable(OpenTK.Graphics.OpenGL.EnableCap.Blend, BlendEnable);
            }

            // blend on, we have new values for blend equation
            if (BlendEnable == true)
            {
                if (BlendSourceRGB != newstate.BlendSourceRGB || BlendDestRGB != newstate.BlendDestRGB ||
                    BlendSourceA != newstate.BlendSourceA || BlendDestA != newstate.BlendDestA)
                {
                    BlendSourceRGB = newstate.BlendSourceRGB;
                    BlendDestRGB = newstate.BlendDestRGB;
                    BlendSourceA = newstate.BlendSourceA;
                    BlendDestA = newstate.BlendDestA;
                    GL.BlendFuncSeparate(BlendSourceRGB, BlendDestRGB, BlendSourceA, BlendDestA);
                }

                if (BlendEquationRGB != newstate.BlendEquationRGB || BlendEquationA != newstate.BlendEquationA)
                {
                    BlendEquationRGB = newstate.BlendEquationRGB;
                    BlendEquationA = newstate.BlendEquationA;
                    GL.BlendEquationSeparate(BlendEquationRGB, BlendEquationA);
                }
            }

            if (ColorMasking != newstate.ColorMasking)
            {
                ColorMasking = newstate.ColorMasking;

                GL.ColorMask((ColorMasking & ColorMask.Red) == ColorMask.Red, (ColorMasking & ColorMask.Green) == ColorMask.Green,
                                    (ColorMasking & ColorMask.Blue) == ColorMask.Blue, (ColorMasking & ColorMask.Alpha) == ColorMask.Alpha);
            }

            // ---------------------------------- Below are default null

            // Geo shaders

            if (newstate.PatchSize.HasValue && PatchSize != newstate.PatchSize )
            {
                PatchSize = newstate.PatchSize;
                GL.PatchParameter(PatchParameterInt.PatchVertices, PatchSize.Value);
            }

            // points

            if (newstate.PointSize.HasValue && PointSize != newstate.PointSize )
            {
                if ( newstate.PointSize>0 )     // if fixed point size
                {
                    if ( PointSize == null || newstate.PointSize != PointSize.Value )
                        GL.PointSize(newstate.PointSize.Value); // set if different

                    if ( PointSize == null || PointSize == 0 )       // if previous was off
                        GL.Disable(EnableCap.ProgramPointSize);
                }
                else
                    GL.Enable(EnableCap.ProgramPointSize);

                PointSize = newstate.PointSize;
            }

            if (newstate.PointSprite.HasValue && PointSprite != newstate.PointSprite )
            {
                PointSprite = newstate.PointSprite;
                GLStatics.SetEnable(OpenTK.Graphics.OpenGL.EnableCap.PointSprite, PointSprite.Value);
            }

            if (newstate.PointSmooth.HasValue && PointSmooth != newstate.PointSmooth)
            {
                PointSmooth = newstate.PointSmooth;
                GLStatics.SetEnable(OpenTK.Graphics.OpenGL.EnableCap.PointSmooth, PointSmooth.Value);
            }

            // lines

            if (newstate.LineWidth.HasValue && LineWidth != newstate.LineWidth)
            {
                LineWidth = newstate.LineWidth;
                GL.LineWidth(LineWidth.Value);
            }

            if (newstate.LineSmooth.HasValue && LineSmooth != newstate.LineSmooth)
            {
                LineSmooth = newstate.LineSmooth;
                GLStatics.SetEnable(OpenTK.Graphics.OpenGL.EnableCap.LineSmooth, LineSmooth.Value);
            }

            // triangles

            if (newstate.PolygonModeFrontAndBack.HasValue && PolygonModeFrontAndBack != newstate.PolygonModeFrontAndBack)
            {
                PolygonModeFrontAndBack = newstate.PolygonModeFrontAndBack;
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonModeFrontAndBack.Value);
            }

            if (newstate.PolygonSmooth.HasValue && PolygonSmooth != newstate.PolygonSmooth)
            {
                PolygonSmooth = newstate.PolygonSmooth;
                GLStatics.SetEnable(OpenTK.Graphics.OpenGL.EnableCap.PolygonSmooth, PolygonSmooth.Value);
            }

            if (newstate.CullFace.HasValue && CullFace != newstate.CullFace)
            {
                CullFace = newstate.CullFace;
                GLStatics.SetEnable(OpenTK.Graphics.OpenGL.EnableCap.CullFace, CullFace.Value);
                // System.Diagnostics.Debug.WriteLine("Cull mode " + CullFace.Value);
            }

            if (newstate.FrontFace.HasValue && FrontFace != newstate.FrontFace)
            {
                FrontFace = newstate.FrontFace;
                GL.FrontFace(FrontFace.Value);
            }
        }

        public PrimitiveType PrimitiveType { get; set; }             // Draw type for front end - may not be draw type after geo shader note

        // these are only set for particular primitive types - so the default construction is don't care.

        public int? PatchSize { get; set; } = null;                 // patches (Geo shaders)
        public float? PointSize { get; set; } = null;               // points
        public bool? PointSprite { get; set; } = null;              // points
        public bool? PointSmooth { get; set; } = null;              // points
        public float? LineWidth { get;  set;} = null;               // lines
        public bool? LineSmooth { get; set; } = null;               // lines
        public PolygonMode? PolygonModeFrontAndBack { get; set; } = null;        // triangles/quads
        public bool? PolygonSmooth { get; set; } = null;            // triangles/quads, not normally set
        public bool? CullFace { get; set; } = null;                 // triangles/quads
        public FrontFaceDirection? FrontFace { get; set; } = null;  // triangles/quads

        // Fixed set - these affect all types so are configured to their defaults for normal drawing

        public uint? PrimitiveRestart { get; set; } = null;         // its either null (disabled) or value (enabled). null does not mean don't care.
        public int ClipDistanceEnable { get; set; } = 0;            // set for number of clip/cull distances to enable. 0 means none. 
        public bool DepthTest { get; set; } = true;
        public DepthFunction DepthFunctionMode { get; set; } = DepthFunction.Less;
        public bool WriteDepthBuffer { get; set; } = true;
        public bool DepthClamp { get; set; } = false;              
        public bool BlendEnable { get;  set; } = true;
        public BlendingFactorSrc BlendSourceRGB { get; set; } = BlendingFactorSrc.SrcAlpha;
        public BlendingFactorDest BlendDestRGB { get; set; } = BlendingFactorDest.OneMinusSrcAlpha;
        public BlendingFactorSrc BlendSourceA { get; set; } = BlendingFactorSrc.SrcAlpha;
        public BlendingFactorDest BlendDestA { get; set; } = BlendingFactorDest.OneMinusSrcAlpha;
        public BlendEquationMode BlendEquationRGB { get; set; } = BlendEquationMode.FuncAdd;
        public BlendEquationMode BlendEquationA { get; set; } = BlendEquationMode.FuncAdd;
       
        [System.Flags] public enum ColorMask { Red=1,Green=2,Blue=4,Alpha=8};
        public ColorMask ColorMasking = ColorMask.Red | ColorMask.Green | ColorMask.Blue | ColorMask.Alpha;

    }

}
