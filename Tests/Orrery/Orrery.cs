using GLOFC;
using GLOFC.Controller;
using GLOFC.GL4;
using GLOFC.GL4.Bitmaps;
using GLOFC.GL4.Controls;
using GLOFC.GL4.Shaders;
using GLOFC.GL4.Shaders.Basic;
using GLOFC.GL4.Shaders.Fragment;
using GLOFC.GL4.Shaders.Geo;
using GLOFC.GL4.Shaders.Vertex;
using GLOFC.GL4.ShapeFactory;
using GLOFC.Utils;
using GLOFC.WinForm;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static GLOFC.GL4.Controls.GLBaseControl;

namespace TestOpenTk
{
    public partial class Orrery
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;

        private GLItemsList items = new GLItemsList();
        private GLMatrixCalc matrixcalc;

        // Playfield

        private Controller3Dd gl3dcontroller;
        private float worldsize = 7500e9f;        // size of playfield in meters
        private float mscaling = 1 / 1e6f;       // convert meters to units used by GL, 1e6 = 1e11 m, earth is at 1.49e11 m.  1,000,000m = 1000km = 1 unit
        private float mscalingkm = 1000 / 1e6f;    // if km is used

        // Grid

        public long gridlines = 50000000000;  // m
        Grid grd;

        // Bodies
        private GLRenderProgramSortedList rbodyobjects = new GLRenderProgramSortedList();   // Render body objects
        private GLShaderPipeline bodyfindshader;
        private GLRenderableItem rbodyfindshader;
        private GLBuffer bodymatrixbuffer, sphereshapebuffer, spheretexcobuffer;
        private GLBuffer ringsmatrixbuffer, ringsshapebuffer, ringstexcobuffer;
        private GLShaderPipeline orbitlineshader, bodyshader, ringsshader;

        private const int findbufferblock = 3;
        private const int bodyimagearbblock = 4;

        //private int bodytrack = -1;

        private const float autoscalekm = 1000000;      // body distance divider for autoscaling
        private const float autoscalemin = 1;           // clamp values between these two
        private const float autoscalemax = 1000;
        private const float planetminkm = 100;          // set min/max on objectsweh
        private const float planetmaxkm = 10000000;
        private const float starminkm = 1000000;
        private const float starmaxkm = 3e6f;
        private const float baryminkm = 100000;
        private const float barymaxkm = 100000;


        // Controls 
        private GLControlDisplay displaycontrol;
        private GLLabel timedisplay;
        private GLLabel mastersystem;
        private GLLabel datalabel;
        private GLLabel status;
        private GLContextMenu rightclickmenubody;
        private GLContextMenu rightclickmenuscreen;

        // Time
        private double currentjd;
        private double jdscaling;

