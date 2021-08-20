using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OFC;
using OFC.GL4;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using System.Diagnostics;

namespace TestOpenTk
{
    class GalaxyStars
    {
        public Vector3 CurrentPos { get; set; } = new Vector3(-1000000, -1000000, -1000000);
        public int Sectors { get { return displayedsectorsposhash.Count; } }
        public Font Font { get; set; } = new Font("Ms Sans Serif", 14f);
        public Color ForeText { get; set; } = Color.White;
        public Color BackText { get; set; } = Color.Red;

        public GalaxyStars(GLItemsList items, GLRenderProgramSortedList rObjects, float sunsize, int findbufferfindbinding)
        {
            sunvertex = new GLPLVertexShaderModelCoordWithWorldTranslationCommonModelTranslation(new Color[] { Color.FromArgb(255, 220, 220, 10), Color.FromArgb(255, 0,0,0) } );
            items.Add(sunvertex);
            sunshader = new GLShaderPipeline(sunvertex, new GLPLStarSurfaceFragmentShader());
            //sunshader.StartAction += (s, w) => { Monitor.Enter(slset); System.Diagnostics.Debug.WriteLine("Begin render suns"); };
            //sunshader.FinishAction += (s, w) => { System.Diagnostics.Debug.WriteLine("End render suns"); Monitor.Exit(slset); };
            items.Add(sunshader);
            shapebuf = new GLBuffer();
            items.Add(shapebuf);
            var shape = GLSphereObjectFactory.CreateSphereFromTriangles(2, sunsize);
            shapebuf.AllocateFill(shape);

            GLRenderControl starrc = GLRenderControl.Tri();     // render is triangles, with no depth test so we always appear
            starrc.DepthTest = true;
            starrc.DepthClamp = true;

            var textrc = GLRenderControl.Quads();
            textrc.DepthTest = true;
            textrc.ClipDistanceEnable = 1;  // we are going to cull primitives which are deleted

            int texunitspergroup = 16;
            var textshader = new GLShaderPipeline(new GLPLVertexShaderQuadTextureWithMatrixTranslation(), new GLPLFragmentShaderTexture2DIndexedMulti(0, 0, true, texunitspergroup));
            //textshader.StartAction += (s, w) => { Monitor.Enter(slset); System.Diagnostics.Debug.WriteLine("Begin render text"); };
            //textshader.FinishAction += (s, w) => { System.Diagnostics.Debug.WriteLine("End render text"); Monitor.Exit(slset); };
            items.Add(textshader);

            slset = new GLSetOfObjectsWithLabels("SLSet", rObjects, texunitspergroup, 100, 10,
                                                            sunshader, shapebuf, shape.Length, starrc,
                                                            textshader, new Size(128, 32), textrc);

            items.Add(slset);
        }

        public void Start()
        {
            requestorthread = new Thread(Requestor);
            requestorthread.Start();
        }

        public void Stop()
        {
            //System.Diagnostics.Debug.WriteLine("Request stop on gal stars");
            stop.Cancel();
            requestorthread.Join();
            while(subthreadsrunning > 0)
            {
                System.Diagnostics.Debug.WriteLine("Sub thread running");
                Thread.Sleep(100);
            }
            System.Diagnostics.Debug.WriteLine("Stopped on gal stars");
        }

        public void Request9BoxConditional(Vector3 newpos)
        {
            if ((CurrentPos - newpos).Length >= SectorSize)
                Request9x3Box(newpos);
        }

