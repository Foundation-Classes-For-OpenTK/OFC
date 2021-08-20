/*
 * Copyright 2019-2020 Robbyxp1 @ github.com
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


using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OFC.GL4
{
    // A Null renderable item to insert into a render list under a shader, to perform an action

    public class GLRenderableItemNull : IGLRenderableItem
    {
        public bool Visible { get; set; } = true;                           // is visible (in this case active)

        public GLRenderControl RenderControl { get; set; }                  // Not used
        public IGLRenderItemData RenderData { get; set; }                   // Not used
        public int DrawCount { get; set; } = 0;                             // A+E : Draw count, IE+IA MultiDraw count, ICA+ICE Maximum draw count(don't exceed buffer size when setting this)
        public int InstanceCount { get; set; } = 0;                         // A+E: Instances (not used in indirect - this comes from the buffer)
        public Action<GLRenderControl, IGLProgramShader, GLMatrixCalc> StartAction { get; set; }

        public GLRenderableItemNull(Action<GLRenderControl, IGLProgramShader, GLMatrixCalc> startaction = null)
        {
            StartAction = startaction;
        }

        // called before Render (normally by RenderList::Render) to set up data for the render.
        // currentstate may be null, meaning, don't apply
        // RenderControl must be set for normal renders. 
        public void Bind(GLRenderControl currentstate, IGLProgramShader shader, GLMatrixCalc c)      
        {
            StartAction?.Invoke(currentstate,shader, c);
        }

        // Render - no action

        public void Render()
        {
        }
    }
}

