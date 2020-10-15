/*
 * Copyright 2015 - 2019 EDDiscovery development team
 * Copyright 2020 Robbyxp1 @ github.com
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
 * 
 * 
 */

using OpenTK;
using System;
using System.Drawing;

namespace OFC
{
    // GL           World Space                         View Space                                  Clip Space
    //           p5----------p6		                  p5----------p6                     Zfar     p5----------p6 1 = far clip
    //          /|           /|                      /|           /|                             /|           /|
    //         / |          / | 	                / |          / |                            / |          / | 
    //        /  |         /  |                    /  |         /  |                           /  |         /  |
    //       /   p4-------/--p7  Z++              /   p4-------/--p7	                      /   p4-------/--p7	
    //      /   /        /   /                   /   /        /   /                          /   / +1     /   /
    //     p1----------p2   /	x ModelView     p1----------p2   /	    x Projection        p1----------p2   /	
    //     |  /         |  /                    |  /         |  /                           |  /         |  /  
    //     | /          | /			            | /          | /		                 -1 | /          | / +1		
    //     |/	        |/                      |/	         |/                             |/	         |/
    //     p0----------p3			            p0----------p3	  Viewer Pos      ZNear     p0----------p3  0 = near clip
    //     p0-p7 are in world coords                                                               -1
    // https://learnopengl.com/Getting-started/Coordinate-Systems

    // GL           GL Window                          View Port                                 Screen Coords Area
    //                                   Viewport in GL window coord, 0,0 top left  ScreenCoordClipSpace/Offset defines the area
    //    ----------------------------       ----------------------------               -----------------------------------
    //    |0,0                       |       |Defined in screen coords  |               |                                 |
    //    |                          |       |    -------------------   |               |  -----------------------------  |
    //    |                          |       |    |       +1        |   |               |  |Viewport                   |  |
    //    |                          |       |    |                 |   |               |  |    ====================== |  |
    //    |                          |       |    |                 |   |               |  |    |Screen Coord Area   | |  |
    //    |                          |       |    | -1 Clip Space +1|   |               |  |    |ScreenCoordMax      | |  |
    //    |                          |       |    |                 |   |               |  |    |Defines the logical | |  |
    //    |                          |       |    |                 |   |               |  |    |size of this        | |  |
    //    |                          |       |    |                 |   |               |  |    ====================== |  |
    //    |                          |       |    |       -1        |   |               |  |                           |  |
    //    |                          |       |    -------------------   |               |  -----------------------------  |
    //    ----------------------------       ----------------------------               -----------------------------------
    
    // this class computes the model and projection matrices
    // also the screen co-ord matrix

    public class GLMatrixCalc
    {
        public bool InPerspectiveMode { get; set; } = true;                     // perspective mode

        public float PerspectiveFarZDistance { get; set; } = 100000.0f;         // Model maximum z (corresponding to GL viewport Z=1)
                                                                                // Model minimum z (corresponding to GL viewport Z=0) 
        public float PerspectiveNearZDistance { get; set; } = 1f;               // Don't set this too small othersize depth testing starts going wrong as you exceed the depth buffer resolution

        public float OrthographicDistance { get; set; } = 5000.0f;              // Orthographic, give scale

        public float Fov { get; set; } = (float)(Math.PI / 2.0f);               // field of view, radians, in perspective mode
        public float FovDeg { get { return Fov.Degrees(); } }
        public float FovFactor { get; set; } = 1.258925F;                       // scaling

        public Size ScreenSize { get; protected set; }                          // screen size, total, of GL window.

        public Rectangle ViewPort { get; protected set; }                       // area of window GL is drawing to - note 0,0 is top left, not the GL method of bottom left
        public Vector2 DepthRange { get; set; } = new Vector2(0, 1);            // depth range (near,far) of Z to apply to viewport transformation to screen coords from normalised device coords (0..1)

        public Size ScreenCoordMax { get; set; }                                // screen co-ords max. Does not have to match the screensize. If not, you get a fixed scalable area
        public virtual SizeF ScreenCoordClipSpaceSize { get; set; } = new SizeF(2, 2);   // Clip space size to use for screen coords, override to set up another
        public virtual PointF ScreenCoordClipSpaceOffset { get; set; } = new PointF(-1, 1);     // Origin in clip space (-1,1) = top left, screen coord (0,0)

        // after Calculate model matrix
        public Vector3 EyePosition { get; private set; }                       
        public Vector3 TargetPosition { get; private set; }                    
        public float EyeDistance { get; private set; }
        public Matrix4 ModelMatrix { get; private set; }
        public Matrix4 ProjectionMatrix { get; private set; }
        public Matrix4 ProjectionModelMatrix { get; private set; }
        public Matrix4 GetResMat { get { return ProjectionModelMatrix; } }      // used for calculating positions on the screen from pixel positions.  Remembering Apollo

        // after Projection calc
        public float ZNear { get; private set; }

        // Screen coords are different from windows cooreds. Used for controls

        // axis flip to set direction of +x,+y,+z.
        // notice flipping y affects the order of vertex for winding.. the vertex models need to have a opposite winding order
        // to make the ccw cull test work.  this also affects things in shaders if you do rotations inside them
        public bool ModelAxisFlipY = true;      // normally we have Y pointing + up

