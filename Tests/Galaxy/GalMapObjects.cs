using EliteDangerousCore.EDSM;
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

namespace TestOpenTk
{
    public class GalMapObjects
    {
        public GalMapObjects()
        {
        }

        public bool Enable { get { return objectshader.Enable; } set { textrenderer.Enable = objectshader.Enable = value; } }

        public void SetGalObjectTypeEnable(string id, bool state) { State[id] = state; UpdateEnables(); }
        public bool GetGalObjectTypeEnable(string id) { return !State.ContainsKey(id) || State[id] == true; }
        public void SetAllEnables(string settings)
        {
            string[] ss = settings.Split(',');
            int i = 0;
            foreach (var o in galmap.RenderableMapTypes)
            {
                State[o.Typeid] = i >= ss.Length || !ss[i].Equals("-");              // on if we don't have enough, or on if its not -
                i++;
            }
            UpdateEnables();
        }
        public string GetAllEnables()
        {
            string s = "";
            foreach (var o in galmap.RenderableMapTypes)
            {
                s += GetGalObjectTypeEnable(o.Typeid) ? "+," : "-,";
            }
            return s;
        }

        public void CreateObjects(GLItemsList items, GLRenderProgramSortedList rObjects, GalacticMapping galmap, int bufferfindbinding)
        {
            this.galmap = galmap;

            Bitmap[] images = galmap.RenderableMapTypes.Select(x => x.Image as Bitmap).ToArray();
            IGLTexture texarray = items.Add(new GLTexture2DArray(images, mipmaplevel:1, genmipmaplevel:3), "GalObjTex");

            // a look at vertex shader
            var vert = new GLPLVertexScaleLookat(rotate:true, rotateelevation:false, commontransform:false, autoscale:1000, autoscalemin:0.1f, autoscalemax:2f);
            var tcs = new GLPLTesselationControl(10f);
            tes = new GLPLTesselationEvaluateSinewave(0.2f,1f);         // this uses the world position from the vertex scaler to position the image, w controls image + animation (b16)
            var frag = new GLPLFragmentShaderTexture2DDiscard(1);       // binding 1 - takes image pos from tes. imagepos < 0 means discard

            objectshader = new GLShaderPipeline(vert, tcs, tes, null, frag);
            items.Add( objectshader);

            objectshader.StartAction += (s,m) =>
            {
                texarray.Bind(1);   // bind tex array to 1, matching above
            };

            GLRenderControl rt = GLRenderControl.Patches(4);
            rt.DepthTest = false;

            // create a quad and all entries of the renderable map objects, zero at this point, with a zero instance count. UpdateEnables will fill it in later
            // but we need to give it the maximum buffer length at this point

            ridisplay = GLRenderableItem.CreateVector4Vector4(items, rt,
                                GLShapeObjectFactory.CreateQuad2(50.0f, 50.0f),         // quad2 4 vertexts
                                new Vector4[galmap.RenderableMapObjects.Length],        // world positions
                                ic: 0, seconddivisor: 1);

            modelworldbuffer = items.LastBuffer();
            int modelpos = modelworldbuffer.Positions[0];
            worldpos = modelworldbuffer.Positions[1];

            rObjects.Add(objectshader,  ridisplay);

            // find

            var geofind = new GLPLGeoShaderFindTriangles(bufferfindbinding, 16);        // pass thru normal vert/tcs/tes then to geoshader for results
            items.Add(geofind);

            findshader = items.NewShaderPipeline(null, vert, tcs, tes, new GLPLGeoShaderFindTriangles(bufferfindbinding, 16), null, null, null);

            // hook to modelworldbuffer, at modelpos and worldpos.  UpdateEnables will fill in instance count
            rifind = GLRenderableItem.CreateVector4Vector4(items, GLRenderControl.Patches(4), modelworldbuffer, modelpos, ridisplay.DrawCount, 
                                                                            modelworldbuffer, worldpos, null, ic: 0, seconddivisor: 1);

            GLStatics.Check();

            // Text renderer
            textrenderer = new GLBitmaps(rObjects, new Size(128, 40), depthtest: false, cullface: false);
            items.Add(textrenderer);

            var renderablegalmapobjects = galmap.RenderableMapObjects; // list of enabled entries

            Font fnt = new Font("Arial", 8.5F);
            using (StringFormat fmt = new StringFormat())
            {
                fmt.Alignment = StringAlignment.Center;
                for(int i = 0 ; i < renderablegalmapobjects.Length; i++)
                {
                    var o = renderablegalmapobjects[i];
                    float offset = -12;
                    for( int j = 0; j < i; j++)
                    {
                        var diff = new Vector3(o.points[0].X, o.points[0].Y, o.points[0].Z) - new Vector3(renderablegalmapobjects[j].points[0].X, renderablegalmapobjects[j].points[0].Y, renderablegalmapobjects[j].points[0].Z);

                        if (diff.Length < 50)
                            offset -= 12;
                    }

                    Vector3 pos = new Vector3(o.points[0].X, o.points[0].Y + offset, o.points[0].Z);
                    textrenderer.Add(o.id, o.name, fnt, 
                        Color.White,Color.Transparent, 
                        pos,
                        new Vector3(30, 0, 0), new Vector3(0, 0, 0), fmt: fmt, rotatetoviewer: false, rotateelevation: false, 
                        alphafadedistance: -1000, alphaenddistance: 2000); //at 2000ly ed, 2000-2000/1000 = 0, at 1000ly, 2000-1000/1000 = 1. 0-1000 is at full alpha
                }
            }

            UpdateEnables();      // fill in worldpos's and update instance count, taking into 
        }

