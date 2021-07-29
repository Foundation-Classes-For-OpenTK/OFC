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
    class GalaxyStars
    {
        public GalaxyStars()
        {
        }

        public void Create(GLItemsList items, float sunsize, int bufferfindbinding)
        {
            sunvertex = new GLPLVertexShaderModelCoordWithWorldTranslationCommonModelTranslation();
            items.Add(sunvertex);
            sunshader = new GLShaderPipeline(sunvertex, new GLPLStarSurfaceFragmentShader());
            items.Add(sunshader);

            shapebuf = new GLBuffer();
            items.Add(shapebuf);
            var shape = GLSphereObjectFactory.CreateSphereFromTriangles(3, sunsize);
            shapebuf.AllocateFill(shape);

            findshader = items.NewShaderPipeline(null, sunvertex, null, null, new GLPLGeoShaderFindTriangles(bufferfindbinding, 16), null, null, null);
            items.Add(findshader);

            rifindlist = new List<GLRenderableItem>();
            starposbuflist = new List<GLBuffer>();
            renderersunlist = new List<GLRenderableItem>();
        }

        public void Fill(GLItemsList items, GLRenderProgramSortedList rObjects, Vector3 pos, int lyradius)
        {
            Vector4[] array = new Vector4[1];
            Random rnd = new Random(21);
            for (int i = 0; i < array.Length; i++)
                array[i] = new Vector4(pos.X + rnd.Next(lyradius), pos.Y + rnd.Next(lyradius), pos.Z + rnd.Next(lyradius),0);

            var deflist = array;
//            var deflist = new Vector4[] { new Vector4(-1, -1, -1, -1) };

            var starposbuf = items.NewBuffer();         // where we hold the vertexes for the suns, used by renderer and by finder
            starposbuf.AllocateFill(deflist);
            starposbuflist.Add(starposbuf);

            GLRenderControl rt = GLRenderControl.Tri();     // render is triangles, with no depth test so we always appear
            rt.DepthTest = false;
            rt.DepthClamp = true;

            var renderersun = GLRenderableItem.CreateVector4Vector4(items, rt, shapebuf, shapebuf.Length / GLLayoutStandards.Vec4size, starposbuf, null, deflist.Length,1);
            rObjects.Add(sunshader, renderersun);
            renderersunlist.Add(renderersun);

            var rifind = GLRenderableItem.CreateVector4Vector4(items, GLRenderControl.Tri(), shapebuf, shapebuf.Length / GLLayoutStandards.Vec4size, starposbuf, null, deflist.Length, 1);
            rifindlist.Add(rifind);
        }

        public void Update(ulong time, float eyedistance)
        {
            const int rotperiodms = 10000;
            time = time % rotperiodms;
            float fract = (float)time / rotperiodms;
            float angle = (float)(2 * Math.PI * fract);
            sunvertex.ModelTranslation = Matrix4.CreateRotationY(-angle);
            float scale = Math.Max(1, Math.Min(4, eyedistance / 5000));
       //     System.Diagnostics.Debug.WriteLine("Scale {0}", scale);
            sunvertex.ModelTranslation *= Matrix4.CreateScale(scale);           // scale them a little with distance to pick them out better
        }


        public bool FindSystem(Point viewportloc, GLRenderControl state, Size viewportsize)
        {
            var geo = findshader.Get<GLPLGeoShaderFindTriangles>(OpenTK.Graphics.OpenGL4.ShaderType.GeometryShader);
            geo.SetScreenCoords(viewportloc, viewportsize);

            foreach (var find in rifindlist)
            {
                find.Execute(findshader, state, discard: true); // execute, discard
                var res = geo.GetResult();
                if (res != null)
                {
                    //for (int i = 0; i < res.Length; i++) System.Diagnostics.Debug.WriteLine(i + " = " + res[i]);
                    return true; //tbd currentfilteredlist[(int)res[0].Y];
                }
            }

            return false;
        }


        private GLShaderPipeline sunshader;
        private GLPLVertexShaderModelCoordWithWorldTranslationCommonModelTranslation sunvertex;
        private GLBuffer shapebuf;
        private List<GLBuffer> starposbuflist;
        private List<GLRenderableItem> renderersunlist;

        private GLShaderPipeline findshader;        // finder
        private List<GLRenderableItem> rifindlist;

        private int MaxStars;
    }

}
