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

using GLOFC.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;


namespace GLOFC.GL4.Controls
{

    /// <summary>
    /// A single line text box control with autocomplete
    /// Set InErrorCondition if required, next text entry will clear text box.
    /// </summary>
    public class GLTextBoxAutoComplete : GLMultiLineTextBox
    {
        /// <summary> Callback to collect a list, into the sorted set, of autocomplete texts guided by string
        /// Fired in a thread, first before UI perform</summary>
        public Action<string, GLTextBoxAutoComplete, SortedSet<string>> PerformAutoCompleteInThread { get; set; } = null;
        /// <summary> Callback to collect a list, into the sorted set, of autocomplete texts guided by string
        /// Fired in the UI thread, after the thread perform</summary>
        public Action<string, GLTextBoxAutoComplete, SortedSet<string>> PerformAutoCompleteInUIThread { get; set; } = null;

        /// <summary> Callback. Run when use hits return or clicks on an autocompleted item in drop down, runs in the UI thread</summary>
        public Action<GLTextBoxAutoComplete> SelectedEntry { get; set; } = null;        

        /// <summary> Delay in ms before autocomplete starts</summary>
        public int AutoCompleteInitialDelay { get; set; } = 250;

        /// <summary> List box for autocomplete. For Themeing</summary>
        public GLListBox ListBox { get; set; } = new GLListBox();

        /// <summary> Constructor with name, bounds and optional text </summary>
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

        /// <summary> Default constructor </summary>
        public GLTextBoxAutoComplete() : this("TBAC?", DefaultWindowRectangle, "")
        {
        }

        /// <summary> Ask for autocomplete on this string </summary>
        public void AutoComplete(string autocomplete)           // call to autocomplete this
        {
            autocompletestring = autocomplete;
            InitialDelayOver(null, 0);
        }

        /// <summary> Cancel the autocomplete.  </summary>
        public void CancelAutoComplete()       
        {
            // Sometimes, the user is quicker than the timer, and has commited to a selection before the results even come back.
            waitforautotimer.Stop();
            triggercomplete.Stop();
            Remove(ListBox);
            this.SetFocus();
        }


        #region Implementation

        private protected override void OnTextChanged()
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

        private void InitialDelayOver(PolledTimer t, long tick)
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

                autocompletestrings = new SortedSet<string>();

                if ( PerformAutoCompleteInThread != null)           // first see if a thread wants action
                {
                    System.Diagnostics.Debug.WriteLine("AC in thread");
                    PerformAutoCompleteInThread(string.Copy(autocompletestring), this, autocompletestrings);
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

        private void AutoCompleteInUI(PolledTimer t, long tick)      // in UI thread, fired by autocompleteinui timer
        {
            System.Diagnostics.Debug.WriteLine("{0} Perform in UI ", tick);
            PerformAutoCompleteInUIThread.Invoke(string.Copy(autocompletestring), this, autocompletestrings);        // we know its not null
            AutocompleteUIDone.Set();
        }
            
        private void AutoCompleteFinished(PolledTimer t, long tick)        // in UI thread
        {
            System.Diagnostics.Debug.WriteLine("{0} Auto Complete finished", tick);

            if (autocompletestrings != null && autocompletestrings.Count > 0)
            {
                //foreach (var s in autocompletestrings) { System.Diagnostics.Debug.WriteLine("Autocomplete String " + s); }

                Point sc = FindScreenCoords(new Point(ClientLeftMargin, Height + 1));
                ListBox.Font = Font;
                ListBox.Location = sc;
                ListBox.Items = autocompletestrings.ToList();
                ListBox.ScaleWindow = FindScaler();

                if (ListBox.Parent == null)
                    AddToDesktop(ListBox);
            }
            else
                Remove(ListBox);
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnKeyDown(GLKeyEventArgs)"/>
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
                    if (ListBox.Visible && ListBox.FocusIndex>=0)       // if we are showing list and there is a focus, we use that
                    {
                        ListBox.SelectCurrentFocus();
                    }
                    else
                    {
                        CancelAutoComplete();                   // close any list box, and select this text, may be empty
                        SelectedEntry?.Invoke(this);
                    }
                }
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseWheel(GLMouseEventArgs)"/>
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

        private PolledTimer waitforautotimer = new PolledTimer();
        private PolledTimer autocompleteinuitimer = new PolledTimer();
        private AutoResetEvent AutocompleteUIDone = new AutoResetEvent(false);
        private PolledTimer triggercomplete = new PolledTimer();
        private string autocompletestring;
        private bool executingautocomplete = false;
        private bool restartautocomplete = false;
        private SortedSet<string> autocompletestrings = null;
        private System.Threading.Thread ThreadAutoComplete;

        #endregion
    }
}
