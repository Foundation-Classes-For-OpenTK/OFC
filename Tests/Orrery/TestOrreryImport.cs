/*
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
        private Controller3D gl3dcontroller;
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

            displaycontrol.Paint += (o, ts) =>        // subscribing after start means we paint over the scene, letting transparency work
                {
                    // MCUB set up by Controller3DDraw which did the work first
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


            gl3dcontroller = new Controller3D();
            gl3dcontroller.PaintObjects = ControllerDraw;
            gl3dcontroller.ZoomDistance = 4000F;
            gl3dcontroller.PosCamera.ZoomMin = 0.001f;
            gl3dcontroller.PosCamera.ZoomScaling = 1.05f;
            gl3dcontroller.Start(matrixcalc, displaycontrol, new Vector3(0, 0, 0), new Vector3(135f, 0, 0f), 0.025F);
            gl3dcontroller.KeyboardTravelSpeed = (ms, eyedist) =>
            {
                double eyedistr = Math.Pow(eyedist, 1.0);
                float v = (float)Math.Max(eyedistr / 1200, 0);
                //System.Diagnostics.Debug.WriteLine("Speed " + eyedistr + " "+ v);
                return (float)ms * v;
            };

            items.Add(new GLColorShaderWithWorldCoord(), "COSW");

            items.Add(new GLTexture2D(Properties.Resources.golden, SizedInternalFormat.Rgba8), "golden");
            items.Add(new GLTexture2D(Properties.Resources.moonmap1k, SizedInternalFormat.Rgba8), "moon");
            items.Add(new GLTexture2D(Properties.Resources.dotted, SizedInternalFormat.Rgba8), "dotted");

            #region coloured lines


            if ( true)
            {
                int gridsize = (int)(worldsize * mscaling);
                int markers = (int)(gridlines * mscaling);
                int nolines = gridsize / markers * 2 + 1;

                GLRenderState lines = GLRenderState.Lines(1);
                lines.DepthTest = false;

                Color gridcolour = Color.FromArgb(100, 60, 60, 60);
                rObjects.Add(items.Shader("COSW"),
                                GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(-gridsize, -0, -gridsize), new Vector3(-gridsize, -0, gridsize), new Vector3(markers, 0, 0), nolines),
                                                        new Color4[] { gridcolour })
                                    );


                rObjects.Add(items.Shader("COSW"),
                                GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
                                    GLShapeObjectFactory.CreateLines(new Vector3(-gridsize, -0, -gridsize), new Vector3(gridsize, -0, -gridsize), new Vector3(0, 0, markers), nolines),
                                                        new Color4[] { gridcolour }));
            
                var maps = new GLBitmaps("bitmap1", rObjects, new Size(128,20), 3, OpenTK.Graphics.OpenGL4.SizedInternalFormat.Rgba8, false, false);
                using (StringFormat fmt = new StringFormat(StringFormatFlags.NoWrap) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    Font f = new Font("MS sans serif", 12f);
                    for (int i = 0; i < nolines / 2; i++)
                    {
                        maps.Add(i, (i*gridlines/1000).ToString("N0"), f, Color.White, Color.Gray, new Vector3(i*markers+1500, 0, 300),
                                            new Vector3(3000, 0, 0), new Vector3(0, 0, 0), fmt);
                    }
                }
            }

            //rObjects.Add(new GLOperationClearDepthBuffer());


            #endregion

            items.Add(new GLMatrixCalcUniformBlock(), "MCUB");     // create a matrix uniform block 

            string para = File.ReadAllText(@"c:\code\bodies.json");
            JObject jo = JObject.Parse(para);

            BodyList(jo,bodylist,0,-1);

            bodypositions = new GLRenderDataTranslationRotationTexture[bodylist.Count];

            var bodyvertSunshader = new GLPLVertexShaderTextureModelCoordsWithObjectCommonTranslation(100000, 1, 2);
            var bodysunshader = new GLShaderPipeline(bodyvertSunshader, new GLPLFragmentShaderTexture());
            items.Add(bodysunshader);
            var bodyvertplanetshader = new GLPLVertexShaderTextureModelCoordsWithObjectCommonTranslation(0.25e6f * 1000 * mscaling, 1, 10000);
            var bodyplanetshader = new GLShaderPipeline(bodyvertplanetshader, new GLPLFragmentShaderTexture());
            items.Add(bodyplanetshader);
            var baryvertshader = new GLPLVertexShaderTextureModelCoordsWithObjectCommonTranslation(0.25e9f * mscaling, 1, 10000);
            var baryshader = new GLShaderPipeline(baryvertshader, new GLPLFragmentShaderTexture());
            items.Add(baryshader);


            for (int i = 0 ; i < bodylist.Count; i++)
            {
                var kepler = bodylist[i];
                AdditionalInfo ai = kepler.Tag as AdditionalInfo;

                double imageradius = ai.RadiusKm * 1000 * mscaling;
                GLTextureBase img = null;

                Color orbitcolour = Color.FromArgb(255, 255, 0, 0);

                GLShaderPipeline shader;

                if ( ai.RadiusKm == 0 )
                {
                    orbitcolour = Color.Yellow;
                    imageradius = 1000e3f*mscaling;
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
                    Vector4[] orbit = bodylist[i].Orbit(currentjd, 1, mscaling);

                    GLRenderState lines = GLRenderState.Lines(1);
                    lines.DepthTest = false;

                    // TBD need to be able to offset this with a uniform
                    rObjects.Add(items.Shader("COSW"),
                                GLRenderableItem.CreateVector4Color4(items, PrimitiveType.LineStrip, lines, orbit, new Color4[] { orbitcolour }));
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

        private void BodyList(JObject jo, List<KeplerOrbitElements> bodylist, double prevmass, int index)
        {
            var kepler = JSONtoKO(jo);
            AdditionalInfo ai = kepler.Tag as AdditionalInfo;
            if ( prevmass == 0 && kepler.SemiMajorAxis > 0 )
            {
                kepler.CentralMass = kepler.CalculateMass(ai.OrbitalPeriod);
            }
            else 
                kepler.CentralMass = prevmass;

            ai.CentralBodyIndex = index;

            index = bodylist.Count;
            bodylist.Add(kepler);

            if (jo.ContainsKey("Bodies"))
            {
                JArray ja = jo["Bodies"] as JArray;
                foreach (var o in ja)
                {
                    BodyList(o as JObject, bodylist, ai.Mass , index);
                }
            }
        }

        public class AdditionalInfo
        {
            public string Name { get; set; }                // for naming
            public string FullName { get; set; }                // for naming
            public string NodeType { get; set; }                // for naming
            public string StarClass { get; set; }                // for naming
            public string PlanetClass { get; set; }                // for naming
            public Vector3d CalculatedPosition { get; set; }    // used during calculation
            public int CentralBodyIndex { get; set; }            // central body reference
            public double Mass { get; set; } = 1;        // in KG.  Not needed for orbital parameters
            public double OrbitalPeriod { get; set; }
            public double AxialTiltDeg { get; set; }
            public double RadiusKm { get; set; }          // in km
        }

        private KeplerOrbitElements JSONtoKO(JObject json)
        {
            string time = json["Epoch"].Str();
            DateTime epoch = DateTime.Parse(time, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            KeplerOrbitElements k = new KeplerOrbitElements(true,
                    json["SemiMajorAxis"].Double(0),        // km
                    json["Eccentricity"].Double(0),
                    json["Inclination"].Double(0),
                    json["AscendingNode"].Double(0),
                    json["Periapis"].Double(0),
                    json["MeanAnomaly"].Double(0),
                    epoch.ToJulianDate()
                );
            AdditionalInfo ai = new AdditionalInfo() 
            {
                Name = json["Name"].Str(),
                FullName = json["FullName"].Str(),
                NodeType = json["NodeType"].Str() , 
                StarClass = json["StarClass"].StrNull(), PlanetClass = json["PlanetClass"].StrNull(),
                Mass = json["Mass"].Double(0),
                OrbitalPeriod = json["OrbitalPeriod"].Double(0),
                AxialTiltDeg = json["AxialTilt"].Double(0),
                RadiusKm = json["Radius"].Double(0),
            };
            k.Tag = ai;
            return k;

        }

        private void ShaderTest_Closed(object sender, EventArgs e)
        {
            items.Dispose();
        }

        ulong lasttime = ulong.MaxValue;
        private void ControllerDraw(Controller3D mc, ulong time)
        {
            //System.Diagnostics.Debug.WriteLine("Draw");

            GLMatrixCalcUniformBlock mcub = (GLMatrixCalcUniformBlock)items.UB("MCUB");
            mcub.SetText(gl3dcontroller.MatrixCalc);

            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc,false);

            if (jdscaling != 0 && lasttime != ulong.MaxValue)
            {
                var diff = time - lasttime;     // ms between calls
                currentjd += diff / (60.0 * 60 * 24 * 1000) * jdscaling;        // convert ms delta to days, with scaling
            }
            lasttime = time;

            var azel = gl3dcontroller.PosCamera.EyePosition.AzEl(gl3dcontroller.PosCamera.LookAt, true);

            this.Text = $"Looking at {gl3dcontroller.MatrixCalc.LookAt.X / mscaling / 1000:0.},{gl3dcontroller.MatrixCalc.LookAt.Y / mscaling / 1000:0.},{gl3dcontroller.MatrixCalc.LookAt.Z / mscaling / 1000:0.} " +
                        $"from {gl3dcontroller.MatrixCalc.EyePosition.X / mscaling / 1000:0.},{gl3dcontroller.MatrixCalc.EyePosition.Y / mscaling / 1000:0.},{gl3dcontroller.MatrixCalc.EyePosition.Z / mscaling / 1000:0.} "+
                        $"cdir {gl3dcontroller.PosCamera.CameraDirection} azel {azel} zoom {gl3dcontroller.PosCamera.ZoomFactor} " +
                        $"dist {gl3dcontroller.MatrixCalc.EyeDistance / mscaling/1000:N0}km FOV {gl3dcontroller.MatrixCalc.FovDeg}";

            float scale = 0.25e9f * mscaling;
            this.Text += $" {scale} eye {gl3dcontroller.MatrixCalc.EyeDistance} {gl3dcontroller.MatrixCalc.EyeDistance / scale}";
        }

        private void SystemTick(object sender, EventArgs e )
        {
            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true, OtherKeys);
            status.Text = $"JD {currentjd:#0000000.00000} {currentjd.JulianToDateTime()}";

            Vector3d[] positions = new Vector3d[bodylist.Count];

            for (int i = 0; i < bodylist.Count; i++)
            {
                var kepler = bodylist[i];
                AdditionalInfo ai = kepler.Tag as AdditionalInfo;

                if (kepler.SemiMajorAxis > 0)
                {
                    positions[i] = kepler.ToCartesian(currentjd);

                    positions[i] += positions[ai.CentralBodyIndex];     // offset by central body index

                    // Kepler works around the XY plane, openGL uses the XZ plane
                    Vector3 pos3 = new Vector3((float)(positions[i].X * mscaling), (float)(positions[i].Z * mscaling), (float)(positions[i].Y * mscaling));
                    bodypositions[i].Position = pos3;
                }
            }

            if (track >= 0)
                gl3dcontroller.PosCamera.LookAt = bodypositions[track].Position;

            datalabel.Text = $"{bodypositions[1].Position/mscaling/1000}" + Environment.NewLine;

            gl3dcontroller.Redraw();
        }

        private void OtherKeys( GLOFC.Controller.KeyboardMonitor kb )
        {
            if (kb.HasBeenPressed(Keys.P, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.ChangePerspectiveMode(!gl3dcontroller.MatrixCalc.InPerspectiveMode);
            }
            if (kb.HasBeenPressed(Keys.D0, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                track = -1;
            }
            if (kb.HasBeenPressed(Keys.D1, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                track = 1;
            }
            if (kb.HasBeenPressed(Keys.D2, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                track = 2;
            }
            if (kb.HasBeenPressed(Keys.D3, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                track = 3;
            }



            //System.Diagnostics.Debug.WriteLine("kb check");

        }

    }

}


