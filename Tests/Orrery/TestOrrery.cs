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
using GLOFC.Utils;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Drawing;
using System.Windows.Forms;

// A simpler main for demoing

namespace TestOpenTk
{
    public partial class TestOrrery : Form
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
        KeplerOrbitElements[] bodies;
        GLRenderDataTranslationRotationTexture[] bodypositions;
        double currentjd;
        double jdscaling;

        float worldsize = 200e9f;        // size of playfield in meters
        float mscaling = 1 / 1e6f;       // convert to units used by GL, 1e6 = 1e11 m, earth is at 1.49e11 m.  1000km = 1 unit
        float sunscaleup = 10;
        float planetscaleup = 400; //180;

        float sunradiusm = 696340000;    // m
        float earthradiusm = 6371000;   //m
        double Msol = 1.989e30;
        double AU = 149597870.7;        // km https://en.wikipedia.org/wiki/Astronomical_unit

        public TestOrrery()
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
            gl3dcontroller.ZoomDistance = 3000F;
            gl3dcontroller.Start(matrixcalc, displaycontrol, new Vector3(0, 0, 0), new Vector3(135f, 0, 0f), 0.025F);
            gl3dcontroller.KeyboardTravelSpeed = (ms, eyedist) =>
            {
                return (float)ms * 100.0f;
            };

            items.Add(new GLColorShaderWithWorldCoord(), "COSW");
            items.Add(new GLTexturedShaderWithObjectCommonTranslation(), "TEXOCT");

            items.Add(new GLTexture2D(Properties.Resources.golden, SizedInternalFormat.Rgba8), "golden");
            items.Add(new GLTexture2D(Properties.Resources.moonmap1k, SizedInternalFormat.Rgba8), "moon");

            #region coloured lines

            int gridsize = (int)(worldsize * mscaling);
            int markers = gridsize / 20;

