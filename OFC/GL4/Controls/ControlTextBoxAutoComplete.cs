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
using System.Threading;

namespace OFC.GL4.Controls
{
    // Autocomplete text box
    // PerformAutoCompleteInThread runs in a thread - not the main UI
    // PerformAutoCompleteInUIThread runs in the UI thread
    // you can have both
    // SelectedEntry is run when use hits return or clicks on an autocompleted item in drop down, runs in the UI thread
    // Set InErrorCondition if required, next text will clear it

    public class GLTextBoxAutoComplete : GLMultiLineTextBox
    {
        public Func<string, GLTextBoxAutoComplete, List<string>> PerformAutoCompleteInThread { get; set; } = null;
        public Func<string, GLTextBoxAutoComplete, List<string>> PerformAutoCompleteInUIThread { get; set; } = null;

        public Action<GLTextBoxAutoComplete> SelectedEntry { get; set; } = null;        // called on return, or autocomplete entry selected.

        public int AutoCompleteInitialDelay { get; set; } = 250;

        public GLListBox ListBox { get; set; } = new GLListBox();           // if you want to set its visual properties, do it via this

        public GLTextBoxAutoComplete(string name, Rectangle pos, string text = "") : base(name, pos, text)
        {
            MultiLineMode = false;
            waitforautotimer.Tick += InitialDelayOver;
            autocompleteinuitimer.Tick += AutoCompleteInUI;
            triggercomplete.Tick += AutoCompleteFinished;
            ListBox.AutoSize = true;
            ListBox.SelectedIndexChanged += (c0, i) => {
                Text = ListBox.SelectedItem;
                CancelAutoComplete();
                SelectedEntry?.Invoke(this);
            };
            ListBox.OtherKeyPressed += (c1, e) => { if (e.KeyCode == System.Windows.Forms.Keys.Delete) CancelAutoComplete(); };
            ListBox.Name = name + "_LB";
            ListBox.TopMost = true;
            ListBox.ShowFocusHighlight = true;
        }

        public GLTextBoxAutoComplete() : this("TBAC?", DefaultWindowRectangle, "")
        {
        }

        public void AutoComplete(string autocomplete)           // call to autocomplete this
        {
            autocompletestring = autocomplete;
            InitialDelayOver(null, 0);
        }

        public void CancelAutoComplete()        // Sometimes, the user is quicker than the timer, and has commited to a selection before the results even come back.
        {
            waitforautotimer.Stop();
            triggercomplete.Stop();
            Remove(ListBox);
            this.SetFocus();
        }

        protected override void OnTextChanged()
        {
            System.Diagnostics.Debug.WriteLine("{0} text change event", Environment.TickCount % 10000);

            if (InErrorCondition)               // cancel any error condition on typing
                InErrorCondition = false;

            if (PerformAutoCompleteInThread != null || PerformAutoCompleteInUIThread != null )
            {
                if (!executingautocomplete)     // if not executing, start a timeout
                {
                    System.Diagnostics.Debug.WriteLine("{0} Start timer", Environment.TickCount % 10000);
                    waitforautotimer.Start(AutoCompleteInitialDelay);
                    autocompletestring = String.Copy(this.Text);    // a copy in case the text box changes it after complete starts
                }
                else
                {                               // we are executing a long one, but another autocomplete needs to be done
                    System.Diagnostics.Debug.WriteLine("{0} in ac, go again", Environment.TickCount % 10000);
                    autocompletestring = String.Copy(this.Text);
                    restartautocomplete = true;
                }
            }
        }

