using GLOFC;
using GLOFC.Controller;
using GLOFC.GL4;
using GLOFC.GL4.Controls;
using GLOFC.GL4.Shaders;
using GLOFC.GL4.Shaders.Vertex;
using GLOFC.GL4.Shaders.Basic;
using GLOFC.GL4.Shaders.Fragment;
using GLOFC.GL4.Shaders.Geo;
using GLOFC.Utils;
using GLOFC.WinForm;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GLOFC.GL4.Bitmaps;
using GLOFC.GL4.ShapeFactory;
using static GLOFC.GL4.Controls.GLBaseControl;
using QuickJSON;

namespace TestOpenTk
{
    public class Orrery
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private GLItemsList items = new GLItemsList();
        private GLMatrixCalc matrixcalc;

        private GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        private GLRenderProgramSortedList rBodyObjects = new GLRenderProgramSortedList();

        private Controller3Dd gl3dcontroller;
        private GLControlDisplay displaycontrol;
        private GLLabel timedisplay;
        private GLLabel mastersystem;
        private GLLabel datalabel;
        private GLLabel status;

        private StarScan.ScanNode starsystemnodes;        // heirarchical list, all
        private int displaysubnode = 0; // subnode, 0 all, 1 first, etc
        private List<BodyInfo> bodyinfo;       // linear list pointing to nodes with kepler info etc

        private const double oneAU_m = 149597870700;
        private float worldsize = 7500e9f;        // size of playfield in meters
        private long gridlines = 50000000000;   // m
        private float mscaling = 1 / 1e6f;       // convert to units used by GL, 1e6 = 1e11 m, earth is at 1.49e11 m.  1,000,000m = 1000km = 1 unit

        private int track = -1;

        private const int findblock = 3;
        private const int arbblocknumber = 4;

        private double currentjd;
        private double jdscaling;

        private GLContextMenu rightclickmenubody;
        private GLContextMenu rightclickmenuscreen;

        // shaders and spheres objects

        private GLShaderPipeline orbitlineshader;       // orbit lines
        private GLShaderPipeline bodyshader;            // body shaders
        private GLBuffer spherebuffer, spheretexcobuffer;       // sphere descriptions

        //private GLShaderPipeline bodyplaneshader;

        private GLShaderPipeline findshader;
        private GLRenderableItem rifind;

        private GLBuffer bodymatrixbuffer;


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
            gl3dcontroller.PosCamera.ZoomMax = 300f;
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

            for ( int i = 1; i <= 10; i++ )
            {
                int v = i * i;
                double f = (gl3dcontroller.PosCamera.ZoomMax - gl3dcontroller.PosCamera.ZoomMin) * v / 100.0 + gl3dcontroller.PosCamera.ZoomMin;
                System.Diagnostics.Debug.WriteLine($"{i} {v} {f}");
            }


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

