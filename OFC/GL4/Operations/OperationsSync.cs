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


using OpenTK.Graphics.OpenGL4;

namespace GLOFC.GL4.Operations
{
    /// <summary>
    /// Sync Operations
    /// </summary>
    public class GLOperationFenceSync : GLOperationsBase       // must be in render queue after shader starts
    {
        /// <summary> The fence sync class </summary>
        public GLFenceSync Sync { get; set; }
        /// <summary> The sync condition to apply </summary>
        public SyncCondition Condition { get; set; }
        /// <summary> </summary>
        public WaitSyncFlags Flags { get; set; }

        /// <inheritdoc cref="GLOFC.GL4.GLFenceSync.GLFenceSync"/>
        public GLOperationFenceSync(SyncCondition synccondition = SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags waitflags = WaitSyncFlags.None)
        {
            Condition = synccondition;
            Flags = waitflags;
        }

        /// <summary> Called by render list and executes the operation </summary>
        public override void Execute(GLMatrixCalc c)
        {
            Sync = new GLFenceSync(Condition, Flags);
        }

        /// <summary> Dispose of fence </summary>
        public override void Dispose()               // when dispose, delete query
        {
            Sync.Dispose();
        }

        /// <inheritdoc cref="GLOFC.GL4.GLFenceSync.Get"/>
        public int[] Get(SyncParameterName paraname = SyncParameterName.SyncStatus)
        {
            return Sync.Get(paraname);
        }

        /// <inheritdoc cref="GLOFC.GL4.GLFenceSync.GLWait"/>

        public WaitSyncStatus ClientWait(ClientWaitSyncFlags flags, int timeout)
        {
            return Sync.ClientWait(flags, timeout);
        }
        
        /// <summary> </summary>
        public void GLWait(int timeout)
        {
            Sync.GLWait(timeout);
        }

    }




}

