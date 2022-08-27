/*
 * Copyright 2019-2021 Robbyxp1 @ github.com
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
using System.Windows.Forms;

namespace TestOpenTk
{
    static class Program
    {
        public static GLOFC.CommandArgs ProgramArgs;

        [STAThread]
        static void Main(string[] stringargs)
        {
            ProgramArgs = new GLOFC.CommandArgs(stringargs);

            if (ProgramArgs.Left == 0)
                ProgramArgs = new GLOFC.CommandArgs(new string[] { "TestMain" });

            using (OpenTK.Toolkit.Init(new OpenTK.ToolkitOptions { EnableHighResolution = false, Backend = OpenTK.PlatformBackend.PreferNative }))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);


                while (ProgramArgs.Left > 0)
                {
                    string arg1 = ProgramArgs.Next();

                    Type t = Type.GetType("TestOpenTk." + arg1, false, true);

                    if (t == null || t.BaseType.Name != "Form")
                        t = Type.GetType("TestOpenTk.Test" + arg1, false, true);

                    if (t == null || t.BaseType.Name != "Form")
                        t = Type.GetType("TestOpenTk.Shader" + arg1, false, true);

                    if (t == null || t.BaseType.Name != "Form")
                        t = Type.GetType("TestOpenTk.ShaderTest" + arg1, false, true);

                    if (t != null)
                    {
                        Application.Run((Form)Activator.CreateInstance(t));
                        break;
                    }
                }
            }
        }
    }
}