            rightclickmenubody = new GLContextMenu("RightClickMenuBody", true,
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
                        track = (int)rightclickmenubody.Tag;
                    }
                },
                new GLMenuItem("RCMZoomIn", "Track Central Body")
                {
                    MouseClick = (s, e) =>
                    {
                        int body = (int)rightclickmenubody.Tag;
                        if (bodyinfo[body].ParentIndex >= 0)
                            track = bodyinfo[body].ParentIndex;
                    }
                },
                new GLMenuItem("RCMZoomIn", "Zoom In")
                {
                },
                new GLMenuItem("RCMUntrack", "Untrack")
                {
                    MouseClick = (s1, e1) =>
                    {
                        track = -1;
                    }
                }
                );

            rightclickmenubody.Opening += (ms,tag) =>
            {
                ms["RCMUntrack"].Enabled = track != -1;
            };

            rightclickmenuscreen = new GLContextMenu("RightClickMenuBody", true,
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
                        track = -1;
                    }
                }
                );

            rightclickmenuscreen.Opening += (ms,tag) =>
            {
                ms["RCMUntrack"].Enabled = track != -1;
            };

            // optional grid

            if ( true )
            {
                var shader = new GLColorShaderWorld();
                items.Add(shader);

                GLRenderState lines = GLRenderState.Lines();
                lines.DepthTest = false;

                int gridsize = (int)(worldsize * mscaling);
                int gridoffset = (int)(gridlines * mscaling);
                int nolines = gridsize / gridoffset * 2 + 1;

                Color gridcolour = Color.FromArgb(80, 80, 80, 80);
                rObjects.Add(shader,
                                GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(-gridsize, -0, -gridsize), new Vector3(-gridsize, -0, gridsize), new Vector3(gridoffset, 0, 0), nolines),
                                                        new Color4[] { gridcolour })
                                    );


                rObjects.Add(shader,
                                GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
                                    GLShapeObjectFactory.CreateLines(new Vector3(-gridsize, -0, -gridsize), new Vector3(gridsize, -0, -gridsize), new Vector3(0, 0, gridoffset), nolines),
                                                        new Color4[] { gridcolour }));

                Size bmpsize = new Size(128, 30);
                var maps = new GLBitmaps("bitmap1", rObjects, bmpsize, 3, OpenTK.Graphics.OpenGL4.SizedInternalFormat.Rgba8, false, false);
                using (StringFormat fmt = new StringFormat(StringFormatFlags.NoWrap) { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
                {
                    float hsize = 40e6f * 1000 * mscaling; // million km -> m -> scaling
                    float vsize = hsize * bmpsize.Height / bmpsize.Width;

                    Font f = new Font("MS sans serif", 12f);
                    long pos = -nolines / 2 * (gridlines / 1000);
                    for (int i = -nolines / 2; i < nolines / 2; i++)
                    {
                        if (i != 0)
                        {
                            double v = Math.Abs(pos * 1000);
                            long p = Math.Abs(pos);

                            maps.Add(i, (p).ToString("N0"), f, Color.White, Color.Transparent, new Vector3(i * gridoffset + hsize / 2, 0, vsize / 2),
                                                new Vector3(hsize, 0, 0), new Vector3(0, 0, 0), fmt);
                            maps.Add(i, (v / oneAU_m).ToString("N1") + "AU", f, Color.White, Color.Transparent, new Vector3(i * gridoffset + hsize / 2, 0, -vsize / 2),
                                                new Vector3(hsize, 0, 0), new Vector3(0, 0, 0), fmt);
                            maps.Add(i, (p).ToString("N0"), f, Color.White, Color.Transparent, new Vector3(hsize / 2, 0, i * gridoffset + vsize / 2),
                                                new Vector3(hsize, 0, 0), new Vector3(0, 0, 0), fmt);
                            maps.Add(i, (v / oneAU_m).ToString("N1") + "AU", f, Color.White, Color.Transparent, new Vector3(hsize / 2, 0, i * gridoffset - vsize / 2),
                                                new Vector3(hsize, 0, 0), new Vector3(0, 0, 0), fmt);
                        }
                        pos += 50000000;
                    }
                }
            }

            // orbit shader is solid coloured lines
            // uniform 22 is giving world position offset, with W = colour selector.  We pass a set of colours to use
            var orbitlinesvertshader = new GLPLVertexShaderModelWorldUniform(new Color[] { Color.FromArgb(128, 128, 0, 0), Color.FromArgb(128, 128, 128, 0) });
            // orbit lines shader, fragment shader get loc 0 for colour
            orbitlineshader = new GLShaderPipeline(orbitlinesvertshader, new GLPLFragmentShaderVSColor());

            //bodyplaneshader = new GLShaderPipeline(orbitlinesvertshader, new GLPLFragmentShaderVSColor());  // model pos in, with uniform world pos, vectors out, with vs_colour selected by worldpos.w

            // set up ARB IDs for all images we are going to use..
            var tbs = items.NewBindlessTextureHandleBlock(arbblocknumber);
            var texs = items.NewTexture2D(null, Properties.Resources.golden, SizedInternalFormat.Rgba8);
            var texp = items.NewTexture2D(null, Properties.Resources.moonmap1k, SizedInternalFormat.Rgba8);
            var texb = items.NewTexture2D(null, Properties.Resources.dotted, SizedInternalFormat.Rgba8);
            var texs2 = items.NewTexture2D(null, Properties.Resources.wooden, SizedInternalFormat.Rgba8);
            tbs.WriteHandles(new IGLTexture[] { texs, texp, texb, texs2 });

            // takes 0:Vector4 model, 1: vec2 texture coords, 4:matrix
            // matrix can select autoscale min/max/scaling in [0-2][3]. Image number is given by matrix[3][3]
            // out is 0:tex, 1: modelpos, 2: instance, 4 = image number. 
            var bodyvertshader = new GLPLVertexShaderModelMatrixTexture(1000000 * 1000 * mscaling, useeyedistance: false);

            // fragment shader, using 0 tex coord, 4 image id and arb text binding by giving the arbblock to look images up in.
            var bodyfragshader = new GLPLFragmentShaderBindlessTexture(arbblocknumber, discardiftransparent: true, useprimidover2: false);

            // pipeline shader for bodies
            bodyshader = new GLShaderPipeline(bodyvertshader, bodyfragshader);
            items.Add(bodyshader);

            // hold shere vec4's and tex co-ords
            var sphereshape = GLSphereObjectFactory.CreateTexturedSphereFromTriangles(3, 1.0f);
            spherebuffer = items.NewBuffer();      // fill buffer with model co-ords
            spherebuffer.AllocateFill(sphereshape.Item1);
            spheretexcobuffer = items.NewBuffer(); // fill buffer with tex coords
            spheretexcobuffer.AllocateFill(sphereshape.Item2);

            // holds matrix controlling all the bodies. See the bodyvertshader for content
            bodymatrixbuffer = items.NewBuffer();  

            // find for body matrix's
            GLStorageBlock findbufferresults = items.NewStorageBlock(findblock);
            var geofind = new GLPLGeoShaderFindTriangles(findbufferresults, 16);        // pass thru normal vert/tcs/tes then to geoshader for results
            findshader = items.NewShaderPipeline(null, bodyvertshader, null, null, geofind, null, null, null);

            // scaling and time
            jdscaling = 0;
            currentjd = new DateTime(2021, 11, 18, 12, 0, 0).ToJulianDate();
        }

        public void CreateBodies(string file)
        { 
            string para = File.ReadAllText(file);
            JObject jo = JObject.Parse(para);

            starsystemnodes = StarScan.ReadNode(jo);

            displaysubnode = 0;
            CreateBodies(starsystemnodes, displaysubnode);

        }

        private void CreateBodies(StarScan.ScanNode node, int subnode)
        {
            rBodyObjects.Clear();       // clear the render list

            bodyinfo = new List<BodyInfo>();        // new bodyinfo tree

            bool sysenabled = false;
            if (subnode > 0 && node.NodeType == StarScan.ScanNodeType.barycentre && node.Children != null)      // select display subnode and ignore rest of tree
            {
                node = node.Children.Values[subnode - 1];
                sysenabled = true;
            }

            displaycontrol.ApplyToControlOfName("sys*", (c) => { c.Visible = sysenabled; });        // set up if system selector is enabled

            BodyInfo.CreateInfoTree(node, null, -1, 0, bodyinfo);       // create the info tree, reading the nodes in and filling in our kepler data etc

            foreach (var o in bodyinfo) // for all bodies, now create the opengl artifacts
            {
                System.Diagnostics.Debug.Write($"Body {o.ScanNode.OwnName} {o.ScanNode.scandata?.StarType} {o.ScanNode.scandata?.PlanetClass} Lvl {o.ScanNode.Level} ");

                if (o.KeplerParameters != null)
                {
                    System.Diagnostics.Debug.Write($"SMA {o.KeplerParameters.SemiMajorAxis / oneAU_m} AU {o.KeplerParameters.SemiMajorAxis / 1000} km " +
                                $" Ecc {o.KeplerParameters.Eccentricity} Orbital Period {o.KeplerParameters.OrbitalPeriodS / 24 / 60 / 60 / 365} Y Radius {o.ScanNode.scandata.nRadius} m CM {o.KeplerParameters.CentralMass} axt {o.ScanNode.scandata.nAxialTilt}");
                }

                System.Diagnostics.Debug.WriteLine("");

                if (o.KeplerParameters != null)     // in orbit
                {
                    Vector4[] orbit = o.KeplerParameters.Orbit(currentjd, 0.1, mscaling);
                    GLRenderState lines = GLRenderState.Lines();
                    lines.DepthTest = false;

                    o.orbitpos.ColorIndex = node.scandata?.nRadius != null ? 0 : 1;

                    var riol = GLRenderableItem.CreateVector4(items, PrimitiveType.LineStrip, lines, orbit, o.orbitpos);
                    rBodyObjects.Add(orbitlineshader, riol);

// TBD TURN BACK ON
                    //GLRenderState quad = GLRenderState.Quads(cullface: false);
                    //quad.DepthTest = false;
                    //var s = 100000e3f * mscaling;
                    //var quadpos = new Vector4[] { new Vector4(-s, 0, -s, 1), new Vector4(-s, 0, +s, 1), new Vector4(+s, 0, +s, 1), new Vector4(+s, 0, -s, 1) };
                    //var plane = GLRenderableItem.CreateVector4(items, PrimitiveType.Quads, quad, quadpos, o.bodypos);
                    //rBodyObjects.Add(bodyplaneshader, plane);
                }
            }

            int bodies = bodyinfo.Count;

            // hold planet/barycentre positions/sizes/imageno
            bodymatrixbuffer.AllocateBytes(GLBuffer.Mat4size * bodies);

            GLRenderState rt = GLRenderState.Tri();
            rt.DepthTest = false;
            var ribody = GLRenderableItem.CreateVector4Vector2Matrix4(items, PrimitiveType.Triangles, rt, spherebuffer, spheretexcobuffer, bodymatrixbuffer,
                                            spherebuffer.Length / sizeof(float) / 4,
                                            ic: bodies, matrixdivisor: 1);
            rBodyObjects.Add(bodyshader, ribody);

            rifind = GLRenderableItem.CreateVector4Vector2Matrix4(items, PrimitiveType.Triangles, GLRenderState.Tri(), spherebuffer, spheretexcobuffer, bodymatrixbuffer,
                                            spherebuffer.Length / sizeof(float) / 4,
                                            ic: bodies, matrixdivisor: 1);
        }

        float planetrot = 0;
        public void SystemTick()
        {
            if (bodyinfo != null)
            {
                Matrix4[] bodymats = new Matrix4[bodyinfo.Count];
                Vector3d[] positions = new Vector3d[bodyinfo.Count];

                for (int i = 0; i < bodyinfo.Count; i++)
                {
                    var bi = bodyinfo[i];
                    Vector3 pos = Vector3.Zero;

                    if (bi.KeplerParameters != null)      // this body is orbiting
                    {
                        if (i > 0)              // not the root node, so a normal orbit
                        {
                            positions[i] = bi.KeplerParameters.ToCartesian(currentjd);    // in meters around 0,0,0
                            var cbpos = positions[bi.ParentIndex];              // central body position
                            positions[i] += cbpos;                              // offset

                            bi.orbitpos.WorldPosition = new Vector3((float)cbpos.X, (float)cbpos.Z, (float)cbpos.Y) * mscaling;
                        }
                        else
                        {
                            // node 0 is the root node, always displayed at 0,0,0
                            // If we remove the root node, and just display a body or barycentre which is in orbit around the root node
                            // the orbit of this node body is calculated at T0, and then the orbit position of offset so the line passes thru 0,0,0
                            var orbitpos = bi.KeplerParameters.ToCartesian(bi.KeplerParameters.T0);       // find the orbit of the root at T0 (fixed so it does not move)
                            bi.orbitpos.WorldPosition = new Vector3((float)-orbitpos.X, (float)-orbitpos.Z, (float)-orbitpos.Y) * mscaling;
                        }
                    }

                    pos = new Vector3((float)(positions[i].X * mscaling), (float)(positions[i].Z * mscaling), (float)(positions[i].Y * mscaling));
                    bi.bodypos.WorldPosition = pos;

                    float imageradius = bi.ScanNode.scandata != null && bi.ScanNode.scandata.nRadius.HasValue ? (float)bi.ScanNode.scandata.nRadius.Value : 1000e3f;
                    imageradius *= mscaling;

                    bool planet = bi.ScanNode.scandata != null && bi.ScanNode.scandata.PlanetClass.HasChars();
                    bool bary = bi.ScanNode.scandata == null || bi.ScanNode.scandata.nRadius == null;

                    double axialtilt = (bi.ScanNode.scandata?.nAxialTilt ?? 0).Radians();
                    bodymats[i] = GLStaticsMatrix4.CreateMatrixPlanetRot(pos, new Vector3(1, 1, 1), (float)axialtilt, planetrot.Radians());
                    planetrot += 0.1f;
   // here, getting the rotation speed correct

                    // more clever, knowing orbit paras
                    bodymats[i].M14 = (bary ? 100000 : planet ? 100000 : 1000000) * 1000 * mscaling;      // min size in km
                    bodymats[i].M24 = (bary ? 100000 : planet ? 10000000 : 3e6f) * 1000 * mscaling;           // maximum size in scaling
                    bodymats[i].M34 = imageradius;
                    bodymats[i].M44 = planet ? 1 : bary ? 2 : 0;      // select image
                }

                bodymatrixbuffer.ResetFillPos();
                bodymatrixbuffer.Fill(bodymats);

                if (track != -1)
                {
                    Vector3d pos = positions[track];
                    gl3dcontroller.MoveLookAt(new Vector3d(pos.X, pos.Z, pos.Y) * mscaling, false);
                }
            }


            //            datalabel.Text = $"{bodypositionscaling[1].Position/mscaling/1000}" + Environment.NewLine;

            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true, OtherKeys, 0.0001f, 0.0001f);

            timedisplay.Text = $"JD {currentjd:#0000000.00000} {currentjd.JulianToDateTime()}" + (track >= 0 ? " Tracking " + bodyinfo[track].ScanNode.FullName : "");

            status.Text = $"Looking at {gl3dcontroller.PosCamera.LookAt.X / mscaling / 1000:0.0},{gl3dcontroller.PosCamera.LookAt.Y / mscaling / 1000:0.0},{gl3dcontroller.PosCamera.LookAt.Z / mscaling / 1000:0.0} " +
                        $"from {gl3dcontroller.PosCamera.EyePosition.X / mscaling / 1000:0.0},{gl3dcontroller.PosCamera.EyePosition.Y / mscaling / 1000:0.0},{gl3dcontroller.PosCamera.EyePosition.Z / mscaling / 1000:0.0} " +
                        $"cdir {gl3dcontroller.PosCamera.CameraDirection.X:0.0},{gl3dcontroller.PosCamera.CameraDirection.Y:0.0} zoom {gl3dcontroller.PosCamera.ZoomFactor:0.0000} " +
                        $"dist {gl3dcontroller.PosCamera.EyeDistance / mscaling / 1000:N0}km FOV {gl3dcontroller.MatrixCalc.FovDeg}";

            //float sr = gl3dcontroller.MatrixCalc.EyeDistance / (0.05e6f * 1000 * mscaling);
            //status.Text += $" eye units {gl3dcontroller.MatrixCalc.EyeDistance} sr {sr}";

            //  System.Diagnostics.Debug.WriteLine("Tick");
            gl3dcontroller.Redraw();
        }

        ulong lasttime = ulong.MaxValue;
        private void ControllerDraw(Controller3Dd mc, ulong time)
        {
               // System.Diagnostics.Debug.WriteLine("Controller Draw");

            GLMatrixCalcUniformBlock mcub = (GLMatrixCalcUniformBlock)items.UB("MCUB");
            mcub.SetFull(gl3dcontroller.MatrixCalc);

            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc, false);
            rBodyObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc, false);

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
                var geo = findshader.GetShader<GLPLGeoShaderFindTriangles>(OpenTK.Graphics.OpenGL4.ShaderType.GeometryShader);
                geo.SetScreenCoords(e.ViewportLocation, matrixcalc.ViewPort.Size);

                rifind.Execute(findshader, glwfc.RenderState);

                var res = geo.GetResult();
                if (res != null)
                {
                    for (int i = 0; i < res.Length; i++) System.Diagnostics.Debug.WriteLine(i + " = " + res[i]);

                    if (e.Button == GLMouseEventArgs.MouseButtons.Left)
                    {
                        track = (int)res[0].Y;
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
                int n = res.Item1;
                if (n < bodyinfo.Count)
                    track = bodyinfo[n].Index;
            }

            if (kb.HasBeenPressed(Keys.D0, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                track = -1;
            }
        }

        #endregion


        #region helpers

        public void DisplayNode(int dir)
        {
            if (starsystemnodes.NodeType == StarScan.ScanNodeType.barycentre && starsystemnodes.Children != null)
            {
                int number = starsystemnodes.Children.Count + 1;
                displaysubnode = (displaysubnode + dir + number) % number; // rotate between 0 and N
                CreateBodies(starsystemnodes, displaysubnode);
                mastersystem.Text = displaysubnode == 0 ? "All" : starsystemnodes.Children.Values[displaysubnode - 1].OwnName;
                track = -1;
            }
        }

        #endregion

    }
}
