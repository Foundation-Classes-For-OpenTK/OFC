﻿/*
 * Copyright 2019-2021 Robbyxp1 @ github.com
 * Part of the EDDiscovery Project
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

using GLOFC;
using GLOFC.Controller;
using GLOFC.GL4;
using GLOFC.GL4.Controls;
using Newtonsoft.Json.Linq;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

// A simpler main for demoing

namespace TestOpenTk
{
    public partial class TestOrreryImport : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3Dd gl3dcontroller;
        GLControlDisplay displaycontrol;
        GLLabel status;
        GLLabel datalabel;
        //GLLabel earthmars;

        private Timer systemtimer = new Timer();

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();
        GLMatrixCalc matrixcalc;
        List<KeplerOrbitElements> bodylist = new List<KeplerOrbitElements>();
        GLRenderDataTranslationRotationTexture[] bodypositions;
        GLRenderDataWorldPositionColor[] orbitpositions;

        double currentjd;
        double jdscaling;

        public const double oneAU_m = 149597870700;
        float worldsize = 3000e9f;        // size of playfield in meters
        float gridlines = 50e9f;              // every 10m km
        float mscaling = 1 / 1e6f;       // convert to units used by GL, 1e6 = 1e11 m, earth is at 1.49e11 m.  1,000,000m = 1000km = 1 unit

        int track = -1;

        public TestOrreryImport()
        {
            InitializeComponent();

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer);
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Closed += ShaderTest_Closed;

            matrixcalc = new GLMatrixCalc();
            matrixcalc.PerspectiveNearZDistance = 1f;
            matrixcalc.PerspectiveFarZDistance = worldsize * 2;
            matrixcalc.InPerspectiveMode = true;
            matrixcalc.ResizeViewPort(this, glwfc.Size);

            displaycontrol = new GLControlDisplay(items, glwfc, matrixcalc);       // hook form to the window - its the master, it takes its size fro mc.ScreenCoordMax
            displaycontrol.Focusable = true;          // we want to be able to focus and receive key presses.
            displaycontrol.Name = "displaycontrol";

            gl3dcontroller = new Controller3Dd();
            gl3dcontroller.PaintObjects = ControllerDraw;
            gl3dcontroller.ZoomDistance = 4000F;
            gl3dcontroller.PosCamera.ZoomMin = 0.001f;
            gl3dcontroller.PosCamera.ZoomScaling = 1.05f;
            gl3dcontroller.Start(matrixcalc, displaycontrol, new Vector3d(0, 0, 0), new Vector3d(135f, 0, 0f), 0.025F);
            gl3dcontroller.KeyboardTravelSpeed = (ms, eyedist) =>
            {
                double eyedistr = Math.Pow(eyedist, 1.0);
                float v = (float)Math.Max(eyedistr / 1200, 0);
                //System.Diagnostics.Debug.WriteLine("Speed " + eyedistr + " "+ v);
                return (float)ms * v;
            };

            displaycontrol.Paint += (o, ts) =>        // subscribing after Controller start means we paint over the scene
            {
                // MCUB set up by Controller3DDraw which did the work first
                System.Diagnostics.Debug.WriteLine("Controls Draw");
                displaycontrol.Render(glwfc.RenderState, ts);
            };

            double startspeed = 60 * 60 * 6; // in sec
            GLImage minus = new GLImage("plus", new Rectangle(0, 0, 32, 32), Properties.Resources.GoBackward);
            minus.MouseClick += (e1, m1) => { currentjd -= 365; };
            displaycontrol.Add(minus);
            GLImage back = new GLImage("back", new Rectangle(40, 0, 32, 32), Properties.Resources.Backwards);
            back.MouseClick += (e1, m1) => { if (jdscaling > 0) jdscaling /= 2; else if (jdscaling < 0) jdscaling *= 2; else jdscaling = -startspeed; };
            displaycontrol.Add(back);
            GLImage pause = new GLImage("back", new Rectangle(80, 0, 32, 32), Properties.Resources.Pause);
            pause.MouseClick += (e1, m1) => { jdscaling = 0; };
            displaycontrol.Add(pause);
            GLImage fwd = new GLImage("fwd", new Rectangle(120, 0, 32, 32), Properties.Resources.Forward);
            fwd.MouseClick += (e1, m1) => { if (jdscaling < 0) jdscaling /= 2; else if (jdscaling > 0) jdscaling *= 2; else jdscaling = startspeed; };
            displaycontrol.Add(fwd);
            GLImage plus = new GLImage("plus", new Rectangle(160, 0, 32, 32), Properties.Resources.GoForward);
            plus.MouseClick += (e1, m1) => { currentjd += 365; };
            displaycontrol.Add(plus);
            status = new GLLabel("state", new Rectangle(200, 0, 400, 20), "Label", Color.DarkOrange);
            displaycontrol.Add(status);
            datalabel = new GLLabel("datalabel", new Rectangle(0, 40, 400, 100), "", Color.DarkOrange);
            datalabel.TextAlign = ContentAlignment.TopLeft;
            displaycontrol.Add(datalabel);


            items.Add(new GLTexture2D(Properties.Resources.golden, SizedInternalFormat.Rgba8), "golden");
            items.Add(new GLTexture2D(Properties.Resources.moonmap1k, SizedInternalFormat.Rgba8), "moon");
            items.Add(new GLTexture2D(Properties.Resources.dotted, SizedInternalFormat.Rgba8), "dotted");

            #region coloured lines


            if ( true)
            {
                int gridsize = (int)(worldsize * mscaling);
                int markers = (int)(gridlines * mscaling);
                int nolines = gridsize / markers * 2 + 1;

                var shader = new GLColorShaderWithWorldCoord();
                items.Add(shader);

                GLRenderState lines = GLRenderState.Lines(1);
                lines.DepthTest = false;


                Color gridcolour = Color.FromArgb(100, 60, 60, 60);
                rObjects.Add(shader,
                                GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(-gridsize, -0, -gridsize), new Vector3(-gridsize, -0, gridsize), new Vector3(markers, 0, 0), nolines),
                                                        new Color4[] { gridcolour })
                                    );


                rObjects.Add(shader,
                                GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
                                    GLShapeObjectFactory.CreateLines(new Vector3(-gridsize, -0, -gridsize), new Vector3(gridsize, -0, -gridsize), new Vector3(0, 0, markers), nolines),
                                                        new Color4[] { gridcolour }));

                Size bmpsize = new Size(128, 30);
                var maps = new GLBitmaps("bitmap1", rObjects, bmpsize, 3, OpenTK.Graphics.OpenGL4.SizedInternalFormat.Rgba8, false, false);
                using (StringFormat fmt = new StringFormat(StringFormatFlags.NoWrap) { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
                {
                    float hsize = 20e6f * 1000 * mscaling; // million km -> m -> scaling
                    float vsize = hsize * bmpsize.Height / bmpsize.Width;

                    Font f = new Font("MS sans serif", 12f);
                    for (int i = 1; i < nolines / 2; i++)
                    {
                        maps.Add(i, (i * gridlines / 1000).ToString("N0"), f, Color.White, Color.Transparent, new Vector3(i * markers + hsize / 2, 0, vsize/2),
                                            new Vector3(hsize, 0, 0), new Vector3(0, 0, 0), fmt);
                        maps.Add(i, (i * gridlines / oneAU_m).ToString("N1") + "AU", f, Color.White, Color.Transparent, new Vector3(i * markers + hsize / 2, 0, -vsize/2),
                                            new Vector3(hsize, 0, 0), new Vector3(0, 0, 0), fmt);
                    }
                }
            }

            //rObjects.Add(new GLOperationClearDepthBuffer());


            #endregion

            items.Add(new GLMatrixCalcUniformBlock(), "MCUB");     // create a matrix uniform block 

            string para = File.ReadAllText(@"c:\code\bodies.json");
            JObject jo = JObject.Parse(para);
            OrbitalBodyInformation.AddToBodyList(jo,bodylist,0,-1);

            bodypositions = new GLRenderDataTranslationRotationTexture[bodylist.Count];

            var bodyvertSunshader = new GLPLVertexShaderTextureModelCoordsWithObjectCommonTranslation(100000, 1, 2);
            var bodysunshader = new GLShaderPipeline(bodyvertSunshader, new GLPLFragmentShaderTexture());
            items.Add(bodysunshader);
            var bodyvertplanetshader = new GLPLVertexShaderTextureModelCoordsWithObjectCommonTranslation(0.25e6f * 1000 * mscaling, 1, 10000);
            var bodyplanetshader = new GLShaderPipeline(bodyvertplanetshader, new GLPLFragmentShaderTexture());
            items.Add(bodyplanetshader);
            var baryvertshader = new GLPLVertexShaderTextureModelCoordsWithObjectCommonTranslation(0.25e6f * 1000 * mscaling, 1, 10000);
            var baryshader = new GLShaderPipeline(baryvertshader, new GLPLFragmentShaderTexture());
            items.Add(baryshader);

            var orbitlinesvertshader = new GLPLVertexShaderModelCoordWithWorldUniform(new Color[] { Color.Red, Color.Yellow });
            var orbitlineshader = new GLShaderPipeline(orbitlinesvertshader, new GLPLFragmentShaderVSColor());

            orbitpositions = new GLRenderDataWorldPositionColor[bodylist.Count];

            for (int i = 0 ; i < bodylist.Count; i++)
            {
                var kepler = bodylist[i];
                OrbitalBodyInformation ai = kepler.Tag as OrbitalBodyInformation;

                double imageradius = ai.RadiusKm * 1000 * mscaling;
                GLTextureBase img = null;

                Color orbitcolour = Color.FromArgb(255, 255, 0, 0);

                GLShaderPipeline shader;

                if ( ai.RadiusKm == 0 )
                {
                    orbitcolour = Color.Yellow;
                    imageradius = 2000e3f * mscaling;       // X km sphere
                    img = items.Tex("dotted");
                    shader = baryshader;
                }
                else if ( ai.StarClass.HasChars() )
                {
                    img = items.Tex("golden");
                    shader = bodysunshader;
                }
                else
                {
                    img = items.Tex("moon");
                    shader = bodyplanetshader;
                }

                bodypositions[i] = new GLRenderDataTranslationRotationTexture(img, new Vector3(0, 0, 0));

                System.Diagnostics.Debug.WriteLine($"Body {ai.Name} SMA {kepler.SemiMajorAxis / oneAU_m} AU Ecc {kepler.Eccentricity} Orbital Period {kepler.OrbitalPeriodS / 24 / 60 / 60 / 365} Y Radius {ai.RadiusKm} km");

                if (kepler.SemiMajorAxis > 0)
                {
                    Vector4[] orbit = bodylist[i].Orbit(currentjd, 0.1, mscaling);

                    GLRenderState lines = GLRenderState.Lines(1);
                    lines.DepthTest = false;

                    orbitpositions[i] = new GLRenderDataWorldPositionColor();
                    orbitpositions[i].ColorIndex = ai.RadiusKm > 0 ? 0 : 1;

                    var ri = GLRenderableItem.CreateVector4(items, PrimitiveType.LineStrip, lines, orbit, orbitpositions[i]);
                    // TBD need to be able to offset this with a uniform
                    rObjects.Add(orbitlineshader, ri);
                }

                GLRenderState rt = GLRenderState.Tri();
                rt.DepthTest = false;
                rObjects.Add(shader,
                        "Body" + i,
                        GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Triangles, rt,
                        GLSphereObjectFactory.CreateTexturedSphereFromTriangles(3, (float)(imageradius)), bodypositions[i]));

            }

            // need to be able to click on item and make it follow
            // for now, use 1-n to track body Nt
   
            jdscaling = 0;
            currentjd = new DateTime(2021, 11, 18, 12, 0, 0).ToJulianDate();

            systemtimer.Interval = 25;
            systemtimer.Tick += new EventHandler(SystemTick);
            systemtimer.Start();
        }
        private void ShaderTest_Closed(object sender, EventArgs e)
        {
            items.Dispose();
        }

        private void SystemTick(object sender, EventArgs e )
        {
            status.Text = $"JD {currentjd:#0000000.00000} {currentjd.JulianToDateTime()}";

            Vector3d[] positions = new Vector3d[bodylist.Count];

            for (int i = 0; i < bodylist.Count; i++)
            {
                var kepler = bodylist[i];
                OrbitalBodyInformation ai = kepler.Tag as OrbitalBodyInformation;

                if (kepler.SemiMajorAxis > 0)
                {
                    positions[i] = kepler.ToCartesian(currentjd);       // in meters around 0,0,0
                    var cbpos = positions[ai.CentralBodyIndex];         // central body position
                    positions[i] += cbpos;                              // offset

                    orbitpositions[i].WorldPosition = new Vector3((float)cbpos.X, (float)cbpos.Z, (float)cbpos.Y) * mscaling;
                    //orbitpositions[i].WorldPosition = new Vector3(50e9f*mscaling, 0, 0);

                    // Kepler works around the XY plane, openGL uses the XZ plane

                    Vector3 pos3 = new Vector3((float)(positions[i].X * mscaling), (float)(positions[i].Z * mscaling), (float)(positions[i].Y * mscaling));
                    bodypositions[i].Position = pos3;
                }
            }

            if (track >= 0)
            {
                gl3dcontroller.MoveLookAt(new Vector3d(positions[track].X, positions[track].Z, positions[track].Y) * mscaling);
            }

            datalabel.Text = $"{bodypositions[1].Position/mscaling/1000}" + Environment.NewLine;

            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true, OtherKeys,0.0001f,0.0001f);

            System.Diagnostics.Debug.WriteLine("Tick");
            gl3dcontroller.Redraw();
        }

        ulong lasttime = ulong.MaxValue;
        private void ControllerDraw(Controller3Dd mc, ulong time)
        {
            System.Diagnostics.Debug.WriteLine("Controller Draw");

            GLMatrixCalcUniformBlock mcub = (GLMatrixCalcUniformBlock)items.UB("MCUB");
            mcub.SetText(gl3dcontroller.MatrixCalc);

            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc, false);

            if (jdscaling != 0 && lasttime != ulong.MaxValue)
            {
                var diff = time - lasttime;     // ms between calls
                currentjd += diff / (60.0 * 60 * 24 * 1000) * jdscaling;        // convert ms delta to days, with scaling
            }
            lasttime = time;

            var azel = gl3dcontroller.PosCamera.EyePosition.AzEl(gl3dcontroller.PosCamera.LookAt, true);

            this.Text = $"Looking at {gl3dcontroller.PosCamera.LookAt.X / mscaling / 1000:0.},{gl3dcontroller.PosCamera.LookAt.Y / mscaling / 1000:0.},{gl3dcontroller.PosCamera.LookAt.Z / mscaling / 1000:0.} " +
                        $"from {gl3dcontroller.PosCamera.EyePosition.X / mscaling / 1000:0.},{gl3dcontroller.PosCamera.EyePosition.Y / mscaling / 1000:0.},{gl3dcontroller.PosCamera.EyePosition.Z / mscaling / 1000:0.} " +
                        $"cdir {gl3dcontroller.PosCamera.CameraDirection} azel {azel} zoom {gl3dcontroller.PosCamera.ZoomFactor} " +
                        $"dist {gl3dcontroller.PosCamera.EyeDistance / mscaling / 1000:N0}km FOV {gl3dcontroller.MatrixCalc.FovDeg}";

            this.Text += $" eye units {gl3dcontroller.MatrixCalc.EyeDistance}";
        }



        private void OtherKeys( GLOFC.Controller.KeyboardMonitor kb )
        {
            if (kb.HasBeenPressed(Keys.P, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.ChangePerspectiveMode(!gl3dcontroller.MatrixCalc.InPerspectiveMode);
            }
            var res = kb.HasBeenPressed(Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9);
            if (res != null)
            {
                for (int i = 0; i < bodylist.Count; i++)
                {
                    var kepler = bodylist[i];
                    OrbitalBodyInformation ai = kepler.Tag as OrbitalBodyInformation;
                    if (ai.Name.InvariantParseInt(-1) == res.Item1 + 1)
                    {
                        if (res.Item2 == KeyboardMonitor.ShiftState.Shift)
                            track = ai.CentralBodyIndex;
                        else
                            track = i;
                        break;
                    }
                }
            }
            if (kb.HasBeenPressed(Keys.D0, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                track = -1;
            }



            //System.Diagnostics.Debug.WriteLine("kb check");

        }

    }

}


