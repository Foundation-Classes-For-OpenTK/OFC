using GLOFC.GL4;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
using System.IO;
using QuickJSON;
using GLOFC.Utils;

namespace TestOpenTk
{
    public partial class Orrery
    {
        StarScan.ScanNode starsystemnodes;
        private int displaysubnode = 0;        // subnode, 0 all, 1 first, etc
        private List<BodyInfo> bodyinfo = new List<BodyInfo>();       // linear list pointing to nodes with kepler info etc. Empty on power on in case create failed
        private int ringcount;

        // move to barycentre node displaysubnode
        public void DisplayNode(int dir)        
        {
            if (starsystemnodes != null && starsystemnodes.NodeType == StarScan.ScanNodeType.barycentre && starsystemnodes.Children != null)
            {
                int number = starsystemnodes.Children.Count + 1;
                displaysubnode = (displaysubnode + dir + number) % number; // rotate between 0 and N
                CreateBodies(starsystemnodes, displaysubnode);
                mastersystem.Text = displaysubnode == 0 ? "All" : starsystemnodes.Children.Values[displaysubnode - 1].OwnName;
                SetBodyTrack(-1);
            }
        }

        // read a JSON body file and create nodes from it. Can select barycentre subnode.
        public bool CreateBodiesFile(string file, int subnode = 0)
        {
            if (File.Exists(file))
            {
                string para = GLOFC.Utils.FileHelpers.TryReadAllTextFromFile(file);
                if (para != null)
                {
                    return CreateBodiesJSON(para, subnode);
                }
            }
            return false; 
        }

        public bool CreateBodiesJSON(string json, int subnode = 0)
        {
            JObject jo = JObject.Parse(json);
            if (jo != null)
                return CreateBodies(jo, subnode);
            else
                return false;
        }

        public bool CreateBodies(JObject jo, int subnode = 0)
        {
            starsystemnodes = StarScan.ReadJSON(jo);

            if (starsystemnodes != null)
            {
                displaysubnode = subnode;
                CreateBodies(starsystemnodes, displaysubnode);
                return true;
            }
            else
                return false;
        }

        // create tree. If subnode > 0, and we have a barycentre at top, then it picks a node of the top node to display, instead of the whole tree

        public void CreateBodies(StarScan.ScanNode node, int subnode = 0)
        {
            rbodyobjects.Clear();       // clear the render list

            // select display subnode and ignore rest of tree if barycentre and subnode set

            bool sysenabled = false;

            if (node.NodeType == StarScan.ScanNodeType.barycentre && node.Children != null)      
            {
                if ( subnode > 0)
                    node = node.Children.Values[subnode-1];

                sysenabled = true;
            }

            displaycontrol.ApplyToControlOfName("sys*", (c) => { c.Visible = sysenabled; });        // set up if system selector is enabled
            // From the node tree, create the body list

            bodyinfo = new List<BodyInfo>();        // new bodyinfo tree
            BodyInfo.CreateInfoList(bodyinfo, node, null, -1, 0, 0);       // create the info tree, reading the nodes in and filling in our kepler data etc

            // now process the bodies found to create the opengl artifacts

            ringcount = 0;

            foreach (var bi in bodyinfo) 
            {
                if (bi.KeplerParameters != null)     // in orbit
                {
                    Vector4[] orbit = bi.KeplerParameters.Orbit(currentjd, 0.1, mscaling);  // make an orbit vector

                    GLRenderState lines = GLRenderState.Lines();
                    lines.DepthTest = false;
                    bi.OrbitCentre.ColorIndex = bi.ScanNode?.scandata?.nRadius != null ? 1 : 0;        // pick a colour based on if it has radius

                    var riol = GLRenderableItem.CreateVector4(items, PrimitiveType.LineStrip, lines, orbit, bi.OrbitCentre);
                    rbodyobjects.Add(orbitlineshader, riol);

                    if ( bi.ScanNode?.scandata?.Rings != null )
                    {
                        ringcount++;
                    }

                   // bi.KeplerParameters.ResMat = Matrix4d.CreateRotationZ(-20.0.Radians());
                }
            }

            int bodies = bodyinfo.Count;

            // hold planet and barycentre positions/sizes/imageno for each body
            bodymatrixbuffer.AllocateBytes(GLBuffer.Mat4size * bodies);

            // now create the body objects render - this renders all bodies. Matrix controls the position and image of them

            GLRenderState rtbody = GLRenderState.Tri();

            var ribody = GLRenderableItem.CreateVector4Vector2Matrix4(items, PrimitiveType.Triangles, rtbody, sphereshapebuffer, spheretexcobuffer, bodymatrixbuffer,
                                            sphereshapebuffer.Length / sizeof(float) / 4,
                                            ic: bodies, matrixdivisor: 1);
            rbodyobjects.Add(bodyshader, ribody);


            ringsmatrixbuffer.AllocateBytes(GLBuffer.Mat4size * ringcount);

            GLRenderState rtrings = GLRenderState.Tri();
            rtrings.CullFace = false;

            var rirings = GLRenderableItem.CreateVector4Vector2Matrix4(items, PrimitiveType.TriangleStrip, rtrings, ringsshapebuffer, ringstexcobuffer, ringsmatrixbuffer,
                                            ringsshapebuffer.Length / sizeof(float) / 4,
                                            ic: ringcount, matrixdivisor: 1);

            rbodyobjects.Add(ringsshader, rirings);

            rbodyfindshader = GLRenderableItem.CreateVector4Vector2Matrix4(items, PrimitiveType.Triangles, GLRenderState.Tri(), sphereshapebuffer, spheretexcobuffer, bodymatrixbuffer,
                                            sphereshapebuffer.Length / sizeof(float) / 4,
                                            ic: bodies, matrixdivisor: 1);
        }

    }
}
