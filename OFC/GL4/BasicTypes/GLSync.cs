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


using OpenTK.Graphics.OpenGL4;
using System;

namespace GLOFC.GL4
{
    /// <summary>
    /// Fence Functions
    /// </summary>
    public class GLFenceSync: IDisposable
    {
        /// <summary>GL ID </summary>
        public IntPtr Id { get; set; } = (IntPtr)0;

        /// <summary>Make a new fence, with condition and wait flags 
        /// see <href>https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glFenceSync.xhtml</href>
        /// </summary>
        /// <param name="synccondition">Must be SyncCondition.SyncGpuCommandsComplete</param>
        /// <param name="waitflags">Must be 0</param>
        public GLFenceSync(SyncCondition synccondition = SyncCondition.SyncGpuCommandsComplete,WaitSyncFlags waitflags = WaitSyncFlags.None)
        {
            Id = GL.FenceSync(synccondition, waitflags);
            GLStatics.RegisterAllocation(typeof(GLFenceSync));
        }


        /// <summary> Get the sync status of the fence. </summary>
        /// <param name="paraname">Get SyncCondition, SyncStatus, SyncFlags or ObjectType. Default is to get sync status</param>
        /// <returns>Returns an array of sync properties. Dependent on fence type</returns>
        public int[] Get(SyncParameterName paraname = SyncParameterName.SyncStatus)
        {
            int[] array = new int[20];
            GL.GetSync(Id, paraname, array.Length, out int len, array);
            GLStatics.Check();
            int[] res = new int[len];
            Array.Copy(array, res, len);
            return res;
        }

        /// <summary>
        /// Wait for fence
        /// </summary>
        /// <param name="flags">Only None or SyncFlushCommandsBit</param>
        /// <param name="timeout">In nanoseconds!</param>
        /// <returns>Wait state (AlreadySignalled (pre signalled),Expired (timeout),Satisfied (signalled during timeout), Failed)</returns>

        public WaitSyncStatus ClientWait(ClientWaitSyncFlags flags, int timeout)
        {
            var status = GL.ClientWaitSync(Id, flags, timeout);
            return status;
        }
   
        /// <summary>
        /// Wait for sync
        /// </summary>
        /// <param name="timeout">In nanoseconds!</param>

        public void GLWait(int timeout)
        {
            GL.WaitSync(Id, WaitSyncFlags.None, timeout);
        }

        /// <summary> Dispose of this fence </summary>
        public void Dispose()
        {
            if (Id != (IntPtr)0)
            {
                GL.DeleteSync(Id);
                GLStatics.RegisterDeallocation(typeof(GLFenceSync));
                Id = (IntPtr)0;
            }
            else
                System.Diagnostics.Trace.WriteLine($"OFC Warning - double disposing of ${this.GetType().FullName}");
        }
    }
}
