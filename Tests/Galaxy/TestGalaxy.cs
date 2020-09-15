﻿using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OFC;
using OFC.Common;
using OFC.GL4;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using OFC.GL4.Controls;
using EliteDangerousCore.EDSM;

namespace TestOpenTk
{
    public partial class TestGalaxy : Form
    {
        public TestGalaxy()
        {
            InitializeComponent();

            glwfc = new OFC.WinForm.GLWinFormControl(glControlContainer);

            systemtimer.Interval = 25;
            systemtimer.Tick += new EventHandler(SystemTick);
        }

        private OFC.WinForm.GLWinFormControl glwfc;

        private Timer systemtimer = new Timer();

        private GalacticMapping galacticMapping;
        private GalacticMapping eliteRegions;

        private Map map;


        /// ////////////////////////////////////////////////////////////////////////////////////////////////////

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Closed += ShaderTest_Closed;

            galacticMapping = new GalacticMapping();
            string text = System.Text.Encoding.UTF8.GetString(Properties.Resources.galacticmapping);
            galacticMapping.ParseJson(text);                            // at this point, gal map data has been uploaded - get it into memory

            eliteRegions = new GalacticMapping();
            text = System.Text.Encoding.UTF8.GetString(Properties.Resources.EliteGalacticRegions);
            eliteRegions.ParseJson(text);                            // at this point, gal map data has been uploaded - get it into memory

            map = new Map();
            map.Start(glwfc, galacticMapping, eliteRegions);
            systemtimer.Start();
        }

        private void ShaderTest_Closed(object sender, EventArgs e)
        {
            map.Dispose();
        }

        private void SystemTick(object sender, EventArgs e)
        {
            OFC.Timers.Timer.ProcessTimers();
            map.Systick();
        }
    }
}


