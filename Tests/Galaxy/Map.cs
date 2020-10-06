using EliteDangerousCore.EDSM;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OFC;
using OFC.Controller;
using OFC.GL4;
using OFC.GL4.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestOpenTk
{
    public class Map
    {
        public Controller3D gl3dcontroller;
        public GLControlDisplay displaycontrol;
        public GalacticMapping galmap;
        public GalacticMapping eliteregions;

        private OFC.WinForm.GLWinFormControl glwfc;

        private GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        private GLItemsList items = new GLItemsList();

        private Vector4[] volumetricboundingbox;
        private GLVolumetricUniformBlock volumetricblock;
        private GLRenderableItem galaxyrenderable;
        private GalaxyShader galaxyshader;

        private DynamicGridCoordVertexShader gridbitmapvertshader;
        private GLRenderableItem gridrenderable;
        private DynamicGridVertexShader gridvertshader;

        private TravelPath travelpath;
        private MapMenu galaxymenu;

        private GalMapObjects galmapobjects;
        private GalMapRegions edsmgalmapregions;
        private GalMapRegions elitemapregions;
        private GalaxyStarDots stardots;

        private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();


        private ISystem currentsystem;

        public Map()
        {
        }

        public void Dispose()
        {
            items.Dispose();
        }

        #region Initialise

        public void Start(OFC.WinForm.GLWinFormControl glwfc, GalacticMapping galmap, GalacticMapping eliteregions)
        {
            this.glwfc = glwfc;
            this.galmap = galmap;
            this.eliteregions = eliteregions;

            sw.Start();

            items.Add(new GLMatrixCalcUniformBlock(), "MCUB");     // create a matrix uniform block 

            int front = -20000, back = front + 90000, left = -45000, right = left + 90000, vsize = 2000;
            volumetricboundingbox = new Vector4[]
            {
                new Vector4(left,-vsize,front,1),
                new Vector4(left,vsize,front,1),
                new Vector4(right,vsize,front,1),
                new Vector4(right,-vsize,front,1),

                new Vector4(left,-vsize,back,1),
                new Vector4(left,vsize,back,1),
                new Vector4(right,vsize,back,1),
                new Vector4(right,-vsize,back,1),
            };

            // global buffer blocks used
            const int volumenticuniformblock = 2;
            const int findstarblock = 3;
            const int findgeomapblock = 4;

            if (true) // galaxy
            {
                const int gnoisetexbinding = 3;     //tex bindings are attached per shaders so are not global
                const int gdisttexbinding = 4;
                const int galtexbinding = 1;

                volumetricblock = new GLVolumetricUniformBlock(volumenticuniformblock);
                items.Add(volumetricblock, "VB");

                int sc = 1;
                GLTexture3D noise3d = new GLTexture3D(1024 * sc, 64 * sc, 1024 * sc, OpenTK.Graphics.OpenGL4.SizedInternalFormat.R32f); // red channel only
                items.Add(noise3d, "Noise");
                ComputeShaderNoise3D csn = new ComputeShaderNoise3D(noise3d.Width, noise3d.Height, noise3d.Depth, 128 * sc, 16 * sc, 128 * sc, gnoisetexbinding);       // must be a multiple of localgroupsize in csn
                csn.StartAction += (A,m) => { noise3d.BindImage(gnoisetexbinding); };
                csn.Run();      // compute noise

                GLTexture1D gaussiantex = new GLTexture1D(1024, OpenTK.Graphics.OpenGL4.SizedInternalFormat.R32f); // red channel only
                items.Add(gaussiantex, "Gaussian");

                // set centre=width, higher widths means more curve, higher std dev compensate.
                // fill the gaussiantex with data
                ComputeShaderGaussian gsn = new ComputeShaderGaussian(gaussiantex.Width, 2.0f, 2.0f, 1.4f, gdisttexbinding);
                gsn.StartAction += (A,m) => { gaussiantex.BindImage(gdisttexbinding); };
                gsn.Run();      // compute noise

                GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);

                // load one upside down and horz flipped, because the volumetric co-ords are 0,0,0 bottom left, 1,1,1 top right
                GLTexture2D galtex = new GLTexture2D(Properties.Resources.Galaxy_L180);
                items.Add(galtex, "galtex");
                galaxyshader = new GalaxyShader(volumenticuniformblock, galtexbinding, gnoisetexbinding, gdisttexbinding);
                items.Add(galaxyshader, "Galaxy-sh");
                // bind the galaxy texture, the 3dnoise, and the gaussian 1-d texture for the shader
                galaxyshader.StartAction += (a,m) => { galtex.Bind(galtexbinding); noise3d.Bind(gnoisetexbinding); gaussiantex.Bind(gdisttexbinding); };      // shader requires these, so bind using shader

                GLRenderControl rt = GLRenderControl.ToTri(OpenTK.Graphics.OpenGL4.PrimitiveType.Points);
                galaxyrenderable = GLRenderableItem.CreateNullVertex(rt);   // no vertexes, all data from bound volumetric uniform, no instances as yet
                rObjects.Add(galaxyshader, galaxyrenderable);
            }

            if (true) // star points
            {
                int gran = 8;
                Bitmap img = Properties.Resources.Galaxy_L180;
                Bitmap heat = img.Function(img.Width / gran, img.Height / gran, mode: BitMapHelpers.BitmapFunction.HeatMap);
                heat.Save(@"c:\code\heatmap.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

                Random rnd = new Random(23);

                GLBuffer buf = new GLBuffer(16 * 350000);     // since RND is fixed, should get the same number every time.
                buf.StartWrite(0); // get a ptr to the whole schebang

                int xcw = (right - left) / heat.Width;
                int zch = (back - front) / heat.Height;

                int points = 0;

                for (int x = 0; x < heat.Width; x++)
                {
                    for (int z = 0; z < heat.Height; z++)
                    {
                        int i = heat.GetPixel(x, z).R;
                        if (i > 32)
                        {
                            int gx = left + x * xcw;
                            int gz = front + z * zch;

                            float dx = (float)Math.Abs(gx) / 45000;
                            float dz = (float)Math.Abs(25889 - gz) / 45000;
                            double d = Math.Sqrt(dx * dx + dz * dz);     // 0 - 0.1412
                            d = 1 - d;  // 1 = centre, 0 = unit circle
                            d = d * 2 - 1;  // -1 to +1
                            double dist = ObjectExtensionsNumbersBool.GaussianDist(d, 1, 1.4);

                            int c = Math.Min(Math.Max(i * i * i / 120000, 1), 40);

                            dist *= 2000;
                            //System.Diagnostics.Debug.WriteLine("{0} {1} : dist {2} c {3}", x, z, dist, c);
                            //System.Diagnostics.Debug.Write(c);
                            GLPointsFactory.RandomStars4(buf, c, gx, gx + xcw, gz, gz + zch, (int)dist, (int)-dist, rnd, w: 0.8f);
                            points += c;
                            System.Diagnostics.Debug.Assert(points < buf.Length / 16);
                        }
                    }
                    //System.Diagnostics.Debug.WriteLine(".");
                }

                buf.StopReadWrite();

                stardots = new GalaxyStarDots();

                items.Add(stardots);
                GLRenderControl rp = GLRenderControl.Points(1);
                rp.DepthTest = false;
                rObjects.Add(stardots,
                                GLRenderableItem.CreateVector4(items, rp, buf, points));
                System.Diagnostics.Debug.WriteLine("Stars " + points);
            }

            if (true)  // point sprite
            {
                items.Add(new GLTexture2D(Properties.Resources.StarFlare2), "lensflare");
                items.Add(new GLPointSpriteShader(items.Tex("lensflare"), 64, 40), "PS");
                var p = GLPointsFactory.RandomStars4(1000, 0, 25899, 10000, 1000, -1000);

                GLRenderControl rps = GLRenderControl.PointSprites(depthtest: false);

                rObjects.Add(items.Shader("PS"),
                             GLRenderableItem.CreateVector4Color4(items, rps, p, new Color4[] { Color.White }));

            }

            if (true) // grids
            {
                gridvertshader = new DynamicGridVertexShader(Color.Cyan);
                items.Add(gridvertshader, "PLGRIDVertShader");
                items.Add(new GLPLFragmentShaderVSColor(), "PLGRIDFragShader");

                GLRenderControl rl = GLRenderControl.Lines(1);
                rl.DepthTest = false;

                items.Add(new GLShaderPipeline(items.PLShader("PLGRIDVertShader"), items.PLShader("PLGRIDFragShader")), "DYNGRID");

                gridrenderable = GLRenderableItem.CreateNullVertex(rl, dc: 2);

                rObjects.Add(items.Shader("DYNGRID"), "DYNGRIDRENDER", gridrenderable);

            }

            if (true)       // grid coords
            {
                gridbitmapvertshader = new DynamicGridCoordVertexShader();
                items.Add(gridbitmapvertshader, "PLGRIDBitmapVertShader");
                items.Add(new GLPLFragmentShaderTexture2DIndexed(0), "PLGRIDBitmapFragShader");     // binding 1

                GLRenderControl rl = GLRenderControl.TriStrip(cullface: false);
                rl.DepthTest = false;

                GLTexture2DArray gridtexcoords = new GLTexture2DArray();
                items.Add(gridtexcoords, "PLGridBitmapTextures");

                GLShaderPipeline sp = new GLShaderPipeline(items.PLShader("PLGRIDBitmapVertShader"), items.PLShader("PLGRIDBitmapFragShader"));

                items.Add(sp, "DYNGRIDBitmap");

                rObjects.Add(items.Shader("DYNGRIDBitmap"), "DYNGRIDBitmapRENDER", GLRenderableItem.CreateNullVertex(rl, dc: 4, ic: 9));
            }

            if (true)       // travel path
            {
                Random rnd = new Random(52);
                List<ISystem> pos = new List<ISystem>();
                for (int i = 0; i <= 60000; i += 500)
                {
                    if (i < 30000)
                        pos.Add(new ISystem("s" + i.ToString(), i + rnd.Next(1000) - 500, rnd.Next(100), i));
                    else
                        pos.Add(new ISystem("s" + i.ToString(), 60000 - i + rnd.Next(1000) - 500, rnd.Next(100), i));
                }

                travelpath = new TravelPath();
                travelpath.CreatePath(items, rObjects, pos, 20, 2, findstarblock);

                currentsystem = pos[3];
            }

            if (true)       // Gal map objects
            {
                galmapobjects = new GalMapObjects();
                galmapobjects.CreateObjects(items, rObjects, galmap, findgeomapblock);
            }

            if (true)       // Gal map regions
            {
                var corr = new GalMapRegions.ManualCorrections[] {          // nerf the centeroid position slightly
                    new GalMapRegions.ManualCorrections("The Galactic Aphelion", y: -2000 ),
                    new GalMapRegions.ManualCorrections("The Abyss", y: +3000 ),
                    new GalMapRegions.ManualCorrections("Eurus", y: -3000 ),
                    new GalMapRegions.ManualCorrections("The Perseus Transit", x: -3000, y: -3000 ),
                    new GalMapRegions.ManualCorrections("Zephyrus", x: 0, y: 2000 ),
                };

                edsmgalmapregions = new GalMapRegions();
                edsmgalmapregions.CreateObjects(items, rObjects, galmap, 8000, corr:corr);
            }

            if (true)           // Elite regions
            {
                elitemapregions = new GalMapRegions();
                elitemapregions.CreateObjects(items, rObjects, eliteregions, 8000);
            }

            EliteRegionsEnable = false;

            // menu system

            GLMatrixCalc mc = new GLMatrixCalc();
            mc.PerspectiveNearZDistance = 1f;
            mc.PerspectiveFarZDistance = 120000f;
            mc.InPerspectiveMode = true;
            mc.ResizeViewPort(this, glwfc.Size);          // must establish size before starting

            displaycontrol = new GLControlDisplay(items, glwfc, mc);       // hook form to the window - its the master
            displaycontrol.Font = new Font("Arial", 10f);
            displaycontrol.Focusable = true;          // we want to be able to focus and receive key presses.
            displaycontrol.SetFocus();

            // 3d controller

            gl3dcontroller = new Controller3D();
            gl3dcontroller.ZoomDistance = 5000F;
            gl3dcontroller.PosCamera.ZoomMin = 0.1f;
            gl3dcontroller.PosCamera.ZoomScaling = 1.1f;
            gl3dcontroller.EliteMovement = true;
            gl3dcontroller.PaintObjects = Controller3DDraw;
            gl3dcontroller.KeyboardTravelSpeed = (ms, eyedist) =>
            {
                return (float)ms * 1.0f * Math.Min(eyedist / 1000, 10);
            };

            // hook gl3dcontroller to display control - its the slave
            gl3dcontroller.Start(mc, displaycontrol, new Vector3(0, 0, 0), new Vector3(140.75f, 0, 0), 0.5F);

            if (displaycontrol != null)
            {
                displaycontrol.Paint += (o) =>        // subscribing after start means we paint over the scene, letting transparency work
                {
                    // MCUB set up by Controller3DDraw which did the work first
                    galaxymenu.UpdateCoords(gl3dcontroller.MatrixCalc);
                    displaycontrol.Render(glwfc.RenderState);
                };
            }

            displaycontrol.MouseDown += MouseDownOnMap;

            galaxymenu = new MapMenu(this);

            GLTextBoxAutoComplete tbac = ((GLTextBoxAutoComplete)displaycontrol["EntryText"]);
            tbac.PerformAutoComplete = (s, a) => 
            {
                var glist = galmap.galacticMapObjects.Where(x => s.Length < 3 ? x.name.StartsWith(s, StringComparison.InvariantCultureIgnoreCase) : x.name.Contains(s,StringComparison.InvariantCultureIgnoreCase)).Select(x => x).ToList();
                List<string> list = glist.Select(x => x.name).ToList();
                return list;
            };
            tbac.SelectedEntry = (a) => 
            {
                System.Diagnostics.Debug.WriteLine("Selected " + tbac.Text);
                var gmo = galmap.galacticMapObjects.Find(x=>x.name.Equals(tbac.Text,StringComparison.InvariantCultureIgnoreCase));
                if ( gmo != null )
                {
                    System.Diagnostics.Debug.WriteLine("Move to gmo " + gmo.points[0]);
                    gl3dcontroller.SlewToPosition(new Vector3((float)gmo.points[0].X, (float)gmo.points[0].Y, (float)gmo.points[0].Z), -1);
                }
            };
        }

        #endregion

        #region Display

        public void Systick()
        {
            if (displaycontrol != null && displaycontrol.RequestRender)
                glwfc.Invalidate();
            var cdmt = gl3dcontroller.HandleKeyboardSlewsInvalidate(true, OtherKeys);
            glwfc.Invalidate();
        }

        double fpsavg = 0;
        long lastms;
        float lasteyedistance = 100000000;
        int lastgridwidth;

        private void Controller3DDraw(GLMatrixCalc mc, long time)
        {
            ((GLMatrixCalcUniformBlock)items.UB("MCUB")).SetFull(gl3dcontroller.MatrixCalc);        // set the matrix unform block to the controller 3d matrix calc.

            // set up the grid shader size

            if (Math.Abs(lasteyedistance - gl3dcontroller.MatrixCalc.EyeDistance) > 10)     // a little histerisis, set the vertical shader grid size
            {
                gridrenderable.InstanceCount = gridvertshader.ComputeGridSize(gl3dcontroller.MatrixCalc.EyeDistance, out lastgridwidth);
                lasteyedistance = gl3dcontroller.MatrixCalc.EyeDistance;
            }

            gridvertshader.SetUniforms(gl3dcontroller.MatrixCalc.TargetPosition, lastgridwidth, gridrenderable.InstanceCount);

            // set the coords fader

            float coordfade = lastgridwidth == 10000 ? (0.7f - (mc.EyeDistance / 20000).Clamp(0.0f, 0.7f)) : 0.7f;
            Color coordscol = Color.FromArgb(coordfade < 0.05 ? 0 : 150, Color.Cyan);
            gridbitmapvertshader.ComputeUniforms(lastgridwidth, gl3dcontroller.MatrixCalc, gl3dcontroller.PosCamera.CameraDirection, coordscol, Color.Transparent);

            // set the galaxy volumetric block

            if (galaxyrenderable != null)
            {
                galaxyrenderable.InstanceCount = volumetricblock.Set(gl3dcontroller.MatrixCalc, volumetricboundingbox, gl3dcontroller.MatrixCalc.InPerspectiveMode ? 50.0f : 0);        // set up the volumentric uniform
                //System.Diagnostics.Debug.WriteLine("GI {0}", galaxyrendererable.InstanceCount);
                galaxyshader.SetDistance(gl3dcontroller.MatrixCalc.InPerspectiveMode ? mc.EyeDistance : -1f);
            }

            travelpath.Update(time, gl3dcontroller.MatrixCalc.EyeDistance);

            galmapobjects.Update(time, gl3dcontroller.MatrixCalc.EyeDistance);

            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc);

            long t = sw.ElapsedMilliseconds;
            long diff = t - lastms;
            lastms = t;
            double fps = (1000.0 / diff);
            if (fpsavg <= 1)
                fpsavg = fps;
            else
                fpsavg = (fpsavg * 0.9) + fps * 0.1;

            //            this.Text = "FPS " + fpsavg.ToString("N0") + " Looking at " + gl3dcontroller.MatrixCalc.TargetPosition + " eye@ " + gl3dcontroller.MatrixCalc.EyePosition + " dir " + gl3dcontroller.Pos.CameraDirection + " Dist " + gl3dcontroller.MatrixCalc.EyeDistance + " Zoom " + gl3dcontroller.Pos.ZoomFactor;
        }

        #endregion

        #region Turn on/off, move, etc.

        public bool EnableGalaxy { get { return galaxyshader.Enable; } set { galaxyshader.Enable = value; glwfc.Invalidate(); } }
        public bool EnableStarDots { get { return stardots.Enable; } set { stardots.Enable = value; glwfc.Invalidate(); } }
        public bool EnableTravelPath { get { return travelpath.Enable; } set { travelpath.Enable = value; glwfc.Invalidate(); } }

        public void TravelPathMoveForward()
        {
            var sys = travelpath.NextSystem();
            if (sys != null)
            { 
                gl3dcontroller.SlewToPosition(new Vector3((float)sys.X, (float)sys.Y, (float)sys.Z), -1);
                SetEntryText(sys.Name);
            }
        }

        public void TravelPathMoveBack()
        {
            var sys = travelpath.PrevSystem();
            if (sys != null)
            {
                gl3dcontroller.SlewToPosition(new Vector3((float)sys.X, (float)sys.Y, (float)sys.Z), -1);
                SetEntryText(sys.Name);
            }
        }

        public void GoToCurrentSystem()
        {
            if (currentsystem != null)
            {
                gl3dcontroller.SlewToPosition(new Vector3((float)currentsystem.X, (float)currentsystem.Y, (float)currentsystem.Z), -1);
                travelpath.SetSystem(currentsystem);
                SetEntryText(currentsystem.Name);
            }
        }

        public void SetEntryText(string text)
        {
            ((GLTextBoxAutoComplete)displaycontrol["EntryText"]).Text = text;
        }

        public bool GalObjectEnable { get { return galmapobjects.Enable; } set { galmapobjects.Enable = value; glwfc.Invalidate(); } }

        public void UpdateGalObjectsStates()
        {
            galmapobjects.UpdateEnables(galmap);
            glwfc.Invalidate();
        }

        public bool EDSMRegionsEnable { get { return edsmgalmapregions.Enable; } set { edsmgalmapregions.Enable = value; glwfc.Invalidate(); } }
        public bool EDSMRegionsOutlineEnable { get { return edsmgalmapregions.Outlines; } set { edsmgalmapregions.Outlines = value; glwfc.Invalidate(); } }
        public bool EDSMRegionsShadingEnable { get { return edsmgalmapregions.Regions; } set { edsmgalmapregions.Regions = value; glwfc.Invalidate(); } }
        public bool EDSMRegionsTextEnable { get { return edsmgalmapregions.Text; } set { edsmgalmapregions.Text = value; glwfc.Invalidate(); } }
        public bool EliteRegionsEnable { get { return elitemapregions.Enable; } set { elitemapregions.Enable = value; glwfc.Invalidate(); } }
        public bool EliteRegionsOutlineEnable { get { return elitemapregions.Outlines; } set { elitemapregions.Outlines = value; glwfc.Invalidate(); } }
        public bool EliteRegionsShadingEnable { get { return elitemapregions.Regions; } set { elitemapregions.Regions = value; glwfc.Invalidate(); } }
        public bool EliteRegionsTextEnable { get { return elitemapregions.Text; } set { elitemapregions.Text = value; glwfc.Invalidate(); } }

        #endregion

        #region UI

        private void MouseDownOnMap(Object s, GLMouseEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Mouse down on map");

            var sys = travelpath.FindSystem(e.ViewportLocation, glwfc.RenderState, glwfc.Size);
            if ( sys != null )
            {
                gl3dcontroller.SlewToPosition(new Vector3((float)sys.X, (float)sys.Y, (float)sys.Z), -1);
                travelpath.SetSystem(sys);
                SetEntryText(sys.Name);
            }
            else
            {
                var gmo = galmapobjects.FindPOI(e.ViewportLocation, glwfc.RenderState, glwfc.Size, galmap);

                if ( gmo != null )
                {
                    gl3dcontroller.SlewToPosition(new Vector3((float)gmo.points[0].X, (float)gmo.points[0].Y, (float)gmo.points[0].Z), -1);
                    SetEntryText(gmo.name);
                }
            }
        }

        private void OtherKeys(OFC.Controller.KeyboardMonitor kb)
        {
            if (kb.HasBeenPressed(Keys.F4, OFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.ChangePerspectiveMode(!gl3dcontroller.MatrixCalc.InPerspectiveMode);
            }
            if (kb.HasBeenPressed(Keys.F5, OFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                EnableGalaxy = !EnableGalaxy;
            }
            if (kb.HasBeenPressed(Keys.F6, OFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                EnableStarDots = !EnableStarDots;
            }
            if (kb.HasBeenPressed(Keys.F7, OFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                EnableTravelPath = !EnableTravelPath;
            }
            if (kb.HasBeenPressed(Keys.F8, OFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                GalObjectEnable = !GalObjectEnable;
            }
            if (kb.HasBeenPressed(Keys.F9, OFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                if (EDSMRegionsEnable)
                    edsmgalmapregions.Toggle();
                else
                    elitemapregions.Toggle();
            }
            if (kb.HasBeenPressed(Keys.F9, OFC.Controller.KeyboardMonitor.ShiftState.Alt))
            {
                bool edsm = EDSMRegionsEnable;
                EDSMRegionsEnable = !edsm;
                EliteRegionsEnable = edsm;
            }

            // DEBUG!
            if (kb.HasBeenPressed(Keys.F2, OFC.Controller.KeyboardMonitor.ShiftState.Shift))
            {
                Random rnd = new Random(System.Environment.TickCount);
                List<ISystem> pos = new List<ISystem>();
                for (int i = 0; i <= 60000; i += 500)
                {
                    if (i < 30000)
                        pos.Add(new ISystem(i.ToString(), i + rnd.Next(1000) - 500, rnd.Next(100), i));
                    else
                        pos.Add(new ISystem(i.ToString(), 60000 - i + rnd.Next(1000) - 500, rnd.Next(100), i));
                }

                travelpath.CreatePath(null, null, pos, 20, 2, 0);
                glwfc.Invalidate();
            }
        }

        #endregion


    }
}