        public void Start(GLWinFormControl glwfc)
        {
            this.glwfc = glwfc;

            matrixcalc = new GLMatrixCalc();
            matrixcalc.PerspectiveNearZDistance = 1f;
            matrixcalc.PerspectiveFarZDistance = worldsize * 2;
            matrixcalc.InPerspectiveMode = true;
            matrixcalc.ResizeViewPort(this, glwfc.Size);

            items.Add(new GLMatrixCalcUniformBlock(), "MCUB");     // create a matrix uniform block 

            //System.Diagnostics.Debug.Assert(false, "Need to be retested after GL4 shift");

            displaycontrol = new GLControlDisplay(items, glwfc, matrixcalc, true, 0.00001f, 0.00001f);       // hook form to the window - its the master
            displaycontrol.Focusable = true;          // we want to be able to focus and receive key presses.
            displaycontrol.Font = new Font("Arial", 12);
            displaycontrol.Paint += (ts) => {
                displaycontrol.Animate(glwfc.ElapsedTimems);
                GLStatics.ClearDepthBuffer();         // clear the depth buffer, so we are on top of all previous renders.
                displaycontrol.Render(glwfc.RenderState, ts);
            };
            displaycontrol.SetFocus();

            gl3dcontroller = new Controller3Dd();
            gl3dcontroller.ZoomDistance = 20e6 * 1000 * mscaling;       // zoom 1 is X km
            gl3dcontroller.PosCamera.ZoomMin = 0.001f;
            gl3dcontroller.PosCamera.ZoomMax = 4000f;
            gl3dcontroller.PosCamera.ZoomScaling = 1.08f;
            gl3dcontroller.PaintObjects = ControllerDraw;
            gl3dcontroller.KeyboardTravelSpeed = (ms, eyedist) =>
            {
                double eyedistr = Math.Pow(eyedist, 1.0);
                float v = (float)Math.Max(eyedistr / 1200, 0);
                //System.Diagnostics.Debug.WriteLine("Speed " + eyedistr + " "+ v);
                return (float)ms * v;
            };

            // start hooks the glwfc paint function up, first, so it gets to go first
            // No ui events from glwfc.
            gl3dcontroller.Start(matrixcalc, glwfc, new Vector3d(0, 0, 0), new Vector3d(135f, 0, 0), 0.025F, false, false);
            gl3dcontroller.Hook(displaycontrol, glwfc); // we get 3dcontroller events from displaycontrol, so it will get them when everything else is unselected
            displaycontrol.Hook();  // now we hook up display control to glwin, and paint

            displaycontrol.MouseClick += MouseClickOnMap;       // grab mouse UI

            // begin

            //for ( int i = 1; i <= 10; i++ )
            //{
            //    int v = i * i;
            //    double f = (gl3dcontroller.PosCamera.ZoomMax - gl3dcontroller.PosCamera.ZoomMin) * v / 100.0 + gl3dcontroller.PosCamera.ZoomMin;
            //    System.Diagnostics.Debug.WriteLine($"{i} {v} {f}");
            //}

            //////////////////////////////////////////////////////////////////////////////////////// 
            /// MENUS
            //////////////////////////////////////////////////////////////////////////////////////// 

            double startspeed = 60 * 60 * 6; // in sec
            GLImage minus = new GLImage("timeplus1y", new Rectangle(0, 0, 32, 32), Properties.Resources.GoBackward);
            minus.MouseClick += (e1, m1) => { currentjd -= 365; };
            displaycontrol.Add(minus);
            GLImage back = new GLImage("timeback", new Rectangle(40, 0, 32, 32), Properties.Resources.Backwards);
            back.MouseClick += (e1, m1) => { if (jdscaling > 0) jdscaling /= 2; else if (jdscaling < 0) jdscaling *= 2; else jdscaling = -startspeed; };
            displaycontrol.Add(back);
            GLImage pause = new GLImage("timepause", new Rectangle(80, 0, 32, 32), Properties.Resources.Pause);
            pause.MouseClick += (e1, m1) => { jdscaling = 0; };
            displaycontrol.Add(pause);
            GLImage fwd = new GLImage("timefwd", new Rectangle(120, 0, 32, 32), Properties.Resources.Forward);
            fwd.MouseClick += (e1, m1) => { if (jdscaling < 0) jdscaling /= 2; else if (jdscaling > 0) jdscaling *= 2; else jdscaling = startspeed; };
            displaycontrol.Add(fwd);
            GLImage plus = new GLImage("timeplus1y", new Rectangle(160, 0, 32, 32), Properties.Resources.GoForward);
            plus.MouseClick += (e1, m1) => { currentjd += 365; };
            displaycontrol.Add(plus);

            GLImage sysleft = new GLImage("sysleft", new Rectangle(200, 0, 32, 32), Properties.Resources.GoBackward);
            sysleft.MouseClick += (e1, m1) => { DisplayNode(-1); };
            displaycontrol.Add(sysleft);

            mastersystem = new GLLabel("sysname", new Rectangle(230, 6, 70, 20), "All", Color.DarkOrange);
            mastersystem.TextAlign = ContentAlignment.MiddleCenter;
            displaycontrol.Add(mastersystem);

            GLImage sysright = new GLImage("sysright", new Rectangle(300, 0, 32, 32), Properties.Resources.GoForward);
            sysright.MouseClick += (e1, m1) => { DisplayNode(1); };
            displaycontrol.Add(sysright);

            timedisplay = new GLLabel("state", new Rectangle(340, 6, 800, 20), "Label", Color.DarkOrange);
            displaycontrol.Add(timedisplay);

            datalabel = new GLLabel("datalabel", new Rectangle(0, 40, 400, 100), "", Color.DarkOrange);
            datalabel.TextAlign = ContentAlignment.TopLeft;
            displaycontrol.Add(datalabel);

            status = new GLLabel("Status", new Rectangle(0, 0, 2000, 24), "x");
            status.Dock = DockingType.BottomLeft;
            status.ForeColor = Color.Orange;
            status.BackColor = Color.FromArgb(50, 50, 50, 50);
            displaycontrol.Add(status);

            rightclickmenubody = new GLContextMenu("RightClickMenuBody",
                new GLMenuItem("RCMInfo", "Information")
                {
                    MouseClick = (s, e) =>
                    {
                    }
                },
                new GLMenuItem("RCMZoomIn", "Track")
                {
                    MouseClick = (s, e) =>
                    {
                        SetBodyTrack((int)rightclickmenubody.Tag);
                    }
                },
                new GLMenuItem("RCMZoomIn", "Track Central Body")
                {
                    MouseClick = (s, e) =>
                    {
                        int body = (int)rightclickmenubody.Tag;
                        if (bodyinfo[body].ParentIndex >= 0)
                            SetBodyTrack(bodyinfo[body].ParentIndex);
                    }
                },
                new GLMenuItem("RCMZoomIn", "Zoom In")
                {
                },
                new GLMenuItem("RCMUntrack", "Untrack")
                {
                    MouseClick = (s1, e1) =>
                    {
                        SetBodyTrack(-1);
                    }
                }
                );

            rightclickmenubody.Opening += (ms,tag) =>
            {
                ms["RCMUntrack"].Enabled = IsBodyTracked();
            };

            rightclickmenuscreen = new GLContextMenu("RightClickMenuBody",
                new GLMenuItem("RCMSysDisplay", "System Display")
                {
                    MouseClick = (s, e) =>
                    {
                    }
                },
                new GLMenuItem("RCMUntrack", "Untrack")
                {
                    MouseClick = (s1, e1) =>
                    {
                        SetBodyTrack(-1);
                    }
                }
                );

            rightclickmenuscreen.Opening += (ms,tag) =>
            {
                ms["RCMUntrack"].Enabled = IsBodyTracked();
            };

            //////////////////////////////////////////////////////////////////////////////////////// 
            /// Plane grid
            //////////////////////////////////////////////////////////////////////////////////////// 

            grd = new Grid();
            grd.Create(items, worldsize, mscaling,gridlines);
           // grd.SetOffset(new Vector3(200000, 200000, 20000));
            //////////////////////////////////////////////////////////////////////////////////////// 
            /// Shaders for objects
            //////////////////////////////////////////////////////////////////////////////////////// 

            // orbit lines
            var orbitlinesvertshader = new GLPLVertexShaderModelWorldUniform(new Color[] { Color.FromArgb(128, 128, 0, 0), Color.FromArgb(128, 128, 128, 0) });
            orbitlineshader = new GLShaderPipeline(orbitlinesvertshader, new GLPLFragmentShaderVSColor());

            // textures for bodies
            var texs = items.NewTexture2D(null, Properties.Resources.golden, SizedInternalFormat.Rgba8);
            var texp = items.NewTexture2D(null, Properties.Resources.moonmap1k, SizedInternalFormat.Rgba8);
            var texb = items.NewTexture2D(null, Properties.Resources.dotted, SizedInternalFormat.Rgba8);
            var texs2 = items.NewTexture2D(null, Properties.Resources.wooden, SizedInternalFormat.Rgba8);
            var ringrocky = items.NewTexture2D(null, Properties.Resources.Rings_1_rocky, SizedInternalFormat.Rgba8);

            var p1 = new GLOFC.GL4.Textures.GLTexture2D(Properties.Resources.HMCv37, new SizeF(0.7f, 0.7f), SizedInternalFormat.Rgba8);
            items.Add(p1);

            // Make an ARB buffer with textures in them. Index select image
            var tbs = items.NewBindlessTextureHandleBlock(bodyimagearbblock);
            tbs.WriteHandles(new IGLTexture[] { texs, texp, texb, texs2, ringrocky, p1 });

            // now body shader
            // Vertex's are autoscaled, by body distance to eye, 
            // takes in: 0:Vector4 model, 1: vec2 text, 4:matrix containing position, autoscale and image no
            // out: 0:tex, 1: modelpos, 2: instance count, 4 = matrix[3][3] image number
            var bodyvertshader = new GLPLVertexShaderModelMatrixTexture(autoscalekm * 1000 * mscaling, autoscalemin, autoscalemax, useeyedistance: false);

            // takes in: 0 tex coord, 4 image index into arb text binding block
            var bodyfragshader = new GLPLFragmentShaderBindlessTexture(bodyimagearbblock, discardiftransparent: true, useprimidover2: false);

            bodyshader = new GLShaderPipeline(bodyvertshader, bodyfragshader);
            items.Add(bodyshader);

            // shape of bodies - spheres, nominal 1.0 big
            var sphereshape = GLSphereObjectFactory.CreateTexturedSphereFromTriangles(3, 1.0f);
            sphereshapebuffer = items.NewBuffer();      // fill buffer with model co-ords
            sphereshapebuffer.AllocateFill(sphereshape.Item1);
            spheretexcobuffer = items.NewBuffer(); // fill buffer with tex coords
            spheretexcobuffer.AllocateFill(sphereshape.Item2);

            // body matrix buffer for bodies - see GLPLVertexShaderModelMatrixTexture for matrix definition.  Autoscale and image index

            bodymatrixbuffer = items.NewBuffer();    // this holds the matrix to set position and size

            // find buffer 

            GLStorageBlock findbufferresults = items.NewStorageBlock(findbufferblock);
            var geofind = new GLPLGeoShaderFindTriangles(findbufferresults, 16);        // pass thru normal vert/tcs/tes then to geoshader for results
            bodyfindshader = items.NewShaderPipeline(null, bodyvertshader, null, null, geofind, null, null, null);

            // shape of rings

            ringsshader = new GLShaderPipeline(bodyvertshader, bodyfragshader);     // reuse the Shaders
            items.Add(ringsshader);

            var ringsshape = GLShapeObjectFactory.CreateQuadTriStrip(1.0f, 1.0f);
            ringsshapebuffer = items.NewBuffer();
            ringsshapebuffer.AllocateFill(ringsshape);
            ringstexcobuffer = items.NewBuffer();
            ringstexcobuffer.AllocateFill(GLShapeObjectFactory.TexTriStripQuad);

            ringsmatrixbuffer = items.NewBuffer();

            // set date

            jdscaling = 0;
            currentjd = new DateTime(2021, 11, 18, 12, 0, 0).ToJulianDate();
        }

