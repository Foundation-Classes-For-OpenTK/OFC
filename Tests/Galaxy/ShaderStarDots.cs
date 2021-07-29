﻿using OpenTK;
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
    public class GalaxyStarDots : GLShaderStandard
    {
        string vert =
@"
        #version 450 core

        #include UniformStorageBlocks.matrixcalc.glsl

        layout (location = 0) in vec4 position;     // has w=1
        out vec4 vs_color;

        void main(void)
        {
            vec4 p = position;
            vs_color = vec4(position.w,position.w,position.w,0.15);
            p.w = 1;
            gl_Position = mc.ProjectionModelMatrix * p;        // order important
        }
        ";
        string frag =
@"
        #version 450 core

        in vec4 vs_color;
        out vec4 color;

        void main(void)
        {
            color = vs_color;
        }
        ";
        public GalaxyStarDots() : base()
        {
            CompileLink(vert, frag: frag);
        }
    }


}