        private void UpdateEnables()           // rewrite the modelworld buffer with the ones actually enabled
        {
            modelworldbuffer.StartWrite(worldpos);                  // overwrite world positions

            var renderablegalmapobjects = galmap.RenderableMapObjects; // list of displayable entries
            indextoentry = new int[renderablegalmapobjects.Length];
            int mwpos = 0,entry=0;

            foreach (var o in renderablegalmapobjects)
            {
                bool en = GetGalObjectTypeEnable(o.galMapType.Typeid);
                if (en)
                {
                    modelworldbuffer.Write(new Vector4(o.points[0].X, o.points[0].Y, o.points[0].Z, o.galMapType.Index + (!o.galMapType.Animate ? 65536 : 0)));
                    indextoentry[mwpos++] = entry;
                }

                textrenderer.SetVisiblityRotation(o.id, en, true, false);
                entry++;
            }

            modelworldbuffer.StopReadWrite();
            //var f = modelworldbuffer.ReadVector4(worldpos, renderablegalmapobjects.Count());  foreach (var v in f) System.Diagnostics.Debug.WriteLine("Vector " + v);
            ridisplay.InstanceCount = rifind.InstanceCount = mwpos;
        }


        public GalacticMapObject FindPOI(Point viewportloc, GLRenderControl state, Size viewportsize)
        {
            if (!objectshader.Enable)
                return null;

            var geo = findshader.Get<GLPLGeoShaderFindTriangles>(OpenTK.Graphics.OpenGL4.ShaderType.GeometryShader);
            geo.SetScreenCoords(viewportloc, viewportsize);

            GLStatics.Check();
            rifind.Execute(findshader, state, discard:true); // execute, discard

            var res = geo.GetResult();
            if (res != null)
            {
//                for (int i = 0; i < res.Length; i++) System.Diagnostics.Debug.WriteLine(i + " = " + res[i]);

                int instance = (int)res[0].Y;
                // tbd wrong! not a one to one mapping
                return galmap.RenderableMapObjects[indextoentry[instance]];       //TBD
            }

            return null;
        }

        public void Update(ulong time, float eyedistance)
        {
            const int rotperiodms = 5000;
            time = time % rotperiodms;
            float fract = (float)time / rotperiodms;
     //       System.Diagnostics.Debug.WriteLine("Time " + time + "Phase " + fract);
            tes.Phase = fract;
        }

        private GLPLTesselationEvaluateSinewave tes;
        private GLShaderPipeline objectshader;
        private GLBuffer modelworldbuffer;
        private int worldpos;
        private GLRenderableItem ridisplay;

        private GLBitmaps textrenderer;     // gmo names

        private GLShaderPipeline findshader;
        private GLRenderableItem rifind;
        private int[] indextoentry;
        private Dictionary<string, bool> State { get; set; } = new Dictionary<string, bool>();       // if not present, its on, else state 
        private GalacticMapping galmap;
    }

}
