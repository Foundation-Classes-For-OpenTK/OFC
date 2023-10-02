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

using EliteDangerousCore.EDSM;
using GLOFC;
using GLOFC.Controller;
using GLOFC.GL4;
using GLOFC.GL4.Controls;
using GLOFC.GL4.Shaders;
using GLOFC.GL4.Shaders.Vertex;
using GLOFC.GL4.Shaders.Basic;
using GLOFC.GL4.Shaders.Compute;
using GLOFC.GL4.Shaders.Fragment;
using GLOFC.Utils;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GLOFC.GL4.Shaders.Sprites;
using GLOFC.GL4.Operations;
using GLOFC.GL4.ShapeFactory;
using GLOFC.GL4.Textures;
using System.Web.UI.DataVisualization.Charting;
using System.Reflection;
using GLOFC.GL4.Bitmaps;

namespace TestOpenTk
{
    public interface MapSaver           // saver interface
    {
        void PutSetting<T>(string id, T value);
        T GetSetting<T>(string id, T defaultvalue);
        void DeleteSetting(string id);
    }

    public class Map
    {
        public GLMatrixCalc matrixcalc;
        public Controller3D gl3dcontroller;
        public GLControlDisplay displaycontrol;
        public GalacticMapping edsmmapping;

        private GLOFC.WinForm.GLWinFormControl glwfc;

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

        private GalacticMapping elitemapping;
        private GalMapObjects galmapobjects;
        private GalMapRegions edsmgalmapregions;
        private GalMapRegions elitemapregions;
        private GalaxyStarDots stardots;
        private GalaxyStars galaxystars = null;

        private ImageCache userimages;
        private GLBindlessTextureBitmaps usertexturebitmaps;

        private Action<Action> uiinvoker;

        private Bookmarks bookmarks;

        private GLContextMenu rightclickmenu;

        private GLBuffer debugbuffer;

        // global buffer blocks used
        private const int volumenticuniformblock = 2;
        private const int findblock = 3;
        private const int userbitmapsarbblock = 4;
        
        private System.Diagnostics.Stopwatch hptimer = new System.Diagnostics.Stopwatch();

        public Map()
        {
        }

        public void Dispose()
        {
            if (galaxystars != null)
                galaxystars.Stop();
            items.Dispose();
        }

        public ulong ElapsedTimems { get { return glwfc.ElapsedTimems; } }

        #region Initialise

        public void Start(GLOFC.WinForm.GLWinFormControl glwfc, GalacticMapping edsmmapping, GalacticMapping eliteregions, Action<Action> uiinvoker)
        {
            this.glwfc = glwfc;
            this.edsmmapping = edsmmapping;
            this.elitemapping = eliteregions;
            this.uiinvoker = uiinvoker;

            hptimer.Start();

            GLShaderLog.Reset();
            GLShaderLog.AssertOnError = false;
            GLShaderLog.ShaderSourceLog = @"c:\code\logs\shaders";

            items.Add(new GLMatrixCalcUniformBlock(), "MCUB");     // create a matrix uniform block 

            int lyscale = 1;
            int front = -20000 / lyscale, back = front + 90000 / lyscale, left = -45000 / lyscale, right = left + 90000 / lyscale, vsize = 2000 / lyscale;

            if (false)     // debug bounding box
            {
                Vector4[] displaylines = new Vector4[]
                   {
                new Vector4(left,-vsize,front,1),   new Vector4(left,+vsize,front,1),
                new Vector4(left,+vsize,front,1),      new Vector4(right,+vsize,front,1),
                new Vector4(right,+vsize,front,1),     new Vector4(right,-vsize,front,1),
                new Vector4(right,-vsize,front,1),  new Vector4(left,-vsize,front,1),

                new Vector4(left,-vsize,back,1),    new Vector4(left,+vsize,back,1),
                new Vector4(left,+vsize,back,1),       new Vector4(right,+vsize,back,1),
                new Vector4(right,+vsize,back,1),      new Vector4(right,-vsize,back,1),
                new Vector4(right,-vsize,back,1),   new Vector4(left,-vsize,back,1),

                new Vector4(left,-vsize,front,1),   new Vector4(left,-vsize,back,1),
                new Vector4(left,+vsize,front,1),      new Vector4(left,+vsize,back,1),
                new Vector4(right,-vsize,front,1),  new Vector4(right,-vsize,back,1),
                new Vector4(right,+vsize,front,1),     new Vector4(right,+vsize,back,1),
                   };

                GLRenderState rl = GLRenderState.Lines();

                items.Add(new GLShaderPipeline(new GLPLVertexShaderWorldCoord(), new GLPLFragmentShaderFixedColor(Color.Yellow)), "LINEYELLOW");
                rObjects.Add(items.Shader("LINEYELLOW"),
                GLRenderableItem.CreateVector4(items, PrimitiveType.Lines, rl, displaylines));

                items.Add(new GLColorShaderWorld(), "COS-1L");

                float h = 0;

                int dist = 1000 / lyscale;
                Color cr = Color.FromArgb(100, Color.White);
                rObjects.Add(items.Shader("COS-1L"),    // horizontal
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, rl,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(left, h, front), new Vector3(left, h, back), new Vector3(dist, 0, 0), (back - front) / dist + 1),
                                                        new OpenTK.Graphics.Color4[] { cr })
                                   );

