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

using System;
using System.Collections.Generic;
using System.Drawing;

namespace OFC.GL4.Controls
{
    public class GLTextBoxAutoComplete : GLMultiLineTextBox
    {
        public Func<string,GLTextBoxAutoComplete,List<string>> PerformAutoComplete { get; set; } = null;

        public int AutoCompleteInitialDelay { get; set; } = 500;

        public GLListBox ListBox { get; set; } = new GLListBox();           // if you want to set its visual properties, do it via this

        public GLTextBoxAutoComplete(string name, Rectangle pos, string text = "") : base(name, pos, text)
        {
            MultiLineMode = false;
            waitforautotimer.Tick += TimeOutTick;
            triggercomplete.Tick += AutoCompleteFinished;
            ListBox.AutoSize = true;
            ListBox.SelectedIndexChanged += (c0, i) => { Text = ListBox.SelectedItem; CancelAutoComplete(); };
            ListBox.OtherKeyPressed += (c1, e) => { if (e.KeyCode == System.Windows.Forms.Keys.Delete) CancelAutoComplete(); };
        }

        public GLTextBoxAutoComplete() : this("TBAC?", DefaultWindowRectangle, "")
        {
        }

        public void AutoComplete(string autocomplete)           // call to autocomplete this
        {
            autocompletestring = autocomplete;
            TimeOutTick(null, 0);
        }

        public void CancelAutoComplete()        // Sometimes, the user is quicker than the timer, and has commited to a selection before the results even come back.
        {
            waitforautotimer.Stop();
            triggercomplete.Stop();
            if (ListBox.Parent != null)
            {
                FindDisplay()?.Remove(ListBox);
            }
            this.SetFocus();
        }

        protected override void OnTextChanged()
        {
            System.Diagnostics.Debug.WriteLine("{0} text change event", Environment.TickCount % 10000);
            if (PerformAutoComplete != null )
            {
                if (!executingautocomplete)
                {
                    System.Diagnostics.Debug.WriteLine("{0} Start timer", Environment.TickCount % 10000);
                    waitforautotimer.Start(AutoCompleteInitialDelay);
                    autocompletestring = String.Copy(this.Text);    // a copy in case the text box changes it after complete starts
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("{0} in ac, go again", Environment.TickCount % 10000);
                    autocompletestring = String.Copy(this.Text);
                    restartautocomplete = true;
                }
            }
        }

        private void TimeOutTick(OFC.Timers.Timer t, long tick)
        {
            waitforautotimer.Stop();
            executingautocomplete = true;

            ThreadAutoComplete = new System.Threading.Thread(new System.Threading.ThreadStart(AutoComplete));
            ThreadAutoComplete.Name = "AutoComplete";
            ThreadAutoComplete.Start();
        }

        private void AutoComplete()     // in a thread..
        {
            do
            {
                System.Diagnostics.Debug.WriteLine("{0} Begin AC", Environment.TickCount % 10000);
                restartautocomplete = false;
                autocompletestrings = PerformAutoComplete(string.Copy(autocompletestring), this);    // pass a copy, in case we change it out from under it
                System.Diagnostics.Debug.WriteLine("{0} finish func ret {1} restart {2}", Environment.TickCount % 10000, autocompletestrings.Count, restartautocomplete);
            } while (restartautocomplete == true);

            executingautocomplete = false;
            triggercomplete.Start(0);  // fire it immediately.  Next timer call around will trigger in correct thread.  This is thread safe.
        }

        private void AutoCompleteFinished(OFC.Timers.Timer t, long tick)
        {
            System.Diagnostics.Debug.WriteLine("{0} Auto Complete finished", tick);
            foreach (var s in autocompletestrings) { System.Diagnostics.Debug.WriteLine("String {0}", s); }

            if ( autocompletestrings.Count > 0 )
            {
                if ( ListBox.Parent != null )
                {
                    FindDisplay()?.Remove(ListBox);
                }

                Point sc = this.DisplayControlCoords(false);

                ListBox.Font = Font;
                ListBox.Location = new Point(sc.X, sc.Y + Height);
                ListBox.Items = autocompletestrings;
                //                listbox = new GLListBox("ACLB", new Rectangle(sc.X, sc.Y+Height, DropDownSize.Width>0 ? DropDownSize.Width : -this.Width * DropDownSize.Width / 100, DropDownSize.Height), autocompletestrings);
                FindDisplay().Add(ListBox);
            }
        }

        public override void OnKeyDown(GLKeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!e.Handled)
            {
                if (e.KeyCode == System.Windows.Forms.Keys.Up)
                {
                    ListBox?.FocusUpOne();
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.Down)
                {
                    ListBox?.FocusDownOne();
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.Enter || e.KeyCode == System.Windows.Forms.Keys.Return)
                {
                    ListBox?.SelectCurrent();
                }
            }
        }

        public override void OnMouseWheel(GLMouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (!e.Handled)
            {
                if (e.Delta > 0)
                    ListBox?.FocusUpOne();
                else
                    ListBox?.FocusDownOne();
            }
        }

        private OFC.Timers.Timer waitforautotimer = new Timers.Timer();
        private OFC.Timers.Timer triggercomplete = new Timers.Timer();
        private string autocompletestring;
        private bool executingautocomplete = false;
        private bool restartautocomplete = false;
        private List<string> autocompletestrings = null;
        private System.Threading.Thread ThreadAutoComplete;

    }
}
