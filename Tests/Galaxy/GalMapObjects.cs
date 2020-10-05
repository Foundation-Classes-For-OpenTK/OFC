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

        public bool Enable { get { return objectshader.Enable; } set { objectshader.Enable = value; } }

        public void CreateObjects(GLItemsList items, GLRenderProgramSortedList rObjects, GalacticMapping galmap, int bufferfindbinding)
        {
            Bitmap[] images = galmap.RenderableMapTypes.Select(x => x.Image as Bitmap).ToArray();
            IGLTexture texarray = items.Add(new GLTexture2DArray(images, mipmaplevel:1, genmipmaplevel:3), "GalObjTex");

            var vert = new GLPLVertexScaleLookat(rotate:true, rotateelevation:false, commontransform:false, autoscale:1000, autoscalemin:0.1f, autoscalemax:2f);
            var tcs = new GLPLTesselationControl(10f);
            tes = new GLPLTesselationEvaluateSinewave(0.2f,1f);
            var frag = new GLPLFragmentShaderTexture2DDiscard(1);

            objectshader = new GLShaderPipeline(vert, tcs, tes, null, frag);
            items.Add( objectshader, "ShaderGalObj");

            objectshader.StartAction += (s,m) =>
            {
                texarray.Bind(1);
            };

            GLRenderControl rt = GLRenderControl.Patches(4);
            rt.DepthTest = false;

            // create a quad and all entries of the renderable map objects, zero at this point, with a zero instance count. UpdateEnables will fill it in later
            // but we need to give it the maximum buffer length at this point

            ridisplay = GLRenderableItem.CreateVector4Vector4(items, rt,
                                GLShapeObjectFactory.CreateQuad2(50.0f, 50.0f), new Vector4[galmap.RenderableMapObjects.Length],
                                ic: 0, seconddivisor: 1);

            modelworldbuffer = items.LastBuffer();
            int modelpos = modelworldbuffer.Positions[0];
            worldpos = modelworldbuffer.Positions[1];

            rObjects.Add(objectshader, "GalObj", ridisplay);

            // find

            var geofind = new GLPLGeoShaderFindTriangles(bufferfindbinding, 16);        // pass thru normal vert/tcs/tes then to geoshader for results
            findshader = items.NewShaderPipeline("GEOMAP_FIND", vert, tcs, tes, new GLPLGeoShaderFindTriangles(bufferfindbinding, 16), null, null, null);

            // hook to modelworldbuffer, at modelpos and worldpos.  UpdateEnables will fill in instance count
            rifind = GLRenderableItem.CreateVector4Vector4(items, GLRenderControl.Patches(4), modelworldbuffer, modelpos, ridisplay.DrawCount, 
                                                                            modelworldbuffer, worldpos, null, ic: 0, seconddivisor: 1);

            GLStatics.Check();

            UpdateEnables(galmap);      // fill in worldpos's and update instance count, taking into 
        }

        public void UpdateEnables(GalacticMapping galmap)           // rewrite the modelworld buffer with the ones actually enabled
        {
            modelworldbuffer.StartWrite(worldpos);

            var renderablegalmapobjects = galmap.RenderableMapObjectsEnabled; // list of enabled entries

            foreach (var o in renderablegalmapobjects)
                modelworldbuffer.Write(new Vector4(o.points[0].X, o.points[0].Y, o.points[0].Z, o.galMapType.Index + (!o.galMapType.Animate ? 65536:0)));

            modelworldbuffer.StopReadWrite();

            //var f = modelworldbuffer.ReadVector4(worldpos, renderablegalmapobjects.Count());  foreach (var v in f) System.Diagnostics.Debug.WriteLine("Vector " + v);

            ridisplay.InstanceCount = rifind.InstanceCount = renderablegalmapobjects.Length;
        }


        public GalacticMapObject FindPOI(Point l, GLRenderControl state, Size screensize, GalacticMapping galmap)
        {
            if (!objectshader.Enable)
                return null;

            var geo = findshader.Get<GLPLGeoShaderFindTriangles>(OpenTK.Graphics.OpenGL4.ShaderType.GeometryShader);
            geo.SetScreenCoords(l, screensize);

            GLStatics.Check();
            rifind.Execute(findshader, state, discard:true); // execute, discard

            var res = geo.GetResult();
            if (res != null)
            {
                for (int i = 0; i < res.Length; i++)
                {
                    System.Diagnostics.Debug.WriteLine(i + " = " + res[i]);
                }

                int instance = (int)res[0].Y;
                return galmap.RenderableMapObjectsEnabled[instance];
            }

            return null;
        }

        public void Update(long time, float eyedistance)
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

        private GLShaderPipeline findshader;
        private GLRenderableItem rifind;


    }

}