        public void Request9x3Box(Vector3 pos)
        {
            CurrentPos = pos;
            //System.Diagnostics.Debug.WriteLine($"Request 9 box ${pos}");

          //  Stopwatch sw = new Stopwatch(); sw.Start();

            for (int i = 0; i <= 3; i++)
            {
                int y = i == 0 ? 0 : i == 1 ? SectorSize : -SectorSize;
                Request(new Vector3(pos.X , pos.Y + y, pos.Z));
                Request(new Vector3(pos.X + SectorSize, pos.Y + y, pos.Z));
                Request(new Vector3(pos.X - SectorSize, pos.Y + y, pos.Z));
                Request(new Vector3(pos.X, pos.Y+y, pos.Z + SectorSize));
                Request(new Vector3(pos.X, pos.Y + y, pos.Z - SectorSize));
                Request(new Vector3(pos.X + SectorSize, pos.Y + y, pos.Z + SectorSize));
                Request(new Vector3(pos.X + SectorSize, pos.Y + y, pos.Z - SectorSize));
                Request(new Vector3(pos.X - SectorSize, pos.Y + y, pos.Z + SectorSize));
                Request(new Vector3(pos.X - SectorSize, pos.Y + y, pos.Z - SectorSize));
            }
         //   System.Diagnostics.Debug.WriteLine($"Time search for sectors {sw.ElapsedMilliseconds}");
        }

        // send the request to the requestor using a blocking queue
        private void Request(Vector3 pos)
        {
            int mm = 100000;
            pos.X = (int)(pos.X + mm) / SectorSize * SectorSize - mm;
            pos.Y = (int)(pos.Y + mm) / SectorSize * SectorSize - mm;
            pos.Z = (int)(pos.Z + mm) / SectorSize * SectorSize - mm;
            // System.Diagnostics.Debug.WriteLine($"{Environment.TickCount % 100000} {pos} request");
            requestedsectors.Add(new Sector(pos));
        }

