using GLOFC;
using GLOFC.GL4;
using OpenTK.Graphics.OpenGL4;
using OpenTK;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using GLOFC.Utils;
using GLOFC.GL4.Shaders;
using GLOFC.GL4.Shaders.Vertex;
using GLOFC.GL4.Shaders.Geo;
using GLOFC.GL4.Shaders.Fragment;
using GLOFC.GL4.Shaders.Stars;
using GLOFC.GL4.Buffers;
using GLOFC.GL4.ShapeFactory;
using GLOFC.GL4.Textures;
using System.Collections.ObjectModel;

namespace TestOpenTk
{
    public class Images
    {
        public class ImageEntry
        {
            public string ImagePathOrURL { get; set; }      // http:... or c:\
            public Vector3 TopLeft { get; set; }      
            public Vector3 TopRight { get; set; }
            public Vector3 BottomLeft { get; set; }      
            public Vector3 BottomRight { get; set; }      
            public bool Enabled { get; set; }

            public ImageEntry(string path, bool enabled, Vector3 lefttop, Vector3 righttop, Vector3 leftbot, Vector3 rightbot)
            {
                ImagePathOrURL = path; Enabled = enabled; TopLeft = lefttop; TopRight = righttop; BottomLeft = leftbot; BottomRight = rightbot;
            }
        }

        public bool Enable { get { return enable; } set { enable = value; } }

        public List<ImageEntry> GetImageList() { return new List<ImageEntry>(images); }

        public void SetImageList(List<ImageEntry> newlist) { images = newlist; }

 
        public Images(GLItemsList items, GLRenderProgramSortedList rObjects)
        {
        }

        public void LoadFromString(string res)
        {
            images.Clear();
            var split = res.Split('\u2345');
            foreach( var s in split)
            {
                var entries = s.Split('\u2346');
                if ( entries.Length == 6)
                {
                    Vector3? lefttop = entries[2].InvariantParseVector3();
                    Vector3? righttop = entries[3].InvariantParseVector3();
                    Vector3? leftbottom = entries[4].InvariantParseVector3();
                    Vector3? rightbottom = entries[5].InvariantParseVector3();

                    if ( lefttop != null && righttop != null && leftbottom != null && rightbottom != null)
                    {
                        images.Add(new ImageEntry(entries[0], entries[1].InvariantParseBool(false), lefttop.Value, righttop.Value, leftbottom.Value, rightbottom.Value));
                    }

                }
            }
        }

        public string ImageStringList()
        {
            string res = "";
            for (int i = 0; i < images.Count; i++)
            {
                res = res.AppendPrePad(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}\u2346{1}\u2346{2}\u2346{3}\u2346{4}\u2346{5}",
                                images[i].ImagePathOrURL, images[i].Enabled, 
                                images[i].TopLeft.ToStringInvariant(), images[i].TopRight.ToStringInvariant(),
                                images[i].BottomLeft.ToStringInvariant(), images[i].BottomRight.ToStringInvariant()), "\u2345");
            }

            return res;
        }
        public void Add(ImageEntry img)
        {
            images.Add(img);
        }
        public void Remove(ImageEntry img)
        {
            images.Remove(img);
        }

        private bool enable = true;
        private List<ImageEntry> images = new List<ImageEntry>();
    }

}
//}