                rObjects.Add(items.Shader("COS-1L"),
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, rl,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(left, h, front), new Vector3(right, h, front), new Vector3(0, 0, dist), (right - left) / dist + 1),
                                                        new OpenTK.Graphics.Color4[] { cr })
                                   );

                rObjects.Add(new GLOperationClearDepthBuffer());
            }

            int ctrlo = 2048 | 32 | 64;
            ctrlo = -1;

            if ((ctrlo & 4096) != 0)
            {
                userimages = new ImageCache(items, rObjects);
                usertexturebitmaps = new GLBindlessTextureBitmaps("UserBitmaps", rObjects, userbitmapsarbblock,false);
                items.Add(usertexturebitmaps);
            }

            if ((ctrlo & 1) != 0) // galaxy
            {
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


                const int gnoisetexbinding = 3;     //tex bindings are attached per shaders so are not global
                const int gdisttexbinding = 4;
                const int galtexbinding = 1;

                volumetricblock = new GLVolumetricUniformBlock(volumenticuniformblock);
                items.Add(volumetricblock, "VB");

                int sc = 1;
                GLTexture3D noise3d = new GLTexture3D(1024 * sc, 64 * sc, 1024 * sc, OpenTK.Graphics.OpenGL4.SizedInternalFormat.R32f); // red channel only
                items.Add(noise3d, "Noise");
                ComputeShaderNoise3D csn = new ComputeShaderNoise3D(noise3d.Width, noise3d.Height, noise3d.Depth, 128 * sc, 16 * sc, 128 * sc, gnoisetexbinding);       // must be a multiple of localgroupsize in csn
                csn.StartAction += (A, m) => { noise3d.BindImage(gnoisetexbinding); };
                csn.Run();      // compute noise
                csn.Dispose();

                GLTexture1D gaussiantex = new GLTexture1D(1024, OpenTK.Graphics.OpenGL4.SizedInternalFormat.R32f); // red channel only
                items.Add(gaussiantex, "Gaussian");

                // set centre=width, higher widths means more curve, higher std dev compensate.
                // fill the gaussiantex with data
                ComputeShaderGaussian gsn = new ComputeShaderGaussian(gaussiantex.Width, 2.0f, 2.0f, 1.4f, gdisttexbinding);
                gsn.StartAction += (A, m) => { gaussiantex.BindImage(gdisttexbinding); };
                gsn.Run();      // compute noise
                gsn.Dispose();

                GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);

                // load one upside down and horz flipped, because the volumetric co-ords are 0,0,0 bottom left, 1,1,1 top right
                GLTexture2D galtex = new GLTexture2D(Properties.Resources.Galaxy_L180, SizedInternalFormat.Rgba8);
                items.Add(galtex, "galtex");
                galaxyshader = new GalaxyShader(volumenticuniformblock, galtexbinding, gnoisetexbinding, gdisttexbinding);
                items.Add(galaxyshader, "Galaxy-sh");
                // bind the galaxy texture, the 3dnoise, and the gaussian 1-d texture for the shader
                galaxyshader.StartAction += (a, m) => { galtex.Bind(galtexbinding); noise3d.Bind(gnoisetexbinding); gaussiantex.Bind(gdisttexbinding); };      // shader requires these, so bind using shader

                GLRenderState rt = GLRenderState.Tri();
                galaxyrenderable = GLRenderableItem.CreateNullVertex(OpenTK.Graphics.OpenGL4.PrimitiveType.Points, rt);   // no vertexes, all data from bound volumetric uniform, no instances as yet
                rObjects.Add(galaxyshader, "galshader", galaxyrenderable);
            }

            if ((ctrlo & 2) != 0)
            {
                var corr = new GalMapRegions.ManualCorrections[] {          // nerf the centeroid position slightly
                    new GalMapRegions.ManualCorrections("The Galactic Aphelion", y: -2000 ),
                    new GalMapRegions.ManualCorrections("The Abyss", y: +3000 ),
                    new GalMapRegions.ManualCorrections("Eurus", y: -3000 ),
                    new GalMapRegions.ManualCorrections("The Perseus Transit", x: -3000, y: -3000 ),
                    new GalMapRegions.ManualCorrections("Zephyrus", x: 0, y: 2000 ),
                };

                edsmgalmapregions = new GalMapRegions();
                edsmgalmapregions.CreateObjects("edsmregions", items, rObjects, edsmmapping, 8000, corr: corr);
            }

            if ((ctrlo & 4) != 0)
            {
                elitemapregions = new GalMapRegions();
                elitemapregions.CreateObjects("eliteregions", items, rObjects, eliteregions, 8000);
                EliteRegionsEnable = false;
            }

            if ((ctrlo & 8) != 0)
            {
                int gran = 8;
                Bitmap img = Properties.Resources.Galaxy_L180;
                Bitmap heat = img.Function(img.Width / gran, img.Height / gran, mode: GLOFC.Utils.BitMapHelpers.BitmapFunction.HeatMap);
                //heat.Save(@"c:\code\heatmap.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

                Random rnd = new Random(23);

                GLBuffer buf = items.NewBuffer(16 * 350000, false);     // since RND is fixed, should get the same number every time.
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
                            //int c = Math.Min(Math.Max(i * i * i / 24000000, 1), 40);

                            dist *= 2000 / lyscale;
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
                GLRenderState rc = GLRenderState.Points(1);
                rc.DepthTest = false; // note, if this is true, there is a wierd different between left and right in view.. not sure why
                rObjects.Add(stardots, "stardots", GLRenderableItem.CreateVector4(items, OpenTK.Graphics.OpenGL4.PrimitiveType.Points, rc, buf, points));
                System.Diagnostics.Debug.WriteLine("Stars " + points);
            }

            rObjects.Add(new GLOperationClearDepthBuffer()); // clear depth buffer and now use full depth testing on the rest

            //if ((ctrlo & 16) != 0) // no longer used in main program
            //{
            //    items.Add(new GLTexture2D(Properties.Resources.StarFlare2, SizedInternalFormat.Rgba8), "lensflare");
            //    items.Add(new GLPointSpriteShader(items.Tex("lensflare"), 64, 40), "PS");
            //    var p = GLPointsFactory.RandomStars4(1000, 0, 25899/lyscale, 10000/lyscale, 1000/lyscale, -1000/lyscale);

            //    GLRenderState rps = GLRenderState.PointsByProgram();

            //    rObjects.Add(items.Shader("PS"), "starsprites", GLRenderableItem.CreateVector4Color4(items,PrimitiveType.Points, rps, p, 
            //                            new Color4[] { Color.White }));

            //}

            if ((ctrlo & 32) != 0)
            {
                gridvertshader = new DynamicGridVertexShader(Color.Cyan);
                //items.Add(gridvertshader, "PLGRIDVertShader");
                var frag = new GLPLFragmentShaderVSColor();
                //items.Add(frag, "PLGRIDFragShader");

                GLRenderState rl = GLRenderState.Lines();

                items.Add(new GLShaderPipeline(gridvertshader, frag), "DYNGRID");

                gridrenderable = GLRenderableItem.CreateNullVertex(OpenTK.Graphics.OpenGL4.PrimitiveType.Lines, rl, drawcount: 2);

                rObjects.Add(items.Shader("DYNGRID"), "DYNGRIDRENDER", gridrenderable);

            }

            if ((ctrlo & 64) != 0)
            {
                gridbitmapvertshader = new DynamicGridCoordVertexShader();
                var frag = new GLPLFragmentShaderTexture2DIndexed(0);

                GLRenderState rl = GLRenderState.Tri(cullface: false);

                GLTexture2DArray gridtexcoords = new GLTexture2DArray();
                items.Add(gridtexcoords, "PLGridBitmapTextures");

                GLShaderPipeline sp = new GLShaderPipeline(gridbitmapvertshader, frag);
                items.Add(sp, "DYNGRIDBitmap");

                rObjects.Add(items.Shader("DYNGRIDBitmap"), "DYNGRIDBitmapRENDER", GLRenderableItem.CreateNullVertex(OpenTK.Graphics.OpenGL4.PrimitiveType.TriangleStrip, rl, drawcount: 4, instancecount: 9));
            }

            var starimagearray = new GLTexture2DArray();
            Bitmap[] starbmps = new Bitmap[] { Properties.Resources.O, Properties.Resources.A, Properties.Resources.F, Properties.Resources.G, Properties.Resources.N };
            Bitmap[] starbmpsreduced = starbmps.CropImages(new RectangleF(16, 16, 68, 68));
            //for (int b = 0; b < starbmpsreduced.Length; b++)  starbmpsreduced[b].Save(@"c:\code\" + $"star{b}.bmp", System.Drawing.Imaging.ImageFormat.Png);
            starimagearray.CreateLoadBitmaps(starbmpsreduced, SizedInternalFormat.Rgba8, ownbmp: true);
            items.Add(starimagearray);
            long[] starimagearraycontrolword = { 0, 1 + 65536, 2, 3, 4 + 65536 };

            GLStorageBlock findresults = items.NewStorageBlock(findblock);

            float sunsize = .5f;
            if ((ctrlo & 128) != 0)
            {
                Random rnd = new Random(52);
                List<HistoryEntry> pos = new List<HistoryEntry>();
                DateTime start = new DateTime(2020, 1, 1);
                Color[] colors = new Color[] { Color.Red, Color.Green, Color.Blue, Color.White, Color.Black, Color.Purple, Color.Yellow };
                for (int j = 0; j <= 200; j++)
                {
                    Color jc = colors[(j / 30) % colors.Length];
                    int i = j * 10;
                    string name = "Kyli Flyuae AA-B h" + j.ToString();
                    if (i < 30000)
                        pos.Add(new HistoryEntry(start, name, i + rnd.Next(50), rnd.Next(50), i, jc));
                    else if (i < 60000)
                        pos.Add(new HistoryEntry(start, name, 60000 - i + rnd.Next(50), rnd.Next(50), i, jc));
                    else if (i < 90000)
                        pos.Add(new HistoryEntry(start, name, -(i - 60000) + rnd.Next(50), rnd.Next(50), 120000 - i, jc));
                    else
                        pos.Add(new HistoryEntry(start, name, -30000 + (i - 90000) + rnd.Next(50), rnd.Next(50), -i + 120000, jc));

                    start = start.AddDays(1);
                }

                // tested to 50k stars

                travelpath = new TravelPath(1000);
                travelpath.Create(items, rObjects, new Tuple<GLTexture2DArray, long[]>(starimagearray, starimagearraycontrolword), pos, sunsize, sunsize, findresults, true);
                travelpath.SetSystem(0);
            }

            if ((ctrlo & 256) != 0)
            {
                galmapobjects = new GalMapObjects();
                galmapobjects.CreateObjects(items, rObjects, edsmmapping, findresults, true);
            }

            if ((ctrlo & 512) != 0)
            {
                galaxystars = new GalaxyStars(items, rObjects, new Tuple<GLTexture2DArray, long[]>(starimagearray, starimagearraycontrolword), sunsize, findresults);
            }

            if ((ctrlo & 1024) != 0)
            {
                rightclickmenu = new GLContextMenu("RightClickMenu", true,
                    new GLMenuItem("RCMInfo", "Information")
                    {
                        MouseClick = (s, e) => {
                            if (e.Button == GLMouseEventArgs.MouseButtons.Left)
                            {
                                var nl = NameLocationDescription(rightclickmenu.Tag);
                                System.Diagnostics.Debug.WriteLine($"Info {nl.Item1} {nl.Item2}");
                                // logical name is important as menu uses it to close down
                                GLMessageBox msg = new GLMessageBox("InfoBoxForm-1", displaycontrol, e.WindowLocation,
                                        nl.Item3, $"{nl.Item1} @ {nl.Item2.X:#.#},{nl.Item2.Y:#.#},{nl.Item2.Z:#.#}", GLMessageBox.MessageBoxButtons.OK, null,
                                            Color.FromArgb(220, 60, 60, 70), Color.DarkOrange);
                            }
                        }
                    },
                    new GLMenuItem("RCMZoomIn", "Goto Zoom In")
                    {
                        Click = (s) => {
                            var nl = NameLocationDescription(rightclickmenu.Tag);
                            gl3dcontroller.SlewToPositionZoom(nl.Item2, 100, -1);
                        }
                    },
                    new GLMenuItem("RCMGoto", "Goto Position")
                    {
                        Click = (s) => {
                            var nl = NameLocationDescription(rightclickmenu.Tag);
                            System.Diagnostics.Debug.WriteLine($"Goto {nl.Item1} {nl.Item2}");
                            gl3dcontroller.SlewToPosition(nl.Item2, -1);
                        }
                    },
                    new GLMenuItem("RCMLookAt", "Look At") {
                        Click = (s) => {
                            var nl = NameLocationDescription(rightclickmenu.Tag);
                            gl3dcontroller.PanTo(nl.Item2, -1);
                        }
                    }
                );

            }

            if ((ctrlo & 2048) != 0)
            {
                bookmarks = new Bookmarks();
                var syslist = new List<SystemClass> { new SystemClass("bk1", 1000, 0, 0), new SystemClass("bk1", 1000, 0, 2000), };
                bookmarks.Create(items, rObjects, syslist, 1.0f, findresults, false);
            }
            // Matrix calc holding transform info

            //string[] extensions = GLStatics.Extensions();
            //string elist = string.Join(Environment.NewLine, extensions);

            matrixcalc = new GLMatrixCalc();
            matrixcalc.PerspectiveNearZDistance = 0.5f;
            matrixcalc.PerspectiveFarZDistance = 120000f / lyscale;
            matrixcalc.DepthRange = new Vector2(0, 1);
            matrixcalc.InPerspectiveMode = true;
            matrixcalc.ResizeViewPort(this, glwfc.Size);          // must establish size before starting

            // menu system

            displaycontrol = new GLControlDisplay(items, glwfc, matrixcalc, true, 0.00001f, 0.00001f);       // hook form to the window - its the master
            displaycontrol.Font = new Font("Arial", 10f);
            displaycontrol.Focusable = true;          // we want to be able to focus and receive key presses.
            displaycontrol.SetFocus();

            displaycontrol.Paint += (ts) => {
                galaxymenu?.UpdateCoords(gl3dcontroller);
                displaycontrol.Animate(glwfc.ElapsedTimems);
            
                GLStatics.ClearDepthBuffer();         // clear the depth buffer, so we are on top of all previous renders.
                displaycontrol.Render(glwfc.RenderState, ts);
            };

            // 3d controller

            gl3dcontroller = new Controller3D();
            gl3dcontroller.PosCamera.ZoomMax = 5000;     // gives 5ly
            gl3dcontroller.ZoomDistance = 3000F / lyscale;
            gl3dcontroller.PosCamera.ZoomMin = 0.1f;
            gl3dcontroller.PosCamera.ZoomScaling = 1.1f;
            gl3dcontroller.YHoldMovement = true;
            gl3dcontroller.PaintObjects = Controller3DDraw;
            gl3dcontroller.KeyboardTravelSpeed = (ms, eyedist) =>
            {
                double eyedistr = Math.Pow(eyedist, 1.0);
                float v = (float)Math.Max(eyedistr / 1200, 0);
                //System.Diagnostics.Debug.WriteLine("Speed " + eyedistr + " "+ v);
                return (float)ms * v;
            };

            // start hooks the glwfc paint function up, first, so it gets to go first
            // No ui events from glwfc.
            gl3dcontroller.Start(matrixcalc,glwfc, new Vector3(0, 0, 10000), new Vector3(140.75f, 0, 0), 0.5F, false, false);
            gl3dcontroller.Hook(displaycontrol, glwfc); // we get 3dcontroller events from displaycontrol, so it will get them when everything else is unselected
            displaycontrol.Hook();  // now we hook up display control to glwin, and paint

            displaycontrol.MouseClick += MouseClickOnMap;


            galaxymenu = new MapMenu(this, userimages);

            // Autocomplete text box at top for searching

            GLTextBoxAutoComplete tbac = ((GLTextBoxAutoComplete)displaycontrol[MapMenu.EntryTextName]);

            if (tbac != null)
            { 
                tbac.PerformAutoCompleteInUIThread = (s, a, set) =>
                {
                    System.Diagnostics.Debug.Assert(Application.MessageLoop);       // must be in UI thread
                    var glist = edsmmapping.GalacticMapObjects.Where(x => s.Length < 3 ? x.Name.StartsWith(s, StringComparison.InvariantCultureIgnoreCase) : x.Name.Contains(s, StringComparison.InvariantCultureIgnoreCase)).Select(x => x).ToList();
                    List<string> list = glist.Select(x => x.Name).ToList();
                    list.AddRange(travelpath.CurrentList.Where(x => s.Length < 3 ? x.System.Name.StartsWith(s, StringComparison.InvariantCultureIgnoreCase) : x.System.Name.Contains(s, StringComparison.InvariantCultureIgnoreCase)).Select(x => x.System.Name));
                    foreach (var x in list)
                        set.Add(x);
                };

                tbac.SelectedEntry = (a) =>     // in UI thread
                {
                    System.Diagnostics.Debug.Assert(Application.MessageLoop);       // must be in UI thread
                    System.Diagnostics.Debug.WriteLine("Selected " + tbac.Text);
                    var gmo = edsmmapping.GalacticMapObjects.Find(x => x.Name.Equals(tbac.Text, StringComparison.InvariantCultureIgnoreCase));
                    if (gmo != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Move to gmo " + gmo.Points[0]);
                        gl3dcontroller.SlewToPosition(new Vector3((float)gmo.Points[0].X, (float)gmo.Points[0].Y, (float)gmo.Points[0].Z), -1);
                    }
                    else
                    {
                        var he = travelpath.CurrentList.Find(x => x.System.Name.Equals(tbac.Text, StringComparison.InvariantCultureIgnoreCase));
                        if (he != null)
                        {
                            System.Diagnostics.Debug.WriteLine("Move to sys " + he.System.Name);
                            gl3dcontroller.SlewToPosition(new Vector3((float)he.System.X, (float)he.System.Y, (float)he.System.Z), -1);
                        }
                        else
                            tbac.InErrorCondition = true;
                    }
                };
            }

            if (galaxystars != null)
                galaxystars.Start();

            if (false)        // enable for debug buffer
            {
                debugbuffer = new GLStorageBlock(31, true);
                debugbuffer.AllocateBytes(32000, OpenTK.Graphics.OpenGL4.BufferUsageHint.DynamicCopy);       // set size of vec buffer
            }

            if (false)          // enable for debug
            {
                items.Add(new GLColorShaderObjectTranslation(), "COSOT");
                GLRenderState rc = GLRenderState.Tri(cullface: false);
                rc.DepthTest = false;

                Vector3[] markers = new Vector3[] { new Vector3(0, 0, 0), new Vector3(0, -5, 0), new Vector3(0, -5 - 3.125f / 2f, 0) };

                for (int i = 0; i < markers.Length; i++)
                {
                    rObjects.Add(items.Shader("COSOT"), "marker" + i,
                            GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Triangles, rc,
                                            GLCubeObjectFactory.CreateSolidCubeFromTriangles(0.5f),
                                            new Color4[] { Color4.Red, Color4.Green, Color4.Blue, Color4.White, Color4.Cyan, Color4.Orange },
                                            new GLRenderDataTranslationRotation(markers[i])
                            ));
                }

            }

            string shaderlog = GLShaderLog.ShaderLog;
            if (shaderlog.HasChars())
                MessageBox.Show(shaderlog, "Shader Log - report to EDD");
            if (GLShaderLog.Okay == false)
                MessageBox.Show(shaderlog, "Map broken - abort");
           
        }

        public void LoadState(MapSaver defaults)
        {
            gl3dcontroller.ChangePerspectiveMode(defaults.GetSetting("GAL3DMode", true));

            long gs = defaults.GetSetting("GALSTARS", (long)3);        // due to JSON all data comes back as longs
            GalaxyStars = (int)gs;

            GalaxyDisplay = defaults.GetSetting("GD", true);
            StarDotsDisplay = defaults.GetSetting("SDD", true);

            TravelPathDisplay = defaults.GetSetting("TPD", true);
            TravelPathStartDate = defaults.GetSetting("TPSD", new DateTime(2014, 12, 16));
            TravelPathStartDateEnable = defaults.GetSetting("TPSDE", false);
            TravelPathEndDate = defaults.GetSetting("TPED", DateTime.UtcNow.AddMonths(1));
            TravelPathEndDateEnable = defaults.GetSetting("TPEDE", false);
            if (TravelPathStartDateEnable || TravelPathEndDateEnable)
                travelpath.Refresh();       // and refresh it if we set the data

            GalObjectDisplay = defaults.GetSetting("GALOD", true);
            SetAllGalObjectTypeEnables(defaults.GetSetting("GALOBJLIST", ""));

            UserImagesEnable = defaults.GetSetting("ImagesEnable", true);
            userimages?.LoadFromString(defaults.GetSetting("ImagesList", ""));

            EDSMRegionsEnable = defaults.GetSetting("ERe", false);
            EDSMRegionsOutlineEnable = defaults.GetSetting("ERoe", false);
            EDSMRegionsShadingEnable = defaults.GetSetting("ERse", false);
            EDSMRegionsTextEnable = defaults.GetSetting("ERte", false);

            EliteRegionsEnable = defaults.GetSetting("ELe", true);
            EliteRegionsOutlineEnable = defaults.GetSetting("ELoe", true);
            EliteRegionsShadingEnable = defaults.GetSetting("ELse", false);
            EliteRegionsTextEnable = defaults.GetSetting("ELte", true);
            gl3dcontroller.SetPositionCamera(defaults.GetSetting("POSCAMERA", ""));     // go thru gl3dcontroller to set default position, so we reset the model matrix

        }

        public void SaveState(MapSaver defaults)
        {
            defaults.PutSetting("GAL3DMode", gl3dcontroller.MatrixCalc.InPerspectiveMode);
            defaults.PutSetting("GALSTARS", GalaxyStars);
            defaults.PutSetting("GD", GalaxyDisplay);
            defaults.PutSetting("SDD", StarDotsDisplay);
            defaults.PutSetting("TPD", TravelPathDisplay);
            defaults.PutSetting("TPSD", TravelPathStartDate);
            defaults.PutSetting("TPSDE", TravelPathStartDateEnable);
            defaults.PutSetting("TPED", TravelPathEndDate);
            defaults.PutSetting("TPEDE", TravelPathEndDateEnable);
            defaults.PutSetting("GALOD", GalObjectDisplay);
            defaults.PutSetting("GALOBJLIST", GetAllGalObjectTypeEnables());
            defaults.PutSetting("ERe", EDSMRegionsEnable);
            defaults.PutSetting("ERoe", EDSMRegionsOutlineEnable);
            defaults.PutSetting("ERse", EDSMRegionsShadingEnable);
            defaults.PutSetting("ERte", EDSMRegionsTextEnable);
            defaults.PutSetting("ELe", EliteRegionsEnable);
            defaults.PutSetting("ELoe", EliteRegionsOutlineEnable);
            defaults.PutSetting("ELse", EliteRegionsShadingEnable);
            defaults.PutSetting("ELte", EliteRegionsTextEnable);
            defaults.PutSetting("POSCAMERA", gl3dcontroller.PosCamera.StringPositionCamera);

            defaults.PutSetting("ImagesEnable", UserImagesEnable);
            if (userimages!=null)
                defaults.PutSetting("ImagesList", userimages.ImageStringList());
        }

        public void LoadBitmaps()
        {
            if (userimages != null)
            {
                usertexturebitmaps.Clear();
                userimages.LoadBitmaps(Assembly.GetExecutingAssembly(), "TestOpenTk.Properties.Resources",
                    (ie) =>
                    {
                        usertexturebitmaps.Add(null, null, ie.Bitmap, 1, ie.Centre, 
                            new Vector3(ie.Size.X, 0, ie.Size.Y),
                            new Vector3(ie.RotationDegrees.X.Radians(), ie.RotationDegrees.Y.Radians(), ie.RotationDegrees.Z.Radians()),
                            ie.RotateToViewer, ie.RotateElevation, ie.AlphaFadeScalar, ie.AlphaFadePosition);
                    },
                    (ie) => // http load
                    {
                        System.Threading.Tasks.Task.Run(() =>
                        {
                            string name = ie.ImagePathOrURL;
                            string path = @"c:\code\" + ie.ImagePathOrURL.Replace("http://", "", StringComparison.InvariantCultureIgnoreCase).
                                        Replace("https://", "", StringComparison.InvariantCultureIgnoreCase).SafeFileString();
                            System.Diagnostics.Debug.WriteLine($"HTTP load {name} to {path}");

                            bool res = BaseUtils.DownloadFile.HTTPDownloadFile(name, path, false, out bool newfile);

                            if (res)
                            {
                                uiinvoker(() =>
                                {
                                    if (ie.LoadBitmap(path))
                                    {
                                        usertexturebitmaps.Add(null, null, ie.Bitmap, 1,
                                            new Vector3(ie.Centre.X, ie.Centre.Y, ie.Centre.Z),
                                            new Vector3(ie.Size.X, 0, ie.Size.Y),
                                            new Vector3(ie.RotationDegrees.X.Radians(), ie.RotationDegrees.Y.Radians(), ie.RotationDegrees.Z.Radians()),
                                            ie.RotateToViewer, ie.RotateElevation, ie.AlphaFadeScalar, ie.AlphaFadePosition);
                                    }
                                });
                            }
                        });
                    },
                    null);

            }

        }
        
        #endregion

        #region Display

        public void Systick()
        {
            var cdmt = gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true, OtherKeys);
            //if (cdmt)
            glwfc.Invalidate();
        }

        double fpsavg = 0;
        long lastms;
        float lasteyedistance = 100000000;
        int lastgridwidth;

        private void Controller3DDraw(Controller3D c3d, ulong time)
        {
            long t1 = hptimer.ElapsedTicks;
            //TBD
            //    GL.Finish();      // use GL finish to ensure last frame is done - if we are operating above sys tick rate, this will be small time. If we are rendering too much, it will stall
            long t2 = hptimer.ElapsedTicks;

            GLMatrixCalcUniformBlock mcb = ((GLMatrixCalcUniformBlock)items.UB("MCUB"));
            mcb.SetFull(gl3dcontroller.MatrixCalc);        // set the matrix unform block to the controller 3d matrix calc.

            // set up the grid shader size

            if (gridrenderable != null)
            {
                gridrenderable.InstanceCount = gridvertshader.ComputeGridSize(gl3dcontroller.MatrixCalc.LookAt.Y, gl3dcontroller.MatrixCalc.EyeDistance, out lastgridwidth);
                lasteyedistance = gl3dcontroller.MatrixCalc.EyeDistance;

                gridvertshader.SetUniforms(gl3dcontroller.MatrixCalc.LookAt, lastgridwidth, gridrenderable.InstanceCount);
            }

            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);      // ensure clear before start

            if (gridbitmapvertshader != null)
            {
                float coordfade = lastgridwidth == 10000 ? (0.7f - (c3d.MatrixCalc.EyeDistance / 20000).Clamp(0.0f, 0.7f)) : 0.7f;
                Color coordscol = Color.FromArgb(coordfade < 0.05 ? 0 : 150, Color.Cyan);
                gridbitmapvertshader.ComputeUniforms(lastgridwidth, gl3dcontroller.MatrixCalc, gl3dcontroller.PosCamera.CameraDirection, coordscol, Color.Transparent);
            }

            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out glasserterr), glasserterr);      // ensure clear before start

            if (edsmgalmapregions != null)
                edsmgalmapregions.SetY(gl3dcontroller.PosCamera.LookAt.Y);

            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out glasserterr), glasserterr);      // ensure clear before start

            if (elitemapregions != null)
                elitemapregions.SetY(gl3dcontroller.PosCamera.LookAt.Y);

            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out glasserterr), glasserterr);      // ensure clear before start
            // set the galaxy volumetric block

            if (galaxyrenderable != null)
            {
                galaxyrenderable.InstanceCount = volumetricblock.Set(gl3dcontroller.MatrixCalc, volumetricboundingbox, gl3dcontroller.MatrixCalc.InPerspectiveMode ? 50.0f : 0);        // set up the volumentric uniform

                System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out glasserterr), glasserterr);      // ensure clear before start

                galaxyshader.SetFader(gl3dcontroller.MatrixCalc.EyePosition, gl3dcontroller.MatrixCalc.EyeDistance, gl3dcontroller.MatrixCalc.InPerspectiveMode);
            }

            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out glasserterr), glasserterr);      // ensure clear before start

            long t3 = hptimer.ElapsedTicks;

            if (travelpath != null)
                travelpath.Update(time, gl3dcontroller.MatrixCalc.EyeDistance);

            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out glasserterr), glasserterr);      // ensure clear before start

            if (galmapobjects != null)
                galmapobjects.Update(time, gl3dcontroller.MatrixCalc.EyeDistance);

            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out glasserterr), glasserterr);      // ensure clear before start

            if (galaxystars != null)
                galaxystars.Update(time, gl3dcontroller.MatrixCalc.EyeDistance);

            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out glasserterr), glasserterr);      // ensure clear before start

            if (galaxystars != null && gl3dcontroller.MatrixCalc.EyeDistance < 400 && Math.Abs(gl3dcontroller.PosCamera.LookAt.Z) < 800)
                galaxystars.Request9BoxConditional(gl3dcontroller.PosCamera.LookAt);

            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out glasserterr), glasserterr);      // ensure clear before start

            long t4 = hptimer.ElapsedTicks;

            //int[] queryID = new int[2];
            //GL.GenQueries(2, queryID);
            //GL.QueryCounter(queryID[0], QueryCounterTarget.Timestamp);

            var tmr1 = new GLOperationQueryTimeStamp();
            var tmr2 = new GLOperationQueryTimeStamp();
            tmr1.Execute(null);

            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc, verbose: false);
            tmr2.Execute(null);

            // GL.QueryCounter(queryID[1], QueryCounterTarget.Timestamp);

            //   GL.Flush(); // ensure everything is in the grapghics pipeline

            //while (tmr2.IsAvailable() == false)
            //    ;

            long a = tmr1.GetCounter();
            long b = tmr2.GetCounter();

            //int done = 0;
            //while (done == 0)
            //{
            //    GL.GetQueryObject(queryID[1], GetQueryObjectParam.QueryResultAvailable, out done);
            //}

            //GL.GetQueryObject(queryID[0], GetQueryObjectParam.QueryResult, out long a);
            //GL.GetQueryObject(queryID[1], GetQueryObjectParam.QueryResult, out long b);
            //System.Diagnostics.Debug.WriteLine($"timer {a} {b} {b-a}" );

            long t5 = hptimer.ElapsedTicks;

            if (debugbuffer != null)
            {
                GLMemoryBarrier.All();
                Vector4[] debugout = debugbuffer.ReadVector4s(0, 4);
                System.Diagnostics.Debug.WriteLine("{0},{1},{2},{3}", debugout[0], debugout[1], debugout[2], debugout[3]);
            }

            long t = hptimer.ElapsedMilliseconds;
            long diff = t - lastms;

            for (int i = frametimes.Length - 1; i > 0; i--)
                frametimes[i] = frametimes[i - 1];

            frametimes[0] = diff;

            lastms = t;
            double fps = (1000.0 / diff);
            if (fpsavg <= 1)
                fpsavg = fps;
            else
                fpsavg = (fpsavg * 0.95) + fps * 0.05;

            tmr1.Dispose();
            tmr2.Dispose();
            if (diff > 0)
            {
                //                System.Diagnostics.Debug.Write($"Frame {hptimer.ElapsedMilliseconds,6} {diff,3} fps {fpsavg:#.0} frames {frametimes[0],3} {frametimes[1],3} {frametimes[2],3} {frametimes[3],3} {frametimes[4],3} {frametimes[5],3} sec {galaxystars.Sectors,3}");
                //                System.Diagnostics.Debug.WriteLine($" finish {(t2 - t1) * 1000000 / Stopwatch.Frequency,5} t3 {(t3 - t2) * 1000000 / Stopwatch.Frequency,4} t4 {(t4 - t3) * 1000000 / Stopwatch.Frequency,4} render {(t5 - t4) * 1000000 / Stopwatch.Frequency,5} tot {(t5 - t1) * 1000000 / Stopwatch.Frequency,5}");
            }
            //            this.Text = "FPS " + fpsavg.ToString("N0") + " Looking at " + gl3dcontroller.MatrixCalc.TargetPosition + " eye@ " + gl3dcontroller.MatrixCalc.EyePosition + " dir " + gl3dcontroller.Pos.CameraDirection + " Dist " + gl3dcontroller.MatrixCalc.EyeDistance + " Zoom " + gl3dcontroller.Pos.ZoomFactor;
        }

        long[] frametimes = new long[6];

        #endregion

        #region Turn on/off, move, etc.

        public bool GalaxyDisplay { get { return galaxyshader?.Enable ?? false; } set { if (galaxyshader != null) { galaxyshader.Enable = value; glwfc.Invalidate(); } } }
        public bool StarDotsDisplay { get { return stardots?.Enable ?? false; } set { if (stardots != null) { stardots.Enable = value; glwfc.Invalidate(); } } }
        public bool TravelPathDisplay { get { return travelpath?.Enable ?? false; } set { if (travelpath != null) { travelpath.Enable = value; glwfc.Invalidate(); } } }

        public int GalaxyStars { get { return galaxystars?.EnableMode ?? 0; } set { if (galaxystars != null) galaxystars.EnableMode = value; glwfc.Invalidate(); } }

        public void TravelPathRefresh() { travelpath.Refresh(); }   // travelpath.Refresh() manually after these have changed
        public DateTime TravelPathStartDate { get { return travelpath?.TravelPathStartDate ?? DateTime.MinValue; } set { if (travelpath != null) { if (travelpath.TravelPathStartDate != value) { travelpath.TravelPathStartDate = value; } } } }
        public bool TravelPathStartDateEnable { get { return travelpath?.TravelPathStartDateEnable ?? false; } set { if (travelpath != null) { if (travelpath.TravelPathStartDateEnable != value) { travelpath.TravelPathStartDateEnable = value; } } } }
        public DateTime TravelPathEndDate { get { return travelpath?.TravelPathEndDate ?? DateTime.MinValue; } set { if (travelpath != null) { if (travelpath.TravelPathEndDate != value) { travelpath.TravelPathEndDate = value; } } } }
        public bool TravelPathEndDateEnable { get { return travelpath?.TravelPathEndDateEnable ?? false; } set { if (travelpath != null) { if (travelpath.TravelPathEndDateEnable != value) { travelpath.TravelPathEndDateEnable = value; } } } }

        public bool GalObjectDisplay { get { return galmapobjects?.Enable ?? false; } set { if (galmapobjects != null) { galmapobjects.Enable = value; glwfc.Invalidate(); } } }
        public void SetGalObjectTypeEnable(string id, bool state) { galmapobjects.SetGalObjectTypeEnable(id, state); glwfc.Invalidate(); }
        public bool GetGalObjectTypeEnable(string id) { return galmapobjects?.GetGalObjectTypeEnable(id) ?? false; }
        public void SetAllGalObjectTypeEnables(string set) { if (galmapobjects != null) { galmapobjects.SetAllEnables(set); glwfc.Invalidate(); } }
        public string GetAllGalObjectTypeEnables() { return galmapobjects?.GetAllEnables() ?? ""; }
        public bool EDSMRegionsEnable { get { return edsmgalmapregions?.Enable ?? false; } set { if (edsmgalmapregions != null) { edsmgalmapregions.Enable = value; glwfc.Invalidate(); } } } 
        public bool EDSMRegionsOutlineEnable { get { return edsmgalmapregions?.Outlines ?? false; } set { if (edsmgalmapregions != null) { edsmgalmapregions.Outlines = value; glwfc.Invalidate(); } } }
        public bool EDSMRegionsShadingEnable { get { return edsmgalmapregions?.Regions ?? false; } set { if (edsmgalmapregions != null) { edsmgalmapregions.Regions = value; glwfc.Invalidate(); } } }
        public bool EDSMRegionsTextEnable { get { return edsmgalmapregions?.Text ?? false; } set { if (edsmgalmapregions != null) { edsmgalmapregions.Text = value; glwfc.Invalidate(); } } }
        public bool EliteRegionsEnable { get { return elitemapregions?.Enable ?? false; } set { if (elitemapregions != null) { elitemapregions.Enable = value; glwfc.Invalidate(); } } }
        public bool EliteRegionsOutlineEnable { get { return elitemapregions?.Outlines ?? false; } set { if (elitemapregions != null) { elitemapregions.Outlines = value; glwfc.Invalidate(); } } }
        public bool EliteRegionsShadingEnable { get { return elitemapregions?.Regions ?? false; } set { if (elitemapregions != null) { elitemapregions.Regions = value; glwfc.Invalidate(); } } }
        public bool EliteRegionsTextEnable { get { return elitemapregions?.Text ?? false; } set { if (elitemapregions != null) { elitemapregions.Text = value; glwfc.Invalidate(); } } }

        public bool UserImagesEnable { get { return usertexturebitmaps?.Enable ?? false; } set { if (usertexturebitmaps != null) { usertexturebitmaps.Enable = value; glwfc.Invalidate(); } } }

        public void GoToTravelSystem(int dir)      //0 = home, 1 = next, -1 = prev
        {
            var he = dir == 0 ? travelpath.CurrentSystem : (dir < 0 ? travelpath.PrevSystem() : travelpath.NextSystem());
            if ( he!= null)
            {
                gl3dcontroller.SlewToPosition(new Vector3((float)he.System.X, (float)he.System.Y, (float)he.System.Z), -1);
                SetEntryText(he.System.Name);
            }
        }

