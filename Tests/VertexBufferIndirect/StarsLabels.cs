using OFC.GL4;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestOpenTk
{
    class StarsLabels
    {
        private GLVertexBufferIndirect dataindirectbuffer;
        private GLTexture2DArray[] textures;
        private int sunshapelength;
        private GLRenderableItem starrenderer;
        private GLRenderableItem textrenderer;
        private int textmapinuse = 0;

        // starsortextures, >0 stars, else -N = textures to use (therefore stars set by max texture depth)

        public StarsLabels(string name, GLItemsList items, GLRenderProgramSortedList robjects,
                                int starsortextures, int maxgroups,
                                IGLProgramShader sunshader, GLBuffer sunshapebuf, int sunshapelength ,
                                IGLProgramShader textshader, Size texturesize )
        {
            this.sunshapelength = sunshapelength;

            // find gl parameters
            int maxtexturesbound = GL4Statics.GetMaxFragmentTextures();
            int maxtextper2darray = GL4Statics.GetMaxTextureDepth();
            maxtextper2darray = 2;

            // set up number of textmaps
            int textmaps = starsortextures < 0 ? -starsortextures : starsortextures / maxtextper2darray + 1;
            textmaps = Math.Min(textmaps, maxtexturesbound);

            // which then give us the number of stars we can do
            int stars = textmaps * maxtextper2darray;

            // estimate maximum vert buffer needed, allowing for extra due to the need to align the mat4
            int vertbufsize = stars * (GLBuffer.Vec4size + GLBuffer.Mat4size) + maxgroups * GLBuffer.Mat4size;      

            // create the vertex indirect buffer
            dataindirectbuffer = new GLVertexBufferIndirect(items,vertbufsize, GLBuffer.WriteIndirectArrayStride * maxgroups, true);

            // stars
            GLRenderControl starrc = GLRenderControl.Tri();     // render is triangles, with no depth test so we always appear
            starrc.DepthTest = true;
            starrc.DepthClamp = true;

            starrenderer = GLRenderableItem.CreateVector4Vector4(items, starrc,
                                                                        sunshapebuf, 0, 0,     // binding 0 is shapebuf, offset 0, no draw count yet
                                                                        dataindirectbuffer.Vertex, 0, // binding 1 is vertex's world positions, offset 0
                                                                        null, 0, 1);        // no ic, second divisor 1
            starrenderer.BaseIndexOffset = 0;     // offset in bytes where commands are stored
            starrenderer.MultiDrawCountStride = GLBuffer.WriteIndirectArrayStride;

            robjects.Add(sunshader, name + "Stars", starrenderer);

            // text

            textures = new GLTexture2DArray[textmaps];

            for (int i = 0; i < textures.Length; i++)
            {
                int n = Math.Min(stars, maxtextper2darray);
                textures[i] = new GLTexture2DArray(texturesize.Width,texturesize.Height, n);
                items.Add(textures[i]);
                stars -= maxtextper2darray;
            }

            var textrc = GLRenderControl.Quads();
            textrc.DepthTest = true;
            textrc.ClipDistanceEnable = 1;  // we are going to cull primitives which are deleted

            textrenderer = GLRenderableItem.CreateMatrix4(items, textrc,
                                                                dataindirectbuffer.Vertex, 0, 0, //attach buffer with matrices, no draw count
                                                                new GLRenderDataTexture(textures,0),        // binding 0..N for textures
                                                                0, 1);     //no ic, and matrix divide so 1 matrix per vertex set
            textrenderer.BaseIndexOffset = 0;     // offset in bytes where commands are stored
            textrenderer.MultiDrawCountStride = GLBuffer.WriteIndirectArrayStride;

            robjects.Add(textshader, name + "text", textrenderer);
        }

        // returns position where it stopped, or -1 if all added
        public int DrawStars(Object tag, Vector4[] array, string[] text, 
                                Font fnt, Color fore, Color back, 
                                Vector3 size, Vector3 rot, bool rotatetoviewer, bool rotateelevation,   // see GLPLVertexShaderQuadTextureWithMatrixTranslation.CreateMatrix
                                StringFormat fmt, float backscale, float yoffset)
        {
            int pos = 0;

            do
            {
                if (textmapinuse >= textures.Length)       // out of textures
                    return pos;

                // how many can we take..
                int touse = Math.Min(array.Length - pos, textures[textmapinuse].DepthLeftIndex);

                if ( pos == 0 )     // at pos 0, we can just directly fill
                {
                    if (!dataindirectbuffer.Fill(array, 0, sunshapelength, 0, touse, -1))
                        return pos;
                }
                else
                {                   // otherwise, horrible array copy because of lack of opentk interfaces
                    Vector4[] subset = new Vector4[touse];
                    Array.Copy(array, pos, subset,0,touse);
                    if (!dataindirectbuffer.Fill(subset, 0, sunshapelength, 0, touse, -1))
                        return pos;
                }

                Matrix4[] matrix = new Matrix4[touse];      // create the text bitmaps and the matrices
                for (int i = 0; i < touse; i++)
                {
                    int imgpos = textures[textmapinuse].DepthIndex + textmapinuse * 65536;      // bits 16+ has textmap
                    System.Diagnostics.Debug.WriteLine($"Write Mat {pos} {pos + i}");
                    textures[textmapinuse].DrawText(text[pos+i] + ":" + textmapinuse, fnt, fore,back, -1, fmt, backscale);

                    var mat = GLPLVertexShaderQuadTextureWithMatrixTranslation.CreateMatrix(new Vector3(array[pos + i].X, array[pos + i].Y + yoffset, array[pos + i].Z),
                                    size,
                                    rot,
                                    rotatetoviewer, rotateelevation,
                                    imagepos: imgpos);
                    matrix[i] = mat;
                }

                dataindirectbuffer.Vertex.AlignMat4();          // instancing counts in mat4 sizes (mat4 0 @0, mat4 1 @ 64 etc) so align to it
                if ( !dataindirectbuffer.Fill(matrix, 1, 4, 0, touse, -1) )
                    return pos;

                starrenderer.DrawCount = dataindirectbuffer.Indirects[0].Positions.Count;       // update draw count
                starrenderer.IndirectBuffer = dataindirectbuffer.Indirects[0];                  // and buffer

                textrenderer.DrawCount = dataindirectbuffer.Indirects[1].Positions.Count;
                textrenderer.IndirectBuffer = dataindirectbuffer.Indirects[1];

                if (textures[textmapinuse].DepthLeftIndex == 0)                                 // out of bitmap space, next please!
                    textmapinuse++;

                pos += touse;

            } while (pos < array.Length);

            return -1;
        }

    }
}
