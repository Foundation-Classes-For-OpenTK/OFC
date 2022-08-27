/*
 * Copyright 2019-2021 Robbyxp1 @ github.com
 * Part of the EDDiscovery Project
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
 */

using System;
using System.Reflection;
using System.Windows.Forms;

// A simpler main for demoing

namespace TestOpenTk
{
    public partial class TestOrrery : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;

        private Timer systemtimer = new Timer();

        Orrery orrery;

        public TestOrrery()
        {
            InitializeComponent();

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer);
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Closed += ShaderTest_Closed;

            orrery = new Orrery();
            orrery.Start(glwfc);

            string file = TestOpenTk.Program.ProgramArgs.Next();
            if (file == null)
                file = "HIP 22566";

            var str = GLOFC.Utils.ResourceHelpers.GetResourceAsString("TestOpenTk.Orrery.TestFiles." + file  +".json");
            if ( str == null )
                str = System.IO.File.ReadAllText(file);

            orrery.CreateBodiesJSON(str);

            systemtimer.Interval = 25;
            systemtimer.Tick += new EventHandler(SystemTick);
            systemtimer.Start();
        }

        private void ShaderTest_Closed(object sender, EventArgs e)
        {
            orrery.Dispose();
        }

        private void SystemTick(object sender, EventArgs e )
        {
            orrery.SystemTick();
        }


    }

}