#endregion

#region Helpers

        private void SetEntryText(string text)
        {
            ((GLTextBoxAutoComplete)displaycontrol[MapMenu.EntryTextName]).Text = text;
            ((GLTextBoxAutoComplete)displaycontrol[MapMenu.EntryTextName]).CancelAutoComplete();
            displaycontrol.SetFocus();
        }

        private Object FindObjectOnMap(Point loc)
        {
            var he = travelpath?.FindSystem(loc, glwfc.RenderState, matrixcalc.ViewPort.Size, out float tz);
            if (he != null)
                return he;
            var gmo = galmapobjects?.FindPOI(loc, glwfc.RenderState, matrixcalc.ViewPort.Size, out float gz);
            if (gmo != null)
                return gmo;
            var sys = galaxystars?.Find(loc, glwfc.RenderState, matrixcalc.ViewPort.Size, out float sz);
            if (sys != null)
            {
                string[] nparts = sys.Name.Split(',');
                sys.X = nparts[0].InvariantParseDouble(-10000);
                sys.Y = nparts[1].InvariantParseDouble(-10000);
                sys.Z = nparts[2].Substring(0,nparts[2].IndexOf(':')).InvariantParseDouble(-10000);
                return sys;
            }
            var bk = bookmarks?.Find(loc, glwfc.RenderState, matrixcalc.ViewPort.Size, out float bz);
            if ( bk != null )
            {
                return bk;
            }
            return null;
        }

        private Tuple<string,Vector3,string> NameLocationDescription(Object obj)       // given a type, return its name and location
        {
            var he = obj as HistoryEntry;
            var gmo = obj as GalacticMapObject;
            var sys = obj as SystemClass;
            if (he != null)
            {

                return new Tuple<string, Vector3, string>(he.System.Name,
                                                          new Vector3((float)he.System.X, (float)he.System.Y, (float)he.System.Z),
                                                          $"{he.System.Name} @ {he.System.X:#.##},{he.System.Y:#.##},{he.System.Z:#.##}");
            }
            else if (gmo != null)
            {
  
                string t1 = gmo.Description;

                return new Tuple<string, Vector3, string>(gmo.Name,
                                                          new Vector3((float)gmo.Points[0].X, (float)gmo.Points[0].Y, (float)gmo.Points[0].Z),
                                                          t1);
            }
            else if ( sys != null)
            {
                return new Tuple<string, Vector3, string>(sys.Name, new Vector3((float)sys.X, (float)sys.Y, (float)sys.Z), $"EDSM Star {sys.Name}");
            }
            else
            {
                return null;
            }
        }


        #endregion

        #region UI

        private void MouseClickOnMap(GLBaseControl b, GLMouseEventArgs e)
        {
            int distmovedsq = gl3dcontroller.MouseMovedSq(e);
            if (distmovedsq >= 4)
            {
                System.Diagnostics.Debug.WriteLine($"Moved mouse {distmovedsq}, reject click");
                return;
            }
          //  System.Diagnostics.Debug.WriteLine("map click");
            Object item = FindObjectOnMap(e.ViewportLocation);

            if (item != null)
            {
                if (e.Button == GLMouseEventArgs.MouseButtons.Left)
                {
                    if (item is HistoryEntry)
                        travelpath.SetSystem(item as HistoryEntry);
                    var nl = NameLocationDescription(item);
                    System.Diagnostics.Debug.WriteLine("Click on and slew to " + nl.Item1);
                    SetEntryText(nl.Item1);
                    gl3dcontroller.SlewToPosition(nl.Item2, -1);
                }
                else if (e.Button == GLMouseEventArgs.MouseButtons.Right)
                {
                    rightclickmenu.Tag = item;
                    rightclickmenu.Show(displaycontrol, e.Location);
                }
            }
        }

        private void OtherKeys(GLOFC.Controller.KeyboardMonitor kb)
        {
            if (kb.HasBeenPressed(Keys.F4, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.ChangePerspectiveMode(!gl3dcontroller.MatrixCalc.InPerspectiveMode);
            }
            if (kb.HasBeenPressed(Keys.F5, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                GalaxyDisplay = !GalaxyDisplay;
            }
            if (kb.HasBeenPressed(Keys.F6, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                StarDotsDisplay = !StarDotsDisplay;
            }
            if (kb.HasBeenPressed(Keys.F7, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                TravelPathDisplay = !TravelPathDisplay;
            }
            if (kb.HasBeenPressed(Keys.F8, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                GalObjectDisplay = !GalObjectDisplay;
            }
            if (kb.HasBeenPressed(Keys.F9, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                if (EDSMRegionsEnable)
                    edsmgalmapregions.Toggle();
                else
                    elitemapregions.Toggle();
            }
            if (kb.HasBeenPressed(Keys.F9, GLOFC.Controller.KeyboardMonitor.ShiftState.Alt))
            {
                bool edsm = EDSMRegionsEnable;
                EDSMRegionsEnable = !edsm;
                EliteRegionsEnable = edsm;
            }

            if (kb.HasBeenPressed(Keys.F3, GLOFC.Controller.KeyboardMonitor.ShiftState.Shift))
            {
                HistoryEntry prev = travelpath.CurrentList.Last();
                travelpath.AddSystem(new HistoryEntry(DateTime.UtcNow, "new-" + newsys.ToString(), prev.System.X, prev.System.Y, prev.System.Z + 100, Color.DarkSalmon));
                newsys++;
                glwfc.Invalidate();
            }
        }

        int newsys = 1;
#endregion


    }
}
