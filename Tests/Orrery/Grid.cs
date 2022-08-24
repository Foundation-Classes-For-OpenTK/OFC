using GLOFC;
using GLOFC.Controller;
using GLOFC.GL4;
using GLOFC.GL4.Bitmaps;
using GLOFC.GL4.Shaders.Basic;
using GLOFC.GL4.ShapeFactory;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestOpenTk
{
    public class Grid
    {
        private GLRenderProgramSortedList rgrid;
        private GLColorShaderWorld gridshader;
        private GLBitmaps maps;

        public void Create(GLItemsList items, float worldsize, float mscaling, long gridlines)
        {
            gridshader = new GLColorShaderWorld(worldoffset:true);
            items.Add(gridshader);
            
            rgrid = new GLRenderProgramSortedList();          // Render grid

            float gridsize = worldsize * mscaling;
            float gridoffset = gridlines * mscaling;
            int nolines = (int)(gridsize / gridoffset * 2 + 1);

            var buf1 = items.NewBuffer();
            var lines1 = GLShapeObjectFactory.CreateLines(new Vector3(-gridsize, -0, -gridsize), new Vector3(-gridsize, -0, gridsize), new Vector3(gridoffset, 0, 0), nolines);
            var lines2 = GLShapeObjectFactory.CreateLines(new Vector3(-gridsize, -0, -gridsize), new Vector3(gridsize, -0, -gridsize), new Vector3(0, 0, gridoffset), nolines);
            buf1.AllocateFill(new Vector4[][] { lines1, lines2 });      // DEMO this

            GLRenderState rslines = GLRenderState.Lines();
            rslines.DepthTest = false;

            Color gridcolour1 = Color.FromArgb(80, 80, 80, 80);
            rgrid.Add(gridshader, GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, rslines, buf1, lines1.Length, buf1.Positions[0], new Color4[] { gridcolour1 })
                                );

            Color gridcolour2 = Color.FromArgb(80, 80, 80, 80);
            rgrid.Add(gridshader, GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, rslines, buf1, lines2.Length, buf1.Positions[1], new Color4[] { gridcolour2 }));

            Size bmpsize = new Size(128, 30);
            maps = new GLBitmaps("bitmap1", rgrid, bmpsize, 3, OpenTK.Graphics.OpenGL4.SizedInternalFormat.Rgba8, false, false,worldoffset:true);

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
                        maps.Add(i, (v / BodyPhysicalConstants.oneAU_m).ToString("N1") + "AU", f, Color.White, Color.Transparent, new Vector3(i * gridoffset + hsize / 2, 0, -vsize / 2),
                                            new Vector3(hsize, 0, 0), new Vector3(0, 0, 0), fmt);
                        maps.Add(i, (p).ToString("N0"), f, Color.White, Color.Transparent, new Vector3(hsize / 2, 0, i * gridoffset + vsize / 2),
                                            new Vector3(hsize, 0, 0), new Vector3(0, 0, 0), fmt);
                        maps.Add(i, (v / BodyPhysicalConstants.oneAU_m).ToString("N1") + "AU", f, Color.White, Color.Transparent, new Vector3(hsize / 2, 0, i * gridoffset - vsize / 2),
                                            new Vector3(hsize, 0, 0), new Vector3(0, 0, 0), fmt);
                    }
                    pos += 50000000;
                }
            }
        }

        public void Render(GLRenderState rs, GLMatrixCalc cl )
        {
            rgrid.Render(rs, cl, false);
        }

        public void SetOffset(Vector3 offset)
        {
            maps.SetWorldOffset(offset);
            gridshader.SetOffset(offset);
        }
      
    }
}
