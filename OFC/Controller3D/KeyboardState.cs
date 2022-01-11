/*
 * Copyright © 2015 - 2021 EDDiscovery development team + Robbyxp1 @ github.com
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

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace GLOFC.Controller
{
    /// <summary>
    /// Keyboard monitor - used to track keypresses
    /// </summary>
    public class KeyboardMonitor
    {
        /// <summary> Shift keys state </summary>
        public enum ShiftState
        {
            /// <summary> None </summary>
            None = 0,
            /// <summary> Shift </summary>
            Shift = 1,
            /// <summary> Ctrl </summary>
            Ctrl = 2,
            /// <summary> Alt </summary>
            Alt = 4,
        };

        /// <summary> Is key pressed and in this state, remove from list saying it has been pressed.</summary>
        public bool HasBeenPressed(Keys key, ShiftState state)       
        {
            bool ret = false;
            if (hasbeenpressed.ContainsKey(key) && hasbeenpressed[key] == state)
            {
                ret = true;
                hasbeenpressed.Remove(key);
            }

            return ret;
        }

        /// <summary>Are any keys pressed and in this state, remove from list saying it has been pressed. </summary>
        public int HasBeenPressed(ShiftState state, params Keys[] keys)       
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (hasbeenpressed.ContainsKey(keys[i]) && hasbeenpressed[keys[i]] == state)
                {
                    hasbeenpressed.Remove(keys[i]);
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Are keys pressed, return index and state  </summary>
        public Tuple<int,ShiftState> HasBeenPressed(params Keys[] keys)    
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (hasbeenpressed.ContainsKey(keys[i]) )
                {
                    var state = hasbeenpressed[keys[i]];
                    hasbeenpressed.Remove(keys[i]);
                    return new Tuple<int, ShiftState>(i,state);
                }
            }

            return null;
        }

        /// <summary> Clear all pressed keys </summary>
        public void ClearHasBeenPressed()                          
        {
            hasbeenpressed.Clear();
        }

        /// <summary> Is key currently pressed and in this state </summary>
        public bool IsCurrentlyPressed(ShiftState state, Keys key)            
        {
            return keyspressed.ContainsKey(key) && keyspressed[key] == state;
        }

        /// <summary> Is key currently pressed, if so, return state.  if not, return null </summary>
        public ShiftState? IsCurrentlyPressed(Keys key)                        
        {
            if (keyspressed.ContainsKey(key))
                return keyspressed[key];
            else
                return null;
        }

        /// <summary> Is any keys currently pressed and in this shift state, return index into first key found which is down </summary>
        public int IsCurrentlyPressed(ShiftState state, params Keys[] keys)  
        {
            System.Diagnostics.Debug.Assert(keys.Length > 0);
            for (int i = 0; i < keys.Length; i++)
            {
                if (IsCurrentlyPressed(state, keys[i]))
                    return i;
            }

            return -1;
        }

        /// <summary> Is any keys currently pressed, if so, return shift state of first key found </summary>
        /// <summary> </summary>
        public ShiftState? IsCurrentlyPressed(params Keys[] keys)  // is currently pressed and in this shift state
        {
            System.Diagnostics.Debug.Assert(keys.Length > 0);
            foreach (var k in keys)
            {
                ShiftState? s = IsCurrentlyPressed(k);
                if (s != null)
                    return s;
            }

            return null;
        }

        /// <summary> Are any keys pressed </summary>
        public bool IsAnyCurrentlyPressed()
        {
            return keyspressed.Count > 0;
        }

        /// <summary> Are any keys pressed or been pressed </summary>
        public bool IsAnyCurrentlyOrHasBeenPressed()
        {
            return keyspressed.Count > 0 || hasbeenpressed.Count > 0;
        }

        private Dictionary<Keys, ShiftState> keyspressed = new Dictionary<Keys, ShiftState>();
        private Dictionary<Keys, ShiftState> hasbeenpressed = new Dictionary<Keys, ShiftState>();

        /// <summary> Control down </summary>
        public bool Ctrl { get; private set; } = false;
        /// <summary> Alt down </summary>
        public bool Alt { get; private set; } = false;
        /// <summary> Shift down </summary>
        public bool Shift { get; private set; } = false;

        /// <summary> Reset the key pressed system </summary>
        public void Reset()
        {
            Ctrl = Alt = Shift = false;
            keyspressed.Clear();
            hasbeenpressed.Clear();
        }

        /// <summary> Call during keydown event in key handler </summary>
        public void KeyDown(bool c, bool s, bool a, Keys keycode)      // hook to handler
        {
            Ctrl = c;
            Alt = a;
            Shift = s;
            keyspressed[keycode] = SetShift(c, s, a);
            hasbeenpressed[keycode] = SetShift(c, s, a);
            //System.Diagnostics.Debug.WriteLine("Keycode down " + keycode);
        }

        /// <summary> Call during keyup event in key handler </summary>
        public void KeyUp(bool c, bool s, bool a, Keys keycode)      // hook to handler
        {
            Ctrl = c;
            Alt = a;
            Shift = s;
            keyspressed.Remove(keycode);
            //System.Diagnostics.Debug.WriteLine("Keycode up " + keycode);
        }

        private ShiftState SetShift(bool ctrl, bool shift, bool alt)
        {
            return (ShiftState)((ctrl ? ShiftState.Ctrl : 0) | (shift ? ShiftState.Shift : 0) | (alt ? ShiftState.Alt : 0));
        }

    }
}