        public void SystemTick()
        {
            Vector3d worldcentrem = new Vector3d(0, 0, 0);       // default is world centre here

            if (bodyinfo != null)
            {
                // work out positions of bodies and orbitpos centres, all in doubles

                Vector3d[] bodypositionm = new Vector3d[bodyinfo.Count];    
                Vector3d[] bodycentresm = new Vector3d[bodyinfo.Count];

                for (int i = 0; i < bodyinfo.Count; i++)
                {
                    var bi = bodyinfo[i];

                    if (bi.KeplerParameters != null)      // this body is orbiting
                    {
                        if (i > 0)              // not the root node, so a normal orbit
                        {
                            // kepler returns orbit in meters on xy plane around 0,0,0. But we have a different co-ord system (X,Y up/down,Z forward/backwards) so we need to swap
                            bodypositionm[i] = bi.KeplerParameters.ToCartesian(currentjd).Xzy;
                            var cbpos = bodypositionm[bi.ParentIndex];              // central body position
                            bodypositionm[i] += cbpos;                              // offset
                            bodycentresm[i] = cbpos;
                        }
                        else
                        {
                            // node 0 is the root node, always at 0,0,0
                            bodypositionm[i] = new Vector3d(0, 0, 0);

                            // If we remove the root node, and just display a body or barycentre which is in orbit around the root node
                            // the orbit of this node body is calculated at T0, and then the orbit position of offset so the line passes thru 0,0,0
                            var orbitpos = bi.KeplerParameters.ToCartesian(bi.KeplerParameters.T0);       // find the orbit of the root at T0 (fixed so it does not move) 
                            bodycentresm[i] = new Vector3d(-orbitpos.X, -orbitpos.Z, -orbitpos.Y);   // invert so body centre is away from 0,0,0 and swap axis
                        }
                    }
                }


                if (IsBodyTracked())
                {
                    worldcentrem = bodypositionm[bodytrackid];        // this is the offset to remove from all other positions
                    grd.SetOffset(new Vector3((float)(-worldcentrem.X*mscaling), (float)(-worldcentrem.Y*mscaling), (float)(-worldcentrem.Z*mscaling)));
                }

                Matrix4[] bodymats = new Matrix4[bodyinfo.Count];
                Matrix4[] ringsmats = new Matrix4[ringcount];

                var diffseconds = (currentjd - KeplerOrbitElements.J2000) * 86400;        // seconds since J2000 epoch for use by rotation - Frontier does not write longitude at epoch
                int ringentry = 0;

                for (int i = 0; i < bodyinfo.Count; i++)
                {
                    var bi = bodyinfo[i];
                    Vector3d bodyfinalposd = i == bodytrackid ? new Vector3d(0,0,0) : (bodypositionm[i] - worldcentrem);     // use 0,0,0 for bodytrack to prevent inaccuracies in subtraction

                    Vector3d bodycentrefinalposd = bodycentresm[i] - worldcentrem;
                    bi.orbitpos.WorldPosition = new Vector3((float)(bodycentrefinalposd.X * mscaling), (float)(bodycentrefinalposd.Y * mscaling), (float)(bodycentrefinalposd.Z * mscaling));

                    float bodyradiusm = bi.ScanNode.scandata != null && bi.ScanNode.scandata.nRadius.HasValue ? (float)(bi.ScanNode.scandata.nRadius.Value) : 1000e3f;

                    bool planet = bi.ScanNode.scandata != null && bi.ScanNode.scandata.PlanetClass.HasChars();
                    bool bary = bi.ScanNode.scandata == null || bi.ScanNode.scandata.nRadius == null;

                    double axialtilt = bi.ScanNode.scandata?.nAxialTilt ?? 0;       // axial tilt is in radians

                    float planetrot = 0;
                    if (bi.ScanNode.scandata?.nRotationPeriod != null)
                    {
                        double mod = diffseconds % bi.ScanNode.scandata.nRotationPeriod.Value;
                        mod = mod / bi.ScanNode.scandata.nRotationPeriod.Value;
                        planetrot = (float)(2 * Math.PI * mod);
                    }

                    Vector3 scaledbodypos = new Vector3((float)(bodyfinalposd.X * mscaling), (float)(bodyfinalposd.Y * mscaling), (float)(bodyfinalposd.Z * mscaling));

                    bodymats[i] = GLStaticsMatrix4.CreateMatrixPlanetRot(scaledbodypos, new Vector3(1, 1, 1), (float)axialtilt, planetrot);
                 //  System.Diagnostics.Debug.WriteLine($"Body {i} {scaledbodypos}");

                    // Min/max size of objects. And give the body size

                    bodymats[i].M14 = (bary ? baryminkm : planet ? planetminkm : starminkm) * mscalingkm;      // min scale in km, scaled to m then mscaling
                    bodymats[i].M24 = (bary ? barymaxkm : planet ? planetmaxkm : starmaxkm) * mscalingkm;       // maximum scale
                    bodymats[i].M34 = bodyradiusm * mscaling;       // set the size of the sphere - its 1.0f so scale by this
                    bodymats[i].M44 = planet ? 5 : bary ? 2 : 0;      // select image

                    if (bi.ScanNode?.scandata?.Rings != null)
                    {
                        float ringrot = 0;
                        if (bi.ScanNode.scandata?.nRotationPeriod != null)
                        {
                            double mod = diffseconds % bi.ScanNode.scandata.nRotationPeriod.Value;
                            mod = mod / (bi.ScanNode.scandata.nRotationPeriod.Value * 2);     // for now, fixed 2 times slower
                            ringrot = (float)(2 * Math.PI * mod);
                        }

                        float maxdist = (float)bi.ScanNode.scandata.Rings.Select(x => x.OuterRad).Max();

                        Matrix4 ringmat = GLStaticsMatrix4.CreateMatrixPlanetRot(scaledbodypos, new Vector3(1f, 1f, 1f), (float)axialtilt, ringrot);
                        ringmat.M14 = planetminkm * mscalingkm;
                        ringmat.M24 = planetmaxkm * mscalingkm;
                        ringmat.M34 = maxdist * mscaling;
                        ringmat.M44 = 4;
                        ringsmats[ringentry++] = ringmat;
                    }
                }

                bodymatrixbuffer.ResetFillPos();
                bodymatrixbuffer.Fill(bodymats);

                ringsmatrixbuffer.ResetFillPos();
                ringsmatrixbuffer.Fill(ringsmats);
            }

            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true, OtherKeys, 0.0001f, 0.0001f);