        // camera normal is +Z, so camera(x=elevation=0, y=azimuth=0) is straight up, camera(90,0) is straight forward, samera(90,180) is straight back
        // camera.x rotates around the x axis (elevation) and camera.y rotates around the y axis (aziumth)
        private Vector3 cameranormal = new Vector3(0, 0, 1);

        // Calculate the model matrix, which is the model translated to world space then to view space..

        public void CalculateModelMatrix(Vector3 lookat, Vector2 cameradirection, float distance, float camerarotation)       
        {
            Vector3 eyeposition = lookat.CalculateEyePositionFromLookat(cameradirection, distance);
            CalculateModelMatrix(lookat, eyeposition, cameradirection, camerarotation);
        }

        public void CalculateModelMatrix(Vector3 lookat, Vector3 eyeposition, Vector2 cameradirection, float camerarotation)  
        {
            TargetPosition = lookat;      // record for shader use
            EyePosition = eyeposition;
            EyeDistance = (lookat - eyeposition).Length;

            if (InPerspectiveMode)
            {
                Matrix3 transform = Matrix3.Identity;                   // identity nominal matrix, dir is in degrees

                transform *= Matrix3.CreateRotationX((float)(cameradirection.X.Radians()));     // rotate around cameradir
                transform *= Matrix3.CreateRotationY((float)(cameradirection.Y.Radians()));
                transform *= Matrix3.CreateRotationZ((float)(camerarotation.Radians()));

                Vector3 cameranormalrot = Vector3.Transform(cameranormal, transform);       // move cameranormal to rotate around current direction

                ModelMatrix = Matrix4.LookAt(EyePosition, lookat, cameranormalrot);   // from eye, look at target, with normal giving the rotation of the look
            }
            else
            {
                Size scr = ViewPort.Size;
                float orthoheight = (OrthographicDistance / 5.0f) * scr.Height / scr.Width;  // this comes from the projection calculation, and allows us to work out the scale factor the eye vs lookat has

                Matrix4 scale = Matrix4.CreateScale(orthoheight/EyeDistance);    // create a scale based on eyedistance compensated for by the orth projection scaling

                Matrix4 mat = Matrix4.CreateTranslation(-lookat.X, 0, -lookat.Z);        // we offset by the negative of the position to give the central look
                mat = Matrix4.Mult(mat, scale);          // translation world->View = scale + offset

                Matrix4 rotcam = Matrix4.CreateRotationX((float)((90) * Math.PI / 180.0f));        // flip 90 along the x axis to give the top down view
                ModelMatrix = Matrix4.Mult(mat, rotcam);
            }

            //System.Diagnostics.Debug.WriteLine("MM\r\n{0}", ModelMatrix);

            ProjectionModelMatrix = Matrix4.Mult(ModelMatrix, ProjectionMatrix);        // order order order ! so important.
        }

        // Projection matrix - projects the 3d model space to the 2D screen

        public void CalculateProjectionMatrix()           // calculate and return znear.
        {
            Size scr = ViewPort.Size;

            if (InPerspectiveMode)
            {                                                                   // Fov, perspective, znear, zfar
                ProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView(Fov, (float)scr.Width / scr.Height, PerspectiveNearZDistance, PerspectiveFarZDistance);
                ZNear = PerspectiveNearZDistance;
            }
            else
            {
                ZNear = -OrthographicDistance;
                float orthoheight = (OrthographicDistance / 5.0f) * scr.Height / scr.Width;
                ProjectionMatrix = Matrix4.CreateOrthographic(OrthographicDistance * 2.0f / 5.0f, orthoheight * 2.0F, -OrthographicDistance, OrthographicDistance);

                Matrix4 zoffset = Matrix4.CreateTranslation(0,0,0.5f);     // we ensure all z's are based around 0.5f.  W = 1 in clip space, so Z must be <= 1 to be visible
                ProjectionMatrix = Matrix4.Mult(ProjectionMatrix, zoffset); // doing this means that a control can display with a Z around low 0.
            }

            //System.Diagnostics.Debug.WriteLine("PM\r\n{0}", ProjectionMatrix);

            if ( ModelAxisFlipY )
            {
                ProjectionMatrix = ProjectionMatrix * Matrix4.CreateScale(new Vector3(1, -1, 1));   // flip y to make y+ up
            }

            ProjectionModelMatrix = Matrix4.Mult(ModelMatrix, ProjectionMatrix);
        }

        public bool FovScale(bool direction)        // direction true is scale up FOV - need to tell it its changed
        {
            float curfov = Fov;

            if (direction)
                Fov = (float)Math.Min(Fov * FovFactor, Math.PI * 0.8);
            else
                Fov /= (float)FovFactor;

            return curfov != Fov;
        }

        public virtual void ResizeViewPort(object sender, Size newsize)            // override to change view port to a custom one
        {
            //System.Diagnostics.Debug.WriteLine("Set GL Screensize {0}", newsize);
            ScreenSize = newsize;
            ScreenCoordMax = newsize;
            ViewPort = new Rectangle(new Point(0, 0), newsize);
            SetViewPort();
        }