        private void InitialDelayOver(OFC.Timers.Timer t, long tick)
        {
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

                autocompletestrings = new List<string>();

                if ( PerformAutoCompleteInThread != null)           // first see if a thread wants action
                {
                    System.Diagnostics.Debug.WriteLine("AC in thread");
                    var ret = PerformAutoCompleteInThread(string.Copy(autocompletestring), this);
                    if (ret != null)
                        autocompletestrings.AddRange(ret);
                }

                if ( PerformAutoCompleteInUIThread != null )        // then see if the UI wants action
                {
                    System.Diagnostics.Debug.WriteLine("Fire in ui");
                    autocompleteinuitimer.FireNow();                // fire a UI thread timer off
                    AutocompleteUIDone.WaitOne();                   // and stop thread until AutoCompleteInUI done
                    System.Diagnostics.Debug.WriteLine("Fire in ui done");
                }

                System.Diagnostics.Debug.WriteLine("{0} finish func ret {1} restart {2}", Environment.TickCount % 10000, autocompletestrings?.Count, restartautocomplete);
            } while (restartautocomplete == true);

            executingautocomplete = false;
            triggercomplete.FireNow();  // fire it immediately.  Next timer call around will trigger in correct thread.  This is thread safe.
        }

        private void AutoCompleteInUI(OFC.Timers.Timer t, long tick)      // in UI thread, fired by autocompleteinui timer
        {
            System.Diagnostics.Debug.WriteLine("{0} Perform in UI ", tick);
            var uistrings = PerformAutoCompleteInUIThread.Invoke(string.Copy(autocompletestring), this);        // we know its not null
            if (uistrings != null)
                autocompletestrings.AddRange(uistrings);
            AutocompleteUIDone.Set();
        }
            
        private void AutoCompleteFinished(OFC.Timers.Timer t, long tick)        // in UI thread
        {
            System.Diagnostics.Debug.WriteLine("{0} Auto Complete finished", tick);

            if (autocompletestrings != null && autocompletestrings.Count > 0)
            {
                foreach (var s in autocompletestrings) { System.Diagnostics.Debug.WriteLine("String " + s); }

                Point sc = this.DisplayControlCoords(false);

                ListBox.Font = Font;                                            
                ListBox.Location = new Point(sc.X, sc.Y + Height);      
                ListBox.Items = autocompletestrings;

                if (ListBox.Parent == null)
                    AddToDesktop(ListBox);
            }
            else
                Remove(ListBox);
        }

        protected override void OnKeyDown(GLKeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!e.Handled)
            {
                if (ListBox.Visible)
                {
                    if (e.KeyCode == System.Windows.Forms.Keys.Up)
                    {
                        ListBox.FocusUp();
                    }
                    else if (e.KeyCode == System.Windows.Forms.Keys.Down)
                    {
                        ListBox.FocusDown();
                    }
                    else if (e.KeyCode == System.Windows.Forms.Keys.PageUp)
                    {
                        ListBox.FocusUp(ListBox?.DisplayableItems ?? 0);
                    }
                    else if (e.KeyCode == System.Windows.Forms.Keys.PageDown)
                    {
                        ListBox.FocusDown(ListBox?.DisplayableItems ?? 0);
                    }
                }

                if (e.KeyCode == System.Windows.Forms.Keys.Enter || e.KeyCode == System.Windows.Forms.Keys.Return)
                {
                    if (ListBox.Visible)
                    {
                        ListBox.SelectCurrent();
                    }
                    else
                    {
                        CancelAutoComplete();                   // close any list box, and select this
                        SelectedEntry?.Invoke(this);
                    }
                }
            }
        }

        protected override void OnMouseWheel(GLMouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (!e.Handled && ListBox.Visible)
            {
                if (e.Delta > 0)
                    ListBox.FocusUp();
                else
                    ListBox.FocusDown();
            }
        }

        private OFC.Timers.Timer waitforautotimer = new Timers.Timer();
        private OFC.Timers.Timer autocompleteinuitimer = new Timers.Timer();
        private AutoResetEvent AutocompleteUIDone = new AutoResetEvent(false);
        private OFC.Timers.Timer triggercomplete = new Timers.Timer();
        private string autocompletestring;
        private bool executingautocomplete = false;
        private bool restartautocomplete = false;
        private List<string> autocompletestrings = null;
        private System.Threading.Thread ThreadAutoComplete;

    }
}