            timedisplay.Text = $"JD {currentjd:#0000000.00000} {currentjd.JulianToDateTime()}";// + (bodytrack >= 0 ? " Tracking " + bodyinfo[bodytrack].ScanNode.FullName : "");

            Vector3d reallookat = (gl3dcontroller.PosCamera.LookAt / mscaling + worldcentrem) / 1000.0;

            status.Text = $"Looking at {reallookat.X:N0}, {reallookat.Y:N0}, {reallookat.Z:N0} km " +
                        $"cdir {gl3dcontroller.PosCamera.CameraDirection.X:0.0},{gl3dcontroller.PosCamera.CameraDirection.Y:0.0} zoom {gl3dcontroller.PosCamera.ZoomFactor:0.0000} " +
                        $"dist {gl3dcontroller.PosCamera.EyeDistance / mscaling / 1000:N0}km FOV {gl3dcontroller.MatrixCalc.FovDeg}";

            gl3dcontroller.Redraw();

        }

        ulong lasttime = ulong.MaxValue;
        private void ControllerDraw(Controller3Dd mc, ulong time)
        {
               // System.Diagnostics.Debug.WriteLine("Controller Draw");

            GLMatrixCalcUniformBlock mcub = (GLMatrixCalcUniformBlock)items.UB("MCUB");
            mcub.SetFull(gl3dcontroller.MatrixCalc);

            grd.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc);
            rbodyobjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc, false);

            if (jdscaling != 0 && lasttime != ulong.MaxValue)
            {
                var diff = time - lasttime;     // ms between calls
                currentjd += diff / (60.0 * 60 * 24 * 1000) * jdscaling;        // convert ms delta to days, with scaling
            }
            lasttime = time;
        }

        public void Dispose()
        {
            items.Dispose();
        }


        #region UI

        private void MouseClickOnMap(GLBaseControl s, GLMouseEventArgs e)
        {
            int distmovedsq = gl3dcontroller.MouseMovedSq(e);        //3dcontroller is monitoring mouse movements
            if (distmovedsq < 4)
            {
                var geo = bodyfindshader.GetShader<GLPLGeoShaderFindTriangles>(OpenTK.Graphics.OpenGL4.ShaderType.GeometryShader);
                geo.SetScreenCoords(e.ViewportLocation, matrixcalc.ViewPort.Size);

                rbodyfindshader.Execute(bodyfindshader, glwfc.RenderState);

                var res = geo.GetResult();
                if (res != null)
                {
                    for (int i = 0; i < res.Length; i++) System.Diagnostics.Debug.WriteLine(i + " = " + res[i]);

                    if (e.Button == GLMouseEventArgs.MouseButtons.Left)
                    {
                        SetBodyTrack((int)res[0].Y);
                    }
                    else if(e.Button == GLMouseEventArgs.MouseButtons.Right)
                    {
                        rightclickmenubody.Tag = (int)res[0].Y;
                        rightclickmenubody.Show(displaycontrol, e.Location);
                    }
                }
                else
                {
                    if (e.Button == GLMouseEventArgs.MouseButtons.Right)
                    {
                        rightclickmenuscreen.Show(displaycontrol, e.Location);
                    }
                }
            }
        }

        private void OtherKeys(GLOFC.Controller.KeyboardMonitor kb)
        {
            if (kb.HasBeenPressed(Keys.P, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.ChangePerspectiveMode(!gl3dcontroller.MatrixCalc.InPerspectiveMode);
            }

            var res = kb.HasBeenPressed(Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9);
            if (res != null)
            {
                if (res.Item2 == KeyboardMonitor.ShiftState.Shift)
                {

                }
                else if (res.Item2 == KeyboardMonitor.ShiftState.None)
                {
                    int n = res.Item1;
                    if (n < bodyinfo.Count)
                        SetBodyTrack(bodyinfo[n].Index);
                }
            }

            if (kb.HasBeenPressed(Keys.D0, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                SetBodyTrack(-1);
            }
        }

        #endregion


        #region Body Track

        int bodytrackid = -1;
        
        void SetBodyTrack(int i)
        {
            bodytrackid = i;
            if ( i>=0)
                gl3dcontroller.MoveLookAt(new Vector3d(0, 0, 0), false);
        }

        bool IsBodyTracked()
        {
            return bodytrackid != -1;
        }
        
        #endregion

    }
}