            {
                GLRenderState lines = GLRenderState.Lines(1);

                Color gridcolour = Color.FromArgb(255, 60, 60, 60);
                rObjects.Add(items.Shader("COSW"),
                                GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(-gridsize, -0, -gridsize), new Vector3(-gridsize, -0, gridsize), new Vector3(markers, 0, 0), gridsize / markers * 2 + 1),
                                                        new Color4[] { gridcolour })
                                    );


                rObjects.Add(items.Shader("COSW"),
                                GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
                                    GLShapeObjectFactory.CreateLines(new Vector3(-gridsize, -0, -gridsize), new Vector3(gridsize, -0, -gridsize), new Vector3(0, 0, markers), gridsize / markers * 2 + 1),
                                                        new Color4[] { gridcolour }));
            }

            rObjects.Add(new GLOperationClearDepthBuffer());

            {
                GLRenderState rt = GLRenderState.Tri();

                float sunscaled = sunradiusm * mscaling * sunscaleup;
                rObjects.Add(items.Shader("TEXOCT"), "sun",
                        GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Triangles, rt,
                        GLSphereObjectFactory.CreateTexturedSphereFromTriangles(3, sunscaled),
                            new GLRenderDataTranslationRotationTexture(items.Tex("golden"), new Vector3(0, 0, 0))
                            ));
            }


            #endregion

            {   // debug check
                DateTime t = new DateTime(2000, 1, 1, 12, 0, 0);
                double jd = t.ToJulianDate();
                DateTime t2 = jd.JulianToDateTime();
                System.Diagnostics.Debug.WriteLine($"Date time {t} JD {jd} back to {t2}");
            }


            items.Add(new GLMatrixCalcUniformBlock(), "MCUB");     // create a matrix uniform block 

            currentjd = KeplerOrbitElements.J2000;
            jdscaling = 0;
            bodies = new KeplerOrbitElements[4];
            bodypositions = new GLRenderDataTranslationRotationTexture[4];

            // earth
            bodies[0] = new KeplerOrbitElements(true, semimajoraxiskm: 0.38709893 * AU,  // https://nssdc.gsfc.nasa.gov/planetary/factsheet/mercuryfact.html
                                                                 eccentricity: 0.20563069,
                                                                 inclination: 7.00487,
                                                                 longitudeofascendingnode: 48.33167,
                                                                 longitudeofperihelion: 77.45645,
                                                                 meanlongitude: 252.25084,
                                                                 currentjd);
            bodies[1] = new KeplerOrbitElements(true, semimajoraxiskm: 0.72333199 * AU,  // https://nssdc.gsfc.nasa.gov/planetary/factsheet/venusfact.html
                                                                 eccentricity: 0.00677323,
                                                                 inclination: 3.39471,
                                                                 longitudeofascendingnode: 76.68069,
                                                                 longitudeofperihelion: 131.53298,
                                                                 meanlongitude: 181.97973,
                                                                 currentjd);
            bodies[2] = new KeplerOrbitElements(true, semimajoraxiskm: 1.49596E+08,  // https://nssdc.gsfc.nasa.gov/planetary/factsheet/earthfact.html
                                                                 eccentricity: 0.0167086,
                                                                 inclination: 0.00005,
                                                                 longitudeofascendingnode: -11.26064,
                                                                 longitudeofperihelion: 102.94719,
                                                                 meanlongitude: 100.46435,
                                                                 currentjd);
            bodies[3] = new KeplerOrbitElements(true, semimajoraxiskm: 1.52366231 * AU,  // https://nssdc.gsfc.nasa.gov/planetary/factsheet/marsfact.html
                                                                 eccentricity: 0.09341233,
                                                                 inclination: 1.85061,
                                                                 longitudeofascendingnode: 49.57854,
                                                                 longitudeofperihelion: 336.04084,
                                                                 meanlongitude: 355.45332,
                                                                 currentjd);

            float planetsize = earthradiusm * mscaling * planetscaleup;

            for (int i = 0; i < bodies.Length; i++)
            {
                bodies[i].CentralMass = Msol;

                System.Diagnostics.Debug.WriteLine($"{i} {bodies[i].OrbitalPeriodS/60/60/24} {bodies[i].CalculateMass(bodies[i].OrbitalPeriodS)}");

                bodypositions[i] = new GLRenderDataTranslationRotationTexture(items.Tex("moon"), new Vector3(0, 0, 0));

                GLRenderState rt = GLRenderState.Tri();
                rObjects.Add(items.Shader("TEXOCT"), "Body" + i,
                        GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Triangles, rt,
                        GLSphereObjectFactory.CreateTexturedSphereFromTriangles(3, planetsize), bodypositions[i]));

                Vector4[] orbit = bodies[i].Orbit(currentjd, 1, mscaling);

                GLRenderState lines = GLRenderState.Lines(1);

                rObjects.Add(items.Shader("COSW"),
                                GLRenderableItem.CreateVector4Color4(items, PrimitiveType.LineStrip, lines, orbit, new Color4[] { Color.FromArgb(255,128,0,0) }));

            }

            systemtimer.Interval = 25;
            systemtimer.Tick += new EventHandler(SystemTick);
            systemtimer.Start();
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

            this.Text = "Looking at " + gl3dcontroller.MatrixCalc.LookAt + " from " + gl3dcontroller.MatrixCalc.EyePosition + " cdir " + gl3dcontroller.PosCamera.CameraDirection + " azel " + azel + " zoom " + gl3dcontroller.PosCamera.ZoomFactor + " dist " + gl3dcontroller.MatrixCalc.EyeDistance + " FOV " + gl3dcontroller.MatrixCalc.FovDeg;
        }

        private void SystemTick(object sender, EventArgs e )
        {
            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true, OtherKeys);
            status.Text = $"JD {currentjd:#0000000.00000} {currentjd.JulianToDateTime()}";

            Vector3d[] positions = new Vector3d[bodies.Length];

            for (int i = 0; i < bodies.Length; i++)
            {
                positions[i] = bodies[i].ToCartesian(currentjd);
                // Kepler works around the XY plane, openGL uses the XZ plane
                Vector3 pos3 = new Vector3((float)(positions[i].X * mscaling), (float)(positions[i].Z * mscaling), (float)(positions[i].Y * mscaling));
                bodypositions[i].Position = pos3;
            }

            datalabel.Text = $"Mercury-Sol {positions[0].Length / 1000:N0} km" + Environment.NewLine +
                             $"Venus-Sol {positions[1].Length / 1000:N0} km" + Environment.NewLine +
                             $"Earth-Sol {positions[2].Length / 1000:N0} km" + Environment.NewLine +
                             $"Mars-Sol {positions[3].Length / 1000:N0} km" + Environment.NewLine +
                             $"Earth-Mars {(positions[3]-positions[2]).Length / 1000:N0} km";
            gl3dcontroller.Redraw();
        }

        private void OtherKeys( GLOFC.Controller.KeyboardMonitor kb )
        {
            if (kb.HasBeenPressed(Keys.F4, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.ChangePerspectiveMode(!gl3dcontroller.MatrixCalc.InPerspectiveMode);
            }


            if (kb.HasBeenPressed(Keys.O, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                System.Diagnostics.Debug.WriteLine("Order to 90");
                gl3dcontroller.Pan(new Vector2(90, 0), 3);
            }
            if (kb.HasBeenPressed(Keys.P, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                System.Diagnostics.Debug.WriteLine("Order to -180");
                gl3dcontroller.Pan(new Vector2(90, 180), 3);
            }

            //System.Diagnostics.Debug.WriteLine("kb check");

        }

    }

}