        public void SetViewPort()
        {
            //System.Diagnostics.Debug.WriteLine("Set GL Viewport {0} {1} w {2} h {3}", ViewPort.Left, ScreenSize.Height - ViewPort.Bottom, ViewPort.Width, ViewPort.Height);
            OpenTK.Graphics.OpenGL.GL.Viewport(ViewPort.Left, ScreenSize.Height - ViewPort.Bottom, ViewPort.Width, ViewPort.Height);
            OpenTK.Graphics.OpenGL.GL.DepthRange(DepthRange.X, DepthRange.Y);
        }

        public virtual Matrix4 MatrixScreenCoordToClipSpace()             // matrix to convert a screen co-ord to clip space
        {
            Matrix4 screenmat = Matrix4.Zero;
            screenmat.Column0 = new Vector4(ScreenCoordClipSpaceSize.Width / ScreenCoordMax.Width , 0, 0, ScreenCoordClipSpaceOffset.X);      // transform of x = x * 2 / width - 1
            screenmat.Column1 = new Vector4(0.0f, -ScreenCoordClipSpaceSize.Height / ScreenCoordMax.Height, 0, ScreenCoordClipSpaceOffset.Y);  // transform of y = y * -2 / height +1, y = 0 gives +1 (top), y = sh gives -1 (bottom)
            screenmat.Column2 = new Vector4(0, 0, 1, 0);                  // transform of z = none
            screenmat.Column3 = new Vector4(0, 0, 0, 1);                  // transform of w = none
            return screenmat;
        }

        public virtual Point AdjustWindowCoordToViewPortCoord(int x, int y)     // p is in windows coords (gl_Control), adjust to view port/clip space taking into account view port
        {
            return new Point(x - ViewPort.Left, y - ViewPort.Top);
        }

        public virtual Point AdjustWindowCoordToViewPortCoord(Point p)          // p is in windows coords (gl_Control), adjust 
        {
            return new Point(p.X - ViewPort.Left, p.Y - ViewPort.Top);
        }

        public virtual Point AdjustWindowCoordToScreenCoord(Point p)
        {
            return AdjustViewSpaceToScreenCoord(AdjustWindowCoordToViewPortClipSpace(p));
        }

        public virtual PointF AdjustWindowCoordToViewPortClipSpace(Point p)     // p is in windows coords (gl_Control), adjust to clip space taking into account view port
        {
            float x = p.X - ViewPort.Left;
            float y = p.Y - ViewPort.Top;
            var f = new PointF(-1 + x / ViewPort.Width * 2.0f, 1 - y / ViewPort.Height * 2.0f);
            //  System.Diagnostics.Debug.WriteLine("SC {0} -> CS {1}", p, f);
            return f;
        }

        public virtual Point AdjustViewSpaceToScreenCoord(PointF cs)    // cs is in view port space, scale to ScreenCoords      
        {
            Size scr = ScreenCoordMax;
            SizeF clipsize = ScreenCoordClipSpaceSize;
            PointF clipoff = ScreenCoordClipSpaceOffset;

            float xo = (cs.X - clipoff.X) / clipsize.Width;         // % thru the clipspace
            float yo = (clipoff.Y - cs.Y) / clipsize.Height;

            Point np = new Point((int)(scr.Width * xo), (int)(scr.Height * yo));
            //System.Diagnostics.Debug.WriteLine("cs {0} -> {1} {2} -> {3}", cs, xo,yo, np);
            return np;
        }

        
        public Vector4 WorldToNormalisedClipSpace(Vector4 worldpos)     // World pos -> normalised clip space
        {
            Vector4 m = Vector4.Transform(worldpos, ProjectionModelMatrix);  // go from world-> viewspace -> clip space
            Vector4 c = m / m.W;                                            // to normalised clip space
            return c;
        }

        public bool IsNormalisedClipSpaceInView(Vector4 clipspace)
        {
            return !(clipspace.X <= -1 || clipspace.X >= 1 || clipspace.Y <= -1 || clipspace.Y >= 1 || clipspace.Z >= 1);
        }

        public Vector4 NormalisedClipSpaceToViewPortScreenCoord(Vector4 clipspace)   // world->viewport co-ord, W = 0 if in view. 0,0 = top left to match normal windows co-ords
        {
            bool inview = IsNormalisedClipSpaceInView(clipspace);
            return new Vector4((clipspace.X + 1) / 2 * ViewPort.Width, (-clipspace.Y + 1) / 2 * ViewPort.Height, 0, inview ? 0 : 1);
        }

        public Vector4 NormalisedClipSpaceToWindowCoord(Vector4 clipspace)   // world->window co-ord, W = 0 if in view. 0,0 = top left to match normal windows co-ords
        {
            Vector4 viewportscreencoord = NormalisedClipSpaceToViewPortScreenCoord(clipspace);
            return new Vector4(viewportscreencoord.X + ViewPort.Left, viewportscreencoord.Y + ViewPort.Top, viewportscreencoord.Z, viewportscreencoord.W);
        }
    }
}