        // do this in a thread, as adding threads is computationally expensive so we don't want to do it in the foreground
        private void Requestor()
        {
            while (true)
            {
                try
                {
                    //  System.Diagnostics.Debug.WriteLine($"{Environment.TickCount % 100000} Requestor take");
                    var sector = requestedsectors.Take(stop.Token);       // blocks until take or told to stop

                    if (!displayedsectorsposhash.Contains(sector.pos))      // don't repeat blocks
                    {
                          //      System.Diagnostics.Debug.WriteLine($"{Environment.TickCount % 100000} {sector.pos} requestor accepts, start sub thread");

                        lock (displayedsectorsposhash)     // can't have anyone using displayed sectors until add complete
                        {
                            displayedsectorsposhash.Add(sector.pos);
                        }

                        Thread p = new Thread(FillSectorThread);
                        p.Start(sector);
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine($"{Environment.TickCount % 100000} {sector.pos} request denied");
                    }

                    while( cleanbitmaps.TryDequeue(out sector))         // bitmap cleaning is not high priority, so just do it when we get another request. No need to unblock on it
                    {
                        System.Diagnostics.Debug.WriteLine($"Clean bitmap for {sector.pos}");
                        BitMapHelpers.Dispose(sector.bitmaps);
                        sector.bitmaps = null;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            //System.Diagnostics.Debug.WriteLine("Exit requestor");
        }

        // in a thread, look up the sector 
        private void FillSectorThread(Object seco)
        {
            Interlocked.Add(ref subthreadsrunning, 1);      // count subthreads, on shutdown, we need to wait until they all complete
            Sector d = (Sector)seco;

            //System.Diagnostics.Debug.WriteLine($"{Environment.TickCount % 100000} {d.pos} start");
            Thread.Sleep(10);

            Vector4[] array = new Vector4[100];
            string[] text = new string[array.Length];
            Random rnd = new Random((int)(d.pos.X * d.pos.Y) + 1);
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = new Vector4(d.pos.X + rnd.Next(SectorSize), d.pos.Y + rnd.Next(SectorSize), d.pos.Z + rnd.Next(SectorSize), 0);
                text[i] = $"({d.pos.X},{d.pos.Y},{d.pos.Z})-{i}";
            }

            d.stars = array;       
            d.text = text;
            d.bitmaps = BitMapHelpers.DrawTextIntoFixedSizeBitmaps(slset.LabelSize, text, Font, System.Drawing.Text.TextRenderingHint.ClearTypeGridFit, ForeText, BackText, 0.5f);
            d.textpos = GLPLVertexShaderQuadTextureWithMatrixTranslation.CreateMatrices(array, new Vector3(0, -2f, 0), new Vector3(2f, 0, 0.4f), new Vector3(-90F.Radians(), 0, 0), true, false);

            generatedsectors.Enqueue(d);       // d has been filled
            // System.Diagnostics.Debug.WriteLine($"{Environment.TickCount % 100000} {d.pos} end");

            Interlocked.Add(ref subthreadsrunning, -1);
        }

        ulong timelastadded = 0;

        // foreground, called on each frame, allows update of shader and queuing of new objects
        public void Update(ulong time, float eyedistance)
        {
            if ( time-timelastadded > 50 && generatedsectors.TryDequeue(out Sector d))      // limit fill rate..
            { 
                slset.Add(new Tuple<Vector3, string[]>(d.pos, d.text), d.stars, d.textpos, d.bitmaps);
                cleanbitmaps.Enqueue(d);        // ask for cleaning of bitmaps
                timelastadded = time;
            }

            const int rotperiodms = 10000;
            time = time % rotperiodms;
            float fract = (float)time / rotperiodms;
            float angle = (float)(2 * Math.PI * fract);
            sunvertex.ModelTranslation = Matrix4.CreateRotationY(-angle);
            float scale = Math.Max(1, Math.Min(4, eyedistance / 5000));
            //     System.Diagnostics.Debug.WriteLine("Scale {0}", scale);
            sunvertex.ModelTranslation *= Matrix4.CreateScale(scale);           // scale them a little with distance to pick them out better
        }


        private GLSetOfObjectsWithLabels slset; // main class holding drawing

        private GLShaderPipeline sunshader;     // sun drawer
        private GLPLVertexShaderModelCoordWithWorldTranslationCommonModelTranslation sunvertex;
        private GLBuffer shapebuf;

        private class Sector
        {
            public Vector3 pos;
            public Sector(Vector3 pos) { this.pos = pos; }

            // generated by thread, passed to update, bitmaps pushed to cleanbitmaps and deleted by requestor
            public Vector4[] stars;
            public string[] text;
            public Matrix4[] textpos;
            public Bitmap[] bitmaps;
        }

        // requested sectors from foreground to requestor
        private BlockingCollection<Sector> requestedsectors = new BlockingCollection<Sector>();

        // owned by requestor, only it can add/remove from this list. 
        HashSet<Vector3> displayedsectorsposhash = new HashSet<Vector3>();  // quick lookup

        // added to by subthread when sector is ready, picked up by foreground update. ones ready for final foreground processing
        private ConcurrentQueue<Sector> generatedsectors = new ConcurrentQueue<Sector>();

        // added to by update when cleaned up bitmaps, requestor will clear these for it
        private ConcurrentQueue<Sector> cleanbitmaps = new ConcurrentQueue<Sector>();

        private const int SectorSize = 100;

        private Thread requestorthread;
        private CancellationTokenSource stop =  new CancellationTokenSource();
        private int subthreadsrunning = 0;
    }

}

//findshader = items.NewShaderPipeline(null, sunvertex, null, null, new GLPLGeoShaderFindTriangles(bufferfindbinding, 16), null, null, null);
//            items.Add(findshader);

//        public bool FindSystem(Point viewportloc, GLRenderControl state, Size viewportsize)
//{
//    lock (displayedsectors)     // can't have anyone altering displayed sectors
//    {
//        var geo = findshader.Get<GLPLGeoShaderFindTriangles>(OpenTK.Graphics.OpenGL4.ShaderType.GeometryShader);
//        geo.SetScreenCoords(viewportloc, viewportsize);

//        foreach (var sec in displayedsectors)
//        {
//            sec.rifind.Execute(findshader, state, discard: true); // execute, discard
//            var res = geo.GetResult();
//            if (res != null)
//            {
//                //for (int i = 0; i < res.Length; i++) System.Diagnostics.Debug.WriteLine(i + " = " + res[i]);
//                return true; //tbd currentfilteredlist[(int)res[0].Y];
//            }
//        }
//    }

//    return false;
//}
