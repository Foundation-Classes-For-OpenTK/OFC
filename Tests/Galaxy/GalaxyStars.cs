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
        public int Sectors { get { return displayedsectors.Count; } }

        public GalaxyStars(GLItemsList items, GLRenderProgramSortedList rObjects, float sunsize, int bufferfindbinding)
        {
            sunvertex = new GLPLVertexShaderModelCoordWithWorldTranslationCommonModelTranslation(new Color[] { Color.FromArgb(255, 220, 220, 10), Color.FromArgb(255, 0,0,0) } );
            items.Add(sunvertex);
            sunshader = new GLShaderPipeline(sunvertex, new GLPLStarSurfaceFragmentShader());
            items.Add(sunshader);

            shapebuf = new GLBuffer();
            items.Add(shapebuf);
            var shape = GLSphereObjectFactory.CreateSphereFromTriangles(2, sunsize);
            shapebuf.AllocateFill(shape);

            this.items = items;
            this.rObjects = rObjects;
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
                Request9Box(newpos);
        }

        public void Request9Box(Vector3 pos)
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

                        lock (displayedsectors)     // can't have anyone using displayed sectors until add complete
                        {
                            displayedsectors.Add(sector);
                            displayedsectorsposhash.Add(sector.pos);
                        }

                        Thread p = new Thread(FillSectorThread);
                        p.Start(sector);
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine($"{Environment.TickCount % 100000} {sector.pos} request denied");
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
            Thread.Sleep(5000);

            Vector4[] array = new Vector4[100];
            Random rnd = new Random((int)(d.pos.X * d.pos.Y) + 1);
            for (int i = 0; i < array.Length; i++)
                array[i] = new Vector4(d.pos.X + rnd.Next(SectorSize), d.pos.Y + rnd.Next(SectorSize), d.pos.Z + rnd.Next(SectorSize), 0);

            d.stars = array;        // later more data
            //System.Diagnostics.Debug.WriteLine($"{Environment.TickCount % 100000} {d.pos} end");

            generatedsectors.Enqueue(d);       // d has been filled

            Interlocked.Add(ref subthreadsrunning, -1);
        }


        // foreground, called on each frame, allows update of shader and queuing of new objects
        public void Update(ulong time, float eyedistance)
        {
            int max = 4;        // arbitary, seems not to extend it too much
            while (max-- > 0 && generatedsectors.TryDequeue(out Sector d))      // one per frame to prevent any mighty stalls
            {
                //d.starposbuf = items.NewBuffer();         // where we hold the vertexes for the suns, used by renderer and by finder
                //d.starposbuf.AllocateFill(d.stars);

                //GLRenderControl rt = GLRenderControl.Tri();     // render is triangles, with no depth test so we always appear
                //rt.DepthTest = true;
                //rt.DepthClamp = true;

                //d.renderer = GLRenderableItem.CreateVector4Vector4(items, rt, shapebuf, shapebuf.Length / GLLayoutStandards.Vec4size, d.starposbuf, null, d.stars.Length, 1);
                //rObjects.Add(sunshader, "Sector " + d.pos.ToString(), d.renderer);
                //System.Diagnostics.Debug.WriteLine($"{Environment.TickCount % 100000} {d.pos} add items in foreground left {generatedsectors.Count}");

                //d.rifind = GLRenderableItem.CreateVector4Vector4(items, GLRenderControl.Tri(), shapebuf, shapebuf.Length / GLLayoutStandards.Vec4size, d.starposbuf, null, d.stars.Length, 1);
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


        private GLItemsList items;
        private GLRenderProgramSortedList rObjects;

        private GLShaderPipeline sunshader;     // sun drawer
        private GLPLVertexShaderModelCoordWithWorldTranslationCommonModelTranslation sunvertex;
        private GLBuffer shapebuf;

        private class Sector
        {
            public Vector3 pos;
            public Vector4[] stars;

            public Sector(Vector3 pos) { this.pos = pos; }
        }

        // requested sectors from foreground to requestor
        private BlockingCollection<Sector> requestedsectors = new BlockingCollection<Sector>();

        //owned by requestor, only it can add/remove from this list. Lock it if used in foreground
        List<Sector> displayedsectors = new List<Sector>();     // ones created..
        HashSet<Vector3> displayedsectorsposhash = new HashSet<Vector3>();  // quick lookup

        // added to by subthread when sector is ready, picked up by foreground update. ones ready for final foreground processing
        private ConcurrentQueue<Sector> generatedsectors = new ConcurrentQueue<Sector>();      

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
